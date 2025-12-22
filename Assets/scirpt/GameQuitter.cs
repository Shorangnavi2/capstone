using UnityEngine;

public class GameQuitter : MonoBehaviour
{
    // 이 함수를 Close 버튼의 OnClick() 이벤트에 연결합니다.
    public void QuitGame()
    {
        Debug.Log("게임을 종료합니다...");

        // 1. Unity Editor 환경일 때 (테스트 중)
        // 에디터에서 플레이 모드를 중지합니다.
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        
        // 2. 실제 빌드된 게임 환경일 때
        // 애플리케이션을 완전히 종료합니다.
        #else
        Application.Quit();
        #endif
    }
}