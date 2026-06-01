using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplingDone : MonoBehaviour
{
    [Header("References")]
    private PlayerMovementGrappling pm;
    public Transform cam;
    public Transform gunTip;
    public LayerMask whatIsGrappleable;
    public LineRenderer lr;

    [Header("Grappling")]
    public float maxGrappleDistance = 25f;
    public float grappleDelayTime = 0.5f;
    public float overshootYAxis = 2f;
    public AudioClip grappleSound;

    private Vector3 grapplePoint;

    [Header("Cooldown")]
    public float grapplingCd = 2.5f;
    private float grapplingCdTimer;

    [Header("Input")]
    public KeyCode grappleKey = KeyCode.Mouse1;

    [Header("Prediction")]
    public float predictionSphereCastRadius = 0.5f;
    public Transform predictionPoint;

    private RaycastHit predictionHit;
    private bool grappling;

    private void Start()
    {
        pm = GetComponent<PlayerMovementGrappling>();
    }

    private void Update()
    {
        CheckForGrapplePoint();
        if (Input.GetKeyDown(grappleKey)) StartGrapple();

        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        if (grappling && lr.positionCount > 0)
            lr.SetPosition(0, gunTip.position);
    }

    private void CheckForGrapplePoint()
    {
        if (grappling) return;

        RaycastHit sphereCastHit;
        Physics.SphereCast(cam.position, predictionSphereCastRadius, cam.forward,
                            out sphereCastHit, maxGrappleDistance, whatIsGrappleable);

        RaycastHit raycastHit;
        Physics.Raycast(cam.position, cam.forward,
                            out raycastHit, maxGrappleDistance, whatIsGrappleable);

        Vector3 realHitPoint;

        if (sphereCastHit.point != Vector3.zero)
            realHitPoint = sphereCastHit.point;
        else if (raycastHit.point != Vector3.zero)
            realHitPoint = raycastHit.point;
        else
            realHitPoint = Vector3.zero;

        if (realHitPoint != Vector3.zero)
        {
            predictionPoint.gameObject.SetActive(true);
            predictionPoint.position = realHitPoint;
        }
        else
        {
            predictionPoint.gameObject.SetActive(false);
        }

        predictionHit = sphereCastHit.point == Vector3.zero ? raycastHit : sphereCastHit;
    }

    public void StartGrapple()
    {
        if (grapplingCdTimer > 0) return;
        if (predictionHit.point == Vector3.zero) return;

        grappling = true;

        pm.freeze = true;

        grapplePoint = predictionHit.point;

        Invoke(nameof(ExcecuteGrapple), grappleDelayTime);

        lr.positionCount = 2;
        lr.enabled = true;
        lr.SetPosition(1, grapplePoint);

        if (grappleSound != null)
            AudioSource.PlayClipAtPoint(grappleSound, transform.position);
    }

    public void ExcecuteGrapple()
    {
        pm.freeze = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOfArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOfArc = overshootYAxis;

        pm.JumpToPosition(grapplePoint, highestPointOfArc);

        Invoke(nameof(StopGrapple), 1f);
    }

    public void StopGrapple()
    {
        pm.freeze = false;

        grappling = false;

        grapplingCdTimer = grapplingCd;

        lr.enabled = false;
    }

    public void OnObjectTouch()
    {

    }


    public bool IsGrappling()
    {
        return grappling;
    }

    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }

}