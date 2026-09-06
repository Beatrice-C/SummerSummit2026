using UnityEngine;
using FreeDraw;

public class CustomBrushColours : MonoBehaviour
{
    public DrawingSettings defaultSettings;

    public void SetPenBlack()
    {
        Color c = Color.black;
        c.a = defaultSettings.Transparency;
        defaultSettings.SetMarkerColour(c);
        Drawable.drawable.SetPenBrush();
    }

    public void SetPenWhite()
    {
        Color c = Color.white;
        c.a = defaultSettings.Transparency;
        defaultSettings.SetMarkerColour(c);
        Drawable.drawable.SetPenBrush();
    }
}
