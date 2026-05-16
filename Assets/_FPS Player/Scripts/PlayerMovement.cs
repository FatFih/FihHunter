using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : InterpolatedTransform
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float crouchSpeed = 2f;
    public float swingSpeed;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 8f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float antiBumpFactor = 0.75f;

    [Header("Air Movement")]
    [SerializeField] private float airAcceleration = 30f;
    [SerializeField] private float airAccelCap = 30f;

    [Header("Grappling")]
    public bool freeze = false;
    public bool activeGrapple = false;
    public float maxYSpeed = 25f;

    [Header("Swinging")]
    public bool swinging = false;

    [HideInInspector] public Vector3 moveDirection = Vector3.zero;
    [HideInInspector] public Vector3 grappleVelocity = Vector3.zero;
    [HideInInspector] public Vector3 contactPoint;

    [HideInInspector] public CharacterController controller;

    public bool grounded = false;
    public Vector3 jump = Vector3.zero;

    private bool forceGravity;
    private float forceTime = 0;

    private Vector3 velocityToSet;
    private bool enableGrappleVelocity;

    UnityEvent onReset = new UnityEvent();

    // =========================
    // INIT
    // =========================
    public override void OnEnable()
    {
        base.OnEnable();
        controller = GetComponent<CharacterController>();
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    // =========================
    // SWING
    // =========================
    public void StartSwing() => swinging = true;
    public void StopSwing() => swinging = false;

    // =========================
    // RESET
    // =========================
    public void AddToReset(UnityAction call)
    {
        onReset.AddListener(call);
    }

    // =========================
    // GRAPPLE ENTRY
    // =========================
    public void ResetRestrictions()
    {
        activeGrapple = false;
        enableGrappleVelocity = false;
    }

    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;

        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);

        // IMPORTANT FIX: delay + controlled application
        enableGrappleVelocity = true;
        Invoke(nameof(SetVelocity), 0.08f);
    }

    private void SetVelocity()
    {
        if (!enableGrappleVelocity) return;

        moveDirection = velocityToSet;

        // safety clamp (prevents insane launch)
        moveDirection = Vector3.ClampMagnitude(moveDirection, 35f);

        // cancel upward stacking bugs
        moveDirection.y = Mathf.Clamp(moveDirection.y, -25f, 40f);

        enableGrappleVelocity = false;
    }

    // =========================
    // MAIN MOVE (INPUT)
    // =========================
    public void Move(Vector2 input, bool sprint, bool crouching)
    {
        if (activeGrapple)
        {
            controller.Move(grappleVelocity * Time.deltaTime);
            return;
        }

        if (freeze || swinging || forceTime > 0)
            return;

        // IMPORTANT FIX: stop normal movement from overriding grapple
        if (activeGrapple && enableGrappleVelocity)
            return;

        float speed = sprint ? runSpeed : walkSpeed;
        if (crouching) speed = crouchSpeed;

        Vector3 inputDir = new Vector3(input.x, 0, input.y);
        inputDir = transform.TransformDirection(inputDir).normalized;

        Vector3 horizontalVelocity = new Vector3(moveDirection.x, 0, moveDirection.z);

        if (grounded)
        {
            if (input.magnitude < 0.01f)
                horizontalVelocity = Vector3.zero;
            else
                horizontalVelocity = inputDir * speed;

            moveDirection.x = horizontalVelocity.x;
            moveDirection.z = horizontalVelocity.z;

            moveDirection.y = -antiBumpFactor;

            UpdateJump();
        }
        else
        {
            if (input.magnitude > 0.01f)
            {
                float projectedSpeed = Vector3.Dot(horizontalVelocity, inputDir);
                float accelSpeed = airAcceleration * Time.deltaTime;

                if (projectedSpeed + accelSpeed > airAccelCap)
                    accelSpeed = Mathf.Max(0f, airAccelCap - projectedSpeed);

                horizontalVelocity += inputDir * accelSpeed;
            }

            moveDirection.x = horizontalVelocity.x;
            moveDirection.z = horizontalVelocity.z;
        }

        // IMPORTANT FIX: no gravity stacking during grapple
        if (!activeGrapple)
            moveDirection.y -= gravity * Time.deltaTime;

        grounded =
            (controller.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
    }

    // =========================
    // LEGACY MOVEMENT FIXES
    // =========================
    public void Move(Vector3 direction, float speed, float appliedGravity)
    {
        if (activeGrapple)
        {
            controller.Move(grappleVelocity * Time.deltaTime);
            return;
        }

        Move(direction, speed, appliedGravity, 0f);
    }

    public void Move(Vector3 direction, float speed, float appliedGravity, float setY)
    {
        if (freeze || swinging || forceTime > 0)
            return;

        Vector3 move = direction * speed;

        if (appliedGravity > 0)
        {
            moveDirection.x = move.x;

            if (setY != 0)
                moveDirection.y = setY * speed;

            moveDirection.y -= gravity * Time.deltaTime * appliedGravity;

            moveDirection.z = move.z;
        }
        else
        {
            moveDirection = move;
        }

        UpdateJump();

        grounded =
            (controller.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
    }

    // =========================
    // JUMP
    // =========================
    public void Jump(Vector3 dir, float mult)
    {
        jump = dir * mult;
    }

    public void UpdateJump()
    {
        if (jump != Vector3.zero)
        {
            Vector3 horizontalMomentum = new Vector3(moveDirection.x, 0, moveDirection.z);

            moveDirection.y = jump.y * jumpSpeed;

            if (Mathf.Abs(jump.x) > 0.01f || Mathf.Abs(jump.z) > 0.01f)
            {
                moveDirection.x = jump.x * jumpSpeed;
                moveDirection.z = jump.z * jumpSpeed;
            }
            else
            {
                moveDirection.x = horizontalMomentum.x;
                moveDirection.z = horizontalMomentum.z;
            }
        }

        jump = Vector3.zero;
    }

    // =========================
    // FORCE MOVE
    // =========================
    public void ForceMove(Vector3 direction, float speed, float time, bool applyGravity)
    {
        forceTime = time;
        forceGravity = applyGravity;

        moveDirection = direction * speed;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        contactPoint = hit.point;
    }

    // =========================
    // MATH
    // =========================
    private Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        Vector3 displacement = endPoint - startPoint;
        Vector3 displacementXZ = new Vector3(displacement.x, 0f, displacement.z);

        float gravityValue = Mathf.Abs(gravity);

        float timeToApex = Mathf.Sqrt(2f * trajectoryHeight / gravityValue);
        float timeToDescend = Mathf.Sqrt(2f * Mathf.Max(0.1f, trajectoryHeight - displacement.y) / gravityValue);

        float totalTime = Mathf.Max(0.25f, timeToApex + timeToDescend);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(2f * gravityValue * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / totalTime;

        return velocityXZ + velocityY;
    }
}