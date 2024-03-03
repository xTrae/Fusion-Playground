using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.IO;
using TMPro;
using Newtonsoft.Json;
using System.Linq;


public class CardDisplayManager : MonoBehaviour
{
    public ScryfallCard cardInfo;
    public Image cardImage;
    private DeckManagerBuilder deckManagerScript;
    private bool isGray = false;
    private float lastClickTime;
    private float lastDoubleClickTime;
    private const float DoubleClickTimeThreshold = 0.3f;
    private const float ReClickTimeThreshold = 0.7f;
    private const float apiRequestDelay = 0.02f;
    private Dictionary<string, Texture2D> imageCache = new Dictionary<string, Texture2D>();

    private void Start()
    {
        // Get the DeckManager script from the scene
        deckManagerScript = FindObjectOfType<DeckManagerBuilder>();
    }

    public void PointerClick()
    {
        if (cardInfo.name != null && cardInfo.name.Length > 1)
        {
            // Double-click detected
            if (Time.time - lastClickTime < DoubleClickTimeThreshold && Time.time - lastDoubleClickTime > ReClickTimeThreshold)
            {
                lastDoubleClickTime = Time.time;
                // Gray out the card, make it look disabled
                cardImage.color = new Color(0.7f, 0.7f, 0.7f, 1.0f);
                isGray = true;
                if (deckManagerScript != null) deckManagerScript.AddCardFromSearch(cardInfo);
                else Debug.Log("Something real fucky happened or everything is broken anyway.");
            }
        }
        else Debug.Log("The card info has not been filled out yet.");
        lastClickTime = Time.time;
    }

    private void Update()
    {
        if (isGray && Time.time - lastDoubleClickTime > ReClickTimeThreshold)
        {
            // Set the card back to normal, make it look inabled
            cardImage.color = new Color(1f, 1f, 1f, 1.0f);
            isGray = false;
        }
    }

    public void DisplayCard(ScryfallCard newInfo)
    {
        cardInfo = newInfo;
        cardInfo.pairing = "Unpaired";
        cardInfo.category = new List<String> { "Unassigned" };
        ApplyCardInfo();
    }

    private void ApplyCardInfo()
    {
        if (cardInfo.name != null && cardInfo.name.Length > 1)
        {
            // Apply card information directly from the ScryfallCard object
            StartCoroutine(DownloadAndUseImage(cardInfo.image_uris.large));
            Debug.Log("Downloading image at: " + cardInfo.image_uris.large);
        }
        else
        {
            Debug.Log("Error while applying card info. Name was null.");
            cardInfo.name = "null";
        }
    }

    private IEnumerator DownloadAndUseImage(string imageUrl)
    {
        yield return DownloadImage(imageUrl, (texture) =>
        {
            if (texture != null)
            {
                cardImage.sprite = TextureToSprite(texture);
            }
        });
    }

    private Sprite TextureToSprite(Texture2D texture)
    {
        if (texture != null)
        {
            Debug.Log("Creating sprite from texture: " + texture);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }
        else
        {
            Debug.LogError("Texture is null. Cannot create sprite.");
            return null;
        }
    }

    private IEnumerator DownloadImage(string imageUrl, Action<Texture2D> onComplete)
    {
        if (imageCache.TryGetValue(imageUrl, out Texture2D cachedTexture))
        {
            onComplete?.Invoke(cachedTexture);
            Debug.Log("Using a cached image");
        }
        else
        {
            Debug.Log("NOT using a cached image");
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Image downloaded successfully: " + imageUrl);
                    Texture2D texture = DownloadHandlerTexture.GetContent(www);
                    imageCache[imageUrl] = texture;
                    onComplete?.Invoke(texture);
                }
                else
                {
                    Debug.LogError("Failed to download image. Error: " + www.error);
                    onComplete?.Invoke(null);
                }
            }
        }
    }
}
