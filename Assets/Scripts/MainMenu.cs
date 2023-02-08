using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {
	[SerializeField] private string nextSceneName;
   
    public void PlayGame ()
    {
        SceneManager.LoadScene(nextSceneName);
    }


    public void QuitGame ()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}