using UnityEngine;
using TMPro;

public class PokerUI : MonoBehaviour
{
    public TextMeshProUGUI potText;
    public TextMeshProUGUI betText;
    public TextMeshProUGUI playerWalletText;
    public TextMeshProUGUI bot1WalletText;
    public TextMeshProUGUI bot2WalletText;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI timerText;

    public GameObject gameOverPanel;

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    private void Update()
    {
        if (GameManager.instance == null)
            return;
        
        potText.text = $"Desk: ${GameManager.instance.pot}";
        playerWalletText.text = $"Wallet: ${GameManager.instance.playerWallet}";
        bot1WalletText.text = $"Wallet: ${GameManager.instance.bot1Wallet}";
        bot2WalletText.text = $"Wallet: ${GameManager.instance.bot2Wallet}";

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
