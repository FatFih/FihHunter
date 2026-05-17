using UnityEngine;

public class RopeRenderer : MonoBehaviour
{
    [Header("References")]
    public Grappling grappling;
    public Transform gunTip;

    [Header("Rope Settings")]
    public int segments = 20;
    public float extendSpeed = 12f;

    [Header("Spring Animation")]
    public float springStrength = 40f;
    public float springDamper = 5f;
    public float springLaunchVelocity = 12f;

    [Header("Wave")]
    public float waveHeight = 1.5f;
    public float waveCount = 3f;
    public float waveTravelSpeed = 4f;
    public float wave3DAmount = 0.4f;

    [Header("Physics")]
    public float ropeDamping = 0.99f;
    public float gravityScale = 1f;
    public int constraintIterations = 10;

    [Header("FOV Kick")]
    public float fovIncrease = 5f;
    public float fovDecaySpeed = 4f;
    public float fovReturnDuration = 0.25f;

    private LineRenderer lr;
    private Spring spring;
    private Vector3[] points;
    private Vector3[] prevPoints;
    private Vector3 currentEndPosition;
    private float segmentRestLength;
    private float initialRopeLength;
    private bool initialized;
    private bool extended;
    private GameObject ropeObject;
    private Camera playerCam;
    private float defaultFov;
    private float fovTimer;
    private float returnTimer;
    private float returnStartFov;
    private bool returning;

    void Awake()
    {
        if (grappling == null)
            grappling = GetComponentInParent<Grappling>();
        if (gunTip == null && grappling != null)
            gunTip = grappling.gunTip;

        if (grappling != null && grappling.cam != null)
        {
            playerCam = grappling.cam.GetComponent<Camera>();
            if (playerCam != null)
                defaultFov = playerCam.fieldOfView;
        }

        GrapplingRope old = GetComponent<GrapplingRope>();
        if (old != null)
            old.enabled = false;

        LineRenderer existing = GetComponent<LineRenderer>();
        if (existing != null)
            existing.enabled = false;

        ropeObject = new GameObject("RopeVisual");
        ropeObject.transform.SetParent(transform, false);
        ropeObject.hideFlags = HideFlags.HideAndDontSave;

        lr = ropeObject.AddComponent<LineRenderer>();
        lr.material = existing != null ? existing.material : null;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
        lr.alignment = LineAlignment.View;
        lr.positionCount = 0;

        spring = new Spring();
        points = new Vector3[segments + 1];
        prevPoints = new Vector3[segments + 1];
    }

    void OnDestroy()
    {
        if (ropeObject != null)
            Destroy(ropeObject);
    }

    void LateUpdate()
    {
        if (lr == null) return;

        if (grappling != null && grappling.lr != null)
            grappling.lr.enabled = false;

        if (grappling == null || gunTip == null || !grappling.IsGrappling())
        {
            if (playerCam != null)
            {
                if (!returning)
                {
                    returning = true;
                    returnStartFov = playerCam.fieldOfView;
                    returnTimer = 0;
                }
                returnTimer += Time.deltaTime;
                float t = Mathf.Clamp01(returnTimer / fovReturnDuration);
                playerCam.fieldOfView = Mathf.Lerp(returnStartFov, defaultFov, t);
            }
            if (lr.positionCount > 0)
                lr.positionCount = 0;
            initialized = false;
            return;
        }

        Vector3 start = gunTip.position;
        Vector3 end = grappling.GetGrapplePoint();

        if (!initialized)
        {
            initialized = true;
            extended = false;
            returning = false;
            currentEndPosition = start;
            initialRopeLength = Vector3.Distance(start, end);
            segmentRestLength = initialRopeLength / segments;

            fovTimer = 0;

            spring.Reset();
            spring.SetTarget(0);
            spring.SetVelocity(springLaunchVelocity);
            spring.SetStrength(springStrength);
            spring.SetDamper(springDamper);

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                points[i] = Vector3.Lerp(start, end, t);
                prevPoints[i] = points[i];
            }
        }

        lr.positionCount = segments + 1;
        bool ropeExtended = Vector3.Distance(currentEndPosition, end) < 0.1f;

        if (!ropeExtended)
            currentEndPosition = Vector3.Lerp(currentEndPosition, end, Time.deltaTime * extendSpeed);
        else if (!extended)
        {
            extended = true;
            spring.SetVelocity(0);
            spring.SetValue(0);
        }

        if (!extended)
        {
            spring.Update(Time.deltaTime);

            Transform cam = grappling.cam;
            Vector3 camUp = cam != null ? cam.up : Vector3.up;
            Vector3 camRight = cam != null ? cam.right : Vector3.right;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 basePoint = Vector3.Lerp(start, currentEndPosition, t);

                float phase = t * waveCount * Mathf.PI + Time.time * waveTravelSpeed;
                float amp = waveHeight * spring.Value;

                Vector3 offset = camUp * Mathf.Sin(phase) * amp;
                offset += camRight * Mathf.Cos(phase + 1.57f) * amp * wave3DAmount;

                points[i] = basePoint + offset;
                prevPoints[i] = points[i];
            }
        }
        else
        {
            Vector3 gravity = Physics.gravity * gravityScale;
            float dt = Time.deltaTime;

            for (int i = 1; i < segments; i++)
            {
                Vector3 vel = points[i] - prevPoints[i];
                prevPoints[i] = points[i];
                points[i] += vel * ropeDamping + gravity * (dt * dt);
            }

            for (int iter = 0; iter < constraintIterations; iter++)
            {
                for (int i = 0; i < segments; i++)
                {
                    Vector3 delta = points[i + 1] - points[i];
                    float dist = delta.magnitude;
                    if (dist < 0.0001f) continue;

                    float correction = (dist - segmentRestLength) / dist;
                    Vector3 offset = delta * (correction * 0.5f);

                    points[i] += offset;
                    points[i + 1] -= offset;
                }

                points[0] = start;
                points[segments] = end;
            }
        }

        for (int i = 0; i <= segments; i++)
            lr.SetPosition(i, points[i]);

        if (playerCam != null)
        {
            fovTimer += Time.deltaTime;
            playerCam.fieldOfView = defaultFov + fovIncrease * Mathf.Exp(-fovTimer * fovDecaySpeed);
        }
    }
}
