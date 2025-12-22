using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필요합니다.

public class SceneTransitionManager : MonoBehaviour
{
    // 이동할 씬의 이름을 인스펙터 창에서 설정할 수 있도록 합니다.
    [SerializeField]
    private string targetSceneName = "YourTargetSceneName"; 

    /// <summary>
    /// 설정된 씬 이름으로 이동하는 메서드입니다.
    /// 버튼의 OnClick() 이벤트에 연결하여 사용합니다.
    /// </summary>
    public void GoToTargetScene()
    {
        // 씬을 로드합니다.
        SceneManager.LoadScene(targetSceneName);
        Debug.Log("씬 전환 요청: " + targetSceneName);
    }
    
    // 이 메서드는 특정 씬 이름을 인수로 받아 이동할 때 유용합니다.
    public void GoToSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        Debug.Log("씬 전환 요청: " + sceneName);
    }
}