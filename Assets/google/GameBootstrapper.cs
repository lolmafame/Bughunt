using UnityEngine;
using i5.Toolkit.Core.ServiceCore;
using i5.Toolkit.Core.OpenIDConnectClient;

public class GameBootstrapper : MonoBehaviour
{
    void Awake()
    {
        Debug.Log(">>> BOOTSTRAPPER: Started (Connector Injection Mode).");

        // 1. Cleanup Old Services
        if (ServiceManager.ServiceExists<OpenIDConnectService>())
            ServiceManager.RemoveService<OpenIDConnectService>();

        // 2. Setup the Service
        OpenIDConnectService oidc = new OpenIDConnectService();
        var googleProvider = new GoogleOidcProvider();

        oidc.ServerListener.ListeningUri = "http://localhost:52274/";
        Debug.Log(">>> OIDC: Register this redirect URI in Google Console: " + oidc.ServerListener.ListeningUri + "code");

        // =================================================================
        // THE FIX: Inject our Fake Connector directly into the Provider
        // This bypasses the ServiceManager error AND the SSL crash.
        // =================================================================
        googleProvider.RestConnector = new GoogleMockConnector();
        Debug.Log(">>> INJECTION: Mock Connector attached to Google Provider.");

        // 3. Setup Keys (Paste your Desktop Keys here)
        // =================================================================
        string myID = "39097755959-r4qrmslpn88et84529qr8hmb97afkoj9.apps.googleusercontent.com";
        string mySecret = "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";
        // =================================================================

        googleProvider.ClientData = new ClientData(myID, mySecret);
        oidc.OidcProvider = googleProvider;

        // 4. Register the Main Service
        ServiceManager.RegisterService(oidc);

        Debug.Log(">>> SUCCESS: Service Ready. Login should work now.");
    }
}