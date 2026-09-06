using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class EvaluateCard : MonoBehaviour
{
    public static EvaluateCard instance;
    public string model = "gemini-3.1-flash-lite";
    [SerializeField] private string[] apiKeys;

    private int keyIndex = 0;

    private const string PROMPT =
        "The first two images are real cards from this deck, shown so you can see " +
        "its visual style. The third image is a hand-drawn card of unknown rank.\n\n" +
        "Identify the third card's rank and suit. Use the corner index (letter and " +
        "suit symbol) as the primary evidence for rank where one is present. If you " +
        "genuinely cannot tell what card it is meant to be, return \"?\" rather " +
        "than guessing.\n\n" +
        "Then rate how convincingly it belongs to this deck. Judge it on the same " +
        "criteria you would use to spot a forgery: does it depict the same kind of " +
        "subject in the same way as the references, with the same palette, line " +
        "weight and suit symbols? The references may be a different rank than the " +
        "card under inspection, so do not penalise it for showing a different pose, " +
        "costume or level of detail.\n\n" +
        "Grade generously. This was drawn by hand in seconds, not printed. A rough " +
        "sketch that clearly belongs to this deck should score well.\n\n" +
        "Both scores are integers from 0 to 100, not out of 10. A real printed " +
        "card from this deck would score close to 100 on both. Use the full range.";

    private void Awake()
    {
        instance = this;
    }

    private string URL => $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

    public IEnumerator Inspect(Texture2D referenceA, Texture2D referenceB, Texture2D forgery, Action<Verdict> done)
    {
        string body = BuildBody(Convert.ToBase64String(referenceA.EncodeToPNG()), Convert.ToBase64String(referenceB.EncodeToPNG()), Convert.ToBase64String(forgery.EncodeToPNG()));

        int attempts = 0;

        while (attempts < apiKeys.Length)
        {
            using (var request = new UnityWebRequest(URL, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("x-goog-api-key", apiKeys[keyIndex]);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    done(Parse(request.downloadHandler.text));
                    yield break;
                }

                long code = request.responseCode;
                Debug.LogWarning($"Gemini {code}: {request.downloadHandler.text}");

                // 429 = quota exhausted, 403 = bad/revoked key, try next api key
                if (code == 429 || code == 403)
                {
                    keyIndex = (keyIndex + 1) % apiKeys.Length;
                    attempts++;
                    continue;
                }

                break;
            }
        }

        done(null); // runs local fallback
    }

    private string BuildBody(string refA, string refB, string forgery)
    {
        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = PROMPT },
                        new { inline_data = new { mime_type = "image/png", data = refA } },
                        new { inline_data = new { mime_type = "image/png", data = refB } },
                        new { inline_data = new { mime_type = "image/png", data = forgery } }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0,
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "object",
                    propertyOrdering = new[] { "notes", "rank", "suit", "legibility", "style_match" },
                    properties = new
                    {
                        notes = new { type = "string" },
                        rank = new
                        {
                            type = "string",
                            @enum = new[] { "2","3","4","5","6","7","8","9","10","J","Q","K","A","?" }
                        },
                        suit = new
                        {
                            type = "string",
                            @enum = new[] { "hearts","diamonds","clubs","spades","?" }
                        },
                        legibility = new
                        {
                            type = "integer",
                            description = "Integer from 0 to 100 for how clearly " +
                                "the rank and suit can be read. 100 = instantly " +
                                "and unambiguously readable. 70 = readable with " +
                                "a moment's effort. 40 = ambiguous, could be " +
                                "mistaken for another rank. 0 = no rank is " +
                                "discernible at all. Use the full range."
                        },
                        style_match = new
                        {
                            type = "integer",
                            description = "Integer from 0 to 100 for how well the " +
                                "card matches the deck shown in the reference " +
                                "images. 100 = indistinguishable from a printed " +
                                "card in this deck. 70 = obviously hand-drawn but " +
                                "clearly belongs to this deck. 40 = shares some " +
                                "elements but would draw a second look. 0 = no " +
                                "resemblance to this deck. Use the full range."
                        },
                    },
                    required = new[] { "notes", "rank", "suit", "legibility", "style_match" }
                }
            }
        };

        return JsonConvert.SerializeObject(body, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
    }

    private Verdict Parse(string raw)
    {
        try
        {
            // convert gemini output to Verdict class
            var envelope = JObject.Parse(raw);
            string inner = envelope["candidates"][0]["content"]["parts"][0]["text"].ToString();
            return JsonConvert.DeserializeObject<Verdict>(inner);
        }
        catch (Exception e)
        {
            Debug.LogError($"Couldn't parse verdict: {e.Message}\n{raw}");
            return null;
        }
    }
}