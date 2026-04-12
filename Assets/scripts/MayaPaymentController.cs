using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Functions;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

public class MayaPaymentController : MonoBehaviour
{
    [Header("Purchase Buttons")]
    public Button skinButton;
    public Button subscriptionButton;

    // ─────────────────────────────────────────────
    // NEW: Main Store Panel & Result Popups
    // ─────────────────────────────────────────────
    [Header("Main Store & Result Panels")]
    [Tooltip("Drag the main panel you want to disable here")]
    public GameObject mainStorePanel;

    [Space(10)]
    public GameObject processSuccessPopup;
    public Button confirmSuccessButton;

    [Space(10)]
    public GameObject processFailedPopup;
    public Button okayButton;

    // ─────────────────────────────────────────────
    // ACCOUNT LINK
    // ─────────────────────────────────────────────
    [Header("Account")]
    [SerializeField] private AccountManager accountManager;

    [Header("Owned Item Badges")]
    [SerializeField] private GameObject skinOwnedBadge;
    [SerializeField] private GameObject subscriptionOwnedBadge;

    [Header("Cosmetic Popup")]
    public GameObject cosmeticPopup;
    public TMP_Text cosmeticItemNameText;
    public TMP_Text cosmeticPriceText;
    public Button cosmeticConfirmButton;
    public Button cosmeticCancelButton;

    [Header("Subscription Popup")]
    public GameObject subscriptionPopup;
    public TMP_Text subscriptionPlanNameText;
    public TMP_Text subscriptionPriceText;
    public TMP_Text subscriptionBenefitsText;
    public Button subscriptionConfirmButton;
    public Button subscriptionCancelButton;

    // ─────────────────────────────────────────────
    // Internal Firebase references
    // ─────────────────────────────────────────────
    private FirebaseFunctions functions;
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private ListenerRegistration dbListener;

    private string currentItemId;
    private string currentItemName;
    private string pendingOrderId;

    private readonly ConcurrentQueue<System.Action> _mainThreadQueue =
        new ConcurrentQueue<System.Action>();

    private void RunOnMainThread(System.Action action) => _mainThreadQueue.Enqueue(action);

    void Update()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
            action?.Invoke();
    }

    private readonly Dictionary<string, float> localPriceCatalog = new Dictionary<string, float>
    {
        { "player_skin1",  50.00f  },
        { "premium_plan",  200.00f }
    };

    void Start()
    {
        functions = FirebaseFunctions.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        // Hide all popups on startup
        SetPopupActive(cosmeticPopup, false);
        SetPopupActive(subscriptionPopup, false);
        SetPopupActive(processSuccessPopup, false);
        SetPopupActive(processFailedPopup, false);

        // Wire purchase buttons
        skinButton?.onClick.AddListener(() => OpenCosmeticPopup("player_skin1", "Skin (Player)"));
        subscriptionButton?.onClick.AddListener(() => OpenSubscriptionPopup("premium_plan", "Premium Subscription"));

        // Wire cosmetic popup buttons
        cosmeticConfirmButton?.onClick.AddListener(OnConfirmPurchaseClicked);
        cosmeticCancelButton?.onClick.AddListener(() => ClosePopup(cosmeticPopup, cosmeticConfirmButton));

        // Wire subscription popup buttons
        subscriptionConfirmButton?.onClick.AddListener(OnConfirmPurchaseClicked);
        subscriptionCancelButton?.onClick.AddListener(() => ClosePopup(subscriptionPopup, subscriptionConfirmButton));

        // NEW: Wire Result Popup Buttons
        confirmSuccessButton?.onClick.AddListener(CloseResultAndMainPanels);
        okayButton?.onClick.AddListener(CloseResultAndMainPanels);

        if (accountManager != null)
        {
            accountManager.OnDataRefreshed += ApplyOwnedItemStates;
            accountManager.RefreshUserData();
        }
    }

    void OnDestroy()
    {
        StopListener();
        if (accountManager != null)
            accountManager.OnDataRefreshed -= ApplyOwnedItemStates;
    }

    // ─────────────────────────────────────────────
    // NEW: Close Results & Main Panel Logic
    // ─────────────────────────────────────────────
    private void CloseResultAndMainPanels()
    {
        // Close the result popups
        SetPopupActive(processSuccessPopup, false);
        SetPopupActive(processFailedPopup, false);

        // Disable the main panel that was dragged in
        SetPopupActive(mainStorePanel, false);
    }

    // ─────────────────────────────────────────────
    // OPEN POPUPS
    // ─────────────────────────────────────────────

    private void OpenCosmeticPopup(string itemId, string itemName)
    {
        if (accountManager != null && accountManager.OwnsItem(itemId)) return;

        currentItemId = itemId;
        currentItemName = itemName;

        if (cosmeticItemNameText != null) cosmeticItemNameText.text = itemName;
        if (cosmeticPriceText != null) cosmeticPriceText.text = FormatPrice(itemId);

        SetPopupActive(subscriptionPopup, false);
        SetPopupActive(cosmeticPopup, true);
        SetButtonInteractable(cosmeticConfirmButton, true);
    }

    private void OpenSubscriptionPopup(string itemId, string itemName)
    {
        if (accountManager != null && accountManager.IsPremium) return;

        currentItemId = itemId;
        currentItemName = itemName;

        if (subscriptionPlanNameText != null) subscriptionPlanNameText.text = itemName;
        if (subscriptionPriceText != null) subscriptionPriceText.text = FormatPrice(itemId);

        if (subscriptionBenefitsText != null)
            subscriptionBenefitsText.text =
                "✔  Ad-free experience\n" +
                "✔  Exclusive skins & content\n" +
                "✔  Priority support";

        SetPopupActive(cosmeticPopup, false);
        SetPopupActive(subscriptionPopup, true);
        SetButtonInteractable(subscriptionConfirmButton, true);
    }

    private void ApplyOwnedItemStates()
    {
        if (accountManager == null) return;

        bool ownsSkin1 = accountManager.OwnsItem("player_skin1");
        bool ownsPremium = accountManager.IsPremium;

        SetButtonInteractable(skinButton, !ownsSkin1);
        if (skinOwnedBadge != null) skinOwnedBadge.SetActive(ownsSkin1);

        SetButtonInteractable(subscriptionButton, !ownsPremium);
        if (subscriptionOwnedBadge != null) subscriptionOwnedBadge.SetActive(ownsPremium);
    }

    private void ClosePopup(GameObject popup, Button confirmBtn)
    {
        SetPopupActive(popup, false);
        SetButtonInteractable(confirmBtn, true);
        StopListener();
        pendingOrderId = null;
    }

    private async void OnConfirmPurchaseClicked()
    {
        if (auth.CurrentUser == null) return;

        SetButtonInteractable(cosmeticConfirmButton, false);
        SetButtonInteractable(subscriptionConfirmButton, false);

        await RequestMayaCheckout(currentItemId, currentItemName);
    }

    private async Task RequestMayaCheckout(string itemId, string itemName)
    {
        try
        {
            string idToken = await auth.CurrentUser.TokenAsync(forceRefresh: true);
            if (string.IsNullOrEmpty(idToken))
            {
                ReEnableButtons();
                return;
            }

            var createCheckoutFunc = functions.GetHttpsCallable("createMayaCheckout");
            var dataToSend = new Dictionary<string, string>
            {
                { "itemId",   itemId   },
                { "itemName", itemName },
                { "idToken",  idToken  }
            };

            var result = await createCheckoutFunc.CallAsync(dataToSend);
            var data = result.Data as System.Collections.IDictionary;

            if (data == null || data.Contains("error") || !data.Contains("checkoutUrl"))
            {
                ReEnableButtons();
                return;
            }

            string checkoutUrl = data["checkoutUrl"].ToString();
            string orderId = data["orderId"].ToString();

            UpdatePendingTransactionInDatabase(orderId, itemId, itemName);

            pendingOrderId = orderId;
            ListenForPaymentSuccess(orderId, itemId, itemName);
            Application.OpenURL(checkoutUrl);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Payment Error: " + e.Message);
            ReEnableButtons();
        }
    }

    private void UpdatePendingTransactionInDatabase(string orderId, string itemId, string itemName)
    {
        if (auth.CurrentUser == null || string.IsNullOrEmpty(orderId)) return;

        db.Collection("payments").Document(orderId).SetAsync(new Dictionary<string, object>
        {
            { "clientItemId",   itemId                  },
            { "clientItemName", itemName                },
            { "clientUserId",   auth.CurrentUser.UserId }
        }, SetOptions.MergeAll);
    }

    private void ListenForPaymentSuccess(string orderId, string itemId, string itemName)
    {
        StopListener();

        dbListener = db.Collection("payments").Document(orderId).Listen(snapshot =>
        {
            if (!snapshot.Exists || !snapshot.ContainsField("status")) return;

            string status = snapshot.GetValue<string>("status");

            if (status == "PAID")
            {
                RunOnMainThread(() =>
                {
                    // Close whichever purchase popup is active
                    if (cosmeticPopup != null && cosmeticPopup.activeSelf)
                        ClosePopup(cosmeticPopup, cosmeticConfirmButton);
                    else if (subscriptionPopup != null && subscriptionPopup.activeSelf)
                        ClosePopup(subscriptionPopup, subscriptionConfirmButton);

                    ReEnableButtons();

                    // NEW: Show Success Popup
                    SetPopupActive(processSuccessPopup, true);

                    accountManager?.RefreshUserData();
                });
            }
            else if (status == "FAILED" || status == "CANCELLED")
            {
                RunOnMainThread(() =>
                {
                    // NEW: Close the purchase popup on failure too
                    if (cosmeticPopup != null && cosmeticPopup.activeSelf)
                        ClosePopup(cosmeticPopup, cosmeticConfirmButton);
                    else if (subscriptionPopup != null && subscriptionPopup.activeSelf)
                        ClosePopup(subscriptionPopup, subscriptionConfirmButton);

                    ReEnableButtons();
                    StopListener();

                    // NEW: Show Failed Popup
                    SetPopupActive(processFailedPopup, true);
                });
            }
        });
    }

    private string FormatPrice(string itemId) =>
        localPriceCatalog.TryGetValue(itemId, out float price) ? $"PHP {price:F2}" : "PHP --";

    private void ReEnableButtons()
    {
        SetButtonInteractable(cosmeticConfirmButton, true);
        SetButtonInteractable(subscriptionConfirmButton, true);
    }

    private void StopListener()
    {
        dbListener?.Stop();
        dbListener = null;
    }

    private static void SetPopupActive(GameObject popup, bool active)
    {
        if (popup != null) popup.SetActive(active);
    }

    private static void SetButtonInteractable(Button btn, bool interactable)
    {
        if (btn != null) btn.interactable = interactable;
    }

    private bool _verifyInFlight = false;

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && !string.IsNullOrEmpty(pendingOrderId) && !_verifyInFlight)
        {
            _verifyInFlight = true;
            _ = PollForPaymentResult(pendingOrderId);
        }
    }

    private async Task PollForPaymentResult(string orderId)
    {
        const int maxAttempts = 8;
        const int delaySeconds = 4;

        try
        {
            if (auth == null || auth.CurrentUser == null) return;

            string idToken = await auth.CurrentUser.TokenAsync(forceRefresh: true);
            if (string.IsNullOrEmpty(idToken)) return;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (string.IsNullOrEmpty(pendingOrderId)) return;

                var verifyFunc = functions.GetHttpsCallable("verifyMayaPayment");
                var result = await verifyFunc.CallAsync(new Dictionary<string, string>
                {
                    { "orderId", orderId },
                    { "idToken", idToken }
                });

                var data = result.Data as System.Collections.IDictionary;

                if (data != null && data.Contains("status"))
                {
                    string status = data["status"].ToString();

                    if (status == "PAID")
                    {
                        if (cosmeticPopup != null && cosmeticPopup.activeSelf)
                            ClosePopup(cosmeticPopup, cosmeticConfirmButton);
                        else if (subscriptionPopup != null && subscriptionPopup.activeSelf)
                            ClosePopup(subscriptionPopup, subscriptionConfirmButton);

                        ReEnableButtons();

                        // NEW: Show Success Popup
                        SetPopupActive(processSuccessPopup, true);

                        accountManager?.RefreshUserData();
                        pendingOrderId = null;
                        return;
                    }
                    else if (status == "FAILED" || status == "CANCELLED")
                    {
                        // NEW: Close the purchase popup on failure too
                        if (cosmeticPopup != null && cosmeticPopup.activeSelf)
                            ClosePopup(cosmeticPopup, cosmeticConfirmButton);
                        else if (subscriptionPopup != null && subscriptionPopup.activeSelf)
                            ClosePopup(subscriptionPopup, subscriptionConfirmButton);

                        ReEnableButtons();
                        StopListener();

                        // NEW: Show Failed Popup
                        SetPopupActive(processFailedPopup, true);

                        pendingOrderId = null;
                        return;
                    }
                }
                else if (data != null && data.Contains("error"))
                {
                    return;
                }

                if (attempt < maxAttempts)
                    await Task.Delay(delaySeconds * 1000);
            }
        }
        finally
        {
            _verifyInFlight = false;
        }
    }
}