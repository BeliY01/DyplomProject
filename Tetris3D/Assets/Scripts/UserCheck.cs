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
        private InputField nicknameInputField; // Reference to the textbox (InputField)
        [SerializeField]
        private string databaseFileName = "UserDatabase.db"; // SQLite database file name

        private string databaseFilePath;

        private void Start()
        {
            // Set the full path for the database file
            databaseFilePath = Path.Combine(Application.persistentDataPath, databaseFileName);

            // Create the database if it doesn't exist
            if (!File.Exists(databaseFilePath))
            {
                CreateDatabase();
            }
        }

        private void CreateDatabase()
        {
            string connectionString = $"URI=file:{databaseFilePath}";
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    // Create a table to store user logins and game progress
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Users (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Nickname TEXT UNIQUE NOT NULL,
                            Progress TEXT
                        );";
                    command.ExecuteNonQuery();
                }
            }
            Debug.Log("Database created successfully.");
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
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        // Check if the nickname already exists
                        command.CommandText = "SELECT Progress FROM Users WHERE Nickname = @nickname";
                        command.Parameters.AddWithValue("@nickname", nickname);
                        var result = command.ExecuteScalar();

                        if (result != null)
                        {
                            // Nickname exists, continue the game
                            string progress = result.ToString();
                            Debug.Log($"Welcome back, {nickname}! Continuing game from progress: {progress}");
                            LoadGameScene();
                        }
                        else
                        {
                            // Nickname does not exist, add it to the database
                            command.CommandText = "INSERT INTO Users (Nickname, Progress) VALUES (@nickname, @progress)";
                            command.Parameters.AddWithValue("@progress", "NewGame"); // Default progress for new users
                            command.ExecuteNonQuery();
                            Debug.Log($"Nickname '{nickname}' added to the database. Starting a new game.");
                            LoadGameScene();
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Nickname is empty. Please enter a valid nickname.");
            }
        }

        private void LoadGameScene()
        {
            SceneManager.LoadScene("Game"); // Replace "Game" with the actual name of your game scene
        }
    }
}
