using UnityEngine;
using System.IO;
using System.Collections;
using System.Threading.Tasks;

#if UNITY_EDITOR 
using UnityEditor; 
#endif

public class CaptureCanvasManager : MonoBehaviour
{
    // 캡처할 대상 캔버스
    [Tooltip("캡처할 Canvas의 RectTransform을 연결하세요.")]
    public RectTransform targetCanvasRect;

    // 인스펙터에서 설정 가능한 캡처 너비
    [Tooltip("캔버스 오른쪽에서 캡처할 픽셀 너비입니다.")]
    public int captureWidth = 0; 
    
    // **[추가]** 둥근 모서리 반지름 설정
    [Tooltip("캡처된 이미지에 적용할 모서리 반지름 (픽셀 단위)")]
    public int cornerRadius = 50; 
    
    // 배열 저장 시 사용할 카운터 (자동 증가)
    private int arrayCaptureCounter = 0;
    
    // 덮어쓰기 저장 시 사용할 고정된 파일 이름
    private const string OVERWRITE_FILE_NAME = "current_capture.png";
    
    // **[수정]** 저장 경로 (Application.persistentDataPath 기준)
    private string directoryPath;

    void Awake()
    {
        // **[핵심 수정]** Application.dataPath 대신 Application.persistentDataPath 사용 
        // 빌드된 EXE 환경에서 파일 쓰기가 허용되는 영구 저장 경로입니다.
        directoryPath = Path.Combine(Application.persistentDataPath, "SnowmanCaptures");
        
        // 폴더가 없으면 미리 생성 (InitializeCounter 전에 필요)
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // 카운터 초기화
        InitializeCounter();
    }
    
    // 기존 파일이 있다면 그 다음 번호부터 시작하도록 카운터 초기화
    private void InitializeCounter()
    {
        // 폴더가 생성되었는지 확인 (Awake에서 이미 생성하지만 안전을 위해)
        if (!Directory.Exists(directoryPath)) return;

        // snowman_*.png 패턴으로 파일 검색
        string[] existingFiles = Directory.GetFiles(directoryPath, "snowman_*.png");
        int maxIndex = 0;

        foreach (string file in existingFiles)
        {
            // 파일 이름에서 숫자 추출
            string name = Path.GetFileNameWithoutExtension(file);
            string numberStr = name.Replace("snowman_", "");
            
            if (int.TryParse(numberStr, out int index))
            {
                if (index > maxIndex)
                {
                    maxIndex = index;
                }
            }
        }
        // 다음 파일 번호는 최대 인덱스 + 1
        arrayCaptureCounter = maxIndex + 1;
        Debug.Log($"Array Capture Counter 초기화 완료. 다음 배열 파일 번호는 {arrayCaptureCounter} 입니다. (경로: {directoryPath})");
    }
    
    // ==========================================================
    // 1. "Capture" 버튼 기능: 실시간 캡처 후 임시 파일에 덮어쓰기 저장
    // ==========================================================
    public void CaptureAndOverwrite()
    {
        string fileName = OVERWRITE_FILE_NAME;
        // 실제 캡처 로직을 호출하여 고정된 파일 이름으로 저장
        StartCoroutine(CaptureRoutine(fileName)); 
    }
    
    // ==========================================================
    // 2. "Main" 버튼 기능: 임시 파일을 읽어 배열 형태로 복사 저장
    // ==========================================================
    public void CaptureAndSaveArray()
    {
        string sourceFileName = OVERWRITE_FILE_NAME;
        string sourcePath = Path.Combine(directoryPath, sourceFileName);

        // 덮어쓰기 파일(current_capture.png)이 존재하는지 확인
        if (!File.Exists(sourcePath))
        {
            Debug.LogError($"'Capture' 버튼으로 저장된 임시 파일이 없습니다: {sourcePath}. 먼저 'Capture' 버튼을 눌러 현재 이미지를 준비해주세요.");
            return;
        }
        
        // 다음 순서의 파일 이름 설정 (예: snowman_1.png)
        string targetFileName = $"snowman_{arrayCaptureCounter}.png";
        string targetPath = Path.Combine(directoryPath, targetFileName);

        try
        {
            // 임시 파일을 순차적인 파일 이름으로 복사
            File.Copy(sourcePath, targetPath);

            // 다음 저장을 위해 카운터 증가
            arrayCaptureCounter++;

            Debug.Log($"임시 캡처본을 배열 형태로 저장 완료: {targetPath}");

            // 에디터 새로 고침 (빌드에서는 무시됨)
            #if UNITY_EDITOR
            AssetDatabase.Refresh();
            #endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"파일 복사 중 오류 발생: {e.Message}");
        }
    }
    
    // ==========================================================
    // 3. 핵심 캡처 및 저장 로직 (코루틴)
    // ==========================================================
    IEnumerator CaptureRoutine(string outputFileName)
    {
        if (targetCanvasRect == null)
        {
            Debug.LogError("targetCanvasRect가 할당되지 않았습니다.");
            yield break;
        }
        
        yield return new WaitForEndOfFrame(); // 렌더링 완료 대기

        // 1. 캔버스 전체 크기 및 캡처 영역 설정
        int canvasTotalWidth = Mathf.RoundToInt(targetCanvasRect.rect.width);
        int height = Mathf.RoundToInt(targetCanvasRect.rect.height);
        
        // 캡처 너비 조정 (캔버스 너비 초과 방지)
        int effectiveCaptureWidth = Mathf.Min(captureWidth, canvasTotalWidth);
        
        // 2. Texture2D 생성 (오른쪽 캡처 크기)
        Texture2D screenShot = new Texture2D(effectiveCaptureWidth, height, TextureFormat.RGB24, false);

        // 3. 캔버스 영역의 화면 좌표 계산
        Vector3[] corners = new Vector3[4];
        targetCanvasRect.GetWorldCorners(corners);

        int canvas_x_start = Mathf.RoundToInt(corners[0].x);
        int canvas_y_start = Mathf.RoundToInt(corners[0].y); 
        
        // 4. 오른쪽 영역 캡처 시작 X 좌표 계산: (캔버스 시작 X) + (캔버스 전체 너비) - (캡처할 너비)
        int capture_x = canvas_x_start + canvasTotalWidth - effectiveCaptureWidth;
        int capture_y = canvas_y_start;
        
        // 5. 화면의 지정된 오른쪽 영역을 읽어오기
        screenShot.ReadPixels(new Rect(capture_x, capture_y, effectiveCaptureWidth, height), 0, 0);
        screenShot.Apply();
        
        // 6. 둥근 모서리 적용을 위해 RGBA 포맷으로 변환된 Texture2D 준비
        Texture2D roundedTexture = ApplyRoundedCorners(screenShot, effectiveCaptureWidth, height, cornerRadius);
        
        // 원본 Texture2D 정리
        DestroyImmediate(screenShot);
        
        // 7. PNG 데이터로 변환 및 저장 경로 설정
        byte[] bytes = roundedTexture.EncodeToPNG();

        // **[주의]** directoryPath는 Awake에서 Application.persistentDataPath로 설정됨
        string filePath = Path.Combine(directoryPath, outputFileName);

        // 8. 파일 저장 (덮어쓰기)
        File.WriteAllBytes(filePath, bytes);
        
        // 9. 정리
        DestroyImmediate(roundedTexture);
        
        // 10. 에디터 새로 고침 (빌드에서는 무시됨)
        #if UNITY_EDITOR
        AssetDatabase.Refresh(); 
        #endif

        Debug.Log($"실시간 캡처본 저장 완료 (둥근 모서리 적용): {filePath} ({effectiveCaptureWidth}px 크롭)");
    }
    
    // ==========================================================
    // 4. 둥근 모서리 마스킹 로직
    // ==========================================================
    /// <summary>
    /// 지정된 Texture2D에 둥근 모서리 마스킹을 적용합니다.
    /// </summary>
    /// <param name="source">원본 RGB24 Texture2D</param>
    /// <param name="width">텍스처 너비</param>
    /// <param name="height">텍스처 높이</param>
    /// <param name="radius">둥근 모서리 반지름</param>
    /// <returns>알파 채널이 적용된 RGBA32 Texture2D</returns>
    private Texture2D ApplyRoundedCorners(Texture2D source, int width, int height, int radius)
    {
        // 반지름이 0이거나 너무 크면 적용하지 않음
        if (radius <= 0 || radius > width / 2 || radius > height / 2)
        {
            Debug.LogWarning("둥근 모서리 반지름이 너무 크거나 0 이하입니다. 둥근 모서리 처리를 건너뜁니다.");
            // 원본을 RGBA32로 변환하여 반환
            Texture2D fallback = new Texture2D(width, height, TextureFormat.RGBA32, false);
            fallback.SetPixels(source.GetPixels());
            fallback.Apply();
            return fallback;
        }

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] sourcePixels = source.GetPixels();
        Color[] resultPixels = new Color[width * height];

        // 4개의 모서리 중심 좌표 (텍스처의 로컬 좌표계, 좌측 하단이 0,0)
        Vector2[] centers = new Vector2[]
        {
            // 좌측 하단: (radius, radius)
            new Vector2(radius, radius),
            // 우측 하단: (width - radius, radius)
            new Vector2(width - radius, radius),
            // 좌측 상단: (radius, height - radius)
            new Vector2(radius, height - radius),
            // 우측 상단: (width - radius, height - radius)
            new Vector2(width - radius, height - radius)
        };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                Color pixel = sourcePixels[index];
                float alpha = 1.0f; // 기본 알파값 (불투명)

                // 둥근 모서리 영역 검사
                
                // 좌측 하단 코너 영역
                if (x < radius && y < radius)
                {
                    alpha = CalculateAlpha(x, y, centers[0], radius);
                }
                // 우측 하단 코너 영역
                else if (x >= width - radius && y < radius)
                {
                    alpha = CalculateAlpha(x, y, centers[1], radius);
                }
                // 좌측 상단 코너 영역
                else if (x < radius && y >= height - radius)
                {
                    alpha = CalculateAlpha(x, y, centers[2], radius);
                }
                // 우측 상단 코너 영역
                else if (x >= width - radius && y >= height - radius)
                {
                    alpha = CalculateAlpha(x, y, centers[3], radius);
                }
                
                // 마스킹 적용
                pixel.a = alpha;
                resultPixels[index] = pixel;
            }
        }

        result.SetPixels(resultPixels);
        result.Apply();
        return result;
    }

    /// <summary>
    /// 픽셀 위치가 원형 영역 내에 있는지 확인하고 알파값을 계산합니다.
    /// </summary>
    private float CalculateAlpha(int x, int y, Vector2 center, int radius)
    {
        // 중심점으로부터의 거리 제곱 계산
        float distSq = (x - center.x) * (x - center.x) + (y - center.y) * (y - center.y);
        float radiusSq = radius * radius;
        
        // 원 바깥에 있다면 투명하게 처리 (알파 0)
        if (distSq > radiusSq)
        {
            return 0.0f;
        }
        
        // 원 내부에 있다면 불투명하게 처리 (알파 1, 앤티앨리어싱은 생략)
        
        return 1.0f;
    }
}