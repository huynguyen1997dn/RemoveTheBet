using UnityEngine;
using UnityEngine.EventSystems;

public class CellClickHandler : MonoBehaviour, IPointerClickHandler
{
    public int tapeId;

    public void OnPointerClick(PointerEventData eventData)
    {

        if (TapeGameController.Instance != null)
        {
            TapeGameController.Instance.OnTapeTapped(tapeId);
        }
    }
}
