using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;

public class CardCreatorManager : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private GameObject emptyCard;
    private float targetScreenShotWidth = 500f;
    private float cardAspectRatio = 1.4f;

    void Awake()
    {
        backButton.onClick.AddListener(PlayButtonPress);
        saveButton.onClick.AddListener(SaveButtonPress);
    }

    private void PlayButtonPress()
    {
        Debug.Log("You pressed the Back Button");
        SceneManager.LoadScene("MainMenuScene");
    }

    private void SaveButtonPress()
    {
        Debug.Log("You pressed the Save Button");

        // Define the folder path where screenshots will be saved
        string saveFolder = Path.Combine(Application.persistentDataPath, "SavedScreenshots");

        // Create the folder if it doesn't exist
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        // Define the file name for the screenshot (you can customize the file name if needed)
        string screenshotName = "screenshot_" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".png";

        // Get the RectTransform component of the emptyCard
        RectTransform rectTransform = emptyCard.GetComponent<RectTransform>();

        // Calculate the size of the object in screen space, taking into account scaling
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        float width = corners[2].x - corners[0].x;

        // Skip taking screenshot if width is less than 100 pixels
        if (width < 100f)
        {
            Debug.LogWarning("Width is less than 100 pixels. Skipping screenshot capture.");
            return;
        }

        // Calculate the center position of the object in screen space
        Vector3 center = (corners[0] + corners[2]) / 2f;

        // Calculate the position of the top-left corner of the capture area
        float x = center.x - (width / 2);
        float y = center.y - (width / 2); // Using width to maintain aspect ratio

        // Create a Texture2D with the specified dimensions and read pixels from the screen
        Texture2D screenshotTexture = new Texture2D((int)width, (int)width, TextureFormat.RGB24, false);
        screenshotTexture.ReadPixels(new Rect(x, y, width, width), 0, 0);
        screenshotTexture.Apply();

        // Scale the screenshot to a uniform size of 500 pixels width
        Texture2D scaledTexture = ScaleTexture(screenshotTexture, targetScreenShotWidth);

        // Define the file path for the screenshot
        string screenshotPath = Path.Combine(saveFolder, screenshotName);

        // Save the scaled texture to a file
        byte[] bytes = scaledTexture.EncodeToPNG();
        File.WriteAllBytes(screenshotPath, bytes);

        // Destroy the texture to free up memory
        Destroy(screenshotTexture);

        Debug.Log("Screenshot saved to: " + screenshotPath);
    }

    private Texture2D ScaleTexture(Texture2D source, float targetWidth)
    {
        //THIS IS NOT WORKING
        // This is very close, but I'm tried and can not look at this right now. I was copied from ChatGPT

        float targetHeight = targetWidth * cardAspectRatio;

        // Create a new texture with the target dimensions
        Texture2D scaledTexture = new Texture2D((int)targetWidth, (int)targetHeight, TextureFormat.RGB24, false);

        // Set the filter mode to Bilinear for better quality
        scaledTexture.filterMode = FilterMode.Bilinear;

        // Get the ratio of source to target dimensions
        float widthRatio = (float)source.width / targetWidth;
        float heightRatio = (float)source.height / targetHeight;

        // Iterate through each pixel of the target texture
        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                // Calculate the corresponding pixel position in the source texture
                int sourceX = Mathf.RoundToInt(x * widthRatio);
                int sourceY = Mathf.RoundToInt(y * heightRatio);

                // Get the pixel color from the source texture and set it in the scaled texture
                Color color = source.GetPixel(sourceX, sourceY);
                scaledTexture.SetPixel(x, y, color);
            }
        }

        // Apply changes and return the scaled texture
        scaledTexture.Apply();
        return scaledTexture;
    }
}
