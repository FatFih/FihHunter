using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashingDone : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform playerCam;
    private Rigidbody rb;
    private PlayerMovementDashingDone pm;

    [Header("Dashing")]
    public float dashForce = 70f;
    public float dashUpwardForce = 2f;
    public float maxDashYSpeed;
    public float dashDuration = 0.4f;
    public AudioClip dashSound;

    [Header("CameraEffects")]
    public PlayerCam cam;
    public float dashFov;

    [Header("Settings")]
    public bool useCameraForward = true;
    public bool allowAllDirections = true;
    public bool disableGravity = false;
    public bool resetVel = true;

    [Header("Cooldown")]
    public float dashCd = 1.5f; 
    private float dashCdTimer;

    [Header("Input")]
    public KeyCode dashKey = KeyCode.E;

    private void Start()
    {
        if (playerCam == null)
            playerCam = Camera.main.transform;

        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovementDashingDone>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(dashKey))
            Dash();

        if (dashCdTimer > 0)
            dashCdTimer -= Time.deltaTime;
    }

    private void Dash()
    {
        if (dashCdTimer > 0) return;
        else dashCdTimer = dashCd;

        cam.DoFov(dashFov);

        if (dashSound != null)
            AudioSource.PlayClipAtPoint(dashSound, transform.position);

        
        pm.dashing = true;
        pm.maxYSpeed = maxDashYSpeed;

        Transform forwardT;

        
        if (useCameraForward)
            forwardT = playerCam; 
        else
            forwardT = orientation; 

        
        Vector3 direction = GetDirection(forwardT);

        
        Vector3 forceToApply = direction * dashForce + orientation.up * dashUpwardForce;

        
        if (disableGravity)
            rb.useGravity = false;

        
        delayedForceToApply = forceToApply;

       

        print("dashForce: " + delayedForceToApply);
        Invoke(nameof(DelayedDashForce), 0.025f);

        
        Invoke(nameof(ResetDash), dashDuration);
    }

    private Vector3 delayedForceToApply;
    private void DelayedDashForce()
    {
        if(resetVel)
            rb.linearVelocity = Vector3.zero;

        rb.AddForce(delayedForceToApply, ForceMode.Impulse);
    }

    private void ResetDash()
    {
        pm.dashing = false;
        pm.maxYSpeed = 0;

        cam.DoFov(85f);

       
        if (disableGravity)
            rb.useGravity = true;
    }

    private Vector3 GetDirection(Transform forwardT)
    {
       
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

       
        Vector3 direction = new Vector3();

        if (allowAllDirections)
            direction = forwardT.forward * verticalInput + forwardT.right * horizontalInput;
        else
            direction = forwardT.forward;

        if (verticalInput == 0 && horizontalInput == 0) 
            direction = forwardT.forward;

        return direction.normalized;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(playerCam.position, playerCam.forward);
    }
}
