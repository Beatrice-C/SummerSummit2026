using UnityEngine;
using System.Collections;

// Poker phases in a round
public enum PokerPhase
{
    DealingPockets,
    PreFlopBetting,
    DealingFlop,
    FlopBetting,
    DealingTurn,
    TurnBetting,
    DealingRiver,
    RiverBetting,
    Showdown
}


public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public PokerPhase currentPhase;

    public PokerDealer dealer;

    public int pot = 0;

    // cost of bets of each round
    public int preFlopCost = 10;
    public int flopCost = 20;
    public int turnCost = 30;
    public int riverCost = 40;

    public int playerWallet = 500;
    public int bot1Wallet = 500;
    public int bot2Wallet = 500;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        StartNewPokerHand();
    }

    public void StartNewPokerHand()
    {
        pot = 0;

        dealer.ResetRound();

        SetPhase(PokerPhase.DealingPockets);
    }

    public void SetPhase(PokerPhase newPhase)
    {
        currentPhase = newPhase;

        if (currentPhase == PokerPhase.DealingPockets)
        {
            dealer.DealPockets();
            SetPhase(PokerPhase.PreFlopBetting);
        }
        else if (currentPhase == PokerPhase.DealingFlop)
        {
            dealer.DealFlop();
            SetPhase(PokerPhase.FlopBetting);
        }
        else if (currentPhase == PokerPhase.DealingTurn)
        {
            dealer.DealCommunityCard();
            SetPhase(PokerPhase.TurnBetting);
        }
        else if (currentPhase == PokerPhase.DealingRiver)
        {
            dealer.DealCommunityCard();
            SetPhase(PokerPhase.RiverBetting);
        }
        else if (IsBettingPhase())
        {
            RunAutomatedBotTurns();
        }
        else if (currentPhase == PokerPhase.Showdown)
        {
            ResolveWinnerAtShowdown();
        }
    }

    private void RunAutomatedBotTurns()
    {
        int stayInFee = GetCurrentRoundCost();

        bot1Wallet -= stayInFee;
        pot += stayInFee;

        bot2Wallet -= stayInFee;
        pot += stayInFee;
    }

    public void PlayerButtonCall()
    {
        int stayInFee = GetCurrentRoundCost();

        playerWallet -= stayInFee;
        pot += stayInFee;

        NextPhase();
    }

    private bool IsBettingPhase()
    {
        if (currentPhase == PokerPhase.PreFlopBetting || currentPhase == PokerPhase.FlopBetting || currentPhase == PokerPhase.TurnBetting || currentPhase == PokerPhase.RiverBetting)
            return true;
        
        return false;
    }

    private int GetCurrentRoundCost()
    {
        if (currentPhase == PokerPhase.PreFlopBetting)
            return preFlopCost;
        
        if (currentPhase == PokerPhase.FlopBetting)
            return flopCost;
        
        if (currentPhase == PokerPhase.TurnBetting)
            return turnCost;
        
        if (currentPhase == PokerPhase.RiverBetting)
            return riverCost;
        
        return 0;
    }

    private void NextPhase()
    {
        SetPhase(currentPhase + 1);
    }

    private void ResolveWinnerAtShowdown()
    {
        Showdown.instance.DetermineWinner();
    }
}