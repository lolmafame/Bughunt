using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using i5.Toolkit.Core.Utilities;
using System.Text;

public class GoogleMockConnector : IRestConnector
{
    private UnityWebRequestRestConnector realInternet = new UnityWebRequestRestConnector();

    public async Task<WebResponse<string>> GetAsync(string uri, Dictionary<string, string> headers = null)
    {
        // 1. Intercept Google Config
        if (uri.Contains("openid-configuration"))
        {
            Debug.Log(">>> FIX: Intercepted Google Config! Sending UNIVERSAL JSON.");

            // THE UNIVERSAL JSON
            // We include EVERY naming convention to ensure Unity's parser finds the data.
            string universalJson = "{" +
                // Standard snake_case
                "\"authorization_endpoint\": \"https://accounts.google.com/o/oauth2/v2/auth\"," +
                "\"token_endpoint\": \"https://oauth2.googleapis.com/token\"," +
                "\"userinfo_endpoint\": \"https://openidconnect.googleapis.com/v1/userinfo\"," +
                "\"jwks_uri\": \"https://www.googleapis.com/oauth2/v3/certs\"," +

                // CamelCase (Common in C# libraries)
                "\"authorizationEndpoint\": \"https://accounts.google.com/o/oauth2/v2/auth\"," +
                "\"tokenEndpoint\": \"https://oauth2.googleapis.com/token\"," +
                "\"userInfoEndpoint\": \"https://openidconnect.googleapis.com/v1/userinfo\"," +
                "\"jwksUri\": \"https://www.googleapis.com/oauth2/v3/certs\"" +
                "}";

            byte[] jsonBytes = Encoding.UTF8.GetBytes(universalJson);
            return new WebResponse<string>(universalJson, jsonBytes, 200);
        }

        return await realInternet.GetAsync(uri, headers);
    }

    // 2. Token Exchange Debugging
    public Task<WebResponse<string>> PostAsync(string uri, string postData, Dictionary<string, string> headers = null)
    {
        // If this prints, we know the Endpoints were parsed successfully!
        if (uri.Contains("token"))
        {
            Debug.Log(">>> DEBUG: Token Exchange Attempted! Sending Code to Google...");
        }
        return realInternet.PostAsync(uri, postData, headers);
    }

    // Pass-throughs
    public Task<WebResponse<string>> PostAsync(string uri, byte[] postData, Dictionary<string, string> headers = null) => realInternet.PostAsync(uri, postData, headers);
    public Task<WebResponse<string>> PutAsync(string uri, string postData, Dictionary<string, string> headers = null) => realInternet.PutAsync(uri, postData, headers);
    public Task<WebResponse<string>> PutAsync(string uri, byte[] postData, Dictionary<string, string> headers = null) => realInternet.PutAsync(uri, postData, headers);
    public Task<WebResponse<string>> DeleteAsync(string uri, Dictionary<string, string> headers = null) => realInternet.DeleteAsync(uri, headers);
}