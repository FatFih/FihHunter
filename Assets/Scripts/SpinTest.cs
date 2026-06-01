using UnityEngine;

public class SpinTest : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        transform.Rotate(0, 100 * Time.deltaTime, 0);
    }
}
