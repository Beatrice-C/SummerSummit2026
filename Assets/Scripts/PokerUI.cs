using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PokerUI : MonoBehaviour
{
    public TextMeshProUGUI potText;
    public TextMeshProUGUI betText;
    public TextMeshProUGUI playerWalletText;
    public TextMeshProUGUI bot1BetText;
    public TextMeshProUGUI bot2BetText;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI timerText;

    public List<GameObject> hamsterSprites;

    public GameObject gameOverPanel;

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            foreach (GameObject hamster in hamsterSprites)
            {
                hamster.SetActive(false);
            }
            hamsterSprites[3].SetActive(true);
        }
    }

    private void Update()
    {
        if (GameManager.instance == null)
            return;
        
        potText.text = $"Desk: ${GameManager.instance.pot}";
        playerWalletText.text = $"Wallet: ${GameManager.instance.playerWallet}";
        bot1BetText.text = $"Bet: ${GameManager.instance.betAmount}";
        bot2BetText.text = $"Bet: ${GameManager.instance.betAmount}";

        betText.text = $"Bet: ${GameManager.instance.betAmount}";

        // the only clock is the drawing window so the timer is blank outside it
        if (GameManager.instance.DrawingAllowed)
        {
            timerText.text = $"{Mathf.CeilToInt(GameManager.instance.DrawingTimeRemaining)}";
        }
        else
        {
            timerText.text = "";
        }

        if (GameManager.instance.currentPhase == PokerPhase.Showdown)
        {
            return;
        }
        else if (GameManager.instance.currentPhase == PokerPhase.PlacingBet)
        {
            phaseText.text = "Place your bet";
        }
        else
        {
            phaseText.text = "Dealing cards";
        }
    }

}
