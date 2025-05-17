using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private Text pointsDisplay;
    [SerializeField]
    private GameObject gameOverPanel;
    [SerializeField]
    private Text scoreDisplay;
    [SerializeField]
    private Transform nextShapeDisplay;
    [SerializeField]
    private float nextShapeSpinningSpeed;

    private GameObject previousNextShape;

    // Add this field to track points
    private int currentPoints = 0;

    public void DisplayPoints(int points)
    {
        currentPoints = points; // Store the latest points
        pointsDisplay.text = "Points: " + points;
        var userCheck = FindObjectOfType<YourNamespace.UserCheck>();
        if (userCheck != null)
        {
            string nickname = PlayerPrefs.GetString("CurrentNickname", "");
            userCheck.UpdateUserPoints(nickname, points);
        }
    }

    public int GetCurrentPoints()
    {
        return currentPoints;
    }

    public void DisplayGameOverPanel(int score)
    {
        gameOverPanel.SetActive(true);
        scoreDisplay.text = "Score: " + score;
    }

    public void DisplayNextShape(GameObject shape)
    {
        if (previousNextShape != null)
            Destroy(previousNextShape);

        GameObject gObject = Instantiate(shape, nextShapeDisplay);
        gObject.AddComponent<RectTransform>();
        Spinning spinning = gObject.AddComponent<Spinning>();
        spinning.speed = nextShapeSpinningSpeed;

        previousNextShape = gObject;
    }
}
