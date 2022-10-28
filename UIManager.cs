using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [Header("Managers")]
    public WorldManager WM;
    public PlayerManager PM;
    public ResourceManager RM;

    [Header("UI Elements")]
    public GameObject canvas;

    public GameObject claimButton;
    public Sprite claimButtonUnselected;
    public Sprite claimButtonSelected;

    public GameObject mineButton;
    public Sprite mineButtonUnselected;
    public Sprite mineButtonSelected;

    public GameObject farmButton;
    public Sprite farmButtonUnselected;
    public Sprite farmButtonSelected;

    public GameObject repulsionTowerButton;
    public Sprite repulsionTowerButtonUnselected;
    public Sprite repulsionTowerButtonSelected;

    public GameObject cancelButton;
    public GameObject cancelButtonMask;

    public GameObject resourcePanel;

    [Header("UI Elements That Animate")]
    [SerializeField]
    GameObject[] allUIElementsToAnimateIn = new GameObject[0];
    Vector2[] allUIElementPositions;// will be created in start using all the UI Elements that should animate

    [Header("Resources Stuff")]
    public GameObject[] resourceIcons = new GameObject[4];
    public TextMeshProUGUI[] resourceTexts = new TextMeshProUGUI[4];
    bool[] resourceTextChanging = new bool[4];

    bool UIMoveIn;

    void Start()
    {
        SetUIElementsOffscreen();
    }

    void Update()
    {
        if(PM.started && UIMoveIn == false)
        {
            UIMoveIn = true;
            //move UI onto the screen as soon as the game has started
            AnimateUIElementsMovingIn();
        }
        UpdateResourceText();
    }
    
    //Initially sets UI elements offscreen, so that at the start of the game they can smoothly animate in.
    void SetUIElementsOffscreen()
    {
        //log the correct final position of all the ui elements so that they can be moved back to that position
        allUIElementPositions = new Vector2[allUIElementsToAnimateIn.Length];
        //loop through all the UI
        for (int i = 0; i < allUIElementsToAnimateIn.Length; i++)
        {
            allUIElementPositions[i] = allUIElementsToAnimateIn[i].GetComponent<RectTransform>().anchoredPosition;
        }
        //adjust the position offscreen for all elements that should come from the top
        for(int i = 0; i <= 3; i += 1)
        {
            allUIElementsToAnimateIn[i].GetComponent<RectTransform>().anchoredPosition += Vector2.up * (480);
        }
        //adjust the position offscreen for all elements that should come from the right
        for(int i = 4; i <= 5; i += 1)
        {
            //adjust it further offscreen based on how far on screen it is for the final position
            allUIElementsToAnimateIn[i].GetComponent<RectTransform>().anchoredPosition -= Vector2.right * (2 * allUIElementPositions[i].x);
        }
    }

    //Activated the first frame in which the game has started, animates all game elements to their correct starting location on the screen.
    void AnimateUIElementsMovingIn()
    {
        for (int i = 0; i < allUIElementsToAnimateIn.Length; i++)
        {
            allUIElementsToAnimateIn[i].GetComponent<RectTransform>().DOAnchorPos(allUIElementPositions[i],1.2f).SetEase(Ease.OutBack).SetDelay(UnityEngine.Random.Range(0.0f,0.2f));
        }
    }

    //Update the resource UI text with the current value of the resource from the Resource Manager
    public void UpdateResourceText()
    {
        resourceTexts[0].text = RM.resourceAvailableCounts[(int)ResourceType.energy].ToString();
        resourceTexts[1].text = RM.resourceAvailableCounts[(int)ResourceType.food].ToString();
        resourceTexts[2].text = RM.resourceAvailableCounts[(int)ResourceType.ether].ToString();
        resourceTexts[3].text = RM.resourceAvailableCounts[(int)ResourceType.flux].ToString();
    }

    //Interface with the player managers build mode state machine when each type of button is clicked to allow the player to build the given building
    public void CancelButtonClicked()
    {
        PM.BuildModeStop();
    }

    public void ClaimButtonClicked()
    {
        if (PM.started)
        {
            PM.BuildModeSetUp(BuildType.claim);
        }
    }
    public void MineButtonClicked()
    {
        if (PM.started)
        {
            PM.BuildModeSetUp(BuildType.mine);
        }
    }
    public void FarmButtonClicked()
    {
        if (PM.started)
        {
            PM.BuildModeSetUp(BuildType.farm);
        }
    }
     public void RepulsionTowerButtonClicked()
    {
        if (PM.started)
        {
            PM.BuildModeSetUp(BuildType.repulsionTower);
        }
    }

    //Used by generalButtonActiveAnimation to check if the button should stop animating, as it is no longer selected, and therefore returns false.
    bool claimButtonIsDone()
    {
        return (PM.buildMode != BuildType.claim);
    }
    bool mineButtonIsDone()
    {
        return (PM.buildMode != BuildType.mine);
    }
    bool farmButtonIsDone()
    {
        return (PM.buildMode != BuildType.farm);
    }
    bool repulsionTowerIsDone()
    {
        return (PM.buildMode != BuildType.repulsionTower);
    }

    //When a type of building is selcted, the button corresponding to that building should animate showing that it is selected
    public void activateAnimation(BuildType type)
    {
        if (type == BuildType.claim)
        {
            GeneralButtonActiveAnimation(claimButton, claimButtonSelected, claimButtonUnselected, claimButtonIsDone);
        }
        else if (type == BuildType.mine)
        {
            GeneralButtonActiveAnimation(mineButton, mineButtonSelected, mineButtonUnselected, mineButtonIsDone);
        }
        else if (type == BuildType.farm)
        {
            GeneralButtonActiveAnimation(farmButton, farmButtonSelected, farmButtonUnselected, farmButtonIsDone);
        }
        else if (type == BuildType.repulsionTower)
        {
            GeneralButtonActiveAnimation(repulsionTowerButton, repulsionTowerButtonSelected, repulsionTowerButtonUnselected, repulsionTowerIsDone);
        }
    }

    //Util Func used to tween the mask that makes the cancel button and others fade in and out
    public async Task ScaleMask(GameObject mask, float duration, float startSize, float finalSize)
    {
        // pause any previous scales of the mask and start this new one
        DOTween.Pause(mask.transform);
        mask.SetActive(true);
        Tween maskTween = mask.GetComponent<RectTransform>().DOSizeDelta(Vector3.one * finalSize, duration).SetEase(Ease.InOutSine);
        await maskTween.AsyncWaitForCompletion();
        mask.SetActive(startSize < finalSize);

    }

    //A continuous task that animates the button, and stops when the given isDone fuction becomes true.
    public async Task GeneralButtonActiveAnimation(GameObject button, Sprite buttonSelectedSprite, Sprite buttonUnselectedSprite, Func<bool> isDone)
    {
        //SETUP animation
        float startingSize = button.GetComponent<RectTransform>().sizeDelta.x;
        button.GetComponent<Image>().sprite = buttonSelectedSprite;

        await new WaitForFixedUpdate();
        await new WaitForFixedUpdate();

        //make the cancel button visible, so that the build can be cancelled
        ScaleMask(cancelButtonMask, 0.5f, 0, 100);

        //Tween that makes the button pulse in size! This is looping!
        Tween buttonScaleTween = button.GetComponent<RectTransform>().DOSizeDelta(-Vector3.one * 20, 0.8f).SetRelative().SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        
        
        //LOOP until this build mode is no longer the current state
        while (isDone() == false)
        {
            await new WaitForFixedUpdate();
        }

        //EXIT animation
        button.GetComponent<Image>().sprite = buttonUnselectedSprite;
        ScaleMask(cancelButtonMask, 0.5f, 100, 0);//do this immediately so that if another is increasing the mask size it will happen after this and pause it correctly
        buttonScaleTween.Pause();
        Tween returnScale = button.GetComponent<RectTransform>().DOSizeDelta(Vector2.one * startingSize, 0.2f).SetEase(Ease.OutQuad);

        //Wait for buttton to scale back to starting size from wherever it was cancelled at
        await returnScale.AsyncWaitForCompletion();
        //reset to startingsize just incase tween is slightly off
        button.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, startingSize);
        button.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, startingSize);
    }

    //Called when a build requires more of a resource than the player has, and they try to build
    public void NotEnoughResource(ResourceType resource)
    {
        StartCoroutine(PulseRedResourceText(resourceIcons[(int)resource], resourceTexts[(int)resource]));
    }

    //Animates a quick red pulse to catch the players attention, and show them that they do not have enough.
    //Done without dotween.
    public IEnumerator PulseRedResourceText(GameObject icon, TextMeshProUGUI text)
    {
        text.color = Color.red;
        icon.transform.localScale = Vector3.one;
        float t = 0.0f;
        while (t <= 0.5f)
        {
            float y = 0.3f * Mathf.Sin(t * Mathf.PI * 2) + 1;
            icon.transform.localScale = new Vector3(y, y, y);
            t += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        icon.transform.localScale = Vector3.one;
        text.color = Color.white;
    }
}
