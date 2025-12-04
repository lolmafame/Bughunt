using UnityEngine;

[System.Serializable]
public class GoogleTokenResponse
{
    public string access_token;
    public int expires_in;
    public string scope;
    public string token_type;
    public string id_token;
}