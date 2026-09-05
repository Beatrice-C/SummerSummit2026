// Adapted from John Sener's poker hand evaluator from:
// https://blog.stackademic.com/building-a-simple-poker-hand-evaluator-in-c-1bb81676c25c

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CardHouse;

public enum Suit 
{ 
    Hearts, 
    Diamonds, 
    Clubs, 
    Spades 
}

public enum Rank
{
    Two = 2, 
    Three, 
    Four, 
    Five, 
    Six, 
    Seven, 
    Eight, 
    Nine, 
    Ten, 
    Jack, 
    Queen, 
    King, 
    Ace
}

public class EvaluatorCard
{
    public Suit Suit { get; }
    public Rank Rank { get; }
    
    public EvaluatorCard(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }
}

public class PokerHandEvaluator : MonoBehaviour
{
    public enum HandRank
    {
        HighCard,
        OnePair,
        TwoPair,
        ThreeOfAKind,
        Straight,
        Flush,
        FullHouse,
        FourOfAKind,
        StraightFlush,
        RoyalFlush
    }

    public HandRank EvaluateHand(List<EvaluatorCard> hand)
    {
        if (IsRoyalFlush(hand)) return HandRank.RoyalFlush;
        if (IsStraightFlush(hand)) return HandRank.StraightFlush;
        if (IsFourOfAKind(hand)) return HandRank.FourOfAKind;
        if (IsFullHouse(hand)) return HandRank.FullHouse;
        if (IsFlush(hand)) return HandRank.Flush;
        if (IsStraight(hand)) return HandRank.Straight;
        if (IsThreeOfAKind(hand)) return HandRank.ThreeOfAKind;
        if (IsTwoPair(hand)) return HandRank.TwoPair;
        if (IsOnePair(hand)) return HandRank.OnePair;
        
        return HandRank.HighCard;
    }

    private bool IsRoyalFlush(List<EvaluatorCard> hand)
    {
        return IsStraightFlush(hand) && hand.All(card => card.Rank >= Rank.Ten);
    }

    private bool IsStraightFlush(List<EvaluatorCard> hand)
    {
        return IsFlush(hand) && IsStraight(hand);
    }

    private bool IsFourOfAKind(List<EvaluatorCard> hand)
    {
        var rankGroups = hand.GroupBy(card => card.Rank);
        return rankGroups.Any(group => group.Count() == 4);
    }

    private bool IsFullHouse(List<EvaluatorCard> hand)
    {
        var rankGroups = hand.GroupBy(card => card.Rank);
        return rankGroups.Any(group => group.Count() == 3) && rankGroups.Any(group => group.Count() == 2);
    }

    private bool IsFlush(List<EvaluatorCard> hand)
    {
        return hand.GroupBy(card => card.Suit).Count() == 1;
    }

    private bool IsStraight(List<EvaluatorCard> hand)
    {
        var sortedRanks = hand.Select(card => (int)card.Rank).OrderBy(rank => rank).ToList();
        
        if (sortedRanks.Last() == (int)Rank.Ace && sortedRanks.First() == (int)Rank.Two)
        {
            // Handle A-2-3-4-5 as a valid straight (wheel)
            sortedRanks.Remove(sortedRanks.Last());
            sortedRanks.Insert(0, 1);
        }
        
        for (int i = 1; i < sortedRanks.Count; i++)
        {
            if (sortedRanks[i] != sortedRanks[i - 1] + 1)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsThreeOfAKind(List<EvaluatorCard> hand)
    {
        var rankGroups = hand.GroupBy(card => card.Rank);
        return rankGroups.Any(group => group.Count() == 3);
    }

    private bool IsTwoPair(List<EvaluatorCard> hand)
    {
        var rankGroups = hand.GroupBy(card => card.Rank);
        return rankGroups.Count(group => group.Count() == 2) == 2;
    }

    private bool IsOnePair(List<EvaluatorCard> hand)
    {
        var rankGroups = hand.GroupBy(card => card.Rank);
        return rankGroups.Any(group => group.Count() == 2);
    }
}