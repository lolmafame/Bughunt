using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    public float minOnTime = 0.05f;    // Shortest burst of light
    public float maxOnTime = 0.3f;     // Longest burst of light
    public float minOffTime = 0.05f;   // Shortest blackout
    public float maxOffTime = 0.4f;    // Longest blackout

    [Header("Intensity")]
    public float intensity = 2f;       // Fixed intensity when ON

    private Light _light;

    void Start()
    {
        _light = GetComponent<Light>();
        _light.intensity = intensity;
        StartCoroutine(FlickerLoop());
    }

    System.Collections.IEnumerator FlickerLoop()
    {
        while (true)
        {
            // Turn ON
            _light.enabled = true;
            yield return new WaitForSeconds(Random.Range(minOnTime, maxOnTime));

            // Turn OFF
            _light.enabled = false;
            yield return new WaitForSeconds(Random.Range(minOffTime, maxOffTime));
        }
    }
}