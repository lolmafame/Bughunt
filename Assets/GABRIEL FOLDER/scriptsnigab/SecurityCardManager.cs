using UnityEngine;
using TMPro;

public class SecurityCardManager : MonoBehaviour
{
    public static SecurityCardManager Instance;

    [Header("Card Settings")]
    public int totalCards = 4;
    private int collectedCards = 0;

    [Header("UI Texts (drag from Card Canvas)")]
    public TextMeshProUGUI numbersOfCardsText;    // "Numbers of cards collected"
    public TextMeshProUGUI collectAllCardsText;    // "Collect all Cards notif"
    public TextMeshProUGUI doorsOpenedText;        // "Doors Opened notif"
    public TextMeshProUGUI cardCollectedText;      // "Card Collected"

    [Header("Door")]
    public GameObject doorObject;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateCardCountText();

        if (collectAllCardsText != null) collectAllCardsText.gameObject.SetActive(false);
        if (doorsOpenedText != null) doorsOpenedText.gameObject.SetActive(false);
        if (cardCollectedText != null) cardCollectedText.gameObject.SetActive(false);
    }

    public void CollectCard()
    {
        collectedCards++;
        Debug.Log($"Card collected! {collectedCards}/{totalCards}");
        UpdateCardCountText();
        ShowCardCollectedNotif();

        if (collectedCards >= totalCards)
        {
            ShowDoorsOpenedNotif();
            RemoveDoor();
        }
    }

    // Called when player enters Consolebrah trigger
    public void OnPlayerEnterConsole()
    {
        if (collectedCards >= totalCards)
        {
            if (collectAllCardsText != null) collectAllCardsText.gameObject.SetActive(false);
            if (doorsOpenedText != null)
            {
                doorsOpenedText.text = "Access Granted!";
                doorsOpenedText.gameObject.SetActive(true);
            }
        }
        else
        {
            int remaining = totalCards - collectedCards;
            if (collectAllCardsText != null)
            {
                collectAllCardsText.text = $"Access Denied! Collect {remaining} more card(s). ({collectedCards}/{totalCards})";
                collectAllCardsText.gameObject.SetActive(true);
            }
            if (doorsOpenedText != null) doorsOpenedText.gameObject.SetActive(false);
        }
    }

    // Called when player exits Consolebrah trigger
    public void OnPlayerExitConsole()
    {
        if (collectAllCardsText != null) collectAllCardsText.gameObject.SetActive(false);
        if (doorsOpenedText != null) doorsOpenedText.gameObject.SetActive(false);
    }

    private void UpdateCardCountText()
    {
        if (numbersOfCardsText != null)
            numbersOfCardsText.text = $"{collectedCards}/{totalCards} Security Cards";
    }

    private void ShowCardCollectedNotif()
    {
        if (cardCollectedText != null)
        {
            cardCollectedText.text = $"Card Collected! ({collectedCards}/{totalCards})";
            cardCollectedText.gameObject.SetActive(true);

            CancelInvoke(nameof(HideCardCollectedNotif));
            Invoke(nameof(HideCardCollectedNotif), 2f);
        }
    }

    private void HideCardCollectedNotif()
    {
        if (cardCollectedText != null)
            cardCollectedText.gameObject.SetActive(false);
    }

    private void ShowDoorsOpenedNotif()
    {
        if (doorsOpenedText != null)
        {
            doorsOpenedText.text = "All cards collected! Doors opened!";
            doorsOpenedText.gameObject.SetActive(true);
        }
        if (collectAllCardsText != null) collectAllCardsText.gameObject.SetActive(false);
    }

    private void RemoveDoor()
    {
        if (doorObject != null)
        {
            doorObject.SetActive(false);
            Debug.Log("Door removed!");
        }
    }
}