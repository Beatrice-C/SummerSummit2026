using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using CardHouse;
using FreeDraw;
using TMPro;

public class FreeDrawPokerBridge : MonoBehaviour
{
    public Drawable drawableCanvas;
    public GameObject blankCardPrefab;

    public Texture2D blankCardTexture;
    public TextMeshProUGUI hoverPromptText;
    private CardHouse.Card cardToReplace;

    public float animationSpeed = 4f;
    private Vector3 onScreenPos = new Vector3(0f, 0f, -2f);
    private Vector3 offScreenPos = new Vector3(0f, -12f, -2f);

    private Coroutine activeAnimationCoroutine;

    private bool hoveredCardIsLocked;

    private bool canvasReady;

    private void Update()
    {
        if (drawableCanvas.gameObject.activeSelf)
        {
            // if the canvas is open and they click outside it, close it without swapping the card
            if (canvasReady && Input.GetMouseButtonDown(0) && !IsPointerOverCanvas() && !IsPointerOverUI())
            {
                CancelDrawing();
            }
        }
        // if the canvas is closed and they click on a card, open the canvas to draw over it
        else if (hoverPromptText != null && hoverPromptText.gameObject.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (cardToReplace != null && !hoveredCardIsLocked)
                TriggerDrawing(cardToReplace);
        }
    }

    private bool IsPointerOverCanvas()
    {
        Vector2 pointerWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return Physics2D.OverlapPoint(pointerWorldPos, drawableCanvas.Drawing_Layers.value) != null;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // closes the canvas so the drawing is discarded
    private void CancelDrawing()
    {
        canvasReady = false;
        cardToReplace = null;

        if (hoverPromptText != null)
            hoverPromptText.gameObject.SetActive(false);

        if (activeAnimationCoroutine != null)
            StopCoroutine(activeAnimationCoroutine);

        activeAnimationCoroutine = StartCoroutine(SlideCanvasAnimation(drawableCanvas.transform.position, offScreenPos, false));
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

        // if the read fails at showdown the player falls back to the card they drew over
        PokerCard replacedData = cardToReplace.GetComponent<PokerCard>();
        int fallbackRank = replacedData != null ? replacedData.Rank : 2;
        PokerSuit fallbackSuit = replacedData != null ? replacedData.Suit : PokerSuit.Clubs;

        // unmount hands back the slot so the forgery can take the same place in the hand
        int? slot = playerHandGroup.UnMount(cardToReplace);

        // only true when they drew over an earlier forgery, texture is now safe to release
        bool replacedPreviousForgery = Showdown.instance != null && Showdown.instance.forgedCardIndex == slot;

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
            225f
        );

        if (pokerDataScript != null)
        {
            // write down og card data but will be replaced by the API result if it succeeds
            PokerCardDefinition drawnDefinition = ScriptableObject.CreateInstance<PokerCardDefinition>();
            drawnDefinition.Rank = fallbackRank;
            drawnDefinition.Suit = fallbackSuit;
            drawnDefinition.Art = dynamicDrawingSprite;

            pokerDataScript.Apply(drawnDefinition);
            Destroy(drawnDefinition);

            Debug.Log($"Card read in as {pokerDataScript.Rank}, {pokerDataScript.Suit.ToString()}");
        }

        playerHandGroup.Mount(newCardComponent, slot);
        newCardComponent.SetFacing(CardFacing.FaceUp);

        if (Showdown.instance != null)
        {
            // destroy og texture if they drew over a previous forgery, so the new one can take its place
            if (Showdown.instance.forgedCardTexture != null && replacedPreviousForgery)
                Destroy(Showdown.instance.forgedCardTexture);

            Showdown.instance.forgedCardTexture = runtimeTexture;
            Showdown.instance.forgedCardIndex = playerHandGroup.MountedCards.IndexOf(newCardComponent);

            Debug.Log($"Forgery placed in hand slot {Showdown.instance.forgedCardIndex}");
        }

        cardToReplace = null;

        if (activeAnimationCoroutine != null)
            StopCoroutine(activeAnimationCoroutine);

        activeAnimationCoroutine = StartCoroutine(SlideCanvasAnimation(drawableCanvas.transform.position, offScreenPos, false));
    }

    private void TriggerDrawing(CardHouse.Card selectedCard)
    {
        cardToReplace = selectedCard;

        drawableCanvas.gameObject.SetActive(true);
        hoverPromptText.gameObject.SetActive(false);

        if (drawableCanvas != null)
        {
            ResetCanvas();

            if (activeAnimationCoroutine != null)
                StopCoroutine(activeAnimationCoroutine);
            
            Vector3 targetCardPosition = selectedCard.transform.position;
            Vector3 dynamicOnScreenPos = new Vector3(targetCardPosition.x, targetCardPosition.y, -2f);

            activeAnimationCoroutine = StartCoroutine(SlideCanvasAnimation(offScreenPos, dynamicOnScreenPos, true));
        }
    }

    private System.Collections.IEnumerator SlideCanvasAnimation(Vector3 startPos, Vector3 endPos, bool onOff)
    {
        // not interactable while it's moving
        canvasReady = false;

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

            canvasReady = true;
        }
        else
        {
            drawableCanvas.gameObject.SetActive(false);
            canvasReady = false;
        }
    }

    public void ShowCheatPrompt(GameObject targetedCardObject)
    {
        if (drawableCanvas != null && drawableCanvas.gameObject.activeSelf)
            return;

        // no drawing while the cards are still being dealt, or once the window has closed
        if (!GameManager.instance.DrawingAllowed)
            return;

        cardToReplace = targetedCardObject.GetComponent<CardHouse.Card>();

        if (hoverPromptText != null && cardToReplace != null)
        {
            hoverPromptText.gameObject.SetActive(true);

            Vector3 worldPos = targetedCardObject.transform.position;
            hoverPromptText.transform.position = Camera.main.WorldToScreenPoint(worldPos + new Vector3(0, 1.2f, 0));
            hoverPromptText.text = GetCheatPromptText(cardToReplace);
        }
    }

    // only one card per hand can be forged but that one can be redrawn as often as they like
    private string GetCheatPromptText(CardHouse.Card hoveredCard)
    {
        int forgedIndex = Showdown.instance != null ? Showdown.instance.forgedCardIndex : -1;
        hoveredCardIsLocked = false;

        // change hover text depending on if forged a card or not
        if (forgedIndex < 0)
            return "Click to draw over this card!";

        if (GameManager.instance.dealer.playerHand.MountedCards.IndexOf(hoveredCard) == forgedIndex)
            return "Click to redraw this card!";

        hoveredCardIsLocked = true;
        return "You've already forged a card.";
    }

    public void HideCheatPrompt()
    {
        if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))
            return;
        
        if (hoverPromptText != null) 
            hoverPromptText.gameObject.SetActive(false);
    }

    // called by GameManager when the drawing window closes, just before showdown
    public void FinishDrawing()
    {
        if (!drawableCanvas.gameObject.activeSelf)
            return;

        // if blank canvas, discard the drawing and keep the original card
        if (IsCanvasBlank())
        {
            Debug.Log("Drawing time ran out on a blank canvas, keeping the original card.");
            CancelDrawing();
        }
        else
        {
            SwapFreeDrawCardIntoHand();
        }
    }

    private Texture2D CanvasTexture()
    {
        SpriteRenderer canvasRenderer = drawableCanvas.GetComponent<SpriteRenderer>();
        return canvasRenderer != null && canvasRenderer.sprite != null ? canvasRenderer.sprite.texture : null;
    }

    private void ResetCanvas()
    {
        Texture2D canvasTexture = CanvasTexture();

        if (canvasTexture == null)
            return;

        if (blankCardTexture == null || blankCardTexture.width != canvasTexture.width || blankCardTexture.height != canvasTexture.height)
        {
            Debug.LogWarning("[FORGERY] Blank card texture is missing or a different size to the canvas, falling back to a flat wipe.");
            drawableCanvas.ResetCanvas();
            return;
        }

        canvasTexture.SetPixels(blankCardTexture.GetPixels());
        canvasTexture.Apply();
    }

    // blank means the canvas still matches the card-shaped art it was reset to
    private bool IsCanvasBlank()
    {
        Texture2D canvasTexture = CanvasTexture();

        if (canvasTexture == null)
            return true;

        if (blankCardTexture == null || blankCardTexture.width != canvasTexture.width || blankCardTexture.height != canvasTexture.height)
            return false;

        Color32[] blank = blankCardTexture.GetPixels32();
        Color32[] pixels = canvasTexture.GetPixels32();

        for (int i = 0; i < pixels.Length; i++)
        {
            // compared per channel because Color32 has no == overload and falls back to reflection
            if (pixels[i].r != blank[i].r || pixels[i].g != blank[i].g || pixels[i].b != blank[i].b || pixels[i].a != blank[i].a)
                return false;
        }

        return true;
    }
}
