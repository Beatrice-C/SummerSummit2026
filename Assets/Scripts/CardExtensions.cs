using System;

// convert card info to match schema
public static class CardExtensions
{
    public static bool IsFace(this EvaluatorCard card) => card.Rank.IsFace();

    public static bool IsFace(this Rank rank) => rank == Rank.Jack || rank == Rank.Queen || rank == Rank.King;

    public static string Symbol(this Rank rank)
    {
        switch (rank)
        {
            case Rank.Jack: 
                return "J";
            case Rank.Queen:
                return "Q";
            case Rank.King:
                return "K";
            case Rank.Ace:
                return "A";
            default:
                return ((int)rank).ToString();
        }
    }

    public static string Name(this Suit suit) => suit.ToString().ToLowerInvariant();

    public static string Symbol(this EvaluatorCard card) => card.Rank.Symbol();
    public static string SuitName(this EvaluatorCard card) => card.Suit.Name();

    public static bool Matches(this EvaluatorCard card, Verdict verdict) => card.Symbol() == verdict.Rank && card.SuitName() == verdict.Suit;

    // convert a Verdict to an EvaluatorCard, or return null if the verdict is invalid
    public static EvaluatorCard ToCard(this Verdict verdict)
    {
        Rank rank;
        switch (verdict.Rank)
        {
            case "J":
                rank = Rank.Jack;
                break;
            case "Q":
                rank = Rank.Queen; 
                break;
            case "K": 
                rank = Rank.King;  
                break;
            case "A": 
                rank = Rank.Ace;   
                break;
            default:
                if (!int.TryParse(verdict.Rank, out int n) || n < 2 || n > 10)
                {
                    return null;
                }
                rank = (Rank)n;
                break;
        }

        Suit suit;
        switch (verdict.Suit)
        {
            case "hearts":
                suit = Suit.Hearts;
                break;
            case "diamonds":
                suit = Suit.Diamonds; 
                break;
            case "clubs":
                suit = Suit.Clubs;
                break;
            case "spades":
                suit = Suit.Spades;
                break;
            default:
                return null;
        }

        return new EvaluatorCard(suit, rank);
    }
}