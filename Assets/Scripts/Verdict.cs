using Newtonsoft.Json;

public class Verdict
{
    public string Notes;
    public string Rank;
    public string Suit;
    public int Legibility;
    [JsonProperty("style_match")]
    public int StyleMatch;
    // a card missing either a rank or a suit isn't a card, so it counts as unreadable
    public bool IsUnreadable => IsMissing(Rank) || IsMissing(Suit);

    private static bool IsMissing(string field) => field == "?" || string.IsNullOrEmpty(field);
}