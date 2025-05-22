using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mono.Data.Sqlite;
using System.IO;
using System;


public class ScoreBoard : MonoBehaviour
{
    [SerializeField]
    private Text scoreBoardText; 
    void Start()
    {
        DisplayScoreBoard();
    }

    public void DisplayScoreBoard()
    {
        if (scoreBoardText == null)
        {
            Debug.LogError("ScoreBoard Text не назначен в инспекторе!");
            return;
        }

        string connectionString = "URI=file:UserDatabase.db";
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Nickname, Points FROM Users ORDER BY Points DESC LIMIT 10;";
                using (var reader = command.ExecuteReader())
                {
                    int rank = 1;
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine("“аблиц€ рекорд≥в:");
                    while (reader.Read())
                    {
                        string nickname = reader.GetString(0);
                        int points = reader.GetInt32(1);
                        sb.AppendLine($"{rank}. {nickname} Ч {points} бали");
                        rank++;
                    }
                    scoreBoardText.text = sb.ToString();
                }
            }
        }
    }
}
