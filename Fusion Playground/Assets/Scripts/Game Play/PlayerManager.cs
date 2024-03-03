using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// I would love to have players login, but not really need to save anything on the cloud. You have profile pictures and stuff just send over.
// Better yet, each player picks a Magic: The Gathering card and that can be your pfp. Even for non-magic players. There could be a default 12
// or something to pick from. A big list of 100 if you don't like those (a list picked by us like player avatars), and then you could also just
// type in a card name. Non-magic players would just see a list of default profiles and not think too much about it.
// The exact image would be a circle cut out of the card art. Which would not be as simple as looking for a specific area of the card,
// I'll need to download the art crop, and do some math to find what the biggest circle is, and take that.
// But preferably, these pictures would not actually be sent over the internet themselves. Just the card names.
// Anyway though, these profiles need to be deplays in the lobby menu and within games.

public class PlayerManager : NetworkBehaviour
{
    [SerializeField] private GameObject newCardObject;
    GameObject Canvas;

    private void Start()
    {
        Debug.Log("Player: " + OwnerClientId + " has connected.");
        Canvas = GameObject.Find("Main Canvas");
    }

    private void Update()
    {
        if (!IsOwner) return; //Each player is summoned into each player's scene. You can interact with their player piece, like you could in an FPS.

        if (Input.GetKeyDown(KeyCode.C))
        {
            //Debug.Log("You pressed a thing!");

            CreateCardServerRpc();
            
        }
    }

    [ServerRpc] //A Server Rpc never runs on a client, only on the server, "Whenever you get a chance." The Host's computer runs all of these commands.
    private void CreateCardServerRpc() //Variable parameters must be data-types, not refference types.
    {
        TestClientRpc(); //Calling a Server Rpc that calls a Client Rpc, is a client telling each player to do something.

        if (Canvas != null)
        {
            GameObject newCard = Instantiate(newCardObject);
            newCard.GetComponent<NetworkObject>().Spawn(true); //Spawning objetcs however is different. Where a Debug.Log message would only show up on the host,
                                                               //NetworkObjects are spawned on each Client. And can only be spawned by a Server (host).
                                                               //These objects however, work just like players. Someone owns them, and theoretically, only one one might control them.
            newCard.transform.SetParent(Canvas.transform, false);
        }
        else
        {
            Debug.LogError("Canvas is null. Make sure it's assigned in the Inspector.");
        }
    }


    [ClientRpc] // A Client can not call a Client Rpc. A Server Rpc must call it. By default, *all* clients execute this function.
    private void TestClientRpc()
    {
        Debug.Log("Player " + OwnerClientId + ": Client Ping!"); // This appears to be working.
    }
}
