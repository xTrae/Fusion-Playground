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
    public int Xequals = 3;

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

                    // Notate the card is double sided. Useful for later.
                    face.doubleFaced = true;

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
            ExtractCardType();
            ExtractManaValue();
            ExtractColors();
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

    private void ExtractCardType()
    {
        // Double sided cards have their card types written like "Creature - Dog // Land - Mountain"
        // However, cards which transform should not be counted. We need only worry about modal double sided cards.
        Debug.Log("Card Type: " + cardInfo.type_line);
        string typeLine = cardInfo.type_line;
        string[] parts = typeLine.Split(new[] { " // " }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            int indexOfDash = parts[i].IndexOf('-'); // Find the index of the dash character

            if (indexOfDash != -1) // Check if the dash character exists in the string
            {
                string modifiedPart = parts[i].Substring(0, indexOfDash).Trim(); // Extract the substring before the dash and trim any extra spaces
                parts[i] = modifiedPart;
            }
        }

        string modifiedLine = string.Join(" ", parts);
        cardInfo.type_line = modifiedLine;
        Debug.Log("New Typeline: " + modifiedLine);

        // Create a sorting value based on card type.
        double sortingValue = 0.0;
        // Primary Card Type
        if (cardInfo.type_line.Contains("Land"))         sortingValue += 1;
        else if (cardInfo.type_line.Contains("Creature"))     sortingValue += 2;
        else if (cardInfo.type_line.Contains("Artifact"))     sortingValue += 3;
        else if (cardInfo.type_line.Contains("Enchantment"))  sortingValue += 4;
        else if (cardInfo.type_line.Contains("Sorcery"))      sortingValue += 5;
        else if (cardInfo.type_line.Contains("Instant"))      sortingValue += 6;
        else if (cardInfo.type_line.Contains("Planeswalker")) sortingValue += 7;
        else if (cardInfo.type_line.Contains("Battle"))       sortingValue += 8;
        // Secondary Card Types
        if (cardInfo.type_line.Contains("Land"))         sortingValue += 0.1;
        if (cardInfo.type_line.Contains("Basic")) sortingValue += 0.1;
        if (cardInfo.type_line.Contains("Creature"))     sortingValue += 0.02;
        if (cardInfo.type_line.Contains("Artifact"))     sortingValue += 0.003;
        if (cardInfo.type_line.Contains("Enchantment"))  sortingValue += 0.0004;
        if (cardInfo.type_line.Contains("Sorcery"))      sortingValue += 0.00005;
        if (cardInfo.type_line.Contains("Instant"))      sortingValue += 0.000006;
        if (cardInfo.type_line.Contains("Planeswalker")) sortingValue += 0.0000007;
        if (cardInfo.type_line.Contains("Battle"))       sortingValue += 0.00000008;
        // Highest values first, decending.
        cardInfo.type_sorting = sortingValue;
        Debug.Log("Type Sorting Value: " + sortingValue);
    }

    private void ExtractManaValue()
    {
        // Extract mana value from the mana cost string
        Debug.Log("Name: " + cardInfo.name);
        Debug.Log("Mana Cost: " + cardInfo.mana_cost);
        string manaCost = cardInfo.mana_cost;
        int manaValue = 0;

        // Remove curly braces and split the mana cost into individual symbols
        string[] symbols = manaCost.Split(new[] { '{', '}' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string symbol in symbols)
        {
            // Check if the symbol is a number
            if (int.TryParse(symbol, out int number))
            {
                manaValue += number;
            }
            else if (symbol == "X")
            {
                manaValue += Xequals; // Assume X is 3
            }
            else
            {
                // Mana symbols like {W}, {U/2}, {B/P}, {R/G}, etc.
                manaValue += 1;
            }
            //Debug.Log("I found: " + symbol);
        }

        cardInfo.mana_value = manaValue;
        //Debug.Log("Mana Value: " + manaValue);
    }

    private void ExtractColors()
    {
        //foreach (string color in cardInfo.colors)
        //{
        //   Debug.Log("Color: " + color);
        //}
        double sortingValue = 0.0;
        sortingValue = cardInfo.colors.Count;
        sortingValue += (cardInfo.color_identity.Count / 10);
        if (cardInfo.color_identity.Contains("W")) sortingValue += 0.01;
        if (cardInfo.color_identity.Contains("U")) sortingValue += 0.002;
        if (cardInfo.color_identity.Contains("B")) sortingValue += 0.0003;
        if (cardInfo.color_identity.Contains("R")) sortingValue += 0.00004;
        if (cardInfo.color_identity.Contains("G")) sortingValue += 0.000005;
        // I want to make sure even when sorting mana value, lands are sorted lower than 0 mana value cards.
        if (cardInfo.type_line.Contains("Land")) sortingValue -= 1;
        // Lowest values first, acending.
        cardInfo.color_sorting = sortingValue;
        Debug.Log("Color Sorting Value: " + sortingValue);
    }
}