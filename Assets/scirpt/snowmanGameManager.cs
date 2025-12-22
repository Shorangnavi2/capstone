using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SnowmanGameManager : MonoBehaviour
{
    // 싱글톤
    public static SnowmanGameManager Instance { get; private set; }

    [Header("=== Snowman Creation Tracking ===")]
    public SnowballShadowTracker_OpenCVPlus shadowTracker;
    public int totalSnowballsToMake;
    public int completedSnowballs = 0;

    [Range(0, 100)]
    public float currentSnowballProgress = 0f;

    private bool isCurrentSnowballHandled = false; 
    private float previousSnowballProgress = 0f; 
    private float progressIncreaseRate = 0f; 
    
    // --- 부드러운 애니메이션을 위한 변수 ---
    private float progressAnimatorValue = 0f; 
    [Tooltip("애니메이터 파라미터가 목표값으로 도달하는 속도.")]
    public float animationSmoothingSpeed = 5f; 

    [Header("=== Animation & Audio ===")]
    public Animator progressAnimator; 
    public string progressIncreaseParamName = "OnProgressIncrease"; 
    public string snowballCompletionFloatParamName = "SnowballFinalizeProgress";
    public AudioSource progressIncreaseAudioSource; 
    public float minProgressIncreaseThreshold = 0.5f; 

    [Header("=== Object Movement Based on Progress (Local Position) ===")]
    public Transform progressIndicatorObject; 
    public float startXPosition = -5f; 
    public float endXPosition = 5f; 
    [Tooltip("오브젝트가 목표 위치로 도달하는 속도 (값이 클수록 즉각적/빠름)")]
    public float movementSmoothingSpeed = 3f; 


    [Header("=== 눈덩이 단계 오브젝트 설정 ===")]
    public GameObject[] snowballStageObjects;

    [Header("=== 씬 이름 설정 ===")]
    // ❌ creationSceneName 삭제
    public string resultSceneName = "ResultScene";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        SceneManager.sceneLoaded += OnSceneLoaded;

        HideAllSnowballStages();
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ❌ creationSceneName 비교 로직 삭제 (이전: if (scene.name == creationSceneName))
        
        // 새로운 씬이 로드될 때마다 트래커를 찾아 초기화를 시도합니다.
        SnowballShadowTracker_OpenCVPlus newTracker = FindObjectOfType<SnowballShadowTracker_OpenCVPlus>();
        
        if (newTracker != null)
        {
            // 트래커가 발견되면 해당 씬이 생성(Creation) 씬이라고 가정하고 초기화합니다.
            InitializeCreationScene(newTracker);
        }
        else
        {
            // 트래커가 없으면 연결을 해제하고 초기화하지 않습니다.
            shadowTracker = null;
            
            // 씬의 종류와 관계없이 트래커가 없음을 경고합니다.
            Debug.LogWarning($"[SnowmanGameManager] SnowballShadowTracker_OpenCVPlus instance not found in scene: {scene.name}.");
        }
    }


    private void Update()
    {
        if (shadowTracker != null)
        {
            float newProgress = shadowTracker.currentSnowballProgress;
            
            if (Time.deltaTime > 0)
            {
                progressIncreaseRate = (newProgress - previousSnowballProgress) / Time.deltaTime; 
            }
            else
            {
                progressIncreaseRate = 0f;
            }
            
            // 2. 애니메이터 부드러운 값 업데이트
            progressAnimatorValue = Mathf.Lerp(
                progressAnimatorValue, 
                progressIncreaseRate, 
                Time.deltaTime * animationSmoothingSpeed
            );
            
            UpdateProgressAnimator(progressAnimatorValue);
            HandleProgressSound(progressIncreaseRate); 
            
            previousSnowballProgress = newProgress;
            currentSnowballProgress = newProgress; 

            MoveProgressIndicatorObject(currentSnowballProgress);
            
            UpdateSnowballFinalizeProgress(currentSnowballProgress); 
            
            if (!isCurrentSnowballHandled && IsCurrentSnowballComplete())
            {
                isCurrentSnowballHandled = true;
                OnSnowballCompleted(); 
            }
        }
        else
        {
            // 트래커가 없을 때 애니메이션 값 0으로 부드럽게 조정
            progressAnimatorValue = Mathf.Lerp(
                progressAnimatorValue, 
                0f, 
                Time.deltaTime * animationSmoothingSpeed * 2f
            );
            UpdateProgressAnimator(progressAnimatorValue);
            
            if (progressAnimator != null)
            {
                progressAnimator.SetFloat(snowballCompletionFloatParamName, 0f); 
            }
        }
    }
    
    private void UpdateProgressAnimator(float rate)
    {
        if (progressAnimator != null)
        {
            progressAnimator.SetFloat(progressIncreaseParamName, rate); 
        }
    }
    
    private void HandleProgressSound(float rate)
    {
        if (rate > 0 && 
            rate >= minProgressIncreaseThreshold && 
            progressIncreaseAudioSource != null && 
            !progressIncreaseAudioSource.isPlaying)
        {
            progressIncreaseAudioSource.Play();
        }
    }

    private void UpdateSnowballFinalizeProgress(float progress)
    {
        if (progressAnimator == null) return;

        const float startProgress = 90f;
        const float endProgress = 100f;

        float finalizeValue = Mathf.Clamp01((progress - startProgress) / (endProgress - startProgress));
        
        progressAnimator.SetFloat(snowballCompletionFloatParamName, finalizeValue);
    }

    private void MoveProgressIndicatorObject(float progress)
    {
        if (progressIndicatorObject == null) return;

        float normalizedProgress = Mathf.Clamp01(progress / 100f);
        float targetX = Mathf.Lerp(startXPosition, endXPosition, normalizedProgress);

        Vector3 currentLocalPosition = progressIndicatorObject.localPosition; 
        Vector3 targetLocalPosition = new Vector3(targetX, currentLocalPosition.y, currentLocalPosition.z);

        progressIndicatorObject.localPosition = Vector3.Lerp( 
            currentLocalPosition, 
            targetLocalPosition, 
            Time.deltaTime * movementSmoothingSpeed
        );
        
        Vector3 positionAfterMove = progressIndicatorObject.localPosition;
        Vector3 currentLocalScale = progressIndicatorObject.localScale;

        if (Mathf.Abs(positionAfterMove.x - endXPosition) < 0.01f)
        {
            if (currentLocalScale.x > 0) 
            {
                progressIndicatorObject.localScale = new Vector3(-1f, currentLocalScale.y, currentLocalScale.z);
            }
        }
    }
    
    public void InitializeCreationScene(SnowballShadowTracker_OpenCVPlus tracker)
    {
        shadowTracker = tracker;

        if (shadowTracker != null)
        {
            shadowTracker.ResetProgress(); 
            currentSnowballProgress = 0f;
        }
        
        previousSnowballProgress = 0f; 
        progressIncreaseRate = 0f;
        progressAnimatorValue = 0f; 
        UpdateProgressAnimator(0f); 
        
        if (progressAnimator != null)
        {
            progressAnimator.SetFloat(snowballCompletionFloatParamName, 0f); 
        }

        ResetIndicatorPosition();
        
        isCurrentSnowballHandled = false;
    }

    public bool IsCurrentSnowballComplete()
    {
        if (shadowTracker == null) return false;
        return shadowTracker.currentSnowballProgress >= 100f; 
    }

    public void OnSnowballCompleted()
    {
        if (!IsCurrentSnowballComplete())
        {
            return;
        }

        completedSnowballs++;

        ShowSnowballStage(completedSnowballs - 1);

        if (completedSnowballs < totalSnowballsToMake)
        {
            if (shadowTracker != null)
            {
                shadowTracker.ResetProgress(); 
                currentSnowballProgress = 0f;
            }
            
            previousSnowballProgress = 0f; 
            progressIncreaseRate = 0f;
            progressAnimatorValue = 0f; 
            UpdateProgressAnimator(0f);
            if (progressAnimator != null)
            {
                progressAnimator.SetFloat(snowballCompletionFloatParamName, 0f); 
            }
            
            ResetIndicatorPosition();
            
            isCurrentSnowballHandled = false;
        }
        else
        {
            GoToResultScene();
        }
    }
    
    public void ResetCurrentSnowball()
    {
        if (shadowTracker != null)
        {
            shadowTracker.ResetProgress();
        }
        currentSnowballProgress = 0f;
        previousSnowballProgress = 0f; 
        progressIncreaseRate = 0f;
        progressAnimatorValue = 0f;
        UpdateProgressAnimator(0f);
        
        if (progressAnimator != null)
        {
            progressAnimator.SetFloat(snowballCompletionFloatParamName, 0f); 
        }
        
        ResetIndicatorPosition();
        
        isCurrentSnowballHandled = false;
    }

    private void ResetIndicatorPosition()
    {
        if (progressIndicatorObject != null)
        {
            Vector3 currentLocalPos = progressIndicatorObject.localPosition;
            progressIndicatorObject.localPosition = new Vector3(startXPosition, currentLocalPos.y, currentLocalPos.z);
            
            Vector3 currentLocalScale = progressIndicatorObject.localScale;
            if (currentLocalScale.x != 1f)
            {
                 progressIndicatorObject.localScale = new Vector3(1f, currentLocalScale.y, currentLocalScale.z);
            }
        }
    }

    private void HideAllSnowballStages()
    {
        if (snowballStageObjects == null) return;

        foreach (var obj in snowballStageObjects)
        {
            if (obj != null) obj.SetActive(false);
        }
    }

    private void ShowSnowballStage(int index)
    {
        if (snowballStageObjects == null || snowballStageObjects.Length == 0)
        {
            return;
        }

        if (index < 0 || index >= snowballStageObjects.Length)
        {
            return;
        }

        for (int i = 0; i < snowballStageObjects.Length; i++)
        {
            if (snowballStageObjects[i] != null)
            {
                snowballStageObjects[i].SetActive(i <= index); 
            }
        }
    }

    public void GoToResultScene()
    {
        if (string.IsNullOrEmpty(resultSceneName))
        {
            return;
        }
        
        completedSnowballs = 0;

        if (shadowTracker != null)
        {
            shadowTracker.ResetProgress(); 
        }
        currentSnowballProgress = 0f;

        SceneManager.LoadScene(resultSceneName);
    }
}