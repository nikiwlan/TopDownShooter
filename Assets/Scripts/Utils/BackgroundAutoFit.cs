using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BackgroundAutoFit : MonoBehaviour
{
    private Image img;
    private RectTransform rt;

    void Awake()
    {
        img = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        FitBackground();
    }

    private void FitBackground()
    {
        if (img.sprite == null) return;

        float screenRatio = (float)Screen.width / Screen.height;
        float imageRatio = (float)img.sprite.texture.width / img.sprite.texture.height;

        if (screenRatio > imageRatio)
        {
            // Bildschirm breiter als Bild → Zoom in die Höhe
            rt.localScale = new Vector3(screenRatio / imageRatio, 1, 1);
        }
        else
        {
            // Bildschirm höher als Bild → Zoom in die Breite
            rt.localScale = new Vector3(1, imageRatio / screenRatio, 1);
        }
    }
}
