using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mono.Data.Sqlite;
using System.IO;
using System;


public class ScoreBoard : MonoBehaviour
{
    [SerializeField] private Transform contentPanel; // Контейнер Content из Scroll View
    [SerializeField] private GameObject rowPrefab;   // Префаб строки (например, с Text или TextMeshProUGUI)
    [SerializeField] private string databaseFileName = "UserDatabase.db"; // SQLite database file name

    private string databaseFilePath;

    void Start()
    {
        #if UNITY_EDITOR
        databaseFilePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, databaseFileName);
        #else
        databaseFilePath = Path.Combine(Application.dataPath, databaseFileName);
        #endif
        ShowScores();
    }

    void ShowScores()
    {
        string connectionString = $"URI=file:{databaseFilePath}";
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        // Select all columns
        command.CommandText = "SELECT Id, Nickname, Progress, Points FROM Users ORDER BY Points DESC";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            int id = reader["Id"] != DBNull.Value ? reader.GetInt32(0) : 0;
            string nickname = reader["Nickname"].ToString();
            string progress = reader["Progress"] != DBNull.Value ? reader["Progress"].ToString() : "";
            int points = reader["Points"] != DBNull.Value ? reader.GetInt32(3) : 0;

            GameObject row = Instantiate(rowPrefab, contentPanel);
            Text[] texts = row.GetComponentsInChildren<Text>();
            // Expecting at least 4 Texts: Id, Nickname, Progress, Points
            if (texts.Length >= 4)
            {
                texts[0].text = id.ToString();
                texts[1].text = nickname;
                texts[2].text = progress;
                texts[3].text = points.ToString();
            }
            else if (texts.Length == 1) // If you use a single Text (e.g., for a multiline summary)
            {
                texts[0].text = $"ID: {id} | Name: {nickname} | Progress: {progress} | Points: {points}";
            }
        }
    }
}
