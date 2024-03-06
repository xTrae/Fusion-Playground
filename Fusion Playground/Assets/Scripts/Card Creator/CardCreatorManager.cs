using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CardCreatorManager : MonoBehaviour
{
    [SerializeField] private Button backButton;

    void Awake()
    {
        backButton.onClick.AddListener(PlayButtonPress);
    }

    private void PlayButtonPress()
    {
        Debug.Log("You pressed the Back Button");
        SceneManager.LoadScene("MainMenuScene");
    }
}
