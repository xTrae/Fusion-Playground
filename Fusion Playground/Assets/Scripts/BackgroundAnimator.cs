using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundAnimator : MonoBehaviour
{
    [SerializeField] private Sprite Image1;
    [SerializeField] private Sprite Image2;
    [SerializeField] private Sprite Image3;
    [SerializeField] private Sprite Image4;
    [SerializeField] private GameObject backBackground;
    private Sprite[] spriteArray;

    private void Start()
    {
        spriteArray = new Sprite[] {Image1, Image2, Image3, Image4};
        CallCoroutine();
        // Call the FadeImage Coroutine randomly every 20-40 seconds.
        InvokeRepeating("CallCoroutine", Random.Range(20f, 40f), Random.Range(20f, 40f));
    }

    private void CallCoroutine()
    {
        StartCoroutine(FadeImage());
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            //Debug.Log("You pressed a thing!");
            StartCoroutine(FadeImage());
        }
    }

    IEnumerator FadeImage()
    {
        // fade from opaque to transparent
        Image currentSprite = GetComponent<Image>();
        int excludedNumber = int.Parse(currentSprite.sprite.name[^1].ToString());
        int randomNumber;
        do
        {
            randomNumber = Random.Range(1, 5);
        } while (randomNumber == excludedNumber);
        //Debug.Log("Random Number (excluding " + excludedNumber + "): " + randomNumber);
        backBackground.GetComponent<Image>().sprite = spriteArray[randomNumber - 1];

        // loop over 2 second backwards
        for (float i = 2; i >= 0; i -= Time.deltaTime)
        {
            // set color with i as alpha
            currentSprite.color = new Color(1, 1, 1, i);
            yield return null;
        }
        GetComponent<Image>().sprite = spriteArray[randomNumber - 1];
    }
}
