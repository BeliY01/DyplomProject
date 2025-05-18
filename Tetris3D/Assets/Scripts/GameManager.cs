using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public float deletedRowBonus;

    [Header("Components")]
    public UIManager UIManager;
    public Movement movement;
    public TileController tileController;

    public static bool gameOver { private set; get; }

    private static GameManager instance;
    private static int points = 0;

    // Reference to your UI panel
    [SerializeField]
    private GameObject Canvas_Main_UserCheck;

    private void Awake()
    {
        instance = this;
        gameOver = false;
        AddPoints(0);
    }

    private void Update()
    {
        // Pause if Canvas_Main_UserCheck is active, resume if not
        if (Canvas_Main_UserCheck != null)
        {
            if (Canvas_Main_UserCheck.activeInHierarchy)
                Time.timeScale = 0f;
            else
                Time.timeScale = 1f;
        }
    }

    public static void GameOver()
    {
        PlayerPrefs.SetString("SavedScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();

        instance.UIManager.DisplayGameOverPanel(points);
        instance.DeactivateScripts();
        gameOver = true;
    }

    public static void FullRow(int index)
    {
        GridController.instance.DeleteRow(index);
        Movement.CheckSpeedUp();
        AddPoints(Mathf.FloorToInt(instance.deletedRowBonus * GridController.instance.size.x * GridController.instance.size.y));
    }

    public static void AddPoints(int p)
    {
        points += p;
        instance.UIManager.DisplayPoints(points);

        if (points >= 10)
        {
            Movement.CheckSpeedUp();
        }
    }

    public void PlayAgain()
    {
        points = 0;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    private void DeactivateScripts()
    {
        tileController.enabled = false;
        movement.enabled = false;
    }
}
