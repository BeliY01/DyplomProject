using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

public class MainMenu : MonoBehaviour
{
    public string gameScene;
    public string Options;
    public string Achievementss;

    public void Play()
    {
        SceneManager.LoadScene(gameScene);
    }

    public void Option()
    {
        SceneManager.LoadScene(1);
    }

    public void Achievements()
    {
        SceneManager.LoadScene(2);
    }
}
