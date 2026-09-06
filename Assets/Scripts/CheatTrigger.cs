using UnityEngine;
using CardHouse;

public class CheatTrigger : MonoBehaviour
{
    private CardHouse.Card currentCardComponent;

    private void Awake()
    {
        currentCardComponent = GetComponent<CardHouse.Card>();
    }

    private void OnMouseEnter()
    {
        if (currentCardComponent != null && currentCardComponent.Group == GameManager.instance.dealer.playerHand)
        {
            FreeDrawPokerBridge bridge = FindFirstObjectByType<FreeDrawPokerBridge>();
            if (bridge != null)
                bridge.ShowCheatPrompt(this.gameObject);
        }
    }

    private void OnMouseExit()
    {
        FreeDrawPokerBridge bridge = FindFirstObjectByType<FreeDrawPokerBridge>();
        if (bridge != null)
            bridge.HideCheatPrompt();
    }
}
