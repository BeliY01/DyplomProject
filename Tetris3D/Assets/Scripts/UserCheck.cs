using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Mono.Data.Sqlite; // Ensure you have the Mono.Data.Sqlite DLL in your project
using UnityEngine.SceneManagement;

namespace YourNamespace
{
    public class UserCheck : MonoBehaviour
    {
        [SerializeField]
        private InputField nicknameInputField;
        [SerializeField]
        private string databaseFileName = "UserDatabase.db";
        [SerializeField]
        private Button saveProgressButton;
        [SerializeField]
        private GameObject enterNamePanel; // Assign your UI panel for entering the name


        private string databaseFilePath;

        private void Start()
        {
            #if UNITY_EDITOR
            databaseFilePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, databaseFileName);
            #else
            databaseFilePath = Path.Combine(Application.dataPath, databaseFileName);
            #endif
            if (!File.Exists(databaseFilePath))
            {
                CreateDatabase();
            }

            if (saveProgressButton != null)
            {
                saveProgressButton.onClick.AddListener(SaveCurrentPoints);
            }
        }

        private void CreateDatabase()
        {
            string connectionString = $"URI=file:{databaseFilePath}";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nickname TEXT UNIQUE NOT NULL,
                        Progress TEXT,
                        Points INTEGER DEFAULT 0
                    );";
            command.ExecuteNonQuery();
            Debug.Log("Database created successfully at: " + databaseFilePath);
        }

        // Call this from your "Start Game" button
        public void TryStartGame()
        {
            if (nicknameInputField == null)
            {
                Debug.LogError("Nickname InputField is not assigned in the Inspector.");
                return;
            }
            string nickname = nicknameInputField.text;
            if (string.IsNullOrEmpty(nickname))
            {
                Debug.LogError("Please enter your name before starting the game.");
                return;
            }

            // Hide the enter name UI
            if (enterNamePanel != null)
                enterNamePanel.SetActive(false);

            AddOrContinueGame();
        }

        public void AddOrContinueGame()
        {
            if (nicknameInputField == null)
            {
                Debug.LogError("Nickname InputField is not assigned in the Inspector.");
                return;
            }

            string nickname = nicknameInputField.text;

            if (!string.IsNullOrEmpty(nickname))
            {
                string connectionString = $"URI=file:{databaseFilePath}";
                using var connection = new SqliteConnection(connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Progress FROM Users WHERE Nickname = @nickname";
                command.Parameters.AddWithValue("@nickname", nickname);
                var result = command.ExecuteScalar();

                if (result != null)
                {
                    string progress = result.ToString();
                    Debug.Log($"Welcome back, {nickname}! Continuing game from progress: {progress}");
                    //LoadGameScene();
                }
                else
                {
                    command.CommandText = "INSERT INTO Users (Nickname, Progress) VALUES (@nickname, @progress)";
                    command.Parameters.AddWithValue("@progress", "NewGame");
                    command.ExecuteNonQuery();
                    Debug.Log($"Nickname '{nickname}' added to the database. Starting a new game.");
                    //LoadGameScene();
                }
            }
            else
            {
                Debug.LogError("Nickname is empty. Please enter a valid nickname.");
            }
        }

       /* private void LoadGameScene()
        {
            SceneManager.LoadScene("Game");
        }*/

        public void SaveCurrentPoints()
        {
            if (nicknameInputField == null)
            {
                Debug.LogError("Nickname InputField is not assigned in the Inspector.");
                return;
            }
            string nickname = nicknameInputField.text;
            if (string.IsNullOrEmpty(nickname))
            {
                Debug.LogError("Nickname is empty. Please enter a valid nickname.");
                return;
            }

            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("UIManager not found in scene.");
                return;
            }

            int points = uiManager.GetCurrentPoints();

            UpdateUserPoints(nickname, points);
            Debug.Log($"Saved {points} points for {nickname}.");
        }

        private string GetDatabaseFilePath()
        {
            if (string.IsNullOrEmpty(databaseFilePath))
            {
                #if UNITY_EDITOR
                databaseFilePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, databaseFileName);
                #else
                databaseFilePath = Path.Combine(Application.dataPath, databaseFileName);
                #endif
            }
            return databaseFilePath;
        }

        public void UpdateUserPoints(string nickname, int points)
        {
            string connectionString = $"URI=file:{GetDatabaseFilePath()}";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Users SET Points = @points WHERE Nickname = @nickname";
            command.Parameters.AddWithValue("@points", points);
            command.Parameters.AddWithValue("@nickname", nickname);
            command.ExecuteNonQuery();
        }
    }
}
