using UnityEngine;
using FreeDraw;

public class CustomBrushColours : MonoBehaviour
{
    public DrawingSettings defaultSettings;

    public static int markerWidth = 2;

    public static int eraserWidth = 4;

    private void Start()
    {
        // red marker set up
        defaultSettings.SetMarkerWidth(markerWidth);
    }

    public void SetPenRed()
    {
        Color c = Color.red;
        c.a = defaultSettings.Transparency;
        defaultSettings.SetMarkerColour(c);
        defaultSettings.SetMarkerWidth(markerWidth);
        Drawable.drawable.SetPenBrush();
    }

    public void SetPenBlack()
    {
        Color c = Color.black;
        c.a = defaultSettings.Transparency;
        defaultSettings.SetMarkerColour(c);
        defaultSettings.SetMarkerWidth(markerWidth);
        Drawable.drawable.SetPenBrush();
    }

    public void SetPenWhite()
    {
        Color c = Color.white;
        c.a = defaultSettings.Transparency;
        defaultSettings.SetMarkerColour(c);
        defaultSettings.SetMarkerWidth(eraserWidth);
        Drawable.drawable.SetPenBrush();
    }
}
