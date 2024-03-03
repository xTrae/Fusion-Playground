using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CardManagerGame : NetworkBehaviour
{

    bool isDraggable = false;
    bool isDragging = false;

    private void Awake()
    {
        isDraggable = true;
    }

    public void StartDrag()
    {
        Debug.Log("Player ID: " + OwnerClientId + " started to drag a card!"); // This doesn't appear to work. It always says player 0, which is likely itself.
        if (!isDraggable)
        {
            //This stops the card from being dragged. Dragged cards don't automatically move, that's coded in the Update function.
            isDragging = false;
            return;
        }

        isDragging = true;
        
    }

    public void EndDrag()
    {
        isDragging = false;
        if (!isDraggable) return;
    }

    private void Update()
    {
        if (isDragging)
        {
            //Debug.Log("Trying to move a card!");
            Vector2 currentPosition;
            Vector2 goalPosition;
            float speed = 15.0f;
            float step;

            goalPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            currentPosition = new Vector2(transform.position.x, transform.position.y);
            step = Time.deltaTime * speed * (currentPosition - goalPosition).magnitude;
            transform.position = Vector2.MoveTowards(currentPosition, goalPosition, step);

        }
    }
}
