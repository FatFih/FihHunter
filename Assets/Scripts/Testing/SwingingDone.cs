using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwingingDone : MonoBehaviour
{
    [Header("References")]
    public LineRenderer lr;
    public Transform gunTip, cam, player;
    public LayerMask whatIsGrappleable;
    public PlayerMovementGrappling pm;

    [Header("Swinging")]
    private float maxSwingDistance = 25f;
    private Vector3 swingPoint;
    private bool isSwinging;
    private float ropeLength;

    [Header("OdmGear")]
    public Transform orientation;
    public Rigidbody rb;
    public float horizontalThrustForce;
    public float forwardThrustForce;

    [Header("Prediction")]
    public RaycastHit predictionHit;
    public float predictionSphereCastRadius;
    public Transform predictionPoint;

    [Header("Input")]
    public KeyCode swingKey = KeyCode.Mouse0;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Kunai")]
    public Transform kunai;

    [Header("JumpOff")]
    public float swingJumpUpForce = 8f;
    public float swingJumpForwardForce = 5f;

    private void Update()
    {
        if (Input.GetKeyDown(swingKey)) StartSwing();
        if (Input.GetKeyDown(jumpKey) && isSwinging) SwingJump();
        if (Input.GetKeyUp(swingKey)) StopSwing();

        CheckForSwingPoints();
    }

    private void FixedUpdate()
    {
        if (isSwinging) PendulumMovement();
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void CheckForSwingPoints()
    {
        if (isSwinging) return;

        RaycastHit sphereCastHit;
        Physics.SphereCast(cam.position, predictionSphereCastRadius, cam.forward, 
                            out sphereCastHit, maxSwingDistance, whatIsGrappleable);

        RaycastHit raycastHit;
        Physics.Raycast(cam.position, cam.forward, 
                            out raycastHit, maxSwingDistance, whatIsGrappleable);

        Vector3 realHitPoint;

        if (raycastHit.point != Vector3.zero)
            realHitPoint = raycastHit.point;

        else if (sphereCastHit.point != Vector3.zero)
            realHitPoint = sphereCastHit.point;

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

        predictionHit = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;
    }


    private void StartSwing()
    {
        if (predictionHit.point == Vector3.zero) return;

        if(GetComponent<Grappling>() != null)
            GetComponent<Grappling>().StopGrapple();
        pm.ResetRestrictions();

        pm.swinging = true;
        isSwinging = true;

        swingPoint = predictionHit.point;
        ropeLength = Vector3.Distance(player.position, swingPoint) * 0.15f;

        lr.positionCount = 2;
        lr.enabled = true;
        if (kunai != null) kunai.gameObject.SetActive(true);
        currentGrapplePosition = gunTip.position;
    }

    private void PendulumMovement()
    {
        Vector3 dirToPivot = (transform.position - swingPoint).normalized;
        float currentDist = Vector3.Distance(transform.position, swingPoint);
        if (currentDist > ropeLength)
        {
            float pull = (currentDist - ropeLength) * 50f;
            rb.AddForce(-dirToPivot * pull, ForceMode.Acceleration);
        }
        rb.linearVelocity -= Vector3.Project(rb.linearVelocity, dirToPivot);

        Vector3 forward = orientation.forward - Vector3.Project(orientation.forward, dirToPivot);
        Vector3 right = orientation.right - Vector3.Project(orientation.right, dirToPivot);

        if (forward.sqrMagnitude < 0.001f) forward = Vector3.Cross(dirToPivot, Vector3.up);
        if (right.sqrMagnitude < 0.001f) right = Vector3.Cross(dirToPivot, Vector3.up);
        forward.Normalize();
        right.Normalize();

        bool abovePivot = Vector3.Dot(transform.position - swingPoint, Vector3.up) > 0;
        bool forwardIsUp = Vector3.Dot(forward, Vector3.up) > 0.1f;

        if (abovePivot && forwardIsUp)
        {
            forward = (Vector3.down - Vector3.Project(Vector3.down, dirToPivot)).normalized;
            right = Vector3.Cross(dirToPivot, forward).normalized;
        }

        if (Input.GetKey(KeyCode.W)) rb.AddForce(forward * forwardThrustForce, ForceMode.Force);
        if (Input.GetKey(KeyCode.S)) rb.AddForce(-forward * forwardThrustForce, ForceMode.Force);
        if (Input.GetKey(KeyCode.D)) rb.AddForce(right * horizontalThrustForce, ForceMode.Force);
        if (Input.GetKey(KeyCode.A)) rb.AddForce(-right * horizontalThrustForce, ForceMode.Force);
    }

    private void SwingJump()
    {
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;
        rb.AddForce(Vector3.up * swingJumpUpForce + (orientation.forward * swingJumpForwardForce), ForceMode.Impulse);
        StopSwing();
    }

    public void StopSwing()
    {
        pm.swinging = false;
        isSwinging = false;

        lr.positionCount = 0;
        if (kunai != null) kunai.gameObject.SetActive(false);
    }

    private Vector3 currentGrapplePosition;

    private void DrawRope()
    {
        if (!isSwinging) return;

        currentGrapplePosition = 
            Vector3.Lerp(currentGrapplePosition, swingPoint, Time.deltaTime * 8f);

        Vector3 ropeStart = Vector3.Lerp(gunTip.position, currentGrapplePosition, 0.02f);
        Vector3 ropeEnd = Vector3.Lerp(gunTip.position, currentGrapplePosition, 0.9f);
        lr.SetPosition(0, ropeStart);
        lr.SetPosition(1, ropeEnd);
        if (kunai != null)
        {
            kunai.position = currentGrapplePosition;
            kunai.rotation = Quaternion.LookRotation(currentGrapplePosition - gunTip.position);
        }
    }
}
