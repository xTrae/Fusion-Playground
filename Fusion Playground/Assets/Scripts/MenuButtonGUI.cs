using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuButtonGUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button deckButton;
    [SerializeField] private Button gameDesignButton;
    [SerializeField] private Button gameCreatorButton;
    [SerializeField] private Button cardCreatorButton;
    [SerializeField] private Button importCardsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private bool isGameDesignOpen = false;

    private void Awake()
    {
        Debug.developerConsoleVisible = true;
        Debug.Log("Button GUI starting up!");

        playButton.onClick.AddListener(PlayButtonPress);
        deckButton.onClick.AddListener(DeckButtonPress);
        gameDesignButton.onClick.AddListener(GameDesignButtonPress);
        gameCreatorButton.onClick.AddListener(gameCreatorButtonPress);
        cardCreatorButton.onClick.AddListener(cardCreatorButtonPress);
        importCardsButton.onClick.AddListener(importCardsButtonPress);
        settingsButton.onClick.AddListener(SettingsButtonPress);
        quitButton.onClick.AddListener(QuitButtonPress);
    }

    private void PlayButtonPress()
    {
        Debug.Log("You pressed Play!");
        SceneManager.LoadScene("GameLobbyScene");
    }

    private void DeckButtonPress()
    {
        Debug.Log("You pressed Deck Building!");
        SceneManager.LoadScene("DeckBuilderScene");
    }

    private void GameDesignButtonPress()
    {
        Debug.Log("You pressed Custom Game Builder!");
        // do stuff
        if (isGameDesignOpen)
        {
            isGameDesignOpen = false;
            gameCreatorButton.gameObject.SetActive(false);
            cardCreatorButton.gameObject.SetActive(false);
            importCardsButton.gameObject.SetActive(false);
        }
        else
        {
            isGameDesignOpen = true;
            gameCreatorButton.gameObject.SetActive(true);
            cardCreatorButton.gameObject.SetActive(true);
            importCardsButton.gameObject.SetActive(true);
        }
    }

    private void gameCreatorButtonPress()
    {
        Debug.Log("You pressed Game Creator!");
        // do stuff
        Debug.Log("This is not implemented yet.");
    }

    private void cardCreatorButtonPress()
    {
        Debug.Log("You pressed Card Creator!");
        // do stuff
        SceneManager.LoadScene("CardBuilderScene");
    }

    private void importCardsButtonPress()
    {
        Debug.Log("You pressed Import Cards!");
        // do stuff
        Debug.Log("This is not implemented yet.");
    }

    private void SettingsButtonPress()
    {
        Debug.Log("You pressed Settings!");
        // do stuff
        Debug.Log("This is not implemented yet.");
    }

    private void QuitButtonPress()
    {
        Debug.Log("You Quit!");
        Application.Quit();
    }
}
