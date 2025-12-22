using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    // === 싱글톤 구현 영역 ===
    public static SoundManager instance; 
    
    private AudioSource audioSource;
    
    // === 배경 음악(BGM) 리스트 설정 영역 ===
    [Header("BGM Playlist Settings")]
    public List<AudioClip> bgmClips = new List<AudioClip>(); // 유니티 인스펙터에서 3개의 노래를 여기에 할당
    private int currentTrackIndex = 0; // 현재 재생 중인 곡의 인덱스

    void Awake()
    {
        // 1. AudioSource 컴포넌트 가져오기
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("SoundManager requires an AudioSource component.");
            return;
        }

        // 2. 싱글톤 패턴 적용
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
        
        // 3. 첫 곡 재생 시작
        if (bgmClips.Count > 0)
        {
            PlayNextTrack();
        }
    }

    void Update()
    {
        // 현재 재생 중인 음악이 끝났는지 확인 (is not playing)
        if (audioSource != null && !audioSource.isPlaying && bgmClips.Count > 0)
        {
            // 음악이 끝났다면 다음 곡 재생
            PlayNextTrack();
        }
    }

    /// <summary>
    /// 다음 곡으로 인덱스를 업데이트하고 재생을 시작합니다.
    /// (1 -> 2 -> 3 -> 1 순환)
    /// </summary>
    private void PlayNextTrack()
    {
        if (bgmClips.Count == 0) return;

        // 현재 인덱스를 업데이트합니다.
        // 다음 인덱스 = (현재 인덱스 + 1) % 전체 곡 수
        // 이렇게 하면 2 다음에는 3이 되고, 3 다음에는 0(첫 곡)으로 돌아갑니다.
        currentTrackIndex = (currentTrackIndex + 1) % bgmClips.Count; 
        
        // 새로운 곡을 AudioSource에 할당하고 재생
        audioSource.clip = bgmClips[currentTrackIndex];
        audioSource.Play();

        Debug.Log($"BGM Playing: Track Index {currentTrackIndex} ({audioSource.clip.name})");
    }
}