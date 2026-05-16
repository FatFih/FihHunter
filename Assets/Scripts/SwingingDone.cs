using UnityEngine;

public class SwingingDone : MonoBehaviour
{
    [Header("References")]
    public LineRenderer lr;
    public Transform gunTip;
    public Transform cam;
    public Transform player;
    public Rigidbody rb;
    public Transform orientation;
    public LayerMask whatIsGrappleable;
    public PlayerMovement pm;

    [Header("Swing Settings")]
    public float maxSwingDistance = 25f;
    public float horizontalThrustForce = 40f;
    public float forwardThrustForce = 60f;
    public float extendCableSpeed = 2f;

    [Header("Prediction")]
    public float predictionSphereCastRadius = 1f;
    public Transform predictionPoint;

    [Header("Input")]
    public KeyCode swingKey = KeyCode.Mouse0;

    private SpringJoint joint;
    private Vector3 swingPoint;
    private Vector3 currentRopePos;
    private RaycastHit predictionHit;
    private bool isSwinging;

    void Update()
    {
        if (Input.GetKeyDown(swingKey))
            StartSwing();

        if (Input.GetKeyUp(swingKey))
            StopSwing();

        CheckForSwingPoints();

        if (isSwinging && joint != null)
            HandleSwingMovement();
    }

    void LateUpdate()
    {
        DrawRope();
    }

    void CheckForSwingPoints()
    {
        if (isSwinging) return;

        RaycastHit sphereHit;
        Physics.SphereCast(cam.position, predictionSphereCastRadius, cam.forward,
            out sphereHit, maxSwingDistance, whatIsGrappleable);

        RaycastHit rayHit;
        Physics.Raycast(cam.position, cam.forward,
            out rayHit, maxSwingDistance, whatIsGrappleable);

        Vector3 hitPoint = Vector3.zero;

        if (rayHit.point != Vector3.zero)
            hitPoint = rayHit.point;
        else if (sphereHit.point != Vector3.zero)
            hitPoint = sphereHit.point;

        if (predictionPoint != null)
        {
            predictionPoint.gameObject.SetActive(hitPoint != Vector3.zero);

            if (hitPoint != Vector3.zero)
                predictionPoint.position = hitPoint;
        }

        predictionHit = rayHit.point != Vector3.zero ? rayHit : sphereHit;
    }

    void StartSwing()
    {
        if (predictionHit.point == Vector3.zero)
            return;

        isSwinging = true;

        if (pm != null)
            pm.enabled = false; // 🔥 CRITICAL FIX: stop movement fighting physics

        swingPoint = predictionHit.point;

        joint = player.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = swingPoint;

        float dist = Vector3.Distance(player.position, swingPoint);

        joint.maxDistance = dist * 0.75f;
        joint.minDistance = dist * 0.25f;

        // 🔥 IMPORTANT: low values = no floating
        joint.spring = 4f;
        joint.damper = 1.5f;
        joint.massScale = 1f;

        rb.useGravity = true;

        currentRopePos = gunTip.position;

        if (lr != null)
        {
            lr.positionCount = 2;
            lr.enabled = true;
        }
    }

    public void StopSwing()
    {
        isSwinging = false;

        if (pm != null)
            pm.enabled = true;

        if (joint != null)
            Destroy(joint);

        if (lr != null)
        {
            lr.positionCount = 0;
            lr.enabled = false;
        }
    }

    void HandleSwingMovement()
    {
        if (Input.GetKey(KeyCode.D))
            rb.AddForce(orientation.right * horizontalThrustForce, ForceMode.Acceleration);

        if (Input.GetKey(KeyCode.A))
            rb.AddForce(-orientation.right * horizontalThrustForce, ForceMode.Acceleration);

        if (Input.GetKey(KeyCode.W))
            rb.AddForce(orientation.forward * forwardThrustForce, ForceMode.Acceleration);

        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 dir = swingPoint - transform.position;
            rb.AddForce(dir.normalized * forwardThrustForce, ForceMode.Acceleration);
        }

        if (Input.GetKey(KeyCode.S))
        {
            float dist = Vector3.Distance(transform.position, swingPoint) + extendCableSpeed;

            joint.maxDistance = dist * 0.75f;
            joint.minDistance = dist * 0.25f;
        }
    }

    void DrawRope()
    {
        if (!isSwinging || joint == null || lr == null)
            return;

        if (lr.positionCount != 2)
            lr.positionCount = 2;

        currentRopePos = Vector3.Lerp(
            currentRopePos,
            swingPoint,
            Time.deltaTime * 10f
        );

        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, currentRopePos);
    }
}