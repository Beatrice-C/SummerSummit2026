using System;
using System.Collections.Generic;
using UnityEngine;
using CardHouse;

public class Showdown : MonoBehaviour
{
    public static Showdown instance;

    public PokerHandEvaluator evaluator;

    private void Awake()
    {
        instance = this;
    }

    public void DetermineWinner()
    {
        List<EvaluatorCard> communityPool = new List<EvaluatorCard>();

        // fetch cards in community pool
        foreach (CardGroup slot in GameManager.instance.dealer.communitySlots)
        {
            communityPool.AddRange(ExtractCards(slot));
        }

        // fetch cards in each player's hand and add the community pool to create 5-card hand
        List<EvaluatorCard> playerHand = ExtractCards(GameManager.instance.dealer.playerHand);
        playerHand.AddRange(communityPool);
        
        List<EvaluatorCard> bot1Hand = ExtractCards(GameManager.instance.dealer.bot1Hand);
        bot1Hand.AddRange(communityPool);
        
        List<EvaluatorCard> bot2Hand = ExtractCards(GameManager.instance.dealer.bot2Hand);
        bot2Hand.AddRange(communityPool);

        Debug.Log($"[DIAGNOSTIC]: Community cards collected count: {communityPool.Count}");
        Debug.Log($"[DIAGNOSTIC]: Player final 5-card count assembled: {playerHand.Count}");

        string playerCardList = "";
        foreach(var card in playerHand) playerCardList += $"[{card.Rank} of {card.Suit}] ";
        Debug.Log($"[DIAGNOSTIC]: Your physical hand data holds: {playerCardList}");

        // figure out the poker hand created
        PokerHandEvaluator.HandRank playerRank = evaluator.EvaluateHand(playerHand);
        PokerHandEvaluator.HandRank bot1Rank = evaluator.EvaluateHand(bot1Hand);
        PokerHandEvaluator.HandRank bot2Rank = evaluator.EvaluateHand(bot2Hand);

        GameManager.instance.dealer.RevealHand(GameManager.instance.dealer.bot1Hand);
        GameManager.instance.dealer.RevealHand(GameManager.instance.dealer.bot2Hand);

        int pot = GameManager.instance.pot;
        string result = "";

        // win!
        if (playerRank > bot1Rank && playerRank > bot2Rank)
        {
            GameManager.instance.playerWallet += pot;
            result = $"You win ${pot} with a {playerRank}";
        }
        else if (bot1Rank > playerRank && bot1Rank > bot2Rank)
        {
            result = $"Bot 1 wins ${pot} with a {bot1Rank}";
        }
        else if (bot1Rank > playerRank && bot1Rank > bot2Rank)
        {
            result = $"Bot 2 wins ${pot} with a {bot2Rank}";
        }
        else
        {
            result = $"It's a tie! You had a {playerRank}, bot 1 had {bot1Rank}, and bot 2 had {bot2Rank}";
        }

        Debug.Log($"Winner: {result}");

        PokerUI ui = FindFirstObjectByType<PokerUI>();
        if (ui != null && ui.phaseText != null)
        {
            ui.phaseText.text = result;
        }

        GameManager.instance.pot = 0;

    }

    // fetches cards from card group (eg hand or slot)
    private List<EvaluatorCard> ExtractCards(CardGroup cardGroup)
    {
        List<EvaluatorCard> extractedCards = new List<EvaluatorCard>();

        if (cardGroup != null)
        {
            foreach (Card liveCard in cardGroup.MountedCards)
            {
                PokerCard cardData = liveCard.GetComponent<PokerCard>();

                if (cardData != null)
                {
                    int finalRankValue = cardData.Rank;
                    if (finalRankValue == 1) 
                    {
                        finalRankValue = 14; // force ace as poker 14
                    }
                    Rank evaluatorRank = (Rank)finalRankValue;

                    Suit evaluatorSuit = Suit.Clubs;
                    
                    if (cardData.Suit == PokerSuit.Hearts) evaluatorSuit = Suit.Hearts;
                    else if (cardData.Suit == PokerSuit.Diamonds) evaluatorSuit = Suit.Diamonds;
                    else if (cardData.Suit == PokerSuit.Clubs) evaluatorSuit = Suit.Clubs;
                    else if (cardData.Suit == PokerSuit.Spades) evaluatorSuit = Suit.Spades;

                    extractedCards.Add(new EvaluatorCard(evaluatorSuit, evaluatorRank));
                }
            }
        }
        return extractedCards;
    }
}
