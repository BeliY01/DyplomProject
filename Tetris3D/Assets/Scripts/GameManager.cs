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
    //private const int SPEED_UP_THRESHOLD = 50;

    private void Awake()
    {
        instance = this;
        gameOver = false;
        AddPoints(0);
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
        AddPoints( Mathf.FloorToInt( instance.deletedRowBonus * GridController.instance.size.x * GridController.instance.size.y) );
    }

    public static void AddPoints(int p)
    {
        points += p;
        instance.UIManager.DisplayPoints(points);

        // Check if points have reached the threshold for speeding up
        if (points >= 10)
        {
            Movement.CheckSpeedUp(); // Trigger speed-up in the Movement class
           /* points -= 10;*/ // Reset points to allow for the next threshold
        }

    }

    public void PlayAgain()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);   
    }

    private void DeactivateScripts()
    {
        tileController.enabled = false;
        movement.enabled = false;
    }
}
