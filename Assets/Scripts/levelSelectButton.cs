using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class levelSelectButton : MonoBehaviour
{
    public string levelName;
    void Start()
    {
        
    }

    public void changeScene()
    {
        SceneManager.LoadScene(levelName);
    }
    void Update()
    {
        
    }

}
