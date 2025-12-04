using System.Net;
using System.Threading.Tasks;
using UnityEngine;

public class OAuthFlow
{
    // Define a port that your application will listen on for the redirect.
    private const int RedirectPort = 54321;
    public static readonly string RedirectUri = $"http://127.0.0.1:{RedirectPort}/";

    public async Task<string> StartListenerAndGetCode()
    {
        // 1. Initialize the HttpListener
        var listener = new HttpListener();
        listener.Prefixes.Add(RedirectUri);
        listener.Start();
        Debug.Log("Listening for OAuth redirect on: " + RedirectUri);

        // 2. Wait for a request (the callback from Google)
        var context = await listener.GetContextAsync();

        // 3. Extract the authorization 'code' from the query parameters
        var code = context.Request.QueryString["code"];

        // 4. Send a basic response back to the browser to close the page
        string responseString = "<html><body>You can now close this window.</body></html>";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();

        listener.Stop();
        return code;
    }
}