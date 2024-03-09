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

    private void Awake()
    {
        backButton.onClick.AddListener(PlayButtonPress);
        saveButton.onClick.AddListener(SaveButtonPress);
    }

    private void Update()
    {
        //Debug.Log(Input.mousePosition.x + " : " + Input.mousePosition.y);
    }

    private void PlayButtonPress()
    {
        Debug.Log("You pressed the Back Button");
        SceneManager.LoadScene("MainMenuScene");
    }

    private void SaveButtonPress()
    {
        Debug.Log("You pressed the Save Button");

        TakeScreenShot();
    }

    private void TakeScreenShot()
    {
        // Define the folder path where screenshots will be saved
        string saveFolder = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Saved Screenshots");

        // Create the folder if it doesn't exist
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        // Define the file name for the screenshot (you can customize the file name if needed)
        string screenshotName = "screenshot_" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".png";

        // Get the RectTransform component of the emptyCard
        RectTransform rectTransform = emptyCard.GetComponent<RectTransform>();

        // Calculate the size and position of the object in screen space
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Debug.Log(corners[0] + " : " + corners[1] + " : " + corners[2] + " : " + corners[3]);

        float originX = corners[0][0];
        float originY = corners[0][1];
        float width = corners[2][0] - corners[0][0];
        float height = corners[2][1] - corners[0][1];
        //Debug.Log(originX + " : " + originY + " : " + width + " : " + height);

        // Skip taking screenshot if width is less than 100 pixels
        if (width < 100f)
        {
            Debug.LogWarning("Width is less than 100 pixels. Skipping screenshot capture.");
            return;
        }

        // Create a Texture2D with the specified dimensions and read pixels from the screen
        Texture2D screenshotTexture = new Texture2D((int)width, (int)height, TextureFormat.RGB24, false);
        screenshotTexture.ReadPixels(new Rect(originX, originY, width, height), 0, 0);
        screenshotTexture.Apply();

        // Scale the screenshot to a uniform size of 500 pixels width
        Texture2D scaledTexture = ScaleTexture(screenshotTexture);

        // Define the file path for the screenshot
        string screenshotPath = Path.Combine(saveFolder, screenshotName);

        // Save the scaled texture to a file
        byte[] bytes = scaledTexture.EncodeToPNG();
        File.WriteAllBytes(screenshotPath, bytes);

        // Destroy the texture to free up memory
        Destroy(screenshotTexture);

        Debug.Log("Screenshot saved to: " + screenshotPath);
    }

    private Texture2D ScaleTexture(Texture2D source)
    {
        int targetWidth = 500;
        int targetHeight = 700;

        // Create a new texture with the specified dimensions
        Texture2D reversedTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);

        // Calculate scaling factors
        float scaleX = (float)source.width / targetWidth;
        float scaleY = (float)source.height / targetHeight;

        // Copy pixels from the source texture to the reversed texture
        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                int newX = Mathf.RoundToInt(x * scaleX);
                int newY = Mathf.RoundToInt(y * scaleY);
                Color32 pixel = source.GetPixel(newX, newY);
                reversedTexture.SetPixel(x, y, pixel);
            }
        }

        // Apply changes and return the reversed texture
        reversedTexture.Apply();
        return reversedTexture;
    }
}
