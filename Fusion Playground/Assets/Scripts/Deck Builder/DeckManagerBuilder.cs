using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;

// The save button and other things need to have a visiual indicator. An in-game debug log.
// There needs to be a "new deck" button. Clears everything, removes the current deck name.
// The save button needs to be smarter about the deck name, not accepting blank names or things with unusual characters
//   I created a simple fix, only letting the deck name box accept numbers and letters, but it doesnt allow spaces... weird.
//   In a similar fashion, I should limit what it can parse for the deck name when uploading a text file. I don't want that to also be able
//   to break things. It should convert a messy file time into a safe one, and change the orignal file name.
// Need a way to delete deck files from within the game, since the file browser doesn't I believe.
// When you pair an unpaired card with an already paired card, there's a third card that could be auto-paired with an unpaired card, 
//   just like adding cards from a search does. That could be a setting.
// There is currently a bug where cards being added without a deck being loaded before hand breaks a few things.
//   the first two cards added by search stack on top of each other. And pairing any cards break because it's trying to place things in an empty list.
//   the latter probably just needs a check for list length zero and adding a card instead of placing it at a certain position in the list.

[System.Serializable]
public class ScryfallCard
{
    public string name;
    public string mana_cost;
    public List<string> colors;
    public List<string> color_identity;
    public double color_sorting;
    public string type_line;
    public int mana_value;
    public ImageUris image_uris;
    public List<int> multiverse_ids;
    public string pairing;
    public List<string> category;
    public List<ScryfallCard> card_faces; // Add this field for double-faced cards
    // Add more fields as needed
}

[System.Serializable]
public class ImageUris
{
    public string small;
    public string normal;
    public string large;
    public string png;
    // Add more fields as needed
}

public class DeckManagerBuilder : MonoBehaviour
{
    // UI elements
    public Button saveButton;
    public Button loadButton;
    public Button powerButton; 
    public Button unlikelySwapButton;
    public Button InstantPairingButton;
    // Prefabs and Canvas
    public GameObject cardPrefab; 
    public Canvas canvas;
    public GameObject scrollScreen;
    public GameObject trashCan;
    public LayerMask cardLayer;
    // Varibles used
    private List<ScryfallCard> deckList = new List<ScryfallCard>();
    private Dictionary<string, List<ScryfallCard>> categoryDictionary = new Dictionary<string, List<ScryfallCard>>();
    private List<GameObject> displayedCards = new List<GameObject>();
    private float colum11xPos;
    private float colum12xPos;
    private float colum21xPos;
    private float colum22xPos;
    private float columUnpairedxPos;
    private int cardVerticalSpacing = 45;
    private float cardHeight = 384f;
    private float cardWidth = 272f;
    private float scrollTotalHeight = 2000;
    private float yCenterOffset = 1000;
    private bool isDeckLoading = false; // Some proccesses shouldn't run and will break the game if they opperate while cards are being imported from file.
    public bool isDraggingCard = false;
    public float shiftingCooldown = 0.0f;
    private float shiftingCooldownNew = 0.3f;
    private float trashCanTimeThreshold = 0.3f;
    private float trashCanLastCheck = 0f;
    private int firstUnpairedIndex = 0;

    private bool UnlikelySwapSetting = false; // This may be turned into a dictionary of setting variables in the future
    public Sprite UnlikelySwapImageOn; // This should maybe be toggled by buttons and/or a settings menu instead of this script
    public Sprite UnlikelySwapImageOff; // I will also need to implment a settings storage/memory to file
    private bool InstantPairingSetting = false; 
    public Sprite InstantPairingImageOn;
    public Sprite InstantPairingImageOff;
    public Sprite TrashCanImageOn;
    public Sprite TrashCanImageOff;
    public TMP_InputField inputText;

    void Start()
    {
        Debug.developerConsoleVisible = true;
        // Attach button click listeners
        saveButton.onClick.AddListener(SaveDeck);
        loadButton.onClick.AddListener(LoadDeck);
        powerButton.onClick.AddListener(GoBack); 
        unlikelySwapButton.onClick.AddListener(UnlikelySwapToggle);
        InstantPairingButton.onClick.AddListener(InstantPairingToggle);

        colum11xPos = scrollScreen.transform.Find("Column 1-1").GetComponent<RectTransform>().anchoredPosition.x;
        colum12xPos = scrollScreen.transform.Find("Column 1-2").GetComponent<RectTransform>().anchoredPosition.x;
        colum21xPos = scrollScreen.transform.Find("Column 2-1").GetComponent<RectTransform>().anchoredPosition.x;
        colum22xPos = scrollScreen.transform.Find("Column 2-2").GetComponent<RectTransform>().anchoredPosition.x;
        columUnpairedxPos = scrollScreen.transform.Find("Column Unpaired").GetComponent<RectTransform>().anchoredPosition.x;
    }

    void Update()
    {
        // A timer that constantly ticks down to hopefully not shift cards around too often.
        if (shiftingCooldown > 0) shiftingCooldown -= Time.deltaTime;

        // A trash can to delete cards with. Testing if you're dragging a card on top.
        TrashCanCheck();
    }

    private void TrashCanCheck()
    {
        // A trash can to delete cards with. Testing if you're dragging a card on top.

        if (isDraggingCard || trashCanLastCheck - Time.time > trashCanTimeThreshold)
        {
            // Only check every fraction of a second instead of every frame.
            trashCanLastCheck = Time.time;
            // If the Magnitude is less than two hundred thousand (about a quarter of the screen)
            if ((Input.mousePosition - trashCan.transform.position).sqrMagnitude < 200000)
            {
                // Make the trash can visable.
                trashCan.SetActive(true);
                // If the Magnitude is less than ten thousand (the card should be touching the trash can)
                if ((Input.mousePosition - trashCan.transform.position).sqrMagnitude < 10000)
                {
                    // Set the Image to the red trash can
                    trashCan.GetComponent<Image>().sprite = TrashCanImageOn;
                }
                else
                {
                    // Set the image to the regular trash can image
                    trashCan.GetComponent<Image>().sprite = TrashCanImageOff;
                }
            }
            else
            {
                // Make the trash can invisible
                trashCan.SetActive(false);
            }
        }
        else
        {
            // Make the trash can invisible
            trashCan.SetActive(false);
        }
    }

    public void GoBack()
    {
        Debug.Log("You left the program!");
        // In the future, I would like to add a "Would you like to save?" pop up.
        SceneManager.LoadScene("MainMenuScene");
    }

    public void UnlikelySwapToggle()
    {
        Debug.Log("You toggled a setting.");
        if (UnlikelySwapSetting)
        {
            unlikelySwapButton.GetComponent<Image>().sprite = UnlikelySwapImageOff;
            UnlikelySwapSetting = false;
        }
        else
        {
            unlikelySwapButton.GetComponent<Image>().sprite = UnlikelySwapImageOn;
            UnlikelySwapSetting = true;
        }
    }

    public void InstantPairingToggle()
    {
        Debug.Log("You toggled a setting.");
        if (InstantPairingSetting)
        {
            InstantPairingButton.GetComponent<Image>().sprite = InstantPairingImageOff;
            InstantPairingSetting = false;
        }
        else
        {
            InstantPairingButton.GetComponent<Image>().sprite = InstantPairingImageOn;
            InstantPairingSetting = true;
        }
    }

    public void SaveDeck()
    {
        Debug.Log("Saving Deck!");

        // Example: Save the modified text back to a new text file
        string deckName = inputText.text.Trim() + ".txt";
        string savePath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Deck Lists", deckName);

        try
        {
            using (StreamWriter sw = File.CreateText(savePath))
            {
                foreach (ScryfallCard card in deckList)
                {
                    string line = FormatCardToString(card);
                    sw.WriteLine(line);
                }

                Debug.Log("Deck saved successfully at: " + savePath);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error saving deck: " + e.Message);
        }
    }

    public void LoadDeck()
    {
        Debug.Log("Loading Deck!");
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    private IEnumerator ShowLoadDialogCoroutine()
    {
        // Define the folder path where deck lists should be saved
        string savePath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Saved Screenshots");

        // Create the folder if it doesn't exist
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // Show a load file dialog and wait for a response from the user
        // FileBrowser.WaitForLoadDialog( File or Folder: both, Allow multiple selection: true, Location to start from: savePath,
        // Initial filename: null, Title: "Load File", Submit button text: "Load" );
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, false, savePath, null, "Load Files and Folders", "Load"); 

        // Waits for the File Dialog to be closed.
        // Print whether the user has selected some files/folders or canceled the operation (FileBrowser.Success)
        Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            // Print paths of the selected files (FileBrowser.Result) (null if FileBrowser.Success is false)
            for (int i = 0; i < FileBrowser.Result.Length; i++)
                Debug.Log(FileBrowser.Result[i]);

            // Load text from the selected file
            LoadTextFromFile(FileBrowser.Result[0]);

            // Get the file name without extension from the selected file path
            string fileName = System.IO.Path.GetFileNameWithoutExtension(FileBrowser.Result[0]);

            // Load the deck name from the name of the selected file
            Debug.Log("Selected file name: " + fileName);

            // Set the text of the input field
            inputText.text = fileName;

            DisplayDeck();
        }
    }

    private void LoadTextFromFile(string filePath)
    {
        // Read all lines from the text file
        string[] lines = File.ReadAllLines(filePath);

        // Clear existing data
        deckList.Clear();
        categoryDictionary.Clear();

        // Process each line
        foreach (string line in lines)
        {
            ScryfallCard card = ParseLineToCard(line);

            // Add card to the deckList
            deckList.Add(card);

            // Update the categoryDictionary
            if (card.category != null && card.category.Any())
            {
                foreach (string category in card.category)
                {
                    if (!categoryDictionary.ContainsKey(category))
                    {
                        categoryDictionary.Add(category, new List<ScryfallCard>());
                    }

                    categoryDictionary[category].Add(card);
                }
            }
        }

        // Print the loaded deck list data to the console
        /*foreach (ScryfallCard card in deckList)
        {
            Debug.Log($"Name: {card.name}, Pairing: {card.pairing}, Categories: {string.Join(", ", card.category)}");
        }*/
    }

    private string FormatCardToString(ScryfallCard card)
    {
        string line = $"{card.name}";

        if (!string.IsNullOrEmpty(card.pairing))
        {
            line += $" ({card.pairing})";
        }

        if (card.category != null && card.category.Any())
        {
            line += $" [{string.Join(", ", card.category)}]";
        }

        return line;
    }

    private ScryfallCard ParseLineToCard(string line)
    {
        ScryfallCard card = new ScryfallCard();

        // Split the line into parts
        string[] parts = line.Split(new char[] { '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
        // Card name should always be the first part
        card.name = parts[0].Trim();

        /*foreach (string part in parts)
        {
            Debug.Log(part);
        }*/
        

        // Extract pairing (if available)
        if (parts.Length > 1)
        {
            // Check if the part is a number (pairing)
            if (int.TryParse(parts[1].Trim(), out _))
            {
                card.pairing = parts[1].Trim();
            }
            else
            {
                card.pairing = "Unpaired";
            }
        }
        else
        {
            card.pairing = "Unpaired";
        }

        // Extract categories (if available)
        //card.category.Clear();
        card.category = new List<string>();
        if (parts.Length > 1)
        {
            // Check if the last part is NOT a number (category)
            if (!int.TryParse(parts[^1].Trim(), out _))
            {
                string[] categoriesArray = parts[^1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (categoriesArray != null && categoriesArray.Length > 0)
                {
                    foreach (string cat in categoriesArray)
                    {
                        card.category.Add(cat.Trim());
                    }
                }
                else
                {
                    card.category.Add("Unassigned");
                }

            }
            else
            {
                card.category.Add("Unassigned");
            }
        }
        else
        {
            card.category.Add("Unassigned"); ;
        }

        return card;
    }

    public void DisplayDeck()
    {
        isDeckLoading = true;
        // Clear existing displayed cards
        ClearDisplayedCards();

        // Sort the deck by pair value
        SortCardPairs();

        // Display each card with a delay
        StartCoroutine(DisplayCardsWithDelay());
    }

    public void SetScrollScreen()
    {
        int maxUnpairedCount = 0;

        // Calculate the maximum count of Unpaired cards
        foreach (ScryfallCard card in deckList)
        {
            if (card.pairing == "Unpaired")
            {
                maxUnpairedCount++;
            }
        }
        // Calculate the height of the Scroll View content
        int totalCards = Mathf.Max(maxUnpairedCount, (deckList.Count - maxUnpairedCount) / 2);
        scrollTotalHeight = (totalCards + 2) * cardVerticalSpacing + cardHeight;
        yCenterOffset = totalCards * cardVerticalSpacing / 2;

        // Set the height and Y offset of the Scroll View content
        scrollScreen.GetComponent<RectTransform>().sizeDelta = new Vector2(0, scrollTotalHeight);
    }

    IEnumerator DisplayCardsWithDelay()
    {
        // Set up the zone where the cards will be displayed
        firstUnpairedIndex = 0;
        SetScrollScreen();

        // Display each card
        for (int i = 0; i < deckList.Count; i++)
        {
            yield return new WaitForSeconds(0.02f);
            ScryfallCard card = deckList[i];

            // Calculate position based on sorting order and pair value
            float yPos = 0f;
            float xPos = 0f;

            if (card.pairing == "Unpaired")
            {
                xPos = columUnpairedxPos;
                if (firstUnpairedIndex == 0)
                {
                    firstUnpairedIndex = i;
                }
                yPos = (i - firstUnpairedIndex) * cardVerticalSpacing - yCenterOffset;
            }
            else if (i % 2 == 0)
            {
                xPos = colum21xPos;
                yPos = i / 2 * cardVerticalSpacing - yCenterOffset;
            }
            else
            {
                xPos = colum22xPos;
                yPos = (i - 1) / 2 * cardVerticalSpacing - yCenterOffset;
            }

            // Instantiate the card prefab
            GameObject cardInstance = Instantiate(cardPrefab, scrollScreen.transform);

            // Set the card data and update its display
            // (Assuming you have a CardDisplay script on your card prefab)
            cardInstance.GetComponent<CardManagerBuilder>().SetCardInfo(card.name, xPos, -yPos, card.pairing);

            // Set the card's position
            RectTransform cardTransform = cardInstance.GetComponent<RectTransform>();
            cardTransform.anchoredPosition = new Vector2(xPos, -yPos);

            // Add the card to the displayed cards list
            displayedCards.Add(cardInstance);
        }
        isDeckLoading = false;
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

    public void SortCardPairs()
    {
        // Group cards by pair value
        var groupedPairs = deckList.GroupBy(card => card.pairing);

        // Sort the grouped pairs by pair value
        var sortedPairs = groupedPairs.OrderBy(pair => ConvertPairValue(pair.Key));

        // Create a new list to store the sorted deck
        List<ScryfallCard> sortedDeck = new List<ScryfallCard>();

        // Dictionary to track the count of each pair value
        Dictionary<string, int> pairCount = new Dictionary<string, int>();

        // Iterate through sorted pairs
        foreach (var pair in sortedPairs)
        {
            string pairValue = pair.Key;

            // Check if there is only one card in the pair
            if (pair.Count() == 1)
            {
                pairValue = "Unpaired";
            }

            // Check if the pair value needs adjustment (more than two cards)
            int pairValueCount = pair.Count();
            if (pairValueCount > 2)
            {
                // Preserve exactly two cards with the same pair value, set the rest to "Unpaired"
                int preservedCount = 0;
                foreach (var card in pair)
                {
                    if (preservedCount < 2)
                    {
                        card.pairing = pairValue;
                        sortedDeck.Add(card);
                        preservedCount++;
                    }
                    else
                    {
                        card.pairing = "Unpaired";
                        sortedDeck.Add(card);
                    }
                }
            }
            else
            {
                // Increment the count for the current pair value
                pairValueCount++;
                pairCount[pairValue] = pairValueCount;

                // Update pair values for each card in the pair
                foreach (var card in pair)
                {
                    card.pairing = pairValue;
                    sortedDeck.Add(card);
                }
            }
        }

        // Move "Unpaired" cards to the end
        var unpairedCards = sortedDeck.Where(card => card.pairing == "Unpaired").ToList();
        sortedDeck.RemoveAll(card => card.pairing == "Unpaired");
        sortedDeck.AddRange(unpairedCards);

        // Replace the original deckList with the sorted deck
        deckList = sortedDeck;
    }

    // Helper method to convert pair value to integer (handles "Unpaired" as int.MaxValue)
    private int ConvertPairValue(string pairValue)
    {
        if (pairValue == "Unpaired")
        {
            return int.MaxValue;
        }
        // Trimming the parentheses is probably not necessary, but just in case.
        return int.Parse(pairValue.Trim('(', ')'));
    }

    IEnumerator DelayAction(float time)
    {
        // Wait for some seconds.
        yield return new WaitForSeconds(time);
    }

    public void ShiftCardsDown(GameObject hoveredCard, float hoveredX, float hoveredY)
    {
        if (isDeckLoading) return;
        shiftingCooldown = shiftingCooldownNew;
        // Iterate through cards below the hovered card and shift them down
        for (int index = 0; index < displayedCards.Count; index++)
        {
            // Find and reference the where they should be, not where they actually are.
            float desiredX = displayedCards[index].GetComponent<CardManagerBuilder>().desiredX;
            float desiredY = displayedCards[index].GetComponent<CardManagerBuilder>().desiredY;
            // Find the cards in the same column, or the column paired with it, and below the hovered card
            if (desiredX - cardWidth - cardVerticalSpacing < hoveredX && hoveredX < desiredX + cardWidth + cardVerticalSpacing && desiredY < hoveredY)
            {
                // If the card not the one card being dragged
                if (!displayedCards[index].GetComponent<CardManagerBuilder>().isDragging)
                {
                    SlideToPosition(displayedCards[index], desiredX, desiredY - cardHeight + cardVerticalSpacing);
                }
                
            }
            // Move the card to where it should be, just in case.
            else if (!displayedCards[index].GetComponent<CardManagerBuilder>().isDragging) SlideToPosition(displayedCards[index], desiredX, desiredY);
        }
    }

    public void ShiftCardsBack(bool forced = false)
    {
        if (isDeckLoading) return;
            //DelayAction(0.1f);
            // Send a ray cast to see if the mouse is still over other cards.
            RaycastHit2D[] hits = Physics2D.RaycastAll(Input.mousePosition, Vector2.zero, 0f, cardLayer);
        //Debug.Log("SHIFTING BACK HITS: " + hits.Length);
        // If it found more then one card. It definetly doesn't need to shift back.
        if (shiftingCooldown > 0 && hits.Length > 1 && !forced) return;
        shiftingCooldown = shiftingCooldownNew;
        // For some reason, the following makes it LESS accurate. I think it's hitting itself because of the changing card size? Even though it reverts before calling this.
        // If the one card found isn't being dragged, then don't shift the cards back.
        /*else if (hits.Length == 1) 
        {
            GameObject hitCard = hits[0].collider.gameObject;
            if (!hitCard.GetComponent<CardManagerBuilder>().isDragging) return; 
        }*/

        // Find where the cards should be, and place them there.
        for (int index = 0; index < deckList.Count; index++)
        {
            float desiredX = displayedCards[index].GetComponent<CardManagerBuilder>().desiredX;
            float desiredY = displayedCards[index].GetComponent<CardManagerBuilder>().desiredY;
            // If the card not the one card being dragged
            if (!displayedCards[index].GetComponent<CardManagerBuilder>().isDragging)
            {
                SlideToPosition(displayedCards[index], desiredX, desiredY);
            }
        }
    }

    // Method to slide a card to a given position
    public void SlideToPosition(GameObject card, float targetX, float targetY)
    {
        if (isDeckLoading) return;
        StartCoroutine(SlideAnimation(card, targetX, targetY));
    }

    private IEnumerator SlideAnimation(GameObject card, float targetX, float targetY)
    {
        float duration = 0.25f; // Adjust the duration as needed
        float elapsedTime = 0f;
        RectTransform cardTransform = card.GetComponent<RectTransform>();
        Vector2 startPosition = cardTransform.anchoredPosition;
        Vector2 targetPosition = new Vector2(targetX, targetY);

        while (elapsedTime < duration)
        {
            cardTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the card reaches the exact target position
        cardTransform.anchoredPosition = targetPosition;
    }

    public void TryPairCards(GameObject draggedCard)
    {
        // Raycast from the position of the dragged card to find the card on top.
        RaycastHit2D[] hits = Physics2D.RaycastAll(draggedCard.transform.position, Vector2.zero, 0f, cardLayer);

        GameObject visuallyOnTopCard = null;
        int highestSiblingIndex = -1;

        foreach (RaycastHit2D hit in hits)
        {
            GameObject hitCard = hit.collider.gameObject;

            // Check if the hit card is in the displayedCards list.
            if (displayedCards.Contains(hitCard))
            {
                // This is a card the user might pair with.
                Debug.Log("Potential card: " + hitCard.GetComponent<CardManagerBuilder>().cardInfo.name);

                // Get the sibling index of the card.
                int siblingIndex = hitCard.transform.GetSiblingIndex();

                // Check if this card is visually on top.
                if (siblingIndex > highestSiblingIndex && hitCard != draggedCard)
                {
                    highestSiblingIndex = siblingIndex;
                    visuallyOnTopCard = hitCard;
                }
            }
        }

        // Now, 'visuallyOnTopCard' contains the card that is visually on top.
        if (visuallyOnTopCard != null)
        {
            PairCards(draggedCard, visuallyOnTopCard);
        }
        else
        {
            Debug.Log("Error: Tried to pair with a card that seems to not exist or contain information.");
            return;
        }
    }

    private void PairCards(GameObject card1, GameObject card2)
    {
        //Debug.Log("Holy shit its: " + card2.GetComponent<CardManagerBuilder>().cardInfo.name);

        if (card1.GetComponent<CardManagerBuilder>().cardInfo.name == card2.GetComponent<CardManagerBuilder>().cardInfo.name)
        {
            Debug.Log("Error: Can't pair two cards with the same name.");
            return;
        }

        // Get the pair values of both cards
        int pairIndex1 = GetPairValue(card1.GetComponent<CardManagerBuilder>().cardInfo.name);
        int pairIndex2 = GetPairValue(card2.GetComponent<CardManagerBuilder>().cardInfo.name);
        string pairValue1 = deckList[pairIndex1].pairing;
        string pairValue2 = deckList[pairIndex2].pairing;

        // Check if one or both cards are unpaired
        if (pairValue1 == "Unpaired" && pairValue2 == "Unpaired")
        {
            // Both cards are unpaired, find an unused pair number and give it to both
            string unusedPairNumber = FindUnusedPairNumber();
            deckList[pairIndex1].pairing = unusedPairNumber;
            deckList[pairIndex2].pairing = unusedPairNumber;
        }
        // Check if they have the same pair value
        else if (pairValue1 == pairValue2)
        {
            // They are already paired, nothing needs to happen
            Debug.Log("Cards are already paired.");
            return;
        }
        else if (pairValue1 == "Unpaired" || pairValue2 == "Unpaired")
        {
            // One card has a pair number, and the other is unpaired
            // Set the unpaired to the same value, set the third card in the original pair to unpaired

            string pairedCardValue;
            int oldCardIndex;

            if (pairValue1 != "Unpaired")
            {
                pairedCardValue = pairValue1;
                oldCardIndex = pairIndex1;
            }
            else
            {
                pairedCardValue = pairValue2;
                oldCardIndex = pairIndex2;
            }
            deckList[pairIndex1].pairing = pairedCardValue;
            deckList[pairIndex2].pairing = pairedCardValue;

            // Find the old card in the original pair and unpair it
            oldCardIndex = FindOldCardInPair(oldCardIndex);
            if (oldCardIndex > -1) deckList[oldCardIndex].pairing = "Unpaired";
        }
        else
        {
            // Both cards have different pair numbers
            // Set the higher pair value to the lower one
            // Then, based on the "UnlikelyPairsSetting," either set both unused cards to unpaired
            // Or set both of them to the higher pair value

            int pairValueInt1 = int.Parse(pairValue1);
            int pairValueInt2 = int.Parse(pairValue2);

            // Find the unused cards from the original pair
            int oldIndex1 = FindOldCardInPair(pairIndex1);
            int oldIndex2 = FindOldCardInPair(pairIndex2);

            string higherPairValue = (pairValueInt1 > pairValueInt2) ? pairValue1 : pairValue2;
            string lowerPairValue = (pairValueInt1 > pairValueInt2) ? pairValue2 : pairValue1;

            deckList[pairIndex1].pairing = lowerPairValue;
            deckList[pairIndex2].pairing = lowerPairValue;

            if (UnlikelySwapSetting)
            {
                if (oldIndex1 > -1) deckList[oldIndex1].pairing = higherPairValue;
                if (oldIndex2 > -1) deckList[oldIndex2].pairing = higherPairValue;
            }
            else
            {
                if (oldIndex1 > -1) deckList[oldIndex1].pairing = "Unpaired";
                if (oldIndex2 > -1) deckList[oldIndex2].pairing = "Unpaired";
            }
        }

        // Once the pair values have been changed, they just need to be resorted and sent to the right locations.
        SortCardPairs();
        UpdateCardLocations();
        ShiftCardsBack(true);
        UpdateCardHierarchy();
    }

    private int GetPairValue(string cardName)
    {
        // I wanted to replace this method with simply a newPairValue variable on the CardManagerBuilder Script, 
        // But this might actually be faster. It's hard to say. I'm tired atm.

        List<int> foundIndexes = new List<int>();
        int index = 0;
        foreach (ScryfallCard card in deckList)
        {
            if (card.name == cardName) foundIndexes.Add(index);
            index++;
        }
        if (foundIndexes.Count == 0)
        {
            // This shouldn't happen, but I'm not sure what to do if it does.
            Debug.Log("An error occurred I thought wasn't possible. Tried to find a card name that isn't in the deck list.");
            return 0; 
        }
        if (foundIndexes.Count == 1) return foundIndexes[0];
        else
        {
            // This will probably be a regular occurance. Cards like basic lands will show up in multiples with the same name. I'll probably have to compre locations.
            // For now, returning the first found card.
            return foundIndexes[0];
        }
    }

    private string FindUnusedPairNumber()
    {
        int maxUnpairedCount = 0;

        // Calculate the maximum count of Unpaired cards
        foreach (ScryfallCard card in deckList)
        {
            if (card.pairing == "Unpaired")
            {
                maxUnpairedCount++;
            }
        }
        int index = deckList.Count - maxUnpairedCount - 1;
        return (int.Parse(deckList[index].pairing) + 1).ToString();
    }

    // Helper method to find the third card in the original pair
    private int FindOldCardInPair(int indexOfStayingCard)
    {
        // When a card is gaining a new pair, it's old one needs to go
        // I'm hoping this only gets called before pairing values of either are changed, so that it should be the card next to this one
        if (indexOfStayingCard > 0)
        {
            if (deckList[indexOfStayingCard].pairing == deckList[indexOfStayingCard - 1].pairing) return indexOfStayingCard - 1;
        }
        if (indexOfStayingCard + 1 < deckList.Count)
        {
            if (deckList[indexOfStayingCard].pairing == deckList[indexOfStayingCard + 1].pairing) return indexOfStayingCard + 1;
        }
        // If it is neither the card before or after this, something that shouldn't have happened happend.
        // It's most likely fixable, by searching through all the cards. But I don't want to implment that right now.
        Debug.Log("This error should not happen. Failed to find a matching adjacent pair.");
        return -1;
    }

    public void UpdateCardLocations()
    {
        // This code is almost exactly the same as the code from DisplayDeckWithDelay() but I do not know how make both call the same method/function
        // This can currently only be called when rearanging cards, not adding or removing them.
        firstUnpairedIndex = 0;
        int maxUnpairedCount = 0;

        // Calculate the maximum count of Unpaired cards
        foreach (ScryfallCard card in deckList)
        {
            if (card.pairing == "Unpaired")
            {
                maxUnpairedCount++;
            }
        }
        // Calculate the height of the Scroll View content
        //int totalCards = Mathf.Max(maxUnpairedCount, (deckList.Count - maxUnpairedCount) / 2);
        //scrollTotalHeight = (totalCards + 2) * cardVerticalSpacing + cardHeight;
        //yCenterOffset = totalCards * cardVerticalSpacing / 2;

        // Set the height and Y offset of the Scroll View content
        //scrollScreen.GetComponent<RectTransform>().sizeDelta = new Vector2(0, scrollTotalHeight);

        // Find the location each card should be at
        for (int i = 0; i < deckList.Count; i++)
        {
            ScryfallCard card = deckList[i];

            // Calculate position based on sorting order and pair value
            float yPos = 0f;
            float xPos = 0f;

            if (card.pairing == "Unpaired")
            {
                xPos = columUnpairedxPos;
                if (firstUnpairedIndex == 0)
                {
                    firstUnpairedIndex = i;
                }
                yPos = (i - firstUnpairedIndex) * cardVerticalSpacing - yCenterOffset;
            }
            else if (i % 2 == 0)
            {
                xPos = colum21xPos;
                yPos = i / 2 * cardVerticalSpacing - yCenterOffset;
            }
            else
            {
                xPos = colum22xPos;
                yPos = (i - 1) / 2 * cardVerticalSpacing - yCenterOffset;
            }

            // Find the card on screen and update its info
            foreach (GameObject displayedCard in displayedCards)
            {
                if (displayedCard.GetComponent<CardManagerBuilder>().cardInfo.name == deckList[i].name) displayedCard.GetComponent<CardManagerBuilder>().UpdateCardPositions(xPos, -yPos);
            }
        }
    }

    public void UpdateCardHierarchy()
    {
        // Sort the displayed cards based on their Y positions in descending order.
        displayedCards.Sort((card1, card2) => card2.GetComponent<CardManagerBuilder>().desiredY.CompareTo(card1.GetComponent<CardManagerBuilder>().desiredY));

        // Set the new hierarchy order.
        for (int i = 0; i < displayedCards.Count; i++)
        {
            displayedCards[i].GetComponent<RectTransform>().SetSiblingIndex(i);
        }
    }

    public void AddCardFromSearch(ScryfallCard newCard)
    {
        Debug.Log("Adding: " + newCard.name);

        // Instantiate the card prefab
        GameObject cardInstance = Instantiate(cardPrefab, scrollScreen.transform);

        // Set the card data and update its display
        newCard.pairing = "Unpaired";
        newCard.category = new List<string>{"Unassigned"};
        cardInstance.GetComponent<CardManagerBuilder>().ApplyCardInfo(newCard);

        // Add the card to the displayed cards list
        displayedCards.Add(cardInstance);
        deckList.Add(newCard);

        // Sort and update the deck display
        SortCardPairs();
        UpdateCardLocations();
        ShiftCardsBack(true);
        UpdateCardHierarchy();

        if (InstantPairingSetting)
        {
            GameObject oldCard = FindLastUnpairedCard(cardInstance.GetComponent<CardManagerBuilder>().cardInfo.name);
            if (oldCard != null)
            {
                PairCards(cardInstance, oldCard);
            }
            else
            {
                Debug.Log("Tried to auto pair a card but I couldn't find one to pair with.");
            }
        }
    }

    private GameObject FindLastUnpairedCard(string excludedCardName = "This should not be a card name 9999")
    {
        GameObject bestCard = null;
        float bestCardPos = 9999;

        Debug.Log(displayedCards.Count);
        // Calculate the maximum count of Unpaired cards
        foreach (GameObject card in displayedCards)
        {
            if (card.GetComponent<CardManagerBuilder>().cardInfo.pairing == "Unpaired" && card.transform.position.y < bestCardPos && excludedCardName != card.GetComponent<CardManagerBuilder>().cardInfo.name)
            {
                bestCard = card;
                bestCardPos = card.transform.position.y;
                Debug.Log(bestCardPos);
            }
        }

        return bestCard;
    }
}