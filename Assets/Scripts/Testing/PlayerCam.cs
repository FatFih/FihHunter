using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;
    public float multiplier;

    public Transform orientation;
    public Transform camHolder;

    float xRotation;
    float yRotation;

    [Header("Fov")]
    public bool useFluentFov;
    public Rigidbody rb;
    public Camera cam;
    public float minMovementSpeed;
    public float maxMovementSpeed;
    public float minFov;
    public float maxFov;

    private void Start()
    {
        sensX = PlayerPrefs.GetFloat("SensX", 26000f);
        sensY = PlayerPrefs.GetFloat("SensY", 26000f);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensY;

        yRotation += mouseX * multiplier;

        xRotation -= mouseY * multiplier;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        camHolder.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);

        if (useFluentFov) HandleFov();
    }

    private void HandleFov()
    {
        float moveSpeedDif = maxMovementSpeed - minMovementSpeed;
        float fovDif = maxFov - minFov;

        float rbFlatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        float currMoveSpeedOvershoot = rbFlatVel - minMovementSpeed;
        float currMoveSpeedProgress = currMoveSpeedOvershoot / moveSpeedDif;

        float fov = (currMoveSpeedProgress * fovDif) + minFov;

        float currFov = cam.fieldOfView;

        float lerpedFov = Mathf.Lerp(fov, currFov, Time.deltaTime * 200);

        cam.fieldOfView = lerpedFov;
    }

    private Coroutine fovCoroutine;
    private Coroutine tiltCoroutine;

    public void DoFov(float endValue)
    {
        if (fovCoroutine != null) StopCoroutine(fovCoroutine);
        fovCoroutine = StartCoroutine(LerpFov(endValue, 0.25f));
    }

    public void DoTilt(float zTilt)
    {
        if (tiltCoroutine != null) StopCoroutine(tiltCoroutine);
        tiltCoroutine = StartCoroutine(LerpTilt(zTilt, 0.25f));
    }

    private IEnumerator LerpFov(float endValue, float duration)
    {
        float start = cam.fieldOfView;
        float elapsed = 0;
        while (elapsed < duration)
        {
            cam.fieldOfView = Mathf.Lerp(start, endValue, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cam.fieldOfView = endValue;
    }

    private IEnumerator LerpTilt(float zTilt, float duration)
    {
        Vector3 start = transform.localEulerAngles;
        float elapsed = 0;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localEulerAngles = new Vector3(
                Mathf.LerpAngle(start.x, 0, t),
                Mathf.LerpAngle(start.y, 0, t),
                Mathf.LerpAngle(start.z, zTilt, t)
            );
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localEulerAngles = new Vector3(0, 0, zTilt);
    }
}