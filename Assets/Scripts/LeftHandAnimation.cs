using UnityEngine;

public class LeftHandAnimation : MonoBehaviour
{
    [SerializeField] private Animator leftHand;
    [SerializeField] private string leftHandRaised = "Left Hand was raised";

    private bool playerInTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
        }
    }

    private void Update()
    {
        if (playerInTrigger && Input.GetKeyDown(KeyCode.H))
        {
            leftHand.Play(leftHandRaised, 0, 0.0f);
        }
    }
}