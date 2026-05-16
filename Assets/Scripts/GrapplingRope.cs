using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplingRope : MonoBehaviour
{
    private LineRenderer lr;

    private Vector3 currentGrapplePosition;

    [Header("References")]
    public Grappling grapplingGun;

    [Header("Rope Settings")]
    public int quality = 40;
    public float waveCount = 2f;
    public float waveHeight = 1f;
    public float ropeSnapSpeed = 12f;

    public AnimationCurve affectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    void Awake()
    {
        lr = GetComponent<LineRenderer>();

        if (quality < 2)
            quality = 40;

        lr.positionCount = 0;
    }

    void LateUpdate()
    {
        DrawRope();
    }

    void DrawRope()
    {
        if (!grapplingGun.IsGrappling())
        {
            currentGrapplePosition = grapplingGun.gunTip.position;

            if (lr.positionCount != 0)
                lr.positionCount = 0;

            return;
        }

        Vector3 gunTipPosition = grapplingGun.gunTip.position;
        Vector3 grapplePoint = grapplingGun.GetGrapplePoint();

        // Smooth rope travel
        currentGrapplePosition = Vector3.Lerp(
            currentGrapplePosition,
            grapplePoint,
            Time.deltaTime * ropeSnapSpeed
        );

        if (lr.positionCount != quality + 1)
            lr.positionCount = quality + 1;

        Vector3 up = Quaternion.LookRotation(
            (grapplePoint - gunTipPosition).normalized
        ) * Vector3.up;

        for (int i = 0; i <= quality; i++)
        {
            float delta = i / (float)quality;

            float curve = affectCurve.Evaluate(delta);

            float wave =
                Mathf.Sin(delta * waveCount * Mathf.PI);

            Vector3 offset =
                up * waveHeight * wave * curve;

            Vector3 point =
                Vector3.Lerp(gunTipPosition, currentGrapplePosition, delta)
                + offset;

            lr.SetPosition(i, point);
        }
    }
}