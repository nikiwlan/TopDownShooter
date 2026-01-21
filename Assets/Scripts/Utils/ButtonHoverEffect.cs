using UnityEngine;
using UnityEngine.EventSystems; // WICHTIG für UI Events

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Cursor Settings")]
    [Tooltip("Zieh hier deine Cursor-Textur rein")]
    public Texture2D hoverCursor;

    [Tooltip("Wo ist die 'Spitze' des Cursors? (0,0) ist oben links.")]
    public Vector2 hotSpot = Vector2.zero;

    // Wird aufgerufen, wenn die Maus den Button BERÜHRT
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverCursor != null)
        {
            Cursor.SetCursor(hoverCursor, hotSpot, CursorMode.Auto);
        }
    }

    // Wird aufgerufen, wenn die Maus den Button VERLÄSST
    public void OnPointerExit(PointerEventData eventData)
    {
        // Setzt den Cursor zurück auf Standard (null)
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // Sicherheitsnetz: Falls das Menü geschlossen wird, während die Maus noch drauf ist
    void OnDisable()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}