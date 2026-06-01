using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DualHooks : MonoBehaviour
{
    [Header("References")]
    public List<LineRenderer> lineRenderers;
    public List<Transform> gunTips;
    public Transform cam;
    public Transform player;
    public LayerMask whatIsGrappleable;
    public PlayerMovementGrappling pm;

    [Header("Swinging")]
    private float maxSwingDistance = 25f;
    private List<Vector3> swingPoints;
    private List<SpringJoint> joints;

    [Header("Grappling")]
    public float maxGrappleDistance;
    public float grappleDelayTime;
    public float overshootYAxis;

    private List<bool> grapplesActive;

    [Header("Cooldown")]
    public float grapplingCd;
    private float grapplingCdTimer;

    [Header("OdmGear")]
    public Transform orientation;
    public Rigidbody rb;
    public float horizontalThrustForce;
    public float forwardThrustForce;
    public float extendCableSpeed;

    [Header("Prediction")]
    public List<RaycastHit> predictionHits;
    public List<Transform> predictionPoints;
    public float predictionSphereCastRadius;

    [Header("Input")]
    public KeyCode swingKey1 = KeyCode.Mouse0;
    public KeyCode swingKey2 = KeyCode.Mouse1;


    [Header("DualSwinging")]
    public int amountOfSwingPoints = 2;
    public List<Transform> pointAimers;
    private List<bool> swingsActive;

    private void Awake()
    {
        ListSetup();
    }

    private void ListSetup()
    {
        predictionHits = new List<RaycastHit>();
        predictionPoints ??= new List<Transform>();
        pointAimers ??= new List<Transform>();

        swingPoints = new List<Vector3>();
        joints = new List<SpringJoint>();

        swingsActive = new List<bool>();
        grapplesActive = new List<bool>();

        currentGrapplePositions = new List<Vector3>();

        int count = Mathf.Max(1, amountOfSwingPoints);
        while (predictionPoints.Count < count) predictionPoints.Add(null);
        while (pointAimers.Count < count) pointAimers.Add(null);

        for (int i = 0; i < count; i++)
        {
            predictionHits.Add(new RaycastHit());
            joints.Add(null);
            swingPoints.Add(Vector3.zero);
            swingsActive.Add(false);
            grapplesActive.Add(false);
            currentGrapplePositions.Add(Vector3.zero);
        }
    }

    private void Update()
    {
        MyInput();
        CheckForSwingPoints();

        if (joints[0] != null || joints[1] != null) OdmGearMovement();

        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void MyInput()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKeyDown(swingKey1)) StartGrapple(0);
            if (Input.GetKeyDown(swingKey2)) StartGrapple(1);
        }
        else
        {
            if (Input.GetKeyDown(swingKey1)) StartSwing(0);
            if (Input.GetKeyDown(swingKey2)) StartSwing(1);
        }

        if (Input.GetKeyUp(swingKey1)) StopSwing(0);
        if (Input.GetKeyUp(swingKey2)) StopSwing(1);
    }

    private void CheckForSwingPoints()
    {
        for (int i = 0; i < amountOfSwingPoints; i++)
        {
            if (swingsActive[i]) { }
            else
            {
                bool hasAimer = i < pointAimers.Count && pointAimers[i] != null;
                bool hasPrediction = i < predictionPoints.Count && predictionPoints[i] != null;

                RaycastHit sphereCastHit = default;
                if (hasAimer)
                {
                    Physics.SphereCast(pointAimers[i].position, predictionSphereCastRadius, pointAimers[i].forward,
                                        out sphereCastHit, maxSwingDistance, whatIsGrappleable);
                }

                RaycastHit raycastHit;
                Physics.Raycast(cam.position, cam.forward,
                                    out raycastHit, maxSwingDistance, whatIsGrappleable);

                Vector3 realHitPoint;

                if (raycastHit.point != Vector3.zero)
                    realHitPoint = raycastHit.point;
                else if (hasAimer && sphereCastHit.point != Vector3.zero)
                    realHitPoint = sphereCastHit.point;
                else
                    realHitPoint = Vector3.zero;

                if (realHitPoint != Vector3.zero)
                {
                    if (hasPrediction)
                    {
                        predictionPoints[i].gameObject.SetActive(true);
                        predictionPoints[i].position = realHitPoint;
                    }
                }
                else
                {
                    if (hasPrediction)
                        predictionPoints[i].gameObject.SetActive(false);
                }

                predictionHits[i] = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;
            }
        }
    }

    #region Swinging

    private void StartSwing(int swingIndex)
    {
        if (predictionHits[swingIndex].point == Vector3.zero) return;

        CancelActiveGrapples();
        pm.ResetRestrictions();

        pm.swinging = true;
        swingsActive[swingIndex] = true;

        swingPoints[swingIndex] = predictionHits[swingIndex].point;
        joints[swingIndex] = player.gameObject.AddComponent<SpringJoint>();
        joints[swingIndex].autoConfigureConnectedAnchor = false;
        joints[swingIndex].connectedAnchor = swingPoints[swingIndex];

        float distanceFromPoint = Vector3.Distance(player.position, swingPoints[swingIndex]);

        joints[swingIndex].maxDistance = distanceFromPoint * 0.8f;
        joints[swingIndex].minDistance = distanceFromPoint * 0.25f;

        joints[swingIndex].spring = 15f;
        joints[swingIndex].damper = 7f;
        joints[swingIndex].massScale = 4.5f;

        lineRenderers[swingIndex].positionCount = 2;
        currentGrapplePositions[swingIndex] = gunTips[swingIndex].position;
    }

    public void StopSwing(int swingIndex)
    {
        pm.swinging = false;

        swingsActive[swingIndex] = false;

        Destroy(joints[swingIndex]);
    }

    #endregion

    #region Grappling

    private void StartGrapple(int grappleIndex)
    {
        if (grapplingCdTimer > 0) return;

        CancelActiveSwings();
        CancelAllGrapplesExcept(grappleIndex);

        if (predictionHits[grappleIndex].point != Vector3.zero)
        {
            Invoke(nameof(DelayedFreeze), 0.05f);

            grapplesActive[grappleIndex] = true;

            swingPoints[grappleIndex] = predictionHits[grappleIndex].point;

            StartCoroutine(ExecuteGrapple(grappleIndex));
        }

        else
        {
            swingPoints[grappleIndex] = cam.position + cam.forward * maxGrappleDistance;

            StartCoroutine(StopGrapple(grappleIndex, grappleDelayTime));
        }

        lineRenderers[grappleIndex].positionCount = 2;
        currentGrapplePositions[grappleIndex] = gunTips[grappleIndex].position;
    }

    private void DelayedFreeze()
    {
        pm.freeze = true;
    }

    private IEnumerator ExecuteGrapple(int grappleIndex)
    {
        yield return new WaitForSeconds(grappleDelayTime);

        pm.freeze = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        float grapplePointRelativeYPos = swingPoints[grappleIndex].y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        pm.JumpToPosition(swingPoints[grappleIndex], highestPointOnArc);
    }

    public IEnumerator StopGrapple(int grappleIndex, float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        if (grapplesActive == null || grappleIndex >= grapplesActive.Count) yield break;

        pm.freeze = false;

        pm.ResetRestrictions();

        grapplesActive[grappleIndex] = false;

        grapplingCdTimer = grapplingCd;
    }

    #endregion

    #region OdmGear

    private Vector3 pullPoint;
    private void OdmGearMovement()
    {
        if (swingsActive[0] && !swingsActive[1]) pullPoint = swingPoints[0];
        if (swingsActive[1] && !swingsActive[0]) pullPoint = swingPoints[1];
        if (swingsActive[0] && swingsActive[1])
        {
            Vector3 dirToGrapplePoint1 = swingPoints[1] - swingPoints[0];
            pullPoint = swingPoints[0] + dirToGrapplePoint1 * 0.5f;
        }

        if (Input.GetKey(KeyCode.D)) rb.AddForce(orientation.right * horizontalThrustForce * Time.deltaTime);
        if (Input.GetKey(KeyCode.A)) rb.AddForce(-orientation.right * horizontalThrustForce * Time.deltaTime);
        if (Input.GetKey(KeyCode.W)) rb.AddForce(orientation.forward * forwardThrustForce * Time.deltaTime);
        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 directionToPoint = pullPoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardThrustForce * Time.deltaTime);

            float distanceFromPoint = Vector3.Distance(transform.position, pullPoint);

            UpdateJoints(distanceFromPoint);

            print("shorten " + Time.time);
        }
        if (Input.GetKey(KeyCode.S))
        {
            float extendedDistanceFromPoint = Vector3.Distance(transform.position, pullPoint) + extendCableSpeed;

            UpdateJoints(extendedDistanceFromPoint);

            print("extend " + Time.time);
        }
    }

    private void UpdateJoints(float distanceFromPoint)
    {
        for (int i = 0; i < joints.Count; i++)
        {
            if (joints[i] != null)
            {
                joints[i].maxDistance = distanceFromPoint * 0.8f;
                joints[i].minDistance = distanceFromPoint * 0.25f;
            }
        }
    }

    #endregion

    #region CancleAbilities

    public void CancelActiveGrapples()
    {
        StartCoroutine(StopGrapple(0));
        StartCoroutine(StopGrapple(1));
    }

    private void CancelAllGrapplesExcept(int grappleIndex)
    {
        for (int i = 0; i < amountOfSwingPoints; i++)
            if (i != grappleIndex) StartCoroutine(StopGrapple(i));
    }

    private void CancelActiveSwings()
    {
        StopSwing(0);
        StopSwing(1);
    }

    #endregion

    #region Visualisation

    private List<Vector3> currentGrapplePositions;

    private void DrawRope()
    {
        for (int i = 0; i < amountOfSwingPoints; i++)
        {
            bool hasRenderer = i < lineRenderers.Count && lineRenderers[i] != null;
            bool hasGunTip = i < gunTips.Count && gunTips[i] != null;

            if (!grapplesActive[i] && !swingsActive[i])
            {
                if (hasRenderer) lineRenderers[i].positionCount = 0;
            }
            else
            {
                currentGrapplePositions[i] = Vector3.Lerp(currentGrapplePositions[i], swingPoints[i], Time.deltaTime * 8f);

                if (hasRenderer && hasGunTip)
                {
                    lineRenderers[i].SetPosition(0, gunTips[i].position);
                    lineRenderers[i].SetPosition(1, currentGrapplePositions[i]);
                }
            }
        }
    }

    #endregion
}
