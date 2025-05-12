using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ContinueButton : MonoBehaviour
{
    public void Continue()
    {
        string sceneToContinue = PlayerPrefs.GetString("SavedScene", "");

        if (!string.IsNullOrEmpty(sceneToContinue))
        {
            SceneManager.LoadScene(sceneToContinue);
        }
        else
        {
            SceneManager.LoadScene("Menu");
        }
    }
}
