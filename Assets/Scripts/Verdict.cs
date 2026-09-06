using Newtonsoft.Json;

public class Verdict
{
    public string Notes;
    public string Rank;
    public string Suit;
    public int Legibility;
    public int StyleMatch;
    public bool IsUnreadable => Rank == "?" || string.IsNullOrEmpty(Rank);
}