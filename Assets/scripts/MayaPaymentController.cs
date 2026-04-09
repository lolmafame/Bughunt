using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Functions;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using System.Collections.Generic;

public class MayaPaymentController : MonoBehaviour
{
    [Header("Purchase Buttons")]
    public Button skinButton;
    public Button subscriptionButton;

    [Header("Confirmation Popup")]
    public GameObject confirmationPopup;
    public TMP_Text itemNameText;
    // PRICE TEXT REMOVED!
    public Button confirmPurchaseButton;
    public Button cancelPurchaseButton;

    private FirebaseFunctions functions;
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private ListenerRegistration dbListener;

    private string currentItemId;
    private string currentItemName;

    void Start()
    {
        functions = FirebaseFunctions.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(false);
        }

        // PRICE REMOVED FROM LISTENERS!
        if (skinButton != null)
        {
            skinButton.onClick.AddListener(() => OpenConfirmationPopup("player_skin1", "Skin (Player)"));
        }

        if (subscriptionButton != null)
        {
            subscriptionButton.onClick.AddListener(() => OpenConfirmationPopup("premium_plan", "Premium Subscription"));
        }

        if (confirmPurchaseButton != null)
        {
            confirmPurchaseButton.onClick.AddListener(OnConfirmPurchaseClicked);
        }

        if (cancelPurchaseButton != null)
        {
            cancelPurchaseButton.onClick.AddListener(CloseConfirmationPopup);
        }
    }

    private void OpenConfirmationPopup(string itemId, string itemName)
    {
        currentItemId = itemId;
        currentItemName = itemName;

        if (itemNameText != null) itemNameText.text = itemName;

        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(true);
        }
    }

    private void CloseConfirmationPopup()
    {
        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(false);
        }

        if (confirmPurchaseButton != null)
        {
            confirmPurchaseButton.interactable = true;
        }

        if (dbListener != null)
        {
            dbListener.Stop();
            dbListener = null;
        }
    }

    private async void OnConfirmPurchaseClicked()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("Player is not logged in! Cannot make a purchase.");
            return;
        }

        if (confirmPurchaseButton != null) confirmPurchaseButton.interactable = false;

        Debug.Log($"Player {auth.CurrentUser.UserId} is buying {currentItemId}...");

        await RequestMayaCheckout(currentItemId, currentItemName);
    }

    private async Task RequestMayaCheckout(string itemId, string itemName)
    {
        try
        {
            var createCheckoutFunc = functions.GetHttpsCallable("createMayaCheckout");

            var dataToSend = new Dictionary<string, string>
            {
                { "itemId", itemId },
                { "itemName", itemName }
                // AMOUNT HAS BEEN COMPLETELY REMOVED!
            };

            var result = await createCheckoutFunc.CallAsync(dataToSend);

            var data = result.Data as System.Collections.IDictionary;

            if (data == null)
            {
                Debug.LogError("Server Error: The backend returned invalid data.");
                if (confirmPurchaseButton != null) confirmPurchaseButton.interactable = true;
                return;
            }

            if (!data.Contains("checkoutUrl") || data["checkoutUrl"] == null)
            {
                Debug.LogError("MAYA REJECTED THE REQUEST!");
                if (confirmPurchaseButton != null) confirmPurchaseButton.interactable = true;
                return;
            }

            string checkoutUrl = data["checkoutUrl"].ToString();
            string orderId = data["orderId"].ToString();

            UpdatePendingTransactionInDatabase(orderId, itemId, itemName);

            Debug.Log("Success! Opening browser... Listening for payment completion on " + orderId);

            ListenForPaymentSuccess(orderId, itemId, itemName);
            Application.OpenURL(checkoutUrl);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Payment Error: " + e.Message);
            if (confirmPurchaseButton != null) confirmPurchaseButton.interactable = true;
        }
    }

    private void UpdatePendingTransactionInDatabase(string orderId, string itemId, string itemName)
    {
        if (auth.CurrentUser == null || string.IsNullOrEmpty(orderId)) return;

        DocumentReference docRef = db.Collection("payments").Document(orderId);

        Dictionary<string, object> updateData = new Dictionary<string, object>
        {
            { "clientItemId", itemId },
            { "clientItemName", itemName },
            { "clientUserId", auth.CurrentUser.UserId }
        };

        docRef.SetAsync(updateData, SetOptions.MergeAll);
    }

    private void ListenForPaymentSuccess(string orderId, string itemId, string itemName)
    {
        DocumentReference docRef = db.Collection("payments").Document(orderId);

        dbListener = docRef.Listen(snapshot =>
        {
            if (snapshot.Exists && snapshot.ContainsField("status"))
            {
                string status = snapshot.GetValue<string>("status");

                if (status == "PAID")
                {
                    Debug.Log("PAYMENT SUCCESS! Maya confirmed it.");
                    Debug.Log($"The database has automatically unlocked {itemName} ({itemId}) for the player!");

                    CloseConfirmationPopup();
                }
            }
        });
    }

    void OnDestroy()
    {
        if (dbListener != null) dbListener.Stop();
    }
}