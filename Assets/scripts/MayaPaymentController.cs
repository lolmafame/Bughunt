using UnityEngine;
using UnityEngine.UIElements;
using Firebase.Functions;
using System.Threading.Tasks;
using System.Collections.Generic;

public class MayaPaymentController : MonoBehaviour
{
    private FirebaseFunctions functions;
    private Button buyButton;

    void Start()
    {
        // 1. Initialize Firebase Functions
        functions = FirebaseFunctions.DefaultInstance;

        // 2. Hook up your UI Toolkit button
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Make sure the name inside Q<Button> matches the name of your button in the UI Builder
        buyButton = root.Q<Button>("BuySkinButton");

        if (buyButton != null)
        {
            buyButton.clicked += OnBuyButtonClicked;
        }
        else
        {
            Debug.LogError("Could not find the BuySkinButton in the UI Document!");
        }
    }

    // 3. This runs when the player clicks the button
    private async void OnBuyButtonClicked()
    {
        // Optional: Disable the button so they don't spam click it while loading
        buyButton.SetEnabled(false);
        Debug.Log("Contacting server for PHP 50 Sandbox Checkout...");

        await RequestMayaCheckout();

        // Re-enable the button after the browser opens
        buyButton.SetEnabled(true);
    }

    // 4. The actual call to your Cloud Function
    private async Task RequestMayaCheckout()
    {
        try
        {
            // The string here MUST perfectly match the name of the function you exported in Node.js
            var createCheckoutFunc = functions.GetHttpsCallable("createMayaCheckout");

            // Call the cloud function. We don't even need to send the price, 
            // because the server already knows this function is for the PHP 50 item!
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

    void OnDestroy()
    {
        // Good practice to unregister UI Toolkit events
        if (buyButton != null)
        {
            buyButton.clicked -= OnBuyButtonClicked;
        }
    }
}