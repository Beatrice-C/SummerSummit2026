using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BettingUI : MonoBehaviour
{
    public Slider betSlider;
    public TextMeshProUGUI betAmountText;
    public Button submitButton;
    public GameObject betBox;

    public int minimumBet = 5;
    public int defaultBet = 150;

    private void Start()
    {
        betSlider.wholeNumbers = true;
        betSlider.minValue = minimumBet;
        betSlider.maxValue = GameManager.instance.playerWallet;
        betSlider.value = defaultBet;

        betSlider.onValueChanged.AddListener(ShowBetAmount);
        submitButton.onClick.AddListener(ConfirmBet);

        ShowBetAmount(betSlider.value);
    }

    private void Update()
    {
        // only ask for a bet once the player can see what they were dealt
        betBox.SetActive(GameManager.instance.currentPhase == PokerPhase.PlacingBet);
    }

    private void ShowBetAmount(float amount)
    {
        betAmountText.text = $"${Mathf.RoundToInt(amount)}";
    }

    public void ConfirmBet()
    {
        GameManager.instance.PlaceBet(Mathf.RoundToInt(betSlider.value));
    }
}
