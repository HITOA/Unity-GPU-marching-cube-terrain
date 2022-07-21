using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FpsCounter : MonoBehaviour
{
    float frameRate;
    private void Update()
    {
        frameRate = 1.0f / Time.unscaledDeltaTime;
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 200, 200), $"Fps : {frameRate.ToString()}");
    }
}
