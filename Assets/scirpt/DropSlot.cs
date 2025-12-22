using UnityEngine;
using UnityEngine.EventSystems;

public class DropSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("OnDrop : " + name);

        if (eventData.pointerDrag != null)
        {
            RectTransform draggedRect = eventData.pointerDrag.GetComponent<RectTransform>();
            RectTransform myRect      = GetComponent<RectTransform>();

            // 드롭된 아이템 위치를 이 슬롯 위치로 고정
            draggedRect.anchoredPosition = myRect.anchoredPosition;
        }
    }
}
