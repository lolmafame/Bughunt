using UnityEngine;
using UnityEngine.UI;
using Firebase.Functions;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using System.Collections.Generic;

public class MayaPaymentController : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Button buyButton;

    private FirebaseFunctions functions;
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private ListenerRegistration dbListener;

    void Start()
    {
        functions = FirebaseFunctions.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
    }

    private async void OnBuyButtonClicked()
    {
        // 1. Check if the user is actually logged in with Google SSO
        if (auth.CurrentUser == null)
        {
            Debug.LogError("Player is not logged in! Cannot make a purchase.");
            return;
        }

        buyButton.interactable = false;
        Debug.Log($"Player {auth.CurrentUser.UserId} is buying player_skin1...");

        await RequestMayaCheckout("player_skin1"); // Tell the server which skin to buy
    }

    private async Task RequestMayaCheckout(string skinToBuy)
    {
        try
        {
            var createCheckoutFunc = functions.GetHttpsCallable("createMayaCheckout");

            var dataToSend = new Dictionary<string, object> { { "skinId", skinToBuy } };
            var result = await createCheckoutFunc.CallAsync(dataToSend);

            // 1. Safely cast the data so Unity doesn't crash
            var data = result.Data as System.Collections.IDictionary;

            if (data == null)
            {
                Debug.LogError("Server Error: The backend returned invalid data.");
                buyButton.interactable = true;
                return;
            }

            // 2. Did Maya actually give us a URL, or did they reject us?
            if (!data.Contains("checkoutUrl") || data["checkoutUrl"] == null)
            {
                Debug.LogError("MAYA REJECTED THE REQUEST! Here is what the server returned instead:");

                // Print out every piece of data the server sent us so we can find the bug
                foreach (System.Collections.DictionaryEntry item in data)
                {
                    Debug.Log($"   -> {item.Key}: {item.Value}");
                }

                buyButton.interactable = true;
                return;
            }

            // 3. If we made it here, Maya accepted it!
            string checkoutUrl = data["checkoutUrl"].ToString();
            string orderId = data["orderId"].ToString();

            Debug.Log("Success! Opening browser... Listening for payment completion on " + orderId);

            ListenForPaymentSuccess(orderId, skinToBuy);
            Application.OpenURL(checkoutUrl);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Payment Error: " + e.Message);
            buyButton.interactable = true;
        }
    }

    private void ListenForPaymentSuccess(string orderId, string skinId)
    {
        DocumentReference docRef = db.Collection("payments").Document(orderId);

        // 4. This block constantly watches the specific receipt in the database
        dbListener = docRef.Listen(snapshot =>
        {
            if (snapshot.Exists && snapshot.ContainsField("status"))
            {
                string status = snapshot.GetValue<string>("status");

                if (status == "PAID")
                {
                    Debug.Log("PAYMENT SUCCESS! Maya confirmed it.");
                    Debug.Log($"The database has automatically unlocked {skinId} for the player!");

                    // TODO: Run your visual game logic here (e.g., equip the skin, play a sound)

                    buyButton.interactable = true; // Re-enable button
                    dbListener.Stop(); // Stop listening to save memory
                }
            }
        });
    }

    void OnDestroy()
    {
        if (dbListener != null) dbListener.Stop(); // Clean up if they change scenes
    }
}