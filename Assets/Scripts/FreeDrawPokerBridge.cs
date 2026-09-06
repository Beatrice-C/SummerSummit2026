using System.IO;
using UnityEngine;
using CardHouse;
using FreeDraw;
using TMPro;

public class FreeDrawPokerBridge : MonoBehaviour
{
    public Drawable drawableCanvas;
    public GameObject blankCardPrefab;

    public TextMeshProUGUI hoverPromptText;
    private CardHouse.Card cardToReplace;

    public float animationSpeed = 4f;
    private Vector3 onScreenPos = new Vector3(0f, 0f, -2f);
    private Vector3 offScreenPos = new Vector3(0f, -12f, -2f);

    private Coroutine activeAnimationCoroutine;

    private void Update()
    {
        if (hoverPromptText != null&& hoverPromptText.gameObject.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (cardToReplace != null && !drawableCanvas.gameObject.activeSelf)
                TriggerDrawing(cardToReplace);
        }

    }

    public void SwapFreeDrawCardIntoHand()
    {
        CardGroup playerHandGroup = GameManager.instance.dealer.playerHand;

        if (playerHandGroup == null || cardToReplace == null || !playerHandGroup.MountedCards.Contains(cardToReplace))
        {
            Debug.LogWarning("No card selected in your hand to swap!");
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

            // TODO: Set value of drawn poker card to be what ai interprets
            if (rankProp != null)
                rankProp.SetValue(pokerDataScript, 14);
            
            if (suitProp != null)
                suitProp.SetValue(pokerDataScript, PokerSuit.Spades);
            
            Debug.Log($"Card read in as {pokerDataScript.Rank}, {pokerDataScript.Suit.ToString()}");
        }

        playerHandGroup.Mount(newCardComponent);
        newCardComponent.SetFacing(CardFacing.FaceUp);

        playerHandGroup.OnGroupChanged?.Invoke();

        cardToReplace = null;
        
        if (activeAnimationCoroutine != null)
            StopCoroutine(activeAnimationCoroutine);

        activeAnimationCoroutine = StartCoroutine(SlideCanvasAnimation(drawableCanvas.transform.position, offScreenPos, false));

        // TODO: Duplicate card flipped over
        //if (Showdown.IsCardDuplicateOnTable(newCardComponent))
        //    GameManager.instance.dealer.Invoke("TriggerFraudOver", 0.1f);
    }

    private void TriggerDrawing(CardHouse.Card selectedCard)
    {
        cardToReplace = selectedCard;

        drawableCanvas.gameObject.SetActive(true);
        hoverPromptText.gameObject.SetActive(false);

        if (drawableCanvas != null)
        {
            Drawable drawableScript = drawableCanvas.GetComponent<Drawable>();
            if (drawableScript != null)
            {
                drawableScript.ResetCanvas();
            }

            if (activeAnimationCoroutine != null)
                StopCoroutine(activeAnimationCoroutine);
            
            Vector3 targetCardPosition = selectedCard.transform.position;
            Vector3 dynamicOnScreenPos = new Vector3(targetCardPosition.x, targetCardPosition.y, -2f);

            activeAnimationCoroutine = StartCoroutine(SlideCanvasAnimation(offScreenPos, dynamicOnScreenPos, true));
        }
    }

    private System.Collections.IEnumerator SlideCanvasAnimation(Vector3 startPos, Vector3 endPos, bool onOff)
    {
        if (onOff)
        {
            drawableCanvas.transform.position = startPos;
            drawableCanvas.gameObject.SetActive(true);

            var col = drawableCanvas.GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;
        }

        float timeTracker = 0f;
        while (timeTracker < 1f)
        {
            timeTracker += Time.deltaTime * animationSpeed;
            drawableCanvas.transform.position = Vector3.Lerp(startPos, endPos, timeTracker);
            yield return null;
        }

        drawableCanvas.transform.position = endPos;

        if (onOff)
        {
            var col = drawableCanvas.GetComponent<Collider2D>();
            if (col != null)
                col.enabled = true;
        }
        else
        {
            drawableCanvas.gameObject.SetActive(false);
        }
    }

    public void ShowCheatPrompt(GameObject targetedCardObject)
    {
        if (drawableCanvas != null && drawableCanvas.gameObject.activeSelf)
            return;

        cardToReplace = targetedCardObject.GetComponent<CardHouse.Card>();

        if (hoverPromptText != null && cardToReplace != null)
        {
            hoverPromptText.gameObject.SetActive(true);

            Vector3 worldPos = targetedCardObject.transform.position;
            hoverPromptText.transform.position = Camera.main.WorldToScreenPoint(worldPos + new Vector3(0, 1.2f, 0));
            hoverPromptText.text = "Click to draw over this card!";
        }
    }

    public void HideCheatPrompt()
    {
        if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))
            return;
        
        if (hoverPromptText != null) 
            hoverPromptText.gameObject.SetActive(false);
    }
}
