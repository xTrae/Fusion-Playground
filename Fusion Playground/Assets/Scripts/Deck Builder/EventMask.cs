using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventMask : MonoBehaviour, ICanvasRaycastFilter
{
    // This script is not working. It's intented to allow users to scroll freely, instead of cards blocking the line of sight.
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        // Always allow raycasts to pass through
        return true;
    }
}