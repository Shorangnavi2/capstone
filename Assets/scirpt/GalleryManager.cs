using UnityEngine;
using UnityEngine.UI; // UI 요소를 사용하기 위해 필요
using System.IO; // 파일 시스템 접근을 위해 필요
using System.Collections.Generic; // List를 사용하기 위해 필요

public class GalleryManager : MonoBehaviour
{
    // 이미지를 표시할 RawImage 컴포넌트
    [Header("UI Component")]
    public RawImage galleryImage;

    // 로드된 이미지 파일 (Texture2D) 배열
    private Texture2D[] textures; 
    
    private int currentIndex = 0; // 현재 보여지고 있는 이미지의 인덱스

    // Start 메서드: 스크립트가 시작될 때 이미지 로드 및 초기 설정
    void Start()
    {
        // CaptureCanvasManager와 동일한 영구 저장 경로를 사용합니다.
        string directoryPath = Path.Combine(Application.persistentDataPath, "SnowmanCaptures"); 

        // 폴더가 존재하는지 확인
        if (Directory.Exists(directoryPath))
        {
            // "snowman_숫자.png" 패턴의 모든 PNG 파일 리스트를 가져옵니다.
            string[] filePaths = Directory.GetFiles(directoryPath, "snowman_*.png"); 

            List<Texture2D> loadedTextures = new List<Texture2D>();

            // 파일 경로를 기반으로 Texture2D 로드
            foreach (string filePath in filePaths)
            {
                // .meta 파일과 같은 불필요한 파일 제외
                if (Path.GetExtension(filePath).ToLower() != ".png") continue;
                
                try
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(fileData))
                    {
                        loadedTextures.Add(tex);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[GalleryManager] 이미지 파일 로드 중 오류 발생 ({filePath}): {e.Message}");
                }
            }
            
            // 로드된 텍스처를 배열에 저장합니다.
            textures = loadedTextures.ToArray();
                                                
            if (textures.Length > 0)
            {
                currentIndex = 0;
                UpdateImage();
                Debug.Log($"[GalleryManager] 총 {textures.Length}개의 이미지 로드 완료. 경로: {directoryPath}");
            }
            else
            {
                Debug.LogWarning($"[GalleryManager] 저장 경로에서 이미지를 찾을 수 없습니다: {directoryPath}");
            }
        }
        else
        {
            Debug.LogWarning($"[GalleryManager] 저장 디렉토리가 존재하지 않습니다: {directoryPath}");
        }
    }
    
    // === 버튼에 연결할 공개 메서드 ===

    /// <summary>
    /// 다음 이미지를 표시합니다.
    /// </summary>
    public void ShowNextImage()
    {
        if (textures == null || textures.Length == 0) return;

        currentIndex++;
        if (currentIndex >= textures.Length)
        {
            currentIndex = 0;
        }

        UpdateImage();
    }

    /// <summary>
    /// 이전 이미지를 표시합니다.
    /// </summary>
    public void ShowPreviousImage()
    {
        if (textures == null || textures.Length == 0) return;

        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = textures.Length - 1;
        }

        UpdateImage();
    }
    
    /// <summary>
    /// 현재 인덱스에 해당하는 이미지를 RawImage에 표시하고, NativeSize 설정을 제거하여 UI 영역에 맞춰 확대합니다.
    /// </summary>
    private void UpdateImage()
    {
        if (galleryImage != null && textures != null && textures.Length > 0)
        {
            galleryImage.texture = textures[currentIndex];
            
            // **[수정]** SetNativeSize()를 제거하여 RawImage의 RectTransform 크기에 맞춰 확대됩니다.
            // (Unity Editor에서 RawImage의 Anchor Preset이 Stretch로 설정되어 있어야 합니다.)
        }
    }
    
    // 갤러리 로딩을 재실행해야 할 경우 외부에서 호출 가능
    public void ReloadGallery()
    {
        Start(); 
    }
}