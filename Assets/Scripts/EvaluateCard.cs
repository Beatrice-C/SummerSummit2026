using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using CardHouse;

public class EvaluateCard : MonoBehaviour
{
    public string apiURL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent";

    [SerializeField] private string apiKey = "";
    public static EvaluateCard instance;

    private Texture2D placeholderForgery;
    private Texture2D placeholderReference;

    private void Awake()
    {
        instance = this;
        placeholderForgery = CreatePlaceholderTexture(Color.red);
        placeholderReference = CreatePlaceholderTexture(Color.green);
    }

    public Button sendButton;

    void Start()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(() =>
            {
                SendPrompt();
            });
        }
    }

    public void SendPrompt()
    {
        StartCoroutine(SendRequest(placeholderForgery, placeholderReference, (verdict) =>
        {
            Debug.Log("Verdict: " + verdict);
        }));
    }

    private Texture2D CreatePlaceholderTexture(Color color, int size = 4)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    IEnumerator SendRequest(Texture2D forgery, Texture2D reference, System.Action<string> verdict)
    {
        string fake = System.Convert.ToBase64String(forgery.EncodeToPNG());
        string real = System.Convert.ToBase64String(reference.EncodeToPNG());

        string body = BuildPrompt(fake, real);

        UnityWebRequest request = new UnityWebRequest(apiURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("X-goog-api-key", apiKey);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response: " + request.downloadHandler.text);

            string json = request.downloadHandler.text;
            var response = JsonUtility.FromJson<GeminiResponse>(json);
            if (response != null && response.candidates != null && response.candidates.Length > 0)
            {
                verdict(response.candidates[0].content.parts[0].text);
            }
            else
            {
                Debug.LogError("No candidates returned in Gemini response.");
                verdict(null);
            }
        }
        else
        {
            Debug.LogError("Error: " + request.error + "\n" + request.downloadHandler.text);
            verdict(null); // local fallback
        }
    }

    private string BuildPrompt(string fake, string real)
    {
        var requestBody = new GeminiRequestBody
        {
            contents = new[]
            {
                new RequestContent
                {
                    parts = new[]
                    {
                        new RequestPart { text = "Evaluate the following images and determine if the first image is a forgery compared to the second image. Return 'Forgery' or 'Authentic'." },
                        new RequestPart { inlineData = new InlineData { mimeType = "image/png", data = fake } },
                        new RequestPart { inlineData = new InlineData { mimeType = "image/png", data = real } }
                    }
                }
            }
        };

        return JsonUtility.ToJson(requestBody);
    }

    [System.Serializable]
    public class GeminiRequestBody
    {
        public RequestContent[] contents;
    }

    [System.Serializable]
    public class RequestContent
    {
        public RequestPart[] parts;
    }

    [System.Serializable]
    public class RequestPart
    {
        public string text;
        public InlineData inlineData;
    }

    [System.Serializable]
    public class InlineData
    {
        public string mimeType;
        public string data;
    }

    [System.Serializable]
    public class GeminiResponse
    {
        public Candidate[] candidates;
    }

    [System.Serializable]
    public class Candidate
    {
        public ResponseContent content;
    }

    [System.Serializable]
    public class ResponseContent
    {
        public ResponsePart[] parts;
    }

    [System.Serializable]
    public class ResponsePart
    {
        public string text;
    }

    public void CallGemini()
    {
        // Call the Gemini API
    }
}