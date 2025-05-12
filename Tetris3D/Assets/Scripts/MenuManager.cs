using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField]
    private string[] sceneNames = new string[5]; // Array to hold up to 5 scene names

    // Method to load a scene by index
    public void OpenSceneByIndex(int index)
    {
        if (index >= 0 && index < sceneNames.Length && !string.IsNullOrEmpty(sceneNames[index]))
        {
            SceneManager.LoadScene(sceneNames[index]);
        }
        else
        {
            Debug.LogError("Invalid scene index or scene name is not set!");
        }
    }

    // Method to exit the application
    public void ExitApplication()
    {
        Application.Quit();
        Debug.Log("Application has been exited."); // For debugging in the editor
    }
}
