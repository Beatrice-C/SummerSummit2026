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

    public GameObject winScreen;
    public GameObject loseScreen;

    public List<GameObject> hamsterSprites;
    public List<GameObject> overlays;

    [Header("Forgery Thresholds")]
    [Tooltip("Below this, the dealer calls it a fake. This affects the dealer recognition difficulty, lower is more forgiving.")]
    [Range(0, 100)] public int styleFloor = 15;

    [Tooltip("Only trust a duplicate catch if the read was at least this confident, so a bad misread can't frame the player.")]
    [Range(0, 100)] public int duplicateConfidenceFloor = 60;

    [Header("Forgery Input")]
    // wire to CardCanvas once it exists
    public Texture2D forgedCardTexture;

    [Tooltip("Which slot index in the player's hand holds the forged card. -1 means they played honestly.")]
    public int forgedCardIndex = -1;

    [Tooltip("How long the result stays on screen before the game over panel covers it.")]
    public float resultDisplayTime = 4f;

    private string caughtReason = "";
    private string caughtNotes = "";

    private void Awake()
    {
        instance = this;
    }

    // the reason and the dealer's line appear together
    private string CaughtMessage()
    {
        string message = $"CAUGHT. {caughtReason}";

        if (!string.IsNullOrEmpty(caughtNotes))
        {
            message += $"\n\"{caughtNotes}\"";
        }

        return message;
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
        int res = -1;
        PokerDealer dealer = GameManager.instance.dealer;

        // fetch cards in community pool
        List<TableCard> communityCards = new List<TableCard>();
        foreach (CardGroup slot in dealer.communitySlots)
        {
            communityCards.AddRange(ExtractCards(slot, "on the table"));
        }

        // fetch cards in each player's hand
        List<TableCard> playerCards = ExtractCards(dealer.playerHand, "in your hand");
        List<TableCard> bot1Cards = ExtractCards(dealer.bot1Hand, "in Bot 1's hand");
        List<TableCard> bot2Cards = ExtractCards(dealer.bot2Hand, "in Bot 2's hand");

        // flip every card before judging
        // has wait to let card flip animation finish
        yield return RevealTable();

        List<EvaluatorCard> communityPool = communityCards.Select(t => t.Data).ToList();
        List<EvaluatorCard> playerHand = playerCards.Select(t => t.Data).ToList();

        // forgery check
        if (forgedCardIndex >= 0 && forgedCardTexture != null)
        {
            bool caught = false;

            List<TableCard> tableCards = BuildTablePool(communityCards, playerCards, bot1Cards, bot2Cards);

            yield return CheckForgery(playerHand, tableCards, result => caught = result);

            if (caught)
            {
                Announce(CaughtMessage());
                GameManager.instance.pot = 0;

                yield return EndGame(1);
                yield break;
            }
        }

        // add the community pool to each hand to create 5-card hands
        playerHand.AddRange(communityPool);

        List<EvaluatorCard> bot1Hand = bot1Cards.Select(t => t.Data).ToList();
        bot1Hand.AddRange(communityPool);

        List<EvaluatorCard> bot2Hand = bot2Cards.Select(t => t.Data).ToList();
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

        int pot = GameManager.instance.pot;
        string result = "";

        // win!
        if (playerRank > bot1Rank && playerRank > bot2Rank)
        {
            GameManager.instance.playerWallet += pot;
            result = $"You win ${pot} with a {playerRank}";
            res = 0;
        }
        else if (bot1Rank > playerRank && bot1Rank > bot2Rank)
        {
            GameManager.instance.bot1Wallet += pot;
            result = $"Bot 1 wins ${pot} with a {bot1Rank}";
            res = 2;
        }
        else if (bot2Rank > playerRank && bot2Rank > bot1Rank)
        {
            GameManager.instance.bot2Wallet += pot;
            result = $"Bot 2 wins ${pot} with a {bot2Rank}";
            res = 2;
        }
        else
        {
            // everyone bets the same, so a tie means everyone gets their money back
            int refund = GameManager.instance.betAmount;
            GameManager.instance.playerWallet += refund;
            GameManager.instance.bot1Wallet += refund;
            GameManager.instance.bot2Wallet += refund;

            result = $"It's a tie! You had a {playerRank}, bot 1 had {bot1Rank}, and bot 2 had {bot2Rank}";
            res = 3;
        }

        Debug.Log($"Winner: {result}");
        Announce(result);

        GameManager.instance.pot = 0;

        yield return EndGame(res);
    }

    // showdown result is the last thing that happens
    private IEnumerator EndGame(int result)
    {
        // let the result be read before the game over panel covers it
        yield return new WaitForSeconds(resultDisplayTime);

        foreach (GameObject hamster in hamsterSprites)
        {
            hamster.SetActive(false);
        }
        hamsterSprites[result].SetActive(true);

        if (result == 0)
        {
            winScreen.SetActive(true);
        }
        else
        {
            loseScreen.SetActive(true);
        }
        
        yield return new WaitForSeconds(2f);
        
        overlays[result].SetActive(true);
    }

    // an evaluator card paired with the card object it came from so we can get texture and owner
    private struct TableCard
    {
        public EvaluatorCard Data;
        public Card Obj;
        public string Owner;

        public TableCard(EvaluatorCard data, Card obj, string owner)
        {
            Data = data;
            Obj = obj;
            Owner = owner;
        }
    }

    // flip every card face up while the verdict is calculated
    private IEnumerator RevealTable()
    {
        PokerDealer dealer = GameManager.instance.dealer;

        RevealGroup(dealer.bot1Hand);
        RevealGroup(dealer.bot2Hand);

        // give the flip animation time to finish before anything else happens
        yield return new WaitForSeconds(0.4f);
    }

    private void RevealGroup(CardGroup group)
    {
        if (group == null)
        {
            return;
        }

        // force card group to face up
        CardGroupSettings settings = group.GetComponent<CardGroupSettings>();
        if (settings != null)
        {
            settings.ForcedFacing = CardFacing.FaceUp;
        }

        GameManager.instance.dealer.RevealHand(group);
    }

    // every real card on the table minus the forgery
    private List<TableCard> BuildTablePool(List<TableCard> communityCards, List<TableCard> playerCards, List<TableCard> bot1Cards, List<TableCard> bot2Cards)
    {
        List<TableCard> pool = new List<TableCard>();

        pool.AddRange(communityCards);
        pool.AddRange(bot1Cards);
        pool.AddRange(bot2Cards);

        // add player cards except the forged one
        for (int i = 0; i < playerCards.Count; i++)
        {
            if (i != forgedCardIndex)
            {
                pool.Add(playerCards[i]);
            }
        }

        return pool;
    }

    // fetches cards from card group (eg hand or slot)
    private List<TableCard> ExtractCards(CardGroup cardGroup, string owner)
    {
        List<TableCard> extractedCards = new List<TableCard>();

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

                    extractedCards.Add(new TableCard(new EvaluatorCard(evaluatorSuit, evaluatorRank), liveCard, owner));
                }
            }
        }
        return extractedCards;
    }

    private IEnumerator CheckForgery(List<EvaluatorCard> playerHand, List<TableCard> tableCards, Action<bool> onCaught)
    {
        if (tableCards.Count < 2)
        {
            Debug.LogWarning("[DIAGNOSTIC]: Forgery check skipped, not enough cards on the table to compare against.");
            onCaught(false);
            yield break;
        }

        // find two cards from anywhere on the table to use as references for the forgery check
        var (referenceA, referenceB) = PickReferenceCards(tableCards);

        Texture2D refA = GetCardTexture(referenceA.Obj);
        Texture2D refB = GetCardTexture(referenceB.Obj);

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
        
        // check if the card is unreadable aka ?
        if (verdict.IsUnreadable)
        {
            Debug.Log("[FORGERY CHECK]: Card is unreadable. Player is caught.");
            caughtReason = "The dealer couldn't tell what that card was meant to be.";
            caughtNotes = verdict.Notes;
            onCaught(true);
            yield break;
        }

        // check if the card is a duplicate of any card on the table, if it's legible enough
        if (verdict.Legibility >= duplicateConfidenceFloor)
        {
            int duplicate = tableCards.FindIndex(t => t.Data.Matches(verdict));

            if (duplicate >= 0)
            {
                Debug.Log("[FORGERY CHECK]: Duplicate card detected. Player is caught.");
                caughtReason = $"That card is already {tableCards[duplicate].Owner}.";
                caughtNotes = verdict.Notes;
                onCaught(true);
                yield break;
            }
        }

        // check style
        if (verdict.StyleMatch < styleFloor)
        {
            Debug.Log("[FORGERY CHECK]: Style match below threshold. Player is caught.");
            caughtReason = "The card's style doesn't match the others.";
            caughtNotes = verdict.Notes;
            onCaught(true);
            yield break;
        }

        // the card becomes whatever the dealer read it as, cleanly or not
        var read = verdict.ToCard();
        if (read != null && forgedCardIndex < playerHand.Count)
        {
            Debug.Log($"[FORGERY CHECK]: Card read as {verdict.Rank} of {verdict.Suit} (confidence {verdict.Legibility}). Player is stuck with that read.");
            playerHand[forgedCardIndex] = read;
        }
        else
        {
            Debug.LogWarning($"[FORGERY] Couldn't parse '{verdict.Rank}/{verdict.Suit}'. Leaving the original card in place.");
        }

        onCaught(false);
    }

    // clears the forgery so a new hand doesn't inherit an index pointing at a destroyed card
    public void ResetForgery()
    {
        forgedCardIndex = -1;
        caughtReason = "";
        caughtNotes = "";

        if (forgedCardTexture != null)
        {
            Destroy(forgedCardTexture);
            forgedCardTexture = null;
        }
    }

    private (TableCard, TableCard) PickReferenceCards(List<TableCard> pool)
    {
        // prefer a face card and a number card if possible, otherwise just take the first two
        int face = pool.FindIndex(t => t.Data.IsFace());
        int num  = pool.FindIndex(t => !t.Data.IsFace());

        if (face >= 0 && num >= 0)
        {
            return (pool[face], pool[num]);
        }
        else
        {
            return (pool[0], pool[1]);
        }
    }

    private Texture2D GetCardTexture(Card card)
    {
        // read the front texture of the card
        PokerCard cardData = card.GetComponent<PokerCard>();
        if (cardData == null || cardData.Image == null || cardData.Image.sprite == null)
        {
            return null;
        }

        Sprite sprite = cardData.Image.sprite;

        // crop the sprite so we only get the card face, not the whole texture
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
