using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelComplete : MonoBehaviour
{
    public GameObject levelCompleteUI;
    private bool completed;

    public void OnFinalCollected()
    {
        if (completed) return;
        Complete();
    }

    void Complete()
    {
        completed = true;
        FindObjectOfType<Timer>().running = false;
        if (levelCompleteUI != null) levelCompleteUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartLevel()
    {
        Collectible.total = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
