using UnityEngine;
using UnityEngine.EventSystems;

public class DropTarget : MonoBehaviour, IDropHandler
{
    // 이 드롭 영역이 아이템을 삭제(쓰레기통)하는 곳인지 여부
    [SerializeField] public bool isTrashCan = false; 

    // DropTarget의 이름 (디버깅용)
    [SerializeField] private string targetName = "Normal Slot";

    public void OnDrop(PointerEventData eventData)
    {
        DragItem dragItem = eventData.pointerDrag.GetComponent<DragItem>();

        if (dragItem != null)
        {
            // DropTarget이 감지되었음을 DragItem에 알립니다.
            // isTrashCan 값과 함께 전달합니다.
            dragItem.SetDropSuccess(true, isTrashCan);
            
            if (isTrashCan)
            {
                Debug.Log($"🗑️ {eventData.pointerDrag.name}이/가 쓰레기통에 드롭되었습니다. 삭제 준비 완료.");
            }
            else
            {
                // 일반 슬롯에 드롭된 경우
                Debug.Log($"✅ {eventData.pointerDrag.name}이/가 {targetName}에 성공적으로 드롭되어 유지됩니다.");
            }
        }
    }
}