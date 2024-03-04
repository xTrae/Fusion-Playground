using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuButtonGUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button deckButton;
    [SerializeField] private Button gameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        Debug.developerConsoleVisible = true;
        Debug.Log("Button GUI starting up!");

        playButton.onClick.AddListener(PlayButtonPress);
        deckButton.onClick.AddListener(DeckButtonPress);
        gameButton.onClick.AddListener(GameButtonPress);
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

    private void GameButtonPress()
    {
        Debug.Log("You pressed Custom Game Builder!");
        // do stuff
    }

    private void SettingsButtonPress()
    {
        Debug.Log("You pressed Settings!");
        // do stuff
    }

    private void QuitButtonPress()
    {
        Debug.Log("You Quit!");
        Application.Quit();

    }
}
