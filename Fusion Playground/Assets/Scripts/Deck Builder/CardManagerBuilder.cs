using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CardManagerBuilder : MonoBehaviour
{
    private const string scryfallAPI = "https://api.scryfall.com/cards/named?exact=";
    private const string quotation = "\"";
    public bool isDragging = false;
    public bool isSelected = false;
    private Vector2 startPosition;

    public Vector2 cachedMousPos = new Vector2(0, 0);
    public ScryfallCard cardInfo;
    public Image cardImage; // Reference to the UI Image component
    private DeckManagerBuilder deckManagerScript;
    public CanvasGroup canvasGroup;
    //public LineRenderer lineRenderer;
    public float desiredX = 0;
    public float desiredY = 0;
    private int originalSiblingIndex = 0;
    private int cardWidth = 272;
    private int cardHeight = 384;
    private int cardVerticalSpacing = 45;

    private Dictionary<string, Texture2D> imageCache = new Dictionary<string, Texture2D>();
    private Dictionary<string, ScryfallCard> cardCache = new Dictionary<string, ScryfallCard>();

    private float apiRequestDelay = 0.02f; // Set the delay between API requests in seconds
    private Vector2 hoverTimer = new Vector2(0.0f, 0.0f); // Time, Countdown true/false
    private float hoverWaitTime = 0.2f;

    private void Start()
    {
        // Get the DeckManager script from the scene
        deckManagerScript = FindObjectOfType<DeckManagerBuilder>();

        //DrawLine(new Vector2(0, 0), new Vector2(30, 30));
    }

    /*void DrawLine(Vector2 startPoint, Vector2 endPoint)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }*/

    public void BeginDrag()
    {
        // Not 100% sure what to do with this this variable yet. This tells you when the card has been highlighted (increased size)
        //if (!isSelected) return; 

        // Save the original sibling index for later
        originalSiblingIndex = transform.GetSiblingIndex();
        // Set the sibling index to the highest, this makes it appear on top of the others
        transform.SetSiblingIndex(transform.parent.childCount - 1);
        // Make the card partially transparent
        cardImage.color = new Color(1f, 1f, 1f, 0.75f);
        // Disabling raycasts during dragging allows the Pointer Enter and Exit methods to see the mouse
        canvasGroup.blocksRaycasts = false;
        // Dragging card logic
        isDragging = true;
        deckManagerScript.isDraggingCard = true;
        // This isn't used and I'm not sure it ever will be
        startPosition = transform.position;
    }

    public void EndDrag(bool isCancel = false)
    {
        // Put it back in the hierarchy
        transform.SetSiblingIndex(originalSiblingIndex);
        // Set the card back to full transparency
        cardImage.color = new Color(1f, 1f, 1f, 1f);
        // Disabling raycasts during dragging allows the Pointer Enter and Exit methods to see the mouse
        canvasGroup.blocksRaycasts = true;
        // Dragging card logic
        isDragging = false;
        deckManagerScript.isDraggingCard = false;
        if (!isCancel) deckManagerScript.TryPairCards(gameObject); // Paring the cards already shifts them back
        else deckManagerScript.ShiftCardsBack();
    }

    public void Update()
    {
        // Dragging Logic
        if (isDragging)
        {
            hoverTimer[1] = 1;
            Vector2 currentPosition;
            Vector2 goalPosition;
            float speed = 15.0f;
            float step;

            goalPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            currentPosition = new Vector2(transform.position.x, transform.position.y);
            step = Time.deltaTime * speed * (currentPosition - goalPosition).magnitude;
            transform.position = Vector2.MoveTowards(currentPosition, goalPosition, step);
            transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

            if (Input.GetMouseButtonDown(1))
            {
                EndDrag();
            }
        }
        // Highlighting logic. If the mouse is hovered over for enough time, increase the card's size. Make it draggable.
        else if (hoverTimer[1] == 1)
        {
            // If the mouse is within the bounds of the card, the timer counts down
            if (transform.position.x - (cardWidth / 2) < Input.mousePosition.x && Input.mousePosition.x < transform.position.x + (cardWidth / 2) && transform.position.y - (cardHeight / 2) < Input.mousePosition.y && Input.mousePosition.y < transform.position.y + (cardHeight / 2))
            {
                // Elapse some time
                hoverTimer[0] -= Time.deltaTime;
                // If the time has elapsed, make it bigger and draggable
                if (hoverTimer[0] < 0)
                {
                    isSelected = true;
                    transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                }
            }
            else
            {
                // Reset everything, shuts off the timer
                hoverTimer[1] = 0;
                hoverTimer[0] = hoverWaitTime;
                isSelected = false;
                transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                // In the future, it'd be nice to smoothly shift from one scale to another, instead of being instant
            }
        }
    }

    // Called when the mouse pointer enters the card
    public void OnPointerEnter()
    {
        // In the future we do want cards to move while dragging, but not right now.
        if (isDragging) return;
        // This event should only trigger once. Without caching, sometimes it gets stuck in a loop.
        // I should be working off cached time or a timer. My lord that's probably 10 times better.
        if (cachedMousPos[0] == Input.mousePosition.x && cachedMousPos[1] == Input.mousePosition.y) return;
        cachedMousPos[0] = Input.mousePosition.x; cachedMousPos[1] = Input.mousePosition.y;
        // Set a timer to "highlight" the card, increasing it's size.
        hoverTimer[1] = 1; hoverTimer[0] = hoverWaitTime;
        isSelected = false;
        transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        // Notify the DeckManager to shift cards down
        deckManagerScript.ShiftCardsDown(gameObject, desiredX, desiredY);

        // AHHHH I think I want to completely remove this method of mouse searching and would like to build a by location system where it's constantly just seeing the mouse position and comparing.
    }

    // Called when the mouse pointer exits the card
    public void OnPointerExit()
    {
        // In the future we do want cards to move while dragging, but not right now.
        if (isDragging) return;
        cachedMousPos[0] = Input.mousePosition.x; cachedMousPos[1] = Input.mousePosition.y;
        // Remove the timer to "highlight" the card, increasing it's size.
        hoverTimer[1] = 0; hoverTimer[0] = hoverWaitTime;
        isSelected = false;
        transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        
        // Notify the DeckManager to shift back to their original places
        deckManagerScript.ShiftCardsBack();
    }

    public void SetCardInfo(string newCardName, float xPos, float yPos, string newPairValue)
    {
        desiredX = xPos;
        desiredY = yPos;
        cardInfo.pairing = newPairValue;
        if (cardCache.TryGetValue(newCardName, out ScryfallCard cachedCard))
        {
            // Use cached data if available
            Debug.Log("REALLY using cached data.");
            ApplyCardInfo(cachedCard);
        }
        else
        {
            // Fetch card data from API with delay
            StartCoroutine(FetchCardDataWithDelay(newCardName));
        }
    }

    IEnumerator FetchCardDataWithDelay(string newCardName)
    {
        yield return new WaitForSeconds(apiRequestDelay);
        StartCoroutine(FetchCardData(newCardName));
    }

    IEnumerator FetchCardData(string newCardName)
    {
        //Debug.Log("Fetching Data for: " + newCardName);
        string apiUrl = "https://api.scryfall.com/cards/named?exact=" + quotation + newCardName + quotation;
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ScryfallCard card = JsonUtility.FromJson<ScryfallCard>(request.downloadHandler.text);

                // Check if the card has multiple faces (DFC)
                if (card.card_faces != null && card.card_faces.Count > 0)
                {
                    // Extract the first face of the DFC
                    ScryfallCard face = card.card_faces[0];

                    // Cache the fetched card data
                    cardCache[newCardName] = face;

                    // Apply card information
                    ApplyCardInfo(face);
                }
                else
                {
                    // Cache the fetched card data
                    cardCache[newCardName] = card;

                    // Apply card information
                    ApplyCardInfo(card);
                }
            }
            else
            {
                Debug.LogError("Failed to fetch card data. Error: " + request.error);
                // Handle error (e.g., display an error message to the user)
            }
        }
    }

    IEnumerator DownloadImage(string imageUrl, Action<Texture2D> onComplete)
    {
        if (imageCache.TryGetValue(imageUrl, out Texture2D cachedTexture))
        {
            onComplete?.Invoke(cachedTexture);
            //Debug.Log("Using a cached image");
        }
        else
        {
            //Debug.Log("NOT using a cached image");
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    //Debug.Log("Image downloaded successfully: " + imageUrl);
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

    IEnumerator DownloadAndUseImage(string imageUrl, ScryfallCard card)
    {
        yield return DownloadImage(imageUrl, (texture) =>
        {
            if (texture != null)
            {
                cardImage.sprite = TextureToSprite(texture);
                // Set additional information in the card object
                card.image_uris.large = imageUrl; // This line is just a placeholder; modify as needed
                                                  // You can set other information here as needed
            }
        });
    }

    public void ApplyCardInfo(ScryfallCard card)
    {
        if (card.name != null && card.name.Length > 1)
        {
            // Apply card information directly from the ScryfallCard object
            cardInfo = card;
            StartCoroutine(DownloadAndUseImage(card.image_uris.large, card));
            //Debug.Log("Downloading image at: " + card.image_uris.large);
        }
        else
        {
            Debug.Log("Error while applying card info. Name was null.");
            cardInfo.name = "";
        }
    }

    public void UpdateCardPositions(float xPos, float yPos)
    {
        desiredX = xPos;
        desiredY = yPos;
    }

    Sprite TextureToSprite(Texture2D texture)
    {
        if (texture != null)
        {
            //Debug.Log("Creating sprite from texture: " + texture);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }
        else
        {
            Debug.LogError("Texture is null. Cannot create sprite.");
            return null;
        }
    }
}