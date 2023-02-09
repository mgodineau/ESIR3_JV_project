using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Button Triggers")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private bool pausedGame;

    [Header("Other")]
    public KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private GameObject thePlayer;
	[SerializeField] private string menuName;

    public void PauseGame()
    {
        //Pauses game and player's scripts
        Time.timeScale = 0;
        pausedGame = true;
        thePlayer.GetComponent<PlayerMovement>().enabled = false;
        
        //Shows the cursor for menu view
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        //Resumes game and player's scripts
        Time.timeScale = 1;
        pausedGame = false;
        thePlayer.GetComponent<PlayerMovement>().enabled = true;
        
        //Hides the cursor for fps view
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void QuitGame()
    {
        SceneManager.LoadScene(menuName);
    }

    void StartGame()
    {
        pausedGame = false;
    }

    void Update()
    {
        if(Input.GetKeyUp(pauseKey) && !pausedGame)
        {
            pauseButton.onClick.Invoke();
        }else if(Input.GetKeyUp(pauseKey) && pausedGame)
        {
            resumeButton.onClick.Invoke();
        }
        
    }

}
