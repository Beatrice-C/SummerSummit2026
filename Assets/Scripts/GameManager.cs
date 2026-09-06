using UnityEngine;
using System.Collections;

// Poker phases in a round
public enum PokerPhase
{
    PlacingBet,
    DealingPockets,
    DealingFlop,
    DealingTurn,
    DealingRiver,
    Showdown
}


public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public PokerPhase currentPhase;

    public PokerDealer dealer;

    public int pot = 0;
    public int betAmount = 0;

    public int playerWallet = 500;
    public int bot1Wallet = 500;
    public int bot2Wallet = 500;

    [Header("Reveal Pacing")]
    [Tooltip("Time given for the pocket cards to finish being dealt before the first community card.")]
    public float pocketDealDuration = 6f;

    [Tooltip("Added on top of the drawing timer, so a full drawing window always fits before the next reveal.")]
    public float revealBuffer = 3f;

    private FreeDrawPokerBridge drawingBridge;

    private void Awake()
    {
        instance = this;
        drawingBridge = GetComponent<FreeDrawPokerBridge>();
    }

    private void Start()
    {
        // nothing is dealt until the player places their bet
        SetPhase(PokerPhase.PlacingBet);
    }

    public void StartNewPokerHand()
    {
        pot = 0;

        dealer.ResetRound();

        if (Showdown.instance != null)
        {
            Showdown.instance.ResetForgery();
        }
    }

    public void PlaceBet(int amount)
    {
        StartNewPokerHand();

        betAmount = amount;

        // everyone bets the same
        playerWallet -= betAmount;
        bot1Wallet -= betAmount;
        bot2Wallet -= betAmount;
        pot = betAmount * 3;

        SetPhase(PokerPhase.DealingPockets);
    }

    public void SetPhase(PokerPhase newPhase)
    {
        currentPhase = newPhase;

        if (currentPhase == PokerPhase.DealingPockets)
        {
            dealer.DealPockets();
            StartCoroutine(AdvanceRoundAutomatically());
        }
        else if (currentPhase == PokerPhase.DealingFlop)
        {
            dealer.DealFlop();
        }
        else if (currentPhase == PokerPhase.DealingTurn)
        {
            dealer.DealCommunityCard();
        }
        else if (currentPhase == PokerPhase.DealingRiver)
        {
            dealer.DealCommunityCard();
        }
        else if (currentPhase == PokerPhase.Showdown)
        {
            ResolveWinnerAtShowdown();
        }
    }

    // the dealer reveals a card once a full drawing window has passed, then the window starts over
    private IEnumerator AdvanceRoundAutomatically()
    {
        float revealDelay = GetRevealDelay();

        yield return new WaitForSeconds(pocketDealDuration);
        SetPhase(PokerPhase.DealingFlop);

        yield return new WaitForSeconds(revealDelay);
        SetPhase(PokerPhase.DealingTurn);

        yield return new WaitForSeconds(revealDelay);
        SetPhase(PokerPhase.DealingRiver);

        yield return new WaitForSeconds(revealDelay);
        SetPhase(PokerPhase.Showdown);
    }

    private float GetRevealDelay()
    {
        // tied to the drawing timer so changing one can never desync the other
        float drawingDuration = drawingBridge != null ? drawingBridge.drawingDuration : 0f;
        return drawingDuration + revealBuffer;
    }

    private void ResolveWinnerAtShowdown()
    {
        // start coroutine to wait for API response before determining winner
        StartCoroutine(Showdown.instance.DetermineWinner());
    }
}
