using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwingingDani : MonoBehaviour
{
    [Header("References")]
    public LineRenderer lr;
    public Transform gunTip, cam, player;
    public LayerMask whatIsGrappleable;
    public PlayerMovementGrappling pm;

    [Header("Swinging")]
    private float maxSwingDistance = 25f;
    private Vector3 swingPoint;
    private SpringJoint joint;

    [Header("Input")]
    public KeyCode swingKey = KeyCode.Mouse0;

    [Header("Prediction")]
    public float predictionSphereCastRadius = 0.5f;
    public Transform predictionPoint;

    private RaycastHit predictionHit;

    void Update()
    {
        CheckForSwingPoints();
        if (Input.GetKeyDown(swingKey)) StartSwing();
        if (Input.GetKeyUp(swingKey)) StopSwing();
    }

    void LateUpdate()
    {
        DrawRope();
    }

    private void CheckForSwingPoints()
    {
        if (joint != null) return;

        RaycastHit sphereCastHit;
        Physics.SphereCast(cam.position, predictionSphereCastRadius, cam.forward,
                            out sphereCastHit, maxSwingDistance, whatIsGrappleable);

        RaycastHit raycastHit;
        Physics.Raycast(cam.position, cam.forward,
                            out raycastHit, maxSwingDistance, whatIsGrappleable);

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

    private void StartSwing()
    {
        if (predictionHit.point == Vector3.zero) return;

        pm.swinging = true;

        swingPoint = predictionHit.point;
        joint = player.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = swingPoint;

        float distanceFromPoint = Vector3.Distance(player.position, swingPoint);

        joint.maxDistance = distanceFromPoint * 0.8f;
        joint.minDistance = distanceFromPoint * 0.25f;

        joint.spring = 4.5f;
        joint.damper = 7f;
        joint.massScale = 4.5f;

        lr.positionCount = 2;
        lr.enabled = true;
        currentGrapplePosition = gunTip.position;
    }

    void StopSwing()
    {
        pm.swinging = false;

        lr.positionCount = 0;
        Destroy(joint);
    }

    private Vector3 currentGrapplePosition;

    void DrawRope()
    {
        if (!joint || lr.positionCount < 2) return;

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, swingPoint, Time.deltaTime * 8f);

        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, currentGrapplePosition);
    }
}
