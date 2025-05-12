using System;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace AutomaticTutorialMaker
{
    public static class KeyCodeExtensions
    {
#if ENABLE_INPUT_SYSTEM
        public static Key ToKey(this KeyCode keyCode)
        {
            string keyName = keyCode.ToString();
            if (Enum.TryParse(keyName, out Key key))
            {
                return key;
            }

            return Key.None;
        }
#endif
    }
}