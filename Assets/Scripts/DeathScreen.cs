using UnityEngine;

public class DeathScreen : MonoBehaviour
{
    public LayerMask whatIsLava;
    public GameObject deathScreenUI;
    public AudioClip deathSound;

    private Vector3 startPosition;
    private Rigidbody rb;
    private MonoBehaviour[] allScripts;
    private bool[] wasEnabled;
    private bool dead;

    private void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody>();
        allScripts = GetComponents<MonoBehaviour>();
        wasEnabled = new bool[allScripts.Length];
        if (deathScreenUI != null) deathScreenUI.SetActive(false);
    }

    private void Update()
    {
        if (dead && Input.GetMouseButtonDown(0))
            Respawn();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!dead && ((1 << collision.gameObject.layer) & whatIsLava) != 0)
            Die();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!dead && ((1 << other.gameObject.layer) & whatIsLava) != 0)
            Die();
    }

    private void Die()
    {
        dead = true;
        for (int i = 0; i < allScripts.Length; i++)
        {
            wasEnabled[i] = allScripts[i].enabled;
            if (allScripts[i] != this) allScripts[i].enabled = false;
        }
        rb.constraints = RigidbodyConstraints.FreezeAll;
        if (deathSound != null)
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        if (deathScreenUI != null) deathScreenUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Respawn()
    {
        dead = false;
        transform.position = startPosition;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        var pm = GetComponent<PlayerMovementGrappling>();
        if (pm != null) pm.ResetRestrictions();
        if (deathScreenUI != null) deathScreenUI.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        StartCoroutine(EnableScriptsNextFrame());
    }

    private System.Collections.IEnumerator EnableScriptsNextFrame()
    {
        yield return null;
        for (int i = 0; i < allScripts.Length; i++)
            if (allScripts[i] != this && wasEnabled[i])
                allScripts[i].enabled = true;
    }
}
