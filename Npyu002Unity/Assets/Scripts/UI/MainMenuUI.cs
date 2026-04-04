using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ActionGame
{
    /// <summary>
    /// メインメニューの UI 制御。
    /// START ボタン → ActionGame シーンへ遷移。
    /// QUIT  ボタン → アプリ終了。
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        void Start()
        {
            Bind("Canvas/Background/StartButton", OnStart);
            Bind("Canvas/Background/QuitButton",  OnQuit);
        }

        void Bind(string path, UnityEngine.Events.UnityAction action)
        {
            var go = GameObject.Find(path);
            if (go == null) return;
            var btn = go.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(action);
        }

        public void OnStart()
        {
            SceneManager.LoadScene("ActionGame");
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
