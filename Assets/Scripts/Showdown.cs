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

        // figure out the poker hand created
        PokerHandEvaluator.HandRank playerRank = evaluator.EvaluateHand(playerHand);
        PokerHandEvaluator.HandRank bot1Rank = evaluator.EvaluateHand(bot1Hand);
        PokerHandEvaluator.HandRank bot2Rank = evaluator.EvaluateHand(bot2Hand);

        int pot = GameManager.instance.pot;

        // win!
        if (playerRank > bot1Rank && playerRank > bot2Rank)
        {
            GameManager.instance.playerWallet += pot;
        }
        else
        {
            // you lose
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
                    Suit evaluatorSuit = (Suit)((int)cardData.Suit);
                    Rank evaluatorRank = (Rank)(cardData.Rank);
                    extractedCards.Add(new EvaluatorCard(evaluatorSuit, evaluatorRank));
                }
            }
        }
        return extractedCards;
    }
}
