using UnityEngine;
using TMPro;

public class PokerUI : MonoBehaviour
{
    public TextMeshProUGUI potText;
    public TextMeshProUGUI betText;
    public TextMeshProUGUI callText;
    public TextMeshProUGUI raiseText;
    public TextMeshProUGUI playerWalletText;
    public TextMeshProUGUI bot1WalletText;
    public TextMeshProUGUI bot2WalletText;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI timerText;

    public float timeRemaining = 60f;
    private bool isTimerRunning = true;

    public GameObject gameOverPanel;

    private void Update()
    {
        if (GameManager.instance == null)
            return;
        
        potText.text = $"Desk: ${GameManager.instance.pot}";
        playerWalletText.text = $"Wallet: ${GameManager.instance.playerWallet}";
        bot1WalletText.text = $"Wallet: ${GameManager.instance.bot1Wallet}";
        bot2WalletText.text = $"Wallet: ${GameManager.instance.bot2Wallet}";

        betText.text = $"Bet: ${300-GameManager.instance.playerWallet}";

        HandleCountdownTimer();

        int currentCost = GameManager.instance.GetCurrentRoundCost();

        if (GameManager.instance.currentPhase == PokerPhase.Showdown)
        {
            callText.text = "Call";
            raiseText.text = "Raise";
            return;
        }
        else if (currentCost > 0)
        {
            phaseText.text = $"Current Fee: ${currentCost}";
            callText.text = $"Call: ${currentCost}";
            raiseText.text = $"Raise: ${currentCost*2}";
        }
        else
        {
            phaseText.text = "Dealing cards";
        }
    }

    private void HandleCountdownTimer()
    {
        if (!isTimerRunning)
            return;
        
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;

            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
        }
        else
        {
            timeRemaining = 0;
            isTimerRunning = false;
            timerText.text = "0:00";

            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }
}
