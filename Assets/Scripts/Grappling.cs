using System.Collections;
using UnityEngine;

public class Grappling : MonoBehaviour
{
    [Header("References")]
    private PlayerMovement pm;

    public Transform cam;
    public Transform gunTip;
    public Transform kunai;

    public LayerMask whatIsGrappleable;
    public LineRenderer lr;

    [Header("Grapple")]
    public float maxGrappleDistance = 100f;
    public float grappleDelayTime = 0.1f;
    public float grappleSpeed = 35f;

    private Vector3 grapplePoint;
    private Vector3 currentGrapplePosition;

    [Header("Cooldown")]
    public float grapplingCd = 1f;
    private float grapplingCdTimer;

    [Header("Input")]
    public KeyCode grappleKey = KeyCode.Mouse1;

    private bool grappling;
    private Coroutine routine;

    private void Start()
    {
        pm = GetComponentInParent<PlayerMovement>();

        lr.positionCount = 0;
        lr.enabled = false;

        if (kunai != null)
            kunai.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(grappleKey))
            StartGrapple();

        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        if (!grappling) return;

        currentGrapplePosition = Vector3.Lerp(
            currentGrapplePosition,
            grapplePoint,
            Time.deltaTime * grappleSpeed
        );

        lr.positionCount = 2;
        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, currentGrapplePosition);

        if (kunai != null)
        {
            kunai.position = currentGrapplePosition;
            kunai.forward = (grapplePoint - gunTip.position).normalized;
        }
    }

    private void StartGrapple()
    {
        if (grapplingCdTimer > 0) return;

        RaycastHit hit;

        if (Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            grappling = true;
            grapplePoint = hit.point;
            currentGrapplePosition = gunTip.position;

            pm.freeze = true;
            pm.activeGrapple = true;

            lr.enabled = true;

            if (kunai != null)
                kunai.gameObject.SetActive(true);

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(PullPlayer());
        }
    }

    private IEnumerator PullPlayer()
    {
        yield return new WaitForSeconds(grappleDelayTime);

        while (Vector3.Distance(pm.transform.position, grapplePoint) > 1.5f)
        {
            Vector3 dir = (grapplePoint - pm.transform.position).normalized;

            // SAFE MOVE (no explosion velocity)
            pm.controller.Move(dir * grappleSpeed * Time.deltaTime);

            yield return null;
        }

        StopGrapple();
    }

    public void StopGrapple()
    {
        grappling = false;

        grapplingCdTimer = grapplingCd;

        pm.freeze = false;
        pm.activeGrapple = false;

        lr.enabled = false;
        lr.positionCount = 0;

        if (kunai != null)
            kunai.gameObject.SetActive(false);

        if (routine != null)
            StopCoroutine(routine);
    }

    public bool IsGrappling() => grappling;

    public Vector3 GetGrapplePoint() => grapplePoint;
}