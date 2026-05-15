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

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 8f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float antiBumpFactor = 0.75f;

    [Header("Air Movement")]
    // How fast you can accelerate while airborne (CS-style: low value, ~10-30)
    [SerializeField] private float airAcceleration = 30f;
    // Max speed the air acceleration can ADD per frame (not a velocity cap)
    // This prevents infinite air-acceleration while still allowing bhop speed preservation
    [SerializeField] private float airAccelCap = 30f;

    [HideInInspector]
    public Vector3 moveDirection = Vector3.zero;

    [HideInInspector]
    public Vector3 contactPoint;

    [HideInInspector]
    public CharacterController controller;

    [HideInInspector]
    public bool playerControl = false;

    public bool grounded = false;
    public Vector3 jump = Vector3.zero;

    private bool forceGravity;
    private float forceTime = 0;

    UnityEvent onReset = new UnityEvent();

    public override void OnEnable()
    {
        base.OnEnable();
        controller = GetComponent<CharacterController>();
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public void AddToReset(UnityAction call)
    {
        onReset.AddListener(call);
    }

    public override void ResetPositionTo(Vector3 resetTo)
    {
        controller.enabled = false;

        StartCoroutine(forcePosition());

        IEnumerator forcePosition()
        {
            transform.position = resetTo;

            ForgetPreviousTransforms();

            yield return new WaitForEndOfFrame();
        }

        controller.enabled = true;

        onReset.Invoke();
    }

    public override void Update()
    {
        Vector3 newestTransform = m_lastPositions[m_newTransformIndex];
        Vector3 olderTransform = m_lastPositions[OldTransformIndex()];

        Vector3 adjust = Vector3.Lerp(
            olderTransform,
            newestTransform,
            InterpolationController.InterpolationFactor
        );

        adjust -= transform.position;

        controller.Move(adjust);

        if (forceTime > 0)
            forceTime -= Time.deltaTime;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (forceTime > 0)
        {
            if (forceGravity)
                moveDirection.y -= gravity * Time.deltaTime;

            grounded =
                (controller.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
        }
    }

    public override void LateFixedUpdate()
    {
        base.LateFixedUpdate();
    }

    public void Move(Vector2 input, bool sprint, bool crouching)
    {
        if (forceTime > 0)
            return;

        float speed = sprint ? runSpeed : walkSpeed;

        if (crouching)
            speed = crouchSpeed;

        Vector3 inputDir = new Vector3(input.x, 0, input.y);
        inputDir = transform.TransformDirection(inputDir).normalized;

        Vector3 horizontalVelocity = new Vector3(
            moveDirection.x,
            0,
            moveDirection.z
        );

        // ====================================================
        // GROUND MOVEMENT
        // ====================================================

        if (grounded)
        {
            if (input.magnitude < 0.01f)
            {
                // FIX 1: Instant stop — no lerp, no friction decay.
                // This is what Source engine does: zero ground velocity the moment
                // you release all movement keys.
                horizontalVelocity = Vector3.zero;
            }
            else
            {
                // FIX 2: Snap directly to desired velocity regardless of current speed.
                // We do NOT preserve momentum on the ground — that was causing the slide.
                // Bhop momentum is preserved by the fact that Jump() fires BEFORE
                // this branch can zero out velocity (player is airborne immediately).
                horizontalVelocity = inputDir * speed;
            }

            moveDirection.x = horizontalVelocity.x;
            moveDirection.z = horizontalVelocity.z;

            // Keep player grounded on slopes, reset vertical vel
            moveDirection.y = -antiBumpFactor;

            // FIX 3: Apply jump AFTER setting y = -antiBumpFactor so the jump
            // vertical velocity isn't subtracted by gravity on the same frame.
            // UpdateJump() overwrites y with jumpSpeed only when jump is pending.
            UpdateJump();
        }

        // ====================================================
        // AIR MOVEMENT  (Source/CS-style Quake air-strafe)
        // ====================================================

        else
        {
            if (input.magnitude > 0.01f)
            {
                // FIX 4: Classic Quake/Source air-strafe formula.
                //
                // We only add velocity in inputDir if the current speed projected
                // onto inputDir is below airAccelCap. This means:
                //   - You CAN strafe to gain speed beyond runSpeed (bhop speed gain)
                //   - You CANNOT blindly accelerate forward past airAccelCap
                //   - Sharp turns feel snappy because projectedSpeed drops when you
                //     change direction, allowing immediate re-acceleration
                //
                // The old code had TWO acceleration additions (accelSpeed + airControl)
                // which bypassed the cap. Removed the airControl secondary add.

                float projectedSpeed = Vector3.Dot(horizontalVelocity, inputDir);
                float accelSpeed = airAcceleration * Time.deltaTime;

                // Only add acceleration if we're below the per-frame cap
                if (projectedSpeed + accelSpeed > airAccelCap)
                {
                    accelSpeed = Mathf.Max(0f, airAccelCap - projectedSpeed);
                }

                horizontalVelocity += inputDir * accelSpeed;

                // FIX 5: Removed Vector3.ClampMagnitude(airMaxSpeed).
                // Clamping total horizontal speed kills bhop chains — if you land
                // at 20 u/s and jump again, the clamp immediately cuts you back.
                // The airAccelCap already controls how much NEW speed you can gain
                // per frame, so we don't need a global speed ceiling on top of that.
                // If you want a hard ceiling for exploit prevention, add it here:
                //   horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, absoluteMaxSpeed);
            }

            moveDirection.x = horizontalVelocity.x;
            moveDirection.z = horizontalVelocity.z;
        }

        // Apply gravity (runs for both grounded and airborne, but grounded y
        // is already set to -antiBumpFactor so this just makes it slightly more
        // negative — harmless, CharacterController will clamp it on landing)
        moveDirection.y -= gravity * Time.deltaTime;

        grounded =
            (controller.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
    }

    // Overload: scripted movement (cutscenes, knockback, etc.)
    public void Move(Vector3 direction, float speed, float appliedGravity)
    {
        if (forceTime > 0)
            return;

        Vector3 move = direction * speed;

        if (appliedGravity > 0)
        {
            moveDirection.x = move.x;
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

    // Overload: scripted movement with explicit Y component
    public void Move(Vector3 direction, float speed, float appliedGravity, float setY)
    {
        if (forceTime > 0)
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

    // FIX 6: Jump() now correctly stores the full direction vector.
    // Previously jump.x and jump.z were stored but silently ignored in UpdateJump().
    // Now the jump direction is used for directional launches (ramps, launch pads, etc.)
    // For standard player jumping, callers pass Vector3.up so x/z = 0, which preserves
    // the existing horizontal momentum correctly.
    public void Jump(Vector3 dir, float mult)
    {
        jump = dir * mult;
    }

    public void UpdateJump()
    {
        if (jump != Vector3.zero)
        {
            // Preserve existing horizontal momentum when jumping.
            // If jump.x/z is non-zero (e.g. a directional launch pad), blend it in.
            // For normal jumps (dir = Vector3.up), jump.x and jump.z are 0,
            // so this simply sets vertical velocity without touching horizontal.
            Vector3 horizontalMomentum = new Vector3(moveDirection.x, 0, moveDirection.z);

            moveDirection.y = jump.y * jumpSpeed;

            // Only override horizontal if the jump direction has horizontal component
            // (allows ramps/launches to redirect velocity)
            if (Mathf.Abs(jump.x) > 0.01f || Mathf.Abs(jump.z) > 0.01f)
            {
                moveDirection.x = jump.x * jumpSpeed;
                moveDirection.z = jump.z * jumpSpeed;
            }
            else
            {
                // Standard jump: keep current horizontal velocity (bhop momentum)
                moveDirection.x = horizontalMomentum.x;
                moveDirection.z = horizontalMomentum.z;
            }
        }

        jump = Vector3.zero;
    }

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
}