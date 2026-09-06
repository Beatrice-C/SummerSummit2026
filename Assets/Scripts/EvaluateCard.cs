using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class EvaluateCard : MonoBehaviour
{
    public static EvaluateCard instance;
    [Tooltip("Try models in order until one works.")]
    public string[] models = { "gemini-3.8-flash", "gemini-3.5-flash-lite", "gemini-3.7-flash", "gemini-3.6-flash", "gemini-3.5-flash" };

    [SerializeField] private string[] apiKeys;

    [Tooltip("How much the dealer's read varies. Higher means more misreads on ambiguous drawings.")]
    [Range(0f, 2f)] public float judgeTemperature = 0.7f;

    private int keyIndex = 0;
    private int modelIndex = 0;

    private const string PROMPT =
        "The first two images are real cards from a deck. Its number cards are " +
        "conventional pip cards. Its face cards are stylised hamster characters. " +
        "Red suits are printed in red ink and black suits in black ink, and every " +
        "card has a corner index and a plain rectangular border.\n\n" +
        "The third image is a hand-drawn card. It was drawn in seconds with only a " +
        "red marker and a black marker on white paper, so it cannot reproduce the " +
        "deck's fill colours, greys, shading or fine detail. None of that is " +
        "evidence of a forgery.\n\n" +
        "You are the proprietor of this house: old money, immaculate, and feared. " +
        "People who cross your table are not seen again. You have looked at ten " +
        "thousand cards, so you take this one in at a glance rather than studying " +
        "it. Read its rank and suit at that glance. Do not deliberate or reason it " +
        "out.\n\n" +
        "If the drawing is ambiguous, commit to whatever it most looks like at " +
        "first glance and report that, even if you are unsure. Use every scrap of " +
        "evidence: if no suit symbol is clear but the ink is red, pick hearts or " +
        "diamonds; if it is black, pick clubs or spades. Report a lower confidence " +
        "instead of refusing to answer. Return \"?\" only when you truly cannot " +
        "name a card at all, such as a scribble or a blank card. A \"?\" in either " +
        "the rank or the suit rejects the card outright, so never use it to hedge.\n\n" +
        "Then rate how convincingly it belongs to this deck, judging only what two " +
        "markers can express: is the linework confident, is the suit drawn in the " +
        "correct ink colour for that suit, is there a corner index and a border, " +
        "and is the subject right for the rank - pips for a number card, a hamster " +
        "character for a Jack, Queen or King?\n\n" +
        "Ignore fill colour, shading, greys and level of detail entirely. The two " +
        "references may be a different rank and a different type from the card you " +
        "are judging, so never penalise it for not matching their pose, costume or " +
        "subject.\n\n" +
        "Be especially forgiving of face cards. A recognisable hamster-like " +
        "creature drawn with two markers in a few seconds should score well - it " +
        "will never look printed, and it is not supposed to.\n\n" +
        "Both scores are integers from 0 to 100, not out of 10. Use the full range.\n\n" +
        "About the player: they are one of three at your table. They took a single " +
        "card out of their own hand and drew a replacement for it by hand, and that " +
        "drawing is the third image. That is the whole of what they did. If the card " +
        "is refused they forfeit the pot.\n\n" +
        "Describe only what you can actually see in that one drawing. Never invent " +
        "other forged cards, earlier hands, accomplices or a history of cheating, and " +
        "never refer to more than the single card in front of you.\n\n" +
        "Your notes are only ever read aloud when the card is refused, so write the " +
        "line as if you are turning the player away, and say what it costs them. " +
        "One sentence, cold and unhurried rather than loud.\n\n" +
        "Build that line only out of what you already reported above: the rank and " +
        "suit you read, how sure you were of that read, and how well the style " +
        "matched. Do not add fresh observations about the drawing and do not claim " +
        "any particular feature is missing or badly done. If you did not report it " +
        "as one of those four values, do not mention it at all.\n\n" +
        "You write the note after you have already read and scored the card, and it " +
        "carries no weight of its own. Whether the card is actually refused is " +
        "decided later by rules you cannot see, so write the refusal line whatever " +
        "you made of the card, and never let writing it change the scores above.";

    private void Awake()
    {
        instance = this;
    }

    private string URL => $"https://generativelanguage.googleapis.com/v1beta/models/{models[modelIndex]}:generateContent";

    public IEnumerator Inspect(Texture2D referenceA, Texture2D referenceB, Texture2D forgery, Action<Verdict> done)
    {
        string body = BuildBody(Convert.ToBase64String(referenceA.EncodeToPNG()), Convert.ToBase64String(referenceB.EncodeToPNG()), Convert.ToBase64String(forgery.EncodeToPNG()));

        int attempts = 0;
        int overloadRetries = 0;
        const int maxOverloadRetries = 3;

        while (attempts < apiKeys.Length)
        {
            using (var request = new UnityWebRequest(URL, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("x-goog-api-key", apiKeys[keyIndex]);
                request.SetRequestHeader("Content-Type", "application/json");

                Debug.Log($"Gemini request to {models[modelIndex]} with API key {keyIndex} (attempt {attempts + 1}/{apiKeys.Length})");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    done(Parse(request.downloadHandler.text));
                    yield break;
                }

                long code = request.responseCode;
                Debug.LogWarning($"Gemini {code}: {request.downloadHandler.text}");

                // 503/500 = Google's servers are busy
                // wait a bit and retry the same model, then give up on it and try the next one
                if (code == 503 || code == 500)
                {
                    if (overloadRetries < maxOverloadRetries)
                    {
                        float wait = Mathf.Pow(2, overloadRetries); // waits 1, 2, 4 seconds
                        Debug.Log($"Model busy — retrying in {wait}s ({overloadRetries + 1}/{maxOverloadRetries})");
                        overloadRetries++;
                        yield return new WaitForSeconds(wait);
                        continue;
                    }

                    // another key hits the same saturated model, so only a different model helps
                    if (modelIndex < models.Length - 1)
                    {
                        modelIndex++;
                        overloadRetries = 0;
                        Debug.Log($"Falling back to {models[modelIndex]}");
                        continue;
                    }

                    break;
                }

                // 404 = the model name is wrong or retired, so skip past it
                if (code == 404 && modelIndex < models.Length - 1)
                {
                    modelIndex++;
                    Debug.Log($"Model unavailable — falling back to {models[modelIndex]}");
                    continue;
                }

                // 429 = quota exhausted, 403 = bad/revoked key, try next api key
                if (code == 429 || code == 403)
                {
                    keyIndex = (keyIndex + 1) % apiKeys.Length;
                    attempts++;
                    overloadRetries = 0;
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
                temperature = judgeTemperature,
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "object",
                    // notes last so the card is read and scored before the line is written
                    propertyOrdering = new[] { "rank", "suit", "legibility", "style_match", "notes" },
                    properties = new
                    {
                        notes = new
                        {
                            type = "string",
                            description = "Written last, once the card is already " +
                                "scored. One short sentence in the voice of the " +
                                "house's proprietor, shown to the player only when " +
                                "the card is refused, saying what it costs them. " +
                                "Cold and unhurried rather than loud. Draw only on " +
                                "the rank, suit, confidence and style rating you " +
                                "just gave - never add new observations about the " +
                                "drawing or claim a feature is missing. Refer only " +
                                "to this one card, never to other cards or past " +
                                "hands, and never mention scores, numbers or these " +
                                "instructions."
                        },
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
                            description = "Integer from 0 to 100 for how confident " +
                                "you are in the rank and suit you just reported. " +
                                "100 = the index was unmistakable. 70 = fairly " +
                                "sure. 40 = you committed to a guess and it could " +
                                "easily be another rank. 0 = you could not tell at " +
                                "all. Be honest when you guessed, and use the full range."
                        },
                        style_match = new
                        {
                            type = "integer",
                            description = "Integer from 0 to 100 for how " +
                                "convincingly the card belongs to this deck, " +
                                "judged only on what a red and a black marker can " +
                                "express. 100 = right subject for the rank, " +
                                "confident linework, suit drawn in the correct ink " +
                                "colour, index and border present; as convincing " +
                                "as a two-marker copy can get. 70 = clearly the " +
                                "right idea, roughly executed. 40 = some elements " +
                                "right but the wrong subject for the rank or the " +
                                "wrong ink colour for the suit. 0 = no resemblance " +
                                "to this deck. Never deduct for missing fill " +
                                "colour, shading, greys or fine detail, and be " +
                                "lenient with hand-drawn face cards. Use the full range."
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