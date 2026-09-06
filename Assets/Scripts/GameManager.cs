using UnityEngine;
using System.Collections;

// Poker phases in a round
public enum PokerPhase
{
    DealingPockets,
    PlacingBet,
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

    [Header("Round Pacing")]
    [Tooltip("How long the player gets to draw, from after betting to the showdown.")]
    public float drawingWindow = 20f;

    [Tooltip("Extra drawing time after the last community card is revealed.")]
    public float finalDrawingGrace = 3f;

    // the player can only draw once the bet is made and the window is open
    public bool DrawingAllowed { get; private set; }

    private FreeDrawPokerBridge drawingBridge;

    private void Awake()
    {
        instance = this;
        drawingBridge = GetComponent<FreeDrawPokerBridge>();
    }

    private void Start()
    {
        StartNewPokerHand();
    }

    public void StartNewPokerHand()
    {
        pot = 0;
        betAmount = 0;
        DrawingAllowed = false;

        dealer.ResetRound();

        if (Showdown.instance != null)
        {
            Showdown.instance.ResetForgery();
        }

        SetPhase(PokerPhase.DealingPockets);
    }

    public void PlaceBet(int amount)
    {
        betAmount = amount;

        // everyone bets the same
        playerWallet -= betAmount;
        bot1Wallet -= betAmount;
        bot2Wallet -= betAmount;
        pot = betAmount * 3;

        DrawingAllowed = true;
        StartCoroutine(RevealCommunityCards());
    }

    public void SetPhase(PokerPhase newPhase)
    {
        currentPhase = newPhase;

        if (currentPhase == PokerPhase.DealingPockets)
        {
            StartCoroutine(DealCardsThenBet());
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

    private IEnumerator DealCardsThenBet()
    {
        yield return dealer.DealPockets();

        SetPhase(PokerPhase.PlacingBet);
    }

    // the community cards are revealed across the drawing window and closes just after the last one
    private IEnumerator RevealCommunityCards()
    {
        // the interval between each community card being revealed is half the drawing window minus the final grace period
        float interval = Mathf.Max(0f, (drawingWindow - finalDrawingGrace) / 2f);

        SetPhase(PokerPhase.DealingFlop);

        yield return new WaitForSeconds(interval);
        SetPhase(PokerPhase.DealingTurn);

        yield return new WaitForSeconds(interval);
        SetPhase(PokerPhase.DealingRiver);

        yield return new WaitForSeconds(finalDrawingGrace);

        DrawingAllowed = false;

        if (drawingBridge != null)
        {
            drawingBridge.FinishDrawing();
        }

        SetPhase(PokerPhase.Showdown);
    }

    private void ResolveWinnerAtShowdown()
    {
        // start coroutine to wait for API response before determining winner
        StartCoroutine(Showdown.instance.DetermineWinner());
    }
}
