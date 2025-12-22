using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))] 
[RequireComponent(typeof(CanvasGroup))] 
public class DragItem : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerUpHandler
{
    // ... (Awake, OnPointerDown, OnDrag, GetAngleToCenter 등 기존 코드 유지)
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private Vector2 pointerOffset; 
    
    private bool isCopy = false;            
    private bool isSourceItem = true;       
    private bool dropSuccessful = false;    
    private bool shouldBeDestroyed = false; 
    private Transform originalParent = null; 

    private bool isRotating = false;
    private float initialRotationAngle; 
    private float initialObjectRotationZ; 
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 0.2f;

    public void SetDropSuccess(bool success, bool isTrash)
    {
        dropSuccessful = success;
        shouldBeDestroyed = isTrash;
    }
    
    // ... (Awake, OnPointerDown, OnDrag, GetAngleToCenter 등 기존 코드 유지)
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>(); 
        
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            rootCanvas = parentCanvas.rootCanvas;
        }

        if (rootCanvas == null)
        {
            Debug.LogError(gameObject.name + ": Root Canvas를 찾을 수 없습니다.");
        }
    }
    
    private float GetAngleToCenter(Vector2 mousePosition, Camera eventCamera)
    {
        Vector2 centerScreenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, rectTransform.position);
        Vector2 direction = mousePosition - centerScreenPoint;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            isRotating = true;
            initialRotationAngle = GetAngleToCenter(eventData.position, eventData.pressEventCamera);
            initialObjectRotationZ = rectTransform.localEulerAngles.z;
        }
        else 
        {
            isRotating = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, 
                eventData.position, 
                eventData.pressEventCamera, 
                out pointerOffset
            );
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isRotating) return; 
        
        if (!isCopy && isSourceItem)
        {
            originalParent = transform.parent; 
            GameObject copy = Instantiate(gameObject, originalParent);
            copy.name = name + "_Copy"; 
            
            DragItem copyDragItem = copy.GetComponent<DragItem>();
            copyDragItem.isCopy = true;
            copyDragItem.originalParent = originalParent; 
            copyDragItem.isSourceItem = false; 

            eventData.pointerDrag = copy;
            copyDragItem.OnBeginDrag(eventData); 
            return;
        }
        
        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true); 
        }
        transform.SetAsLastSibling(); 

        dropSuccessful = false;     
        shouldBeDestroyed = false;  
        
        Debug.Log($"[DRAG] {name}: 드래그 시작. isCopy={isCopy}.");
        canvasGroup.alpha = 0.6f;          
        canvasGroup.blocksRaycasts = false; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas == null) return;
        
        if (isRotating)
        {
            float currentRotationAngle = GetAngleToCenter(eventData.position, eventData.pressEventCamera);
            float angleDifference = currentRotationAngle - initialRotationAngle;
            float newRotationZ = initialObjectRotationZ + angleDifference;
            rectTransform.localRotation = Quaternion.Euler(0, 0, newRotationZ);
            return;
        }
        
        RectTransform parentRect = transform.parent.GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPointerPosition
        ))
        {
            rectTransform.localPosition = localPointerPosition - pointerOffset;
        }
    }
    
    // =========================================================================
    // IEndDragHandler (최종 삭제 로직)
    // =========================================================================
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isRotating) return; 
        
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true; 
        
        // 🌟🌟🌟 최종 상태 확인 로그 🌟🌟🌟
        Debug.Log($"[END CHECK] {name}: isCopy={isCopy}, shouldBeDestroyed={shouldBeDestroyed}");

        if (isCopy)
        {
            if (shouldBeDestroyed)
            {
                // 이 블록이 실행되면 무조건 삭제되어야 합니다.
                Debug.Log($"[FINAL DELETE] 🗑️ {name}: Destroy 명령 실행! (ID: {gameObject.GetInstanceID()})");
                Destroy(gameObject); 
            }

                
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            isRotating = false;
        }
    }
}