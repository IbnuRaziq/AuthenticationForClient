using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class AuthHandler : MonoBehaviour
{
    public InputField input_Field;
    public string license;      // The license to validate (must be 18 characters)
    public string url;          // The server url

    public string sceneToLoadAfterAuthentication;   // The name of the string to be loaded if authentication finished

    // Use this for initialization
   public void Startauth()
    {
        license = input_Field.text;

        if (PlayerPrefs.GetInt("LicenseValidated", 0) == 1)     // Check player prefs for validated license
        {
            Debug.Log("Already authenticated");
            LicenseValidated(); // Skip authentication as the validated license detected
        }
        else
        {
            if (license.Length != 18)   // Check for license length
            {
                Debug.Log("Your license either too long or too short. Enter correct license");
                // What to do if license format is wrong ?
            }
            else
            {
                StartCoroutine(AuthStep1(url, license));    // Start authentication step 1
            }
        }
    }

    // Send token and match the response with last half of the license
    IEnumerator AuthStep1(string url, string license)
    {
        int licenseLength = license.Length;     // Get license length
        string token = license.Substring(0, licenseLength / 2); // Extract token
        string expectedTokenResponse = license.Substring(licenseLength / 2);    // Extract last half of the license
        string finalUri = url + "?command=auth&token=" + token;     // Build final url for requesting the server

        UnityWebRequest www = UnityWebRequest.Post(finalUri, "");   // Create webrequest object

        yield return www.Send();    // Sending request

        if (www.isError)    // If error occured
        {
            Debug.Log(www.error);
        }
        else   // If request success
        {
            string jsonResponse = www.downloadHandler.text;  // Get response
            AuthResponse authResponse = JsonUtility.FromJson<AuthResponse>(jsonResponse);   // Convert to response class
            if (authResponse.response == "exceed_limit_cause")  // Check if license limit is reached
            {
                Debug.Log("Quit Application : exceed_limit_cause");
                LicenseNotValidated();  // Fail in authentication
            }
            else   // If response is not exceed_limit_cause
            {
                if (authResponse.response == expectedTokenResponse) // Check if response match the last half of license
                {
                    StartCoroutine(AuthStep2(url, token));  // Start step 2 authentication
                }
                else
                {
                    Debug.Log("Quit Application : wrong response");
                    LicenseNotValidated();  // Fail in authentication
                }
            }
        }
    }

    // Send validated token and increment the license limit
    IEnumerator AuthStep2(string url, string token)
    {
        string finalUri = url + "?command=req&token=" + token;  // Build final uri for requesting the server
        UnityWebRequest www = UnityWebRequest.Post(finalUri, "");   // Create webrequest object

        yield return www.Send();    // Sending request

        if (www.isError)    // If error occured
        {
            Debug.Log(www.error);
        }
        else
        {
            string jsonResponse = www.downloadHandler.text;  // Get response
            AuthResponse authResponse = JsonUtility.FromJson<AuthResponse>(jsonResponse);   // Convert to response class
            if (authResponse.response == "increment_count") // If the server success incrementing the license limit
            {
                Debug.Log("Auth Process finished : increment_count");
                LicenseValidated(); // Finish validation process
            }
            else  // If the server failed to incrementing the license limit
            {
                Debug.Log("Auth Process failed : not_incremented");
            }
        }
    }

    // Call this method for validated license
    private void LicenseValidated()
    {
        PlayerPrefs.SetInt("LicenseValidated", 1);  // Flag the device when the license validation is correct
        SceneManager.LoadScene(sceneToLoadAfterAuthentication); // Load another scene after validation
    }

    // Call this method for not validated license
    private void LicenseNotValidated()
    {
        Application.Quit();     // Quit application when the license is not validated
    }
}