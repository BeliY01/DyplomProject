using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace AutomaticTutorialMaker
{
    [CreateAssetMenu(fileName = "InputData", menuName = "Tutorial System/InputData")]
    public class InputStringsScriptableObject : ScriptableObject
    {
        #region Variables
        [Header("CSV Source")]
        [Tooltip("CSV file containing translation data")]
        public TextAsset csvFile; // CSV file containing translation data
        [Tooltip("CSV file for default tips generation")]
        [Grayed] [SerializeField] private TextAsset csvBackup; // CSV file not meant to edit

        [ReadOnly]
        [Tooltip("Separator used in the CSV file")]
        public string separator = ";";

        [Header("ATM Language")]
        [SerializeField]
        [Tooltip("Currently selected language for text display")]
        public Language _currentLanguage = Language.English; // Currently selected language        
        private int _customLanguageColumnIndex = -1; // Store custom language column index

        [HideInInspector]
        [Tooltip("Flag indicating whether the strings have been refreshed")]
        public bool refreshed = false; // Flag indicating whether the strings have been refreshed

        [Tooltip("List of all tip elements containing translated text")]
        public List<InputElement> tipStrings = new List<InputElement>(); // List of all tip elements

        private string[] lines; // Array of lines from CSV file

        // Cache data
        private string lastProcessedCsvPath;
        private long lastFileModificationTime;
        private long lastFileSize;
        private int minColumn;
        private int maxColumn;

        [Serializable]
        public class InputElement
        {
            [Header("Step")]
            [HideInInspector] public int stepIndex; // Index of the current step
            [HideInInspector] public string currentScene; // Name of the current scene
            [HideInInspector] public string currentATM; // Name of the current ATM

            [Header("Full Text")]
            public string PointerText; // Text displayed for pointer type tips
            public string GraphicText; // Text displayed for graphic type tips
            public string WorldPointerText; // Text displayed for world pointer type tips
            public string WorldGraphicText; // Text displayed for world graphic type tips
        }

        // Define your language enum
        public enum Language
        {
            English = 2, // English language column index
            German = 5, // German language column index
            French = 6, // French language column index
            Spanish = 7, // Spanish language column index
            Portuguese = 8, // Portuguese language column index
            Italian = 9, // Italian language column index
            Ukrainian = 10, // Ukrainian language column index
            Turkish = 26, // Turkish language column index
            Chinese_SpecialFontRequired = 3,
            Japanese_SpecialFontRequired = 4,
            AddCustomLanguage = 11,
            AddCustomLanguage2 = 12,
            AddCustomLanguage3 = 13,
            FoundByString = -1 // Special value for custom language
        }

        #endregion

        #region Methods To Call Manually - Localization
        // Changes the current language and refreshes all tip strings
        public void ChangeLanguage(Language language)
        {
            _currentLanguage = language;
            RefreshTipStrings();
        }

        // Changes the current language by string name and refreshes all tip strings
        public void ChangeLanguage(string languageName)
        {
            if (string.IsNullOrEmpty(languageName))
            {
                Debug.LogError("[ATM] Language name cannot be empty!");
                return;
            }

            // First check if this is a standard language in our enum
            foreach (Language lang in Enum.GetValues(typeof(Language)))
            {
                if (lang.ToString().Equals(languageName, StringComparison.OrdinalIgnoreCase))
                {
                    ChangeLanguage(lang);
                    return;
                }
            }

            // If not found in enum, search in the CSV headers
            int columnIndex = FindLanguageColumnIndex(languageName);
            if (columnIndex >= 0)
            {
                _currentLanguage = Language.FoundByString;
                _customLanguageColumnIndex = columnIndex;
                RefreshTipStrings();
                Debug.Log($"[ATM] Changed to custom language '{languageName}' at column index {columnIndex}");
            }
            else
            {
                Debug.LogError($"[ATM] Language '{languageName}' not found in CSV headers!");
            }
        }

        #endregion

        #region Getting Default Tips

        // Gets the string text for a specific row index in the current language
        public string GetStringText(int rowIndex)
        {
            // If using custom language, use the custom column index
            int columnIndex = _currentLanguage == Language.FoundByString ? _customLanguageColumnIndex : (int)_currentLanguage;
            string extractedValue = GetThirdElementFromLine(rowIndex, columnIndex);
            return extractedValue;
        }

        // Extract the element from a specific line and column in the CSV file
        private string GetThirdElementFromLine(int rows, int whichOne)
        {
            CacheLines();

            string extractedValue = "";
            if (lines.Length > rows)
            {
                string line = lines[rows];
                string[] tipStrings = line.Split(separator);
                if (tipStrings.Length > whichOne)
                {
                    extractedValue = tipStrings[whichOne];
                }
                else
                {
                    Debug.LogWarning($"[ATM] Row {rows} does not contain an element at column {whichOne}");
                    extractedValue = "";
                }
            }
            else
            {
                Debug.LogWarning($"[ATM] Not enough rows in the CSV file. Requested row: {rows}, Total rows: {lines.Length}");
                extractedValue = "";
            }
            return extractedValue;
        }
        #endregion

        #region Tip Update
        // Refreshes all tip strings with translations from the current language
        private void RefreshTipStrings()
        {
            CacheLines();

            foreach (InputElement element in tipStrings)
            {
                if (!string.IsNullOrEmpty(element.PointerText))
                {
                    element.PointerText = TranslateText(element.PointerText, _currentLanguage);
                }

                if (!string.IsNullOrEmpty(element.GraphicText))
                {
                    element.GraphicText = TranslateText(element.GraphicText, _currentLanguage);
                }

                if (!string.IsNullOrEmpty(element.WorldPointerText))
                {
                    element.WorldPointerText = TranslateText(element.WorldPointerText, _currentLanguage);
                }

                if (!string.IsNullOrEmpty(element.WorldGraphicText))
                {
                    element.WorldGraphicText = TranslateText(element.WorldGraphicText, _currentLanguage);
                }
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            refreshed = false;
        }
        #endregion

        #region Tip Localization

        // Finds the column index for a language by its name in the CSV header
        private int FindLanguageColumnIndex(string languageName)
        {
            CacheLines();

            if (lines == null || lines.Length == 0)
            {
                Debug.LogError("[ATM] CSV file is empty or not loaded properly!");
                return -1;
            }

            // Assume the language names are in the first line (header)
            string headerLine = lines[0];
            string[] headers = headerLine.Split(separator);

            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i].Trim().Equals(languageName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1; // Language not found
        }

        // Translates text from any language to the target language
        private string TranslateText(string originalText, Language targetLanguage)
        {
            if (string.IsNullOrEmpty(originalText) || csvFile == null)
                return originalText;

            CacheLines();

            // Determine the actual column index to use
            int columnIndex = targetLanguage == Language.FoundByString ? _customLanguageColumnIndex : (int)targetLanguage;

            // If custom language is selected but no column was found, return original text
            if (targetLanguage == Language.FoundByString && columnIndex < 0)
            {
                Debug.LogWarning("[ATM] Custom language selected but no column index set!");
                return originalText;
            }

            int[] wholeMatch = FindPhraseInAnyLanguage(originalText);
            if (wholeMatch[0] >= 0)
            {
                string translation = GetThirdElementFromLine(wholeMatch[0], columnIndex);

                if (!string.IsNullOrWhiteSpace(translation))
                    return translation;
            }

            List<string> translatedParts = new List<string>();
            string[] words = originalText.Split(' ');

            int currentIndex = 0;
            while (currentIndex < words.Length)
            {
                bool foundMatch = false;

                for (int phraseLength = words.Length - currentIndex; phraseLength > 0; phraseLength--)
                {
                    string phraseToFind = string.Join(" ", words, currentIndex, phraseLength);
                    int[] match = FindPhraseInAnyLanguage(phraseToFind);

                    if (match[0] >= 0)
                    {
                        string translation = GetThirdElementFromLine(match[0], columnIndex);

                        if (!string.IsNullOrWhiteSpace(translation))
                        {
                            translatedParts.Add(translation);
                            currentIndex += phraseLength;
                            foundMatch = true;
                            break;
                        }
                    }
                }

                if (!foundMatch)
                {
                    translatedParts.Add(words[currentIndex]);
                    currentIndex++;
                }
            }

            string result = string.Join(" ", translatedParts);
            while (result.Contains("  "))
            {
                result = result.Replace("  ", " ");
            }

            return result;
        }

        // Finds a phrase in any language column of the CSV file
        private int[] FindPhraseInAnyLanguage(string phrase)
        {
            if (phrase == null || lines == null)
                return new int[] { -1, -1 };

            phrase = phrase.Trim();
            if (string.IsNullOrEmpty(phrase))
                return new int[] { -1, -1 };           

            for (int rowIndex = 0; rowIndex < lines.Length; rowIndex++)
            {
                string line = lines[rowIndex];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] columns = line.Split(separator);

                if (columns.Length <= minColumn)
                    continue;

                for (int colIndex = minColumn; colIndex <= Math.Min(maxColumn, columns.Length - 1); colIndex++)
                {
                    string columnContent = columns[colIndex].Trim();
                    if (columnContent.Equals(phrase, StringComparison.OrdinalIgnoreCase))
                    {
                        return new int[] { rowIndex, colIndex };
                    }
                }
            }

            return new int[] { -1, -1 };
        }
        #endregion

        #region Optimization
        // Caches CSV file lines for faster access
        private void CacheLines()
        {
            if (csvFile == null)
            {
                Debug.LogError($"[ATM] Failed to load CSV file.");
                return;
            }

            if (HasFileChanged(csvFile) || lines == null || lines.Length == 0)
            {
            }
            else
            {
                return;
            }

            lines = csvFile.text.Split('\n');

            Debug.Log("[ATM] CSV file updated.");

            minColumn = int.MaxValue;
            foreach (Language lang in Enum.GetValues(typeof(Language)))
            {
                if (lang != Language.FoundByString)
                {
                    int value = (int)lang;
                    if (value >= 0)
                    {
                        minColumn = Math.Min(minColumn, value);
                    }
                }
            }

            if (minColumn == int.MaxValue)
            {
                minColumn = 0;
            }

            maxColumn = 0;
            if (lines.Length > 0)
            {
                string headerLine = lines[0];
                string[] headers = headerLine.Split(separator);

                for (int i = headers.Length - 1; i >= 0; i--)
                {
                    if (!string.IsNullOrWhiteSpace(headers[i]))
                    {
                        maxColumn = i;
                        break;
                    }
                }
            }
        }

        // Checks if the CSV file has been modified
        private bool HasFileChanged(TextAsset csvAsset)
        {
#if UNITY_EDITOR
            if (csvAsset == null)
                return false;

            try
            {
                string filePath = UnityEditor.AssetDatabase.GetAssetPath(csvAsset);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(filePath);

                    bool timeChanged = fileInfo.LastWriteTimeUtc.Ticks > lastFileModificationTime;
                    bool sizeChanged = fileInfo.Length != lastFileSize;

                    if (timeChanged || sizeChanged)
                    {
                        lastFileModificationTime = fileInfo.LastWriteTimeUtc.Ticks;
                        lastFileSize = fileInfo.Length;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ATM] Error checking file: {ex.Message}");
            }
#endif
            return false;
        }
        #endregion

        #region Export Functionality
        // Export all non-empty strings from Tip Strings to the CSV file
        public void ExportCurrentLines()
        {
#if UNITY_EDITOR
            if (csvFile == null)
            {
                Debug.LogError("[ATM] Cannot export: CSV file is not assigned!");
                return;
            }

            string filePath = AssetDatabase.GetAssetPath(csvFile);
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("[ATM] Cannot get file path for the CSV file!");
                return;
            }

            if (IsFileLocked(filePath))
            {
                Debug.LogError("[ATM] Cannot export: CSV file is currently locked by another application. Please close it and try again.");
                return;
            }

            try
            {
                CacheLines();

                List<string> stringsToExport = new List<string>();

                foreach (InputElement element in tipStrings)
                {
                    if (!string.IsNullOrEmpty(element.PointerText))
                        stringsToExport.Add(element.PointerText);

                    if (!string.IsNullOrEmpty(element.GraphicText))
                        stringsToExport.Add(element.GraphicText);

                    if (!string.IsNullOrEmpty(element.WorldPointerText))
                        stringsToExport.Add(element.WorldPointerText);

                    if (!string.IsNullOrEmpty(element.WorldGraphicText))
                        stringsToExport.Add(element.WorldGraphicText);
                }

                if (stringsToExport.Count == 0)
                {
                    Debug.LogWarning("[ATM] No non-empty strings found to export!");
                    return;
                }

                string[] existingLines = File.ReadAllLines(filePath);
                List<string> updatedLines = new List<string>(existingLines);
                int startIndex = existingLines.Length;

                foreach (string stringToExport in stringsToExport)
                {
                    StringBuilder newLine = new StringBuilder();
                    newLine.Append(separator);
                    newLine.Append(stringToExport);

                    updatedLines.Add(newLine.ToString());
                }

                File.WriteAllLines(filePath, updatedLines);
                AssetDatabase.Refresh();
                csvFile = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
                CacheLines();
                Debug.Log($"[ATM] Successfully exported {stringsToExport.Count} strings to {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ATM] Error exporting strings: {ex.Message}");
            }
#endif
        }

        // Checks if a file is locked by another process
        private bool IsFileLocked(string filePath)
        {
            FileStream stream = null;

            try
            {
                stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
        }
        #endregion

        #region Editor
#if UNITY_EDITOR
        [CustomEditor(typeof(InputStringsScriptableObject))]
        public class DialogueDataEditor : Editor
        {
            private Language lastLanguage; // Tracks the previous language selection

            // Called when the editor is enabled
            private void OnEnable()
            {
                var dialogueData = (InputStringsScriptableObject)target;
                lastLanguage = dialogueData._currentLanguage;
            }

            // Custom inspector GUI implementation
            public override void OnInspectorGUI()
            {
                InputStringsScriptableObject dialogueData = (InputStringsScriptableObject)target;
                var beforeLanguage = dialogueData._currentLanguage;

                EditorGUILayout.Space();
                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.6f, 0.4f, 1f);

                if (GUILayout.Button("EXPORT CURRENT STRINGS TO CSV (SAVE)", GUILayout.Height(30)))
                {
                    dialogueData.ExportCurrentLines();
                }

                GUI.backgroundColor = originalColor;
                EditorGUILayout.Space();

                DrawDefaultInspector();

                if (dialogueData._currentLanguage != beforeLanguage)
                {
                    if (dialogueData._currentLanguage == Language.FoundByString)
                    {
                        Debug.LogWarning("[ATM] Cannot directly select FoundByString language. Call ChangeLanguage(string languageName) instead.");
                        dialogueData._currentLanguage = beforeLanguage; 
                    }
                    else
                    {
                        Debug.Log($"[ATM] Language changed from {beforeLanguage} to {dialogueData._currentLanguage}");
                        dialogueData.RefreshTipStrings();
                        lastLanguage = dialogueData._currentLanguage;
                    }
                }
            }
        }
#endif
        #endregion
    }
}