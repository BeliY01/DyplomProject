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
        command.CommandText = "SELECT Nickname, Points FROM Users ORDER BY Points DESC";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            string nickname = reader["Nickname"].ToString();
            int points = reader["Points"] != DBNull.Value ? reader.GetInt32(1) : 0;

            GameObject row = Instantiate(rowPrefab, contentPanel);
            Text[] texts = row.GetComponentsInChildren<Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = nickname;
                texts[1].text = points.ToString();
            }
        }
    }
}
