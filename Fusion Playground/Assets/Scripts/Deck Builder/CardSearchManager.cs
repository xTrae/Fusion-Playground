using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using System.Linq;

// I would really like to add more complex search a capabilities.
//   1. Less precise name searches. Will find cards with very similar spellings.
//   2. Search by, type, color, cost, and text. Each of these would have a pop up menu with a type box.
//   3. Perhaps each button has a white (nothing) mode, and black (pop up open) mode, and gray (text inside me) mode. And a clear text button on the pop up.

[System.Serializable]
public class ScryfallBulkData
{
    public string download_uri;
}

public class CardSearchManager : MonoBehaviour
{
    public GameObject popupPanel;
    public TMP_InputField inputText;
    public Transform cardDisplayScrollScreen;
    public GameObject cardDisplayPrefab;

    // Buttons
    public Button searchButton;
    public Button searchPopupButton;
    public Sprite searchImageOn;
    public Sprite searchImageOff;
    public Sprite searchPopupImageOn;
    public Sprite searchPopupImageOff;

    private DeckManagerBuilder deckManagerScript;
    private string cachedSeach;
    private bool isPopupOpen = false;
    private List<ScryfallCard> scryfallCards = new List<ScryfallCard>();
    private List<ScryfallCard> foundCards = new List<ScryfallCard>();
    private List<GameObject> displayedCards = new List<GameObject>();
    private const string ScryfallFileName = "scryfall_cards.json";
    private bool doneDownloading = false;
    private float apiRequestDelay = 0.05f;
    private int cardWidth = 272;
    private int cardHeight = 384;
    private float smallCardWidth = 272 * 0.75f;
    private float smallCardHeight = 384 * 0.75f;
    private int cardSpacing = 45;
    private int searchBarVerticalSpacing = 120;
    private int scrollViewTotalHeight = 3450;
    private float yCenterOffset = 1358f - 735 + 51; // I have literally no idea where the 735 and 904 offsets are coming from. Dumbest shit in the world
    private float xCenterOffset = -600f + 904 - 37;
    private float lastActionTime;
    private const float ActionTimeThreshold = 0.3f;

    private void Start()
    {
        deckManagerScript = FindObjectOfType<DeckManagerBuilder>();
        ClosePopup();
        searchButton.onClick.AddListener(SearchCards);
        searchPopupButton.onClick.AddListener(TogglePopup);
        StartCoroutine(RetrieveAPIResults());
    }

    private void Update()
    {
        if (lastActionTime - Time.time > ActionTimeThreshold)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                lastActionTime = Time.time;
                TogglePopup();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                lastActionTime = Time.time;
                SearchCards();
            }
        }
        else
        {
            //Debug.Log("UwU! Nyaa~! Dat pwess of Entew is wike da speedy wind~! o>w<o Awe you twying to bweak da entewtainment system, senpai? OwO Shouwd I ask it to swow down, teehee? o>w<o UwU notices your swift key pwesses OwO What mischief awe we up to, senpai?~ giggles rapidly Teehee~! Nyaa~!");
        }
    }

    public void TogglePopup()
    {
        if (isPopupOpen) ClosePopup();
        else OpenPopup();
    }

    private void OpenPopup()
    {
        isPopupOpen = true;
        popupPanel.SetActive(true);
        searchPopupButton.GetComponent<Image>().sprite = searchPopupImageOn;
    }

    private void ClosePopup()
    {
        // Update the zone where cards will be displayed normally
        deckManagerScript.SetScrollScreen();
        deckManagerScript.UpdateCardLocations();
        deckManagerScript.ShiftCardsBack(true);
        deckManagerScript.UpdateCardHierarchy();
        // Close everything else
        isPopupOpen = false;
        popupPanel.SetActive(false);
        searchPopupButton.GetComponent<Image>().sprite = searchPopupImageOff;
    }

    private IEnumerator RetrieveAPIResults()
    {
        Debug.Log("Starting download!");
        doneDownloading = false;
        string filePath = Path.Combine(Application.persistentDataPath, ScryfallFileName);

        // Check if the file exists and if it was last downloaded today
        if (File.Exists(filePath) && File.GetLastWriteTime(filePath).Date == System.DateTime.Today)
        {
            Debug.Log("Found an existing file!");
            // Load data from the file
            ReadFileLines(filePath);
        }
        else
        {
            // Download data URI from Scryfall API
            string apiUrl = "https://api.scryfall.com/bulk-data/oracle-cards";
            UnityWebRequest request = UnityWebRequest.Get(apiUrl);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("First download success!");
                // Parse and store the API response
                string jsonResponse = request.downloadHandler.text;
                var bulkData = JsonUtility.FromJson<ScryfallBulkData>(jsonResponse);

                // Download the actual card data
                Debug.Log("Downloading all the Data! This may take a minute.");
                UnityWebRequest cardDataRequest = UnityWebRequest.Get(bulkData.download_uri);
                cardDataRequest.downloadHandler = new DownloadHandlerBuffer(); // Use DownloadHandlerBuffer for text data
                yield return cardDataRequest.SendWebRequest();

                if (cardDataRequest.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Second download success!");
                    // Parse and store the card data
                    var cardDataJson = cardDataRequest.downloadHandler.text;
                    Debug.Log(cardDataJson);
                    
                    // Save the data to a file
                    File.WriteAllText(filePath, cardDataJson);
                    Debug.Log("Downloaded and saved Scryfall cards.");

                    // Grab the data (it populates the card list)
                    ReadFileLines(filePath);
                }
                else
                {
                    Debug.LogError("Failed to retrieve Scryfall card data. Error: " + cardDataRequest.error);
                }
            }
            else
            {
                Debug.LogError("Failed to retrieve Scryfall bulk data. Error: " + request.error);
            }
        }
        doneDownloading = true;
    }

    private void ReadFileLines(string filePath)
    {
        scryfallCards.Clear();
        foreach (string line in File.ReadLines(filePath))
        {
            string changeableLine = line;
            //Debug.Log(line);
            if (changeableLine != null && changeableLine.Length > 2)
            {
                changeableLine.Trim();
                if (changeableLine[^1] == ',')
                {
                    //Debug.Log("COMMA!!!!");
                    changeableLine = changeableLine.Remove(changeableLine.Length - 1);
                }
                //Debug.Log("Looks like a card: " + line);
                ScryfallCard card = JsonConvert.DeserializeObject<ScryfallCard>(changeableLine.Trim());
                scryfallCards.Add(card);
            }
        }
        Debug.Log("That's a file with: " + scryfallCards.Count + " cards!");
    }

    private void ClearDisplayedCards()
    {
        // Destroy all previously displayed cards
        foreach (GameObject displayedCard in displayedCards)
        {
            Destroy(displayedCard);
        }

        // Clear the list
        displayedCards.Clear();
    }

    public void SearchCards()
    {
        string searchText = inputText.text.Trim();
        // You need more then 2 characters to do any kind of search
        if (searchText.Length < 3) return;
        if (!doneDownloading)
        {
            Debug.Log("Nya~ I'm stiww downwoading the data. Pwease come back soon-nya!"); 
            return;
        }
        if (scryfallCards.Count < 10)
        {
            // This is how I'm staying entertained.
            Debug.Log("UwU! Nyaa~! Hewwo, senpai~! OwO What's this? Fow some weason the cawd data is missing, teehee! >w< Sho-should I downwoad it again, nya? UwU *notices your tech pwoblem* OwO What awe we gonna do about this, senpai?~ *giggles kawaii-ly* Teehee~! Nyaa~! <3");
            return;
        }
        if (cachedSeach == searchText)
        {
            Debug.Log("UwU! Nyaa~! Dat seawch is da same as da wast one you made. Awe you twying to ovewwoad da system, nya? OwO Shouwd I give it a wittle bweak, senpai? Teehee~! >w< UwU notices your curious quewy OwO What mischief awe we up to, senpai?~ giggles mischievously Teehee~! Nyaa~! ");
            return;
        }
        // The image change is an indicator of the search loading
        searchButton.GetComponent<Image>().sprite = searchImageOn;
        Debug.Log("Searching for: " + searchText);
        cachedSeach = searchText;
        // Then sort, filter, find results within the Scryfall information, DisplayCardResults
        foundCards.Clear();
        ClearDisplayedCards();

        foreach (ScryfallCard card in scryfallCards)
        {
            if (card.name != null && card.name.Length > 2)
            {
                //string tempLowerName = card.name.ToLower();
                if (card != null && card.name.ToLower().Contains(searchText.ToLower()))
                {
                    // Checking for real paper cards. No tokens, online only, art cards, etc
                    if (card.name.Length > 2 && card.multiverse_ids.Count > 0 && card.multiverse_ids[0] > 0)
                    {
                        if (card.multiverse_ids.Count > 1) foundCards.Add(card.card_faces[0]);
                        else foundCards.Add(card);
                    }
                }
            }
        }
        if (foundCards.Count > 0)
        {
            Debug.Log("Found: " + foundCards.Count + " cards!");
            StartCoroutine(DisplayCardResults());
        }
        else
        {
            // A card was not found
            Debug.Log("No card with name " + searchText + " was found.");
        }
        searchButton.GetComponent<Image>().sprite = searchImageOff;
    }

    // Implement IEnumerator SearchScryfallAPI(string cardName) method for API calls

    IEnumerator DisplayCardResults()
    {
        Debug.Log("Attempting to display cards!");
        // Modify the display of the scroll view
        //int rowCount = (int)Math.Ceiling(foundCards.Count / 6.0f);
        //float scrollTotalHeight = (rowCount * (smallCardHeight + cardSpacing)) + searchBarVerticalSpacing + cardSpacing;
        // The xCenterOffset should be the exact yPos of the first column, already calulated
        //yCenterOffset = smallCardHeight + cardSpacing (rowCount * smallCardHeight - searchBarVerticalSpacing / 2);
        // The xCenterOffset should be the exact xPos of the first column.
        //xCenterOffset = (smallCardWidth + cardSpacing) * -2.5f;
        // I think I'm just going to have the scroll view size static
        //cardDisplayScrollScreen.GetComponent<RectTransform>().sizeDelta = new Vector2(0, scrollTotalHeight);
        // Instantiate and display card prefabs in cardDisplayParent
        int index = 0;
        foreach (ScryfallCard cardResult in foundCards)
        {
            yield return new WaitForSeconds(apiRequestDelay);
            //Debug.Log("Card?");
            // Only display the first 60 results. A grid of 6 by 10 cards.
            if (index <= 60 && index < foundCards.Count)
            {
                if (cardResult != null && cardResult.name.Length > 1)
                {
                    //Debug.Log("Card: " + cardResult.name);
                    // Create the card
                    GameObject cardInstance = Instantiate(cardDisplayPrefab, cardDisplayScrollScreen.transform);

                    // Set it's visuals
                    cardInstance.GetComponent<CardDisplayManager>().DisplayCard(cardResult);
                    cardInstance.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                    displayedCards.Add(cardInstance);

                    // Set it's location
                    int columNum = index % 6;
                    float xPos = xCenterOffset + (columNum * (smallCardWidth + cardSpacing));
                    // Don't ask me why this math wants to be difficult
                    int rowNum;
                    if (index > 0) rowNum = (int)((index)/ 6.0f);
                    else rowNum = 0;
                    float yPos = yCenterOffset - (rowNum * (smallCardHeight + cardSpacing));
                    cardInstance.transform.position = new Vector2(xPos, yPos);
                    //Debug.Log("At:: " + columNum + ", " + rowNum);
                    //Debug.Log("At : " + xPos + ", " + yPos);

                    index++;
                }
                else Debug.Log("Um excuse me, but what the actual fuck");
            }
            
        }
    }
}
