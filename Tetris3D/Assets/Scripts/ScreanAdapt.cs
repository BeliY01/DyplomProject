using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreanAdapt : MonoBehaviour
{
    private Vector2 lastScreenSize;

    void Start()
    {
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        AdaptAllChildren();
    }

    void Update()
    {
        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            AdaptAllChildren();
        }
    }

    void AdaptAllChildren()
    {
        // Базовые размеры, под которые проектировалась сцена
        float baseWidth = 1920f;
        float baseHeight = 1080f;

        float scaleX = Screen.width / baseWidth;
        float scaleY = Screen.height / baseHeight;
        float scale = Mathf.Min(scaleX, scaleY);

        foreach (Transform child in transform)
        {
            child.localScale = Vector3.one * scale;
        }
    }
}
