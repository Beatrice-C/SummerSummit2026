using UnityEngine;
using CardHouse;

public class CheatTrigger : MonoBehaviour
{
    private CardHouse.Card currentCardComponent;
    private FreeDrawPokerBridge bridge;

    private void Awake()
    {
        currentCardComponent = GetComponent<CardHouse.Card>();
    }

    private void OnMouseOver()
    {
        if (currentCardComponent == null || currentCardComponent.Group != GameManager.instance.dealer.playerHand)
            return;

        if (bridge == null)
            bridge = FindFirstObjectByType<FreeDrawPokerBridge>();

        if (bridge != null)
            bridge.ShowCheatPrompt(this.gameObject);
    }

    private void OnMouseExit()
    {
        if (bridge == null)
            bridge = FindFirstObjectByType<FreeDrawPokerBridge>();

        if (bridge != null)
            bridge.HideCheatPrompt();
    }
}
