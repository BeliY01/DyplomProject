using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace AutomaticTutorialMaker
{
    public class KeyboardInputHelper
    {
        public enum KeyboardGroup // Defines keyboard key groups
        {
            Alphanumeric,
            Function,
            Navigation,
            NumPad,
            Modifiers,
            Special
        }

        // Groups of keyboard keys by category
        private static readonly Dictionary<KeyboardGroup, KeyCode[]> keyGroups = new Dictionary<KeyboardGroup, KeyCode[]>
        {
            {
                KeyboardGroup.Alphanumeric,
                new KeyCode[] {
                    KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F,
                    KeyCode.G, KeyCode.H, KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L,
                    KeyCode.M, KeyCode.N, KeyCode.O, KeyCode.P, KeyCode.Q, KeyCode.R,
                    KeyCode.S, KeyCode.T, KeyCode.U, KeyCode.V, KeyCode.W, KeyCode.X,
                    KeyCode.Y, KeyCode.Z,
                    KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
                    KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7,
                    KeyCode.Alpha8, KeyCode.Alpha9
                }
            },
            {
                KeyboardGroup.Function,
                new KeyCode[] {
                    KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5,
                    KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10,
                    KeyCode.F11, KeyCode.F12
                }
            },
            {
                KeyboardGroup.Navigation,
                new KeyCode[] {
                    KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
                    KeyCode.Home, KeyCode.End, KeyCode.PageUp, KeyCode.PageDown,
                    KeyCode.Insert, KeyCode.Delete, KeyCode.Tab
                }
            },
            {
                KeyboardGroup.NumPad,
                new KeyCode[] {
                    KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3,
                    KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7,
                    KeyCode.Keypad8, KeyCode.Keypad9, KeyCode.KeypadPeriod, KeyCode.KeypadDivide,
                    KeyCode.KeypadMultiply, KeyCode.KeypadMinus, KeyCode.KeypadPlus,
                    KeyCode.KeypadEnter, KeyCode.KeypadEquals
                }
            },
            {
                KeyboardGroup.Modifiers,
                new KeyCode[] {
                    KeyCode.LeftShift, KeyCode.RightShift,
                    KeyCode.LeftControl, KeyCode.RightControl,
                    KeyCode.LeftAlt, KeyCode.RightAlt,
                    KeyCode.LeftCommand, KeyCode.RightCommand
                }
            },
            {
                KeyboardGroup.Special,
                new KeyCode[] {
                    KeyCode.Space, KeyCode.Return, KeyCode.Escape, KeyCode.Backspace,
                    KeyCode.CapsLock, KeyCode.ScrollLock, KeyCode.Pause, KeyCode.Break,
                    KeyCode.Print, KeyCode.SysReq
                }
            }
        };

        // Returns array of keys in specified group
        public static KeyCode[] GetKeysInGroup(KeyboardGroup group)
        {
            return keyGroups[group];
        }

        // Returns all available keyboard keys
        public static KeyCode[] GetAllAvailableKeys()
        {
            return keyGroups.Values.SelectMany(x => x).ToArray();
        }

        // Checks if any key was pressed this frame
        public static bool GetAnyKeyDown()
        {
            if (!Application.isFocused) return false;
            return Input.anyKeyDown;
        }

        // Returns list of currently pressed keys
        public static List<KeyCode> GetPressedKeys()
        {
            List<KeyCode> pressedKeys = new List<KeyCode>();
            foreach (KeyCode key in GetAllAvailableKeys())
            {
                if (Input.GetKeyDown(key))
                {
                    pressedKeys.Add(key);
                }
            }
            return pressedKeys;
        }

        // Returns list of released keys
        public static List<KeyCode> GetReleasedKeys()
        {
            List<KeyCode> releasedKeys = new List<KeyCode>();
            foreach (KeyCode key in KeyboardInputHelper.GetAllAvailableKeys())
            {
                if (Input.GetKeyUp(key))
                {
                    releasedKeys.Add(key);
                }
            }
            return releasedKeys;
        }

        // Checks if specific keys are currently pressed
        public static bool AreKeysPressed(List<KeyCode> keys)
        {
            return keys.All(key => Input.GetKey(key));
        }

        // Draws keyboard group selector in Unity inspector
#if UNITY_EDITOR
        public static void DrawKeyboardGroupSelector(List<KeyCode> selectedKeys, string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            foreach (KeyboardGroup group in System.Enum.GetValues(typeof(KeyboardGroup)))
            {
                EditorGUILayout.LabelField(group.ToString(), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                var keysInGroup = GetKeysInGroup(group);
                foreach (var key in keysInGroup)
                {
                    bool isSelected = selectedKeys.Contains(key);
                    bool newValue = EditorGUILayout.Toggle(key.ToString(), isSelected);

                    if (newValue != isSelected)
                    {
                        if (newValue)
                            selectedKeys.Add(key);
                        else
                            selectedKeys.Remove(key);
                    }
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            EditorGUI.indentLevel--;
        }
#endif
    }
}