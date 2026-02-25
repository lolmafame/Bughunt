using UnityEngine;
using UnityEngine.UI; 
using Firebase.Functions;
using System.Threading.Tasks;
using System.Collections.Generic;

public class MayaPaymentController : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Button buyButton; // Drag your Button GameObject here in the Unity Editor

    private FirebaseFunctions functions;

    void Start()
    {
        // 1. Initialize Firebase Functions
        functions = FirebaseFunctions.DefaultInstance;

        // 2. Hook up the button click event
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
        else
        {
            Debug.LogError("Buy Button is not assigned! Please drag it into the inspector.");
        }
    }

    // 3. This runs when the player clicks the button
    private async void OnBuyButtonClicked()
    {
        // Disable the button to prevent double-clicking while it loads
        buyButton.interactable = false;
        Debug.Log("Contacting server for PHP 50 Sandbox Checkout...");

        await RequestMayaCheckout();

        // Re-enable the button after the browser opens
        buyButton.interactable = true;
    }

    // 4. The actual call to your Cloud Function
    private async Task RequestMayaCheckout()
    {
        try
        {
            // Call the cloud function you deployed earlier
            var createCheckoutFunc = functions.GetHttpsCallable("createMayaCheckout");
            var result = await createCheckoutFunc.CallAsync();

            // Extract the checkoutUrl that your Node.js code sent back
            var data = (Dictionary<object, object>)result.Data;
            string checkoutUrl = data["checkoutUrl"].ToString();

            Debug.Log("Success! Opening browser to: " + checkoutUrl);

            // 5. Open the Maya Sandbox payment page in the player's web browser
            Application.OpenURL(checkoutUrl);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to get payment link from server: " + e.Message);
        }
    }
}