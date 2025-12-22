using System;
using UnityEngine;
using OpenCvSharp;
using System.Linq; 

public class SnowballShadowTracker_OpenCVPlus : MonoBehaviour
{
    // --- 1. 진행 상태 ---
    [Header("--- 1. 진행 상태 ---")]
    public float currentSnowballProgress = 0f;
    public float targetMovementPerBall = 5000f;
    private float accumulatedMovement = 0f;

    // --- 2. Webcam / OpenCV 설정 ---
    [Header("--- 2. Webcam 설정 ---")]
    private WebCamTexture webcamTexture;
    
    // Mat 리소스
    private Mat rgbaMat;
    private Mat grayMat;
    private Mat thresholdMat;

    // --- 3. 트래킹 설정 ---
    [Header("--- 3. 트래킹 설정 ---")]
    public int shadowThresholdValue = 30;
    public double minShadowArea = 1000;

    // 추적용
    private Point2d previousCenter = new Point2d();
    private bool isTrackingInitialized = false;
    
    // **[핵심 재구성]** 웹캠 실행 해상도 (인스펙터에서 설정)
    [Header("--- 4. 웹캠 초기 해상도 ---")]
    public int targetWebcamWidth = 640;
    public int targetWebcamHeight = 480;

    // ----------------------------------------------------------------------
    // Unity 생명주기
    // ----------------------------------------------------------------------

    void Start()
    {
        // Start에서 즉시 카메라 초기화 시도
        InitializeCamera();
    }

    void OnDestroy()
    {
        // 씬 전환/파괴 시 웹캠과 OpenCV 리소스를 강제 정지 및 해제
        DisposeResources();
    }
    
    // ----------------------------------------------------------------------
    // 카메라 초기화 및 리소스 해제
    // ----------------------------------------------------------------------

    void InitializeCamera()
    {
        // 기존 리소스 정리 후 시작
        DisposeResources(); 

        WebCamDevice[] devices = WebCamTexture.devices;
        
        // ⚠️ 요청에 따라 웹캠 장치가 없을 경우의 에러 처리를 삭제했습니다.
        // if (devices.Length == 0)
        // {
        //     Debug.LogError("웹캠 장치를 찾을 수 없습니다! Tracker 비활성화.");
        //     enabled = false;
        //     return;
        // }

        // 웹캠이 없을 경우 (devices.Length == 0) 인덱스 참조 오류가 발생할 수 있습니다.
        if (devices.Length == 0)
        {
            Debug.LogError("[SnowballShadowTracker] 웹캠 장치를 찾을 수 없어 초기화를 건너뜁니다.");
            return;
        }

        // 1. 기본 카메라 선택 (첫 번째 장치)
        WebCamDevice defaultDevice = devices[0];
        
        // 2. 웹캠 초기화 및 실행 (명시적 해상도 지정)
        webcamTexture = new WebCamTexture(
            defaultDevice.name, 
            targetWebcamWidth, 
            targetWebcamHeight
        );
        webcamTexture.Play();
        
        // 3. Mat 초기화 (Update에서 해상도 확정 후 재할당될 수 있음)
        int w = targetWebcamWidth;
        int h = targetWebcamHeight;
        
        rgbaMat      = new Mat(h, w, MatType.CV_8UC4);
        grayMat      = new Mat(h, w, MatType.CV_8UC1);
        thresholdMat = new Mat(h, w, MatType.CV_8UC1);
        
        Debug.Log($"[ShadowTracker] 웹캠 초기화 시도: {defaultDevice.name} @ {w}x{h}");
    }
    
    // 모든 리소스를 해제하는 단일 함수
    private void DisposeResources()
    {
        // 웹캠 Stop()
        if (webcamTexture != null)
        {
            if (webcamTexture.isPlaying)
            {
                webcamTexture.Stop();
            }
            webcamTexture = null; 
        }

        // Mat 리소스 Dispose
        rgbaMat?.Dispose();
        grayMat?.Dispose();
        thresholdMat?.Dispose();
        
        rgbaMat = grayMat = thresholdMat = null;
    }

    // ----------------------------------------------------------------------
    // 메인 업데이트 루프
    // ----------------------------------------------------------------------

    void Update()
    {
        // **[최소화]** 웹캠이 실행 중인지 확인
        // InitializeCamera에서 웹캠이 없을 경우 webcamTexture가 null이 되므로 이 조건은 유지됩니다.
        if (webcamTexture == null || !webcamTexture.isPlaying)
            return;

        // 프레임 업데이트가 없으면 처리 건너뛰기
        if (!webcamTexture.didUpdateThisFrame)
            return;

        // 웹캠 프레임 → Mat 변환 및 Mat 크기 확인 로직 포함
        ConvertWebcamFrameToMat();

        // Mat이 유효한지 확인
        if (rgbaMat == null || rgbaMat.Empty())
            return;

        // 프레임 처리 (그림자 추적)
        ProcessFrame(rgbaMat);

        // 진행도 계산
        currentSnowballProgress = Mathf.Clamp(
            (accumulatedMovement / targetMovementPerBall) * 100f,
            0f,
            100f
        );
    }

    // ----------------------------------------------------------------------
    // WebCamTexture → OpenCV Mat 변환 (Mat 크기 유동적 처리)
    // ----------------------------------------------------------------------

    private void ConvertWebcamFrameToMat()
    {
        // Mat 크기가 웹캠 해상도와 다르면 재할당 (실제로 잡힌 해상도에 맞춤)
        if (rgbaMat == null || rgbaMat.Rows != webcamTexture.height || rgbaMat.Cols != webcamTexture.width)
        {
            // 웹캠 해상도가 0이거나 너무 작으면 무시
            if (webcamTexture.width <= 16 || webcamTexture.height <= 16) return;

            Debug.Log($"[Mat Resize] Mat Re-initialized to {webcamTexture.width}x{webcamTexture.height}");
            
            // 리소스 Dispose 후 재할당
            rgbaMat?.Dispose();
            grayMat?.Dispose();
            thresholdMat?.Dispose();

            rgbaMat      = new Mat(webcamTexture.height, webcamTexture.width, MatType.CV_8UC4);
            grayMat      = new Mat(webcamTexture.height, webcamTexture.width, MatType.CV_8UC1);
            thresholdMat = new Mat(webcamTexture.height, webcamTexture.width, MatType.CV_8UC1);
        }

        // Unity의 Color32[] (RGBA)
        Color32[] pixelData = webcamTexture.GetPixels32();
        if (pixelData == null || pixelData.Length == 0) return;

        // Color32[] → byte[] (BGRA 순서로 변환해서 Mat에 넣기)
        byte[] data = new byte[pixelData.Length * 4];

        for (int i = 0; i < pixelData.Length; i++)
        {
            int baseIndex = i * 4;
            Color32 c = pixelData[i];

            data[baseIndex + 0] = c.b;
            data[baseIndex + 1] = c.g;
            data[baseIndex + 2] = c.r;
            data[baseIndex + 3] = c.a;
        }

        rgbaMat.SetArray(0, 0, data);
    }

    // ----------------------------------------------------------------------
    // 핵심: 그림자 영역 추출 + 중심점 움직임 추적 
    // ----------------------------------------------------------------------

    private void ProcessFrame(Mat inputMat)
    {
        if (inputMat == null || inputMat.Empty()) return;

        // 1. 그레이스케일 변환
        Cv2.CvtColor(inputMat, grayMat, ColorConversionCodes.BGRA2GRAY);

        // 2. 임계값(Threshold) 처리로 어두운 영역(그림자) 뽑기
        Cv2.Threshold(grayMat, thresholdMat, shadowThresholdValue, 255, ThresholdTypes.BinaryInv);

        // 3. 외곽선(Contour) 검출
        Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(thresholdMat, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        // 4. 가장 큰 외곽선 찾기
        double maxArea = 0;
        Point[] largestContour = null;

        if (contours != null)
        {
            foreach (var cont in contours)
            {
                double area = Cv2.ContourArea(cont);
                if (area > maxArea)
                {
                    maxArea = area;
                    largestContour = cont;
                }
            }
        }

        // 5. 중심점 계산 + 이동 거리 누적
        if (largestContour != null && maxArea > minShadowArea)
        {
            Moments m = Cv2.Moments(largestContour);

            if (Math.Abs(m.M00) > double.Epsilon)
            {
                double cx = m.M10 / m.M00;
                double cy = m.M01 / m.M00;

                Point2d currentCenter = new Point2d(cx, cy);

                if (!isTrackingInitialized)
                {
                    previousCenter = currentCenter;
                    isTrackingInitialized = true;
                }
                else
                {
                    double dx = currentCenter.X - previousCenter.X;
                    double dy = currentCenter.Y - previousCenter.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);  

                    accumulatedMovement += (float)dist;
                    previousCenter = currentCenter;
                }
            }
        }
        else
        {
            isTrackingInitialized = false;
        }
    }

    // ----------------------------------------------------------------------
    // GameManager 등에서 호출용
    // ----------------------------------------------------------------------

    public void ResetProgress()
    {
        accumulatedMovement = 0f;
        currentSnowballProgress = 0f;
        isTrackingInitialized = false;
    }
}