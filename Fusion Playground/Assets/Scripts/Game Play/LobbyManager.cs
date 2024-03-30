using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Wasnt sure a better place to write down some ideas for the game scene.
// Similar to the deck builder, a series of settings buttons may prove useful. Like turning off cards being draggable in opponent's zones,
// or always being able to drag cards you own (starting in your deck). Players able to draw cards, or play cards outsie of their turn, etc.
// And these settings can be changed by players, can be set to certain defaults for each player in the lobby creation menu, or locked.
// I want there to be a game creator, where you can change what zones people have, what tokens are needed, like player tokens and such.
// But I could definetly have some really basic ones, like each player has the MTG field. Each their own play zone, deck, discard, etc.
// And then simpler ones, where it's one shared board and deck.
// Heck, some of those could be turned into settings, simply "Deck: Shared/Individual" and "Play Field: Shared/Individual"
// Where it'll calculate these zones, their sizes, and placement dynamically without needing to create your own preset.

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button listLobbiesButton;
    [SerializeField] private Button joinLobbiesButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private TMP_InputField inputBox;
    private Lobby currentLobby;
    private float lobbyBumpTimer;
    private float lobbyMaxBumpTime = 25.0f;
    private float lastUpdateTimer;
    private float lobbyMaxUpdateTime = 2.0f;
    private string playerName = "Player_";
    string playerId;

    private void Awake()
    {
        Debug.Log("Lobby GUI starting up!");

        createLobbyButton.onClick.AddListener(CreateLobby);
        listLobbiesButton.onClick.AddListener(ListLobbies);
        joinLobbiesButton.onClick.AddListener(JoinLobbyWithCode);
        returnButton.onClick.AddListener(ReturnButtonPress);

        lastUpdateTimer = Time.time;
        lobbyBumpTimer = Time.time;
    }

    private async void Start()
    {
        InitializationOptions options = new InitializationOptions();
        playerName = "Player_" + Random.Range(1000, 9999);
        options.SetProfile(playerName);
        await UnityServices.InitializeAsync(options); //Asynchronous means it will/won't? continue doing other things, while this processes.
                                                    //Since it has to wait for a responce from over the internet.
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Player signed in: " + AuthenticationService.Instance.PlayerId + " as: " + playerName);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Players must sign in anonymously, or with verified accounts like Steam.
    }

    private void Update()
    {
        BumpLobbyTimer();
        // It would be nice to regularly check if the number of players changed, and update each player in the lobby who's all there.
        PullForLobbyUpdates();
    }

    private async void BumpLobbyTimer()
    {
        // It appears the non-host players are attempting to bump the lobby, and getting a 403 error.
        // Also, players within the lobby arent able to see new player added with ListPlayers()
        if (currentLobby != null)
        {
            lobbyBumpTimer -= Time.deltaTime;
            if (lobbyBumpTimer < 0)
            {
                lobbyBumpTimer = lobbyMaxBumpTime;
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
                Debug.Log("Lobby Bump!");
                ListPlayers(currentLobby);
            }
        }
    }

    private async void PullForLobbyUpdates()
    {
        if (currentLobby != null)
        {
            if ((Time.time - lastUpdateTimer) > lobbyMaxUpdateTime)
            {
                lastUpdateTimer = Time.time;
                //await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
                Debug.Log("Checking for Updates? This doesn't do anything right now.");
            }
        }
    }

    private async void CreateLobby()
    {
        try //Lobbies and connecting over the internet are prone to failure. You must be prepaired.
        {
            string lobbyName = "My Lobby"; // Obviously something to be changable by players later
            int lobbyMaxPlayers = 4; // Obviously something to be changable by players later
            CreateLobbyOptions newLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false, //Setting this to true would make it enter-by-code only. It won't show up in searches.
                Player = GetNewPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "Deez Nuts", DataObject.IndexOptions.S1) }
                    // I want to add other pices of information here, but there's rules to follow. There's only so many places for data.
                }
            };
            Lobby newLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, lobbyMaxPlayers, newLobbyOptions);
            currentLobby = newLobby; currentLobby = newLobby;
            Debug.Log("Lobby started! '" + newLobby.Name + "' Max players: " + newLobby.MaxPlayers + " Lobby Id: " + newLobby.Id + " Lobby Code: " + newLobby.LobbyCode);
            ListPlayers(currentLobby);
        }
        catch (LobbyServiceException errorMessage)
        {
            Debug.Log(errorMessage);
        }
    }

    private async void ListLobbies()
    {
        try //Lobbies and connecting over the internet are prone to failure. You must be prepaired.
        {
            QueryLobbiesOptions lobbyOptions = new QueryLobbiesOptions
            {
                Count = 10, // How many results you want to get back at a time.
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT), // GT means "Greater Than". There are many expression opions.
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "Deez Nuts", QueryFilter.OpOptions.EQ) // EQ means "Equal to"
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                    // You can sort the order that lobbies show up. "false vs true" means "descending vs ascending". Atm, this is oldest -> newest.
                }
            };

            QueryResponse lobbiesResponse = await Lobbies.Instance.QueryLobbiesAsync();
            Debug.Log("Lobbies Found: " + lobbiesResponse.Results.Count);
            foreach (Lobby foundLobby in lobbiesResponse.Results)
            {
                Debug.Log("Lobby: '" + foundLobby.Name + "' Max Players: " + foundLobby.MaxPlayers);
            }
        }
        catch (LobbyServiceException errorMessage)
        {
            Debug.Log(errorMessage);
        }
    }

    private async void JoinLobbyWithCode()
    {
        string lobbyCode = inputBox.text;
        if (lobbyCode == null)
        {
            Debug.Log("That's an empty box numb nuts! Where's the code?");
        }
        else if (lobbyCode.Length != 6)
        {
            Debug.Log("A lobby code should only be 6 characters long!");
        }
        else
        {
            try //Lobbies and connecting over the internet are prone to failure. You must be prepaired to get errors.
            {
                JoinLobbyByCodeOptions joinLobbyOptions = new JoinLobbyByCodeOptions 
                { 
                    Player = GetNewPlayer()
                };
                Debug.Log("Searching for lobby: " + lobbyCode);
                // The following both joins the lobby, and returns that lobby object, setting it to a variable.
                currentLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyOptions); 
                Debug.Log("Connected!");
                ListPlayers(currentLobby);
            }
            catch (LobbyServiceException errorMessage)
            {
                Debug.Log(errorMessage);
            }
        }
    }

    private void ListPlayers(Lobby lobby)
    {
        Debug.Log("Name: " + lobby.Name + " Game Mode: " + lobby.Data["GameMode"].Value + " Players: ");
        foreach (Player foundPlayer in lobby.Players)
        {
            Debug.Log(foundPlayer.Id + " as: " + foundPlayer.Data["PlayerName"].Value);
        }
    }

    private Player GetNewPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName)}
            }
        };
    }

    private void ReturnButtonPress()
    {
        Debug.Log("You pressed Play!");
        SceneManager.LoadScene("MainMenuScene");
    }
}