using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {
	[SerializeField] private string nextSceneName;
	[SerializeField] private GameObject loadingText;


	private void Start() {
		loadingText.SetActive(false);
	}
	
	
    public void PlayGame ()
    {
        SceneManager.LoadScene(nextSceneName);
        loadingText.SetActive(true);
    }


    public void QuitGame ()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}