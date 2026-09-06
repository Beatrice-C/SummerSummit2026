using System;
using System.Collections.Generic;
using UnityEngine;
using CardHouse;

public class PokerDealer : MonoBehaviour
{
    public CardGroup deck;
    public List<CardGroup> communitySlots;

    public CardGroup playerHand;
    public CardGroup bot1Hand;
    public CardGroup bot2Hand;

    public float delayBetweenCards = 0.5f;
    public SeekerScriptable<Vector3> dealingStrategy;

    private int currentCommunityIndex = 0;

    public void ResetRound()
    {
        currentCommunityIndex = 0;

        foreach (CardGroup slot in communitySlots)
        {
            Collider2D slotCollider = slot.GetComponent<Collider2D>();

            if (slotCollider != null)
            {
                slotCollider.enabled = false;
            }
        }
    }
    
    public void DealPockets()
    {
        StartCoroutine(AnimatePocketDeal());
    }

    private System.Collections.IEnumerator AnimatePocketDeal()
    {
        yield return new WaitForSeconds(1.5f);

        for (int i = 0; i < 2; i++)
        {
            DealSingleCard(bot1Hand, CardFacing.FaceDown);
            yield return new WaitForSeconds(delayBetweenCards);

            DealSingleCard(playerHand, CardFacing.FaceUp);
            yield return new WaitForSeconds(delayBetweenCards);

            DealSingleCard(bot2Hand, CardFacing.FaceDown);
            yield return new WaitForSeconds(delayBetweenCards);
        }
    }

    public void DealFlop()
    {
        DealCommunityCard();
    }

    public void DealCommunityCard()
    {
        if (currentCommunityIndex >= communitySlots.Count)
        {
            Debug.Log("All community slots are full");
            return;
        }

        CardGroup targetSlot = communitySlots[currentCommunityIndex];

        Collider2D slotCollider = targetSlot.GetComponent<Collider2D>();

        if (slotCollider != null)
        {
            slotCollider.enabled = true;
        }

        DealSingleCard(targetSlot, CardFacing.FaceUp);

        currentCommunityIndex++;
    }

    private void DealSingleCard(CardGroup targetGroup, CardFacing groupFacing)
    {
        CardHouse.Card targetCard = deck.Get();

        if (targetCard != null)
        {
            if (dealingStrategy != null)
            {
                targetGroup.Mount(targetCard, seekerSets: new SeekerSetList { new SeekerSet { Homing = dealingStrategy?.GetStrategy() } });
            }
            else
            {
                targetGroup.Mount(targetCard);
            }

            targetCard.SetFacing(groupFacing);

            Collider2D cardCollider = targetCard.GetComponent<Collider2D>();

            if (cardCollider != null)
            {
                cardCollider.enabled = true;
            }
        }
    }

    public void RevealHand(CardGroup handLayout)
    {
        if (handLayout == null)
            return;
        
        foreach (CardHouse.Card card in handLayout.MountedCards)
        {
            card.SetFacing(CardFacing.FaceUp);
        }
    }

}
