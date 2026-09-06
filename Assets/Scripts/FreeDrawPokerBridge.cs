using System.IO;
using UnityEngine;
using CardHouse;
using FreeDraw;

public class FreeDrawPokerBridge : MonoBehaviour
{
    public Drawable drawableCanvas;
    public GameObject blankCardPrefab;

    public void SwapFreeDrawCardIntoHand()
    {
        CardGroup playerHandGroup = GameManager.instance.dealer.playerHand;

        if (playerHandGroup == null || playerHandGroup.MountedCards.Count == 0)
        {
            Debug.LogWarning("You have no cards in your hand to swap!");
            return;
        }

        SpriteRenderer canvasRenderer = drawableCanvas.GetComponent<SpriteRenderer>();
        if (canvasRenderer == null || canvasRenderer.sprite == null)
        {
            Debug.LogWarning("No SpriteRenderer in FreeDraw canvas!");
            return;
        } 

        Texture2D drawnTexture = canvasRenderer.sprite.texture;
        byte[] pngBytes = drawnTexture.EncodeToPNG();

        string directoryPath = Path.Combine(Application.dataPath, "../CheatedCards/");
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        string finalFile = Path.Combine(directoryPath, "player_forgery.png");
        File.WriteAllBytes(finalFile, pngBytes);
        Debug.Log($"PNG Exported to {finalFile}");

        CardHouse.Card cardToReplace = playerHandGroup.MountedCards[0]; // i think the player has to choose this?
        playerHandGroup.MountedCards.Remove(cardToReplace);
        Destroy(cardToReplace.gameObject);

        GameObject cheatCardClone = Instantiate(blankCardPrefab);
        CardHouse.Card newCardComponent = cheatCardClone.GetComponent<CardHouse.Card>();
        PokerCard pokerDataScript = cheatCardClone.GetComponent<PokerCard>();

        Texture2D runtimeTexture = new Texture2D(drawnTexture.width, drawnTexture.height);
        runtimeTexture.LoadImage(pngBytes);

        Sprite dynamicDrawingSprite = Sprite.Create(
            runtimeTexture,
            new Rect(0, 0, runtimeTexture.width, runtimeTexture.height),
            new Vector2(0.5f, 0.5f),
            150f
        );

        if (pokerDataScript != null && pokerDataScript.Image != null)
        {
            pokerDataScript.Image.sprite = dynamicDrawingSprite;

            System.Reflection.PropertyInfo rankProp = typeof(PokerCard).GetProperty("Rank");
            System.Reflection.PropertyInfo suitProp = typeof(PokerCard).GetProperty("Suit");

            // temp poker data
            if (rankProp != null)
                rankProp.SetValue(pokerDataScript, 14);
            
            if (suitProp != null)
                suitProp.SetValue(pokerDataScript, PokerSuit.Spades);
            
            Debug.Log($"Card read in as {pokerDataScript.Rank}, {pokerDataScript.Suit.ToString()}");
        }

        playerHandGroup.Mount(newCardComponent);
        newCardComponent.SetFacing(CardFacing.FaceUp);

        playerHandGroup.OnGroupChanged?.Invoke();

        Debug.Log("Swapped");

        //if (Showdown.IsCardDuplicateOnTable(newCardComponent))
        //    GameManager.instance.dealer.Invoke("TriggerFraudOver", 0.1f);
    }
}
