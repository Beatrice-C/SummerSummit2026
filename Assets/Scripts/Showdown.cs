using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CardHouse;

public class Showdown : MonoBehaviour
{
    public static Showdown instance;

    public PokerHandEvaluator evaluator;

    [Header("Forgery Thresholds")]
    [Tooltip("The dealer misreads the card and the player is stuck with whatever rank they thought they saw.")]
    [Range(0, 100)] public int legibilityFloor = 45;

    [Tooltip("The loan shark calls it a fake. This affects the dealer recognition difficulty, lower is more forgiving.")]
    [Range(0, 100)] public int styleFloor = 40;

    [Tooltip("Only trust a duplicate catch if the read was at least this confident, so a bad misread can't frame the player.")]
    [Range(0, 100)] public int duplicateConfidenceFloor = 70;

    [Header("Forgery Input")]
    // wire to CardCanvas once it exists
    public Texture2D forgedCardTexture;

    [Tooltip("Which slot index in the player's hand holds the forged card. -1 means they played honestly.")]
    public int forgedCardIndex = -1;

    private string caughtReason = "";

    private void Awake()
    {
        instance = this;
    }

    private void Announce(string message)
    {
        Debug.Log(message);
        var ui = FindFirstObjectByType<PokerUI>();
        if (ui != null && ui.phaseText != null)
        {
            ui.phaseText.text = message;
        }
    }

    public IEnumerator DetermineWinner()
    {
        List<EvaluatorCard> communityPool = new List<EvaluatorCard>();
        // the community cards as game objects to get their textures for forgery check
        List<Card> communityCardObjects = new List<Card>();

        // fetch cards in community pool
        foreach (CardGroup slot in GameManager.instance.dealer.communitySlots)
        {
            communityPool.AddRange(ExtractCards(slot));
            communityCardObjects.AddRange(slot.MountedCards);
        }

        // fetch cards in each player's hand and add the community pool to create 5-card hand
        List<EvaluatorCard> playerHand = ExtractCards(GameManager.instance.dealer.playerHand);

        // forgery check
        if (forgedCardIndex >= 0 && forgedCardTexture != null)
        {
            bool caught = false;

            yield return CheckForgery(playerHand, communityPool, communityCardObjects, result => caught = result);

            if (caught)
            {
                Announce($"CAUGHT. {caughtReason}");
                GameManager.instance.pot = 0;
                yield break;
            }
        }

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
        else if (bot2Rank > playerRank && bot2Rank > bot1Rank)
        {
            result = $"Bot 2 wins ${pot} with a {bot2Rank}";
        }
        else
        {
            result = $"It's a tie! You had a {playerRank}, bot 1 had {bot1Rank}, and bot 2 had {bot2Rank}";
        }

        Debug.Log($"Winner: {result}");
        Announce(result);

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

    private IEnumerator CheckForgery(List<EvaluatorCard> playerHand, List<EvaluatorCard> communityPool, List<Card> communityCardObjects, Action<bool> onCaught)
    {
        // find the two cards in the community pool to use as references for the forgery check
        var (indexA, indexB) = PickReferenceCards(communityPool);

        Texture2D refA = GetCardTexture(communityCardObjects[indexA]);
        Texture2D refB = GetCardTexture(communityCardObjects[indexB]);

        if (refA == null || refB == null || forgedCardTexture == null)
        {
            Debug.LogWarning("[DIAGNOSTIC]: Forgery check skipped due to missing textures.");
            onCaught(false);
            yield break;
        }

        Verdict verdict = null;
        yield return EvaluateCard.instance.Inspect(refA, refB, forgedCardTexture, result => verdict = result);

        // if api call failed, run local fallback -> let them pass
        if (verdict == null)
        {
            Debug.LogWarning("[DIAGNOSTIC]: API call failed, running local fallback for forgery check.");
            onCaught(false);
            yield break;
        }

        Debug.Log("[FORGERY CHECK]: Verdict received: " + $"Rank={verdict.Rank}, Suit={verdict.Suit}, Legibility={verdict.Legibility}, StyleMatch={verdict.StyleMatch}, Notes={verdict.Notes}");
        
        // check if the card is unreadable
        if (verdict.IsUnreadable)
        {
            Debug.Log("[FORGERY CHECK]: Card is unreadable. Player is stuck with the misread card.");
            Announce($"\"{verdict.Notes}\"");
            onCaught(false);
            yield break;
        }

        // check if the card is a duplicate on a confident read
        if (verdict.Legibility >= duplicateConfidenceFloor && communityPool.Exists(c => c.Matches(verdict)))
        {
            Debug.Log("[FORGERY CHECK]: Duplicate card detected. Player is caught.");
            Announce("That card is already on the table.");
            Announce($"\"{verdict.Notes}\"");
            onCaught(true);
            yield break;
        }

        // check style
        if (verdict.StyleMatch < styleFloor)
        {
            Debug.Log("[FORGERY CHECK]: Style match below threshold. Player is caught.");
            Announce("The card's style doesn't match the others.");
            Announce($"\"{verdict.Notes}\"");
            onCaught(true);
            yield break;
        }

        // legible enough but misread -> player is stuck with the misread card
        if (verdict.Legibility < legibilityFloor)
        {
            // try to parse the verdict into a card
            var misread = verdict.ToCard();
            if (misread != null)
            {
                Debug.Log($"[FORGERY CHECK]: Card is misread as {verdict.Rank} of {verdict.Suit}. Player is stuck with the misread card.");
                // replace the forged card in the player's hand with the misread card
                playerHand[forgedCardIndex] = misread;
            }
            else
            {
                Debug.LogWarning($"[FORGERY] Couldn't parse '{verdict.Rank}/{verdict.Suit}'. Leaving the original card in place.");
            }
        }

        onCaught(false);
    }

    private (int, int) PickReferenceCards(List<EvaluatorCard> pool)
    {
        // prefer a face card and a number card if possible, otherwise just take the first two
        int face = pool.FindIndex(c => c.IsFace());
        int num  = pool.FindIndex(c => !c.IsFace());

        if (face >= 0 && num >= 0)
        {
            return (face, num);
        }
        else
        {
            return (0, Mathf.Min(1, pool.Count - 1)); // calculate second index safely in case there's only one card
        }
    }

    private Texture2D GetCardTexture(Card card)
    {
        var obj = card.GetComponentInChildren<SpriteRenderer>();
        if (obj == null || obj.sprite == null)
        {
            return null;
        }

        Sprite sprite = obj.sprite;

        try
        {
            var rect = sprite.textureRect;
            var cropped = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
            cropped.SetPixels(sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height));
            cropped.Apply();
            return cropped;
        }
        catch (UnityException)
        {
            Debug.LogError($"[FORGERY] '{sprite.texture.name}' isn't readable. Tick Read/Write Enabled in its import settings.");
            return null;
        }
    }
}
