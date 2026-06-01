using UnityEngine;

public class FinalCollectible : MonoBehaviour
{
    public float floatSpeed = 1f;
    public float floatHeight = 0.3f;
    public float rotateSpeed = 100f;
    public AudioClip collectSound;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * floatSpeed) * floatHeight);
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (collectSound != null)
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            FindObjectOfType<LevelComplete>()?.OnFinalCollected();
            Destroy(gameObject);
        }
    }
}
