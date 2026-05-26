using UnityEngine;

public class GrapplingAnimation : MonoBehaviour
{
    public Animator myAnimator;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            myAnimator.SetTrigger("Interact");
        }
    }
}
