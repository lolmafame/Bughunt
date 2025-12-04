using UnityEngine;

public class AspectRatioEnforcer : MonoBehaviour
{
    // The aspect ratio we want (16:9)
    private float _targetAspect = 16.0f / 9.0f;

    void Start()
    {
        // Don't run this in the editor
        if (Application.isEditor)
        {
            return;
        }

        // Get the monitor's current resolution
        float screenWidth = Display.main.systemWidth;
        float screenHeight = Display.main.systemHeight;
        float screenAspect = screenWidth / screenHeight;

        int newWidth, newHeight;

        // Compare the monitor's aspect ratio to our target
        if (screenAspect > _targetAspect)
        {
            // Monitor is WIDER than 16:9 (e.g., 21:9 ultrawide)
            // We'll have black bars on the sides (pillarboxing)
            newHeight = (int)screenHeight;
            newWidth = (int)(newHeight * _targetAspect);
        }
        else
        {
            // Monitor is TALLER than 16:9 (e.g., 16:10, 4:3)
            // We'll have black bars on top/bottom (letterboxing)
            newWidth = (int)screenWidth;
            newHeight = (int)(newWidth / _targetAspect);
        }

        // Set the new resolution.
        // FullScreenMode.FullScreenWindow is best for this (borderless windowed)
        Screen.SetResolution(newWidth, newHeight, FullScreenMode.FullScreenWindow);
    }
}