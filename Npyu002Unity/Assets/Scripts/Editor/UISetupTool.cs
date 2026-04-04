using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.Events;

namespace ActionGame.Editor
{
    /// <summary>
    /// ActionGame シーンの Canvas UI 階層を一括生成し、参照ワイヤリングも行う。
    /// Tools/ActionGame/Setup UI → Tools/ActionGame/Wire UI References の順に実行。
    /// </summary>
    public static class UISetupTool
    {
        [MenuItem("Tools/ActionGame/Setup UI")]
        public static void SetupUI()
        {
            // ---- EventSystem ----
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            // ---- Canvas ----
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");

            // ---- HUD ----
            var hud = CreatePanel(canvasGO, "HUD", new Vector2(0, 0), new Vector2(1920, 1080));
            hud.GetComponent<Image>().color = Color.clear;

            var playerHPBar = CreateSlider(hud, "PlayerHPBar");
            SetAnchors(playerHPBar, new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, -30), new Vector2(280, 30));
            playerHPBar.GetComponentInChildren<Image>(true).color = new Color(0.2f, 0.8f, 0.2f);

            var enemyHPBar = CreateSlider(hud, "EnemyHPBar");
            SetAnchors(enemyHPBar, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-160, -30), new Vector2(280, 30));
            enemyHPBar.GetComponentInChildren<Image>(true).color = new Color(0.8f, 0.2f, 0.2f);

            var scoreText = CreateText(hud, "ScoreText", "SCORE  000000", 24);
            SetAnchors(scoreText, new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, -65), new Vector2(260, 36));

            var highScoreText = CreateText(hud, "HighScoreText", "BEST  000000", 24);
            SetAnchors(highScoreText, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-160, -65), new Vector2(260, 36));

            // ---- Pause Panel ----
            var pausePanel = CreatePanel(canvasGO, "PausePanel", Vector2.zero, new Vector2(400, 300));
            pausePanel.SetActive(false);
            var ppTitle = CreateText(pausePanel, "Title", "PAUSED", 54);
            SetAnchors(ppTitle, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(300, 70));
            var ppResume = CreateButton(pausePanel, "ResumeButton", "RESUME", new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(220, 55));
            var ppQuit   = CreateButton(pausePanel, "QuitButton",   "QUIT",   new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(220, 55));

            // ---- Game Over Panel ----
            var gameOverPanel = CreatePanel(canvasGO, "GameOverPanel", Vector2.zero, new Vector2(500, 400));
            gameOverPanel.SetActive(false);
            var goTitle = CreateText(gameOverPanel, "Title", "GAME OVER", 54);
            SetAnchors(goTitle, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(420, 70));
            var gameOverScoreText = CreateText(gameOverPanel, "GameOverScoreText", "SCORE: 0", 30);
            SetAnchors(gameOverScoreText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(320, 40));
            var goRetry = CreateButton(gameOverPanel, "RetryButton", "RETRY", new Vector2(0.5f, 0.5f), new Vector2(-70f, -50f), new Vector2(120, 55));
            var goQuit  = CreateButton(gameOverPanel, "QuitButton",  "QUIT",  new Vector2(0.5f, 0.5f), new Vector2(70f, -50f),  new Vector2(120, 55));

            // ---- Win Panel ----
            var winPanel = CreatePanel(canvasGO, "WinPanel", Vector2.zero, new Vector2(500, 400));
            winPanel.SetActive(false);
            var wpTitle = CreateText(winPanel, "Title", "YOU WIN!", 54);
            SetAnchors(wpTitle, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(420, 70));
            var winScoreText = CreateText(winPanel, "WinScoreText", "SCORE: 0", 30);
            SetAnchors(winScoreText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(320, 40));
            var wpRetry = CreateButton(winPanel, "RetryButton", "RETRY", new Vector2(0.5f, 0.5f), new Vector2(-70f, -50f), new Vector2(120, 55));
            var wpQuit  = CreateButton(winPanel, "QuitButton",  "QUIT",  new Vector2(0.5f, 0.5f), new Vector2(70f, -50f),  new Vector2(120, 55));

            // ---- Mobile Controls ----
            var mobileControls = CreatePanel(canvasGO, "MobileControls", Vector2.zero, new Vector2(1920, 1080));
            mobileControls.GetComponent<Image>().color = Color.clear;

            var joyBG = new GameObject("VirtualJoystick");
            joyBG.transform.SetParent(mobileControls.transform, false);
            joyBG.AddComponent<RectTransform>();
            SetAnchors(joyBG, new Vector2(0, 0), new Vector2(0, 0), new Vector2(130, 130), new Vector2(220, 220));
            joyBG.AddComponent<Image>().color = new Color(1, 1, 1, 0.25f);
            var vj = joyBG.AddComponent<ActionGame.VirtualJoystick>();

            var joyHandle = new GameObject("Handle");
            joyHandle.transform.SetParent(joyBG.transform, false);
            var handleRT = joyHandle.AddComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(90, 90);
            joyHandle.AddComponent<Image>().color = new Color(1, 1, 1, 0.5f);

            CreateButton(mobileControls, "AttackButton", "ATK", new Vector2(1, 0), new Vector2(-110, 110), new Vector2(130, 130));

            // ---- GameManagers ----
            var gameMgrs = GameObject.Find("GameManagers");
            if (gameMgrs == null)
            {
                gameMgrs = new GameObject("GameManagers");
                Undo.RegisterCreatedObjectUndo(gameMgrs, "Create GameManagers");
            }
            AddIfMissing<ActionGame.InputHandler>(gameMgrs);
            AddIfMissing<ActionGame.ScoreManager>(gameMgrs);
            var gameUI = AddIfMissing<ActionGame.GameUI>(gameMgrs);
            AddIfMissing<ActionGame.GameManager>(gameMgrs);

            // ---- AudioManager ----
            var audioMgrGO = GameObject.Find("AudioManager");
            if (audioMgrGO == null)
            {
                audioMgrGO = new GameObject("AudioManager");
                Undo.RegisterCreatedObjectUndo(audioMgrGO, "Create AudioManager");
            }
            AddIfMissing<ActionGame.AudioManager>(audioMgrGO);

            // ---- Wire GameUI references via SerializedObject ----
            var so = new SerializedObject(gameUI);
            so.FindProperty("playerHPBar").objectReferenceValue     = playerHPBar.GetComponent<Slider>();
            so.FindProperty("enemyHPBar").objectReferenceValue      = enemyHPBar.GetComponent<Slider>();
            so.FindProperty("scoreText").objectReferenceValue       = scoreText.GetComponent<Text>();
            so.FindProperty("highScoreText").objectReferenceValue   = highScoreText.GetComponent<Text>();
            so.FindProperty("pausePanel").objectReferenceValue      = pausePanel;
            so.FindProperty("gameOverPanel").objectReferenceValue   = gameOverPanel;
            so.FindProperty("winPanel").objectReferenceValue        = winPanel;
            so.FindProperty("gameOverScoreText").objectReferenceValue = gameOverScoreText.GetComponent<Text>();
            so.FindProperty("winScoreText").objectReferenceValue    = winScoreText.GetComponent<Text>();
            so.FindProperty("mobileControls").objectReferenceValue  = mobileControls;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(canvasGO);
            EditorUtility.SetDirty(gameMgrs);
            Debug.Log("[UISetupTool] Done! Canvas + GameManagers + AudioManager created and wired.");
        }

        // ---- Helpers ----

        static T AddIfMissing<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(go);
            return c;
        }

        static GameObject CreatePanel(GameObject parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            return go;
        }

        static GameObject CreateSlider(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;

            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgRT = bg.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.sizeDelta = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var faRT = fillArea.AddComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one; faRT.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRT = fill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = new Vector2(1, 1); fillRT.sizeDelta = Vector2.zero;
            fill.AddComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f);
            slider.fillRect = fillRT;

            return go;
        }

        static GameObject CreateText(GameObject parent, string name, string text, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            var t = go.AddComponent<Text>();
            t.text      = text;
            t.fontSize  = fontSize;
            t.color     = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return go;
        }

        static GameObject CreateButton(GameObject parent, string name, string label,
            Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = new Color(0.15f, 0.35f, 0.75f, 0.9f);
            go.AddComponent<Button>();

            var txt = CreateText(go, "Label", label, 26);
            var txtRT = txt.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one; txtRT.sizeDelta = Vector2.zero;
            return go;
        }

        static void SetAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        }

        // ---- Wire GameManager ----

        [MenuItem("Tools/ActionGame/Wire Buttons")]
        public static void WireButtons()
        {
            var gameMgrs = GameObject.Find("GameManagers");
            if (gameMgrs == null) { Debug.LogError("[UISetupTool] GameManagers not found."); return; }

            var gameUI     = gameMgrs.GetComponent<ActionGame.GameUI>();
            var inputHandler = gameMgrs.GetComponent<ActionGame.InputHandler>();
            if (gameUI == null) { Debug.LogError("[UISetupTool] GameUI not found."); return; }

            // Pause panel buttons
            BindButton("Canvas/PausePanel/ResumeButton", gameUI,
                typeof(ActionGame.GameUI).GetMethod("Resume"));
            BindButton("Canvas/PausePanel/QuitButton", gameUI,
                typeof(ActionGame.GameUI).GetMethod("OnQuit"));

            // Game Over panel buttons
            BindButton("Canvas/GameOverPanel/RetryButton", gameUI,
                typeof(ActionGame.GameUI).GetMethod("OnRetry"));
            BindButton("Canvas/GameOverPanel/QuitButton", gameUI,
                typeof(ActionGame.GameUI).GetMethod("OnQuit"));

            // Win panel buttons
            BindButton("Canvas/WinPanel/RetryButton", gameUI,
                typeof(ActionGame.GameUI).GetMethod("OnRetry"));
            BindButton("Canvas/WinPanel/QuitButton", gameUI,
                typeof(ActionGame.GameUI).GetMethod("OnQuit"));

            // Mobile attack button
            if (inputHandler != null)
                BindButton("Canvas/MobileControls/AttackButton", inputHandler,
                    typeof(ActionGame.InputHandler).GetMethod("OnMobileAttack"));

            Debug.Log("[UISetupTool] Button OnClick events wired.");
        }

        static void BindButton(string path, UnityEngine.Object target, System.Reflection.MethodInfo method)
        {
            var go = GameObject.Find(path);
            if (go == null) { Debug.LogWarning($"[UISetupTool] Button not found: {path}"); return; }
            var btn = go.GetComponent<Button>();
            if (btn == null) { Debug.LogWarning($"[UISetupTool] No Button component on: {path}"); return; }

            // Clear existing persistent listeners before adding
            var so = new SerializedObject(btn);
            var onClick = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            onClick.ClearArray();
            so.ApplyModifiedProperties();

            UnityAction call = System.Delegate.CreateDelegate(typeof(UnityAction), target, method) as UnityAction;
            UnityEventTools.AddPersistentListener(btn.onClick, call);
            EditorUtility.SetDirty(go);
        }

        [MenuItem("Tools/ActionGame/Fix EventSystem Input Module")]
        public static void FixEventSystemInputModule()
        {
            var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null) { Debug.LogError("[UISetupTool] EventSystem not found."); return; }

            // Remove StandaloneInputModule
            var old = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (old != null) Undo.DestroyObjectImmediate(old);

            // Add InputSystemUIInputModule (New Input System)
            var t = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (t == null)
            {
                Debug.LogError("[UISetupTool] InputSystemUIInputModule type not found. Is Input System package installed?");
                return;
            }
            if (es.GetComponent(t) == null)
                Undo.AddComponent(es.gameObject, t);

            EditorUtility.SetDirty(es.gameObject);
            Debug.Log("[UISetupTool] EventSystem input module switched to InputSystemUIInputModule.");
        }

        [MenuItem("Tools/ActionGame/Wire GameManager")]
        public static void WireGameManager()
        {
            var gameMgrs = GameObject.Find("GameManagers");
            if (gameMgrs == null) { Debug.LogError("[UISetupTool] GameManagers not found. Run Setup UI first."); return; }

            var gm = gameMgrs.GetComponent<ActionGame.GameManager>();
            if (gm == null) { Debug.LogError("[UISetupTool] GameManager component not found."); return; }

            var player = GameObject.FindWithTag("Player");
            var enemy  = GameObject.FindWithTag("Enemy");

            if (player == null) Debug.LogWarning("[UISetupTool] No GameObject with tag 'Player' found.");
            if (enemy  == null) Debug.LogWarning("[UISetupTool] No GameObject with tag 'Enemy' found.");

            var so = new SerializedObject(gm);
            if (player != null)
            {
                var ph = player.GetComponent<ActionGame.Health>();
                if (ph != null) so.FindProperty("playerHealth").objectReferenceValue = ph;
            }
            if (enemy != null)
            {
                var eh = enemy.GetComponent<ActionGame.Health>();
                if (eh != null) so.FindProperty("enemyHealth").objectReferenceValue = eh;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gameMgrs);
            Debug.Log("[UISetupTool] GameManager references wired.");
        }

        // ---- Main Menu UI ----

        [MenuItem("Tools/ActionGame/Setup Main Menu UI")]
        public static void SetupMainMenuUI()
        {
            // EventSystem
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                var t = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (t != null) es.AddComponent(t);
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");

            // Background panel
            var bg = CreatePanel(canvasGO, "Background", Vector2.zero, new Vector2(1920, 1080));
            bg.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.15f, 1f);

            // Title
            var title = CreateText(bg, "Title", "ACTION GAME", 96);
            SetAnchors(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 180), new Vector2(800, 120));

            // Subtitle
            var sub = CreateText(bg, "Subtitle", "Press START to Play", 36);
            SetAnchors(sub, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(600, 50));
            sub.GetComponent<Text>().color = new Color(0.8f, 0.8f, 0.8f, 1f);

            // Start button
            var startBtn = CreateButton(bg, "StartButton", "START", new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(280, 70));

            // Quit button
            var quitBtn = CreateButton(bg, "QuitButton", "QUIT", new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(280, 70));
            quitBtn.GetComponent<Image>().color = new Color(0.4f, 0.15f, 0.15f, 0.9f);

            // Best score display
            var best = CreateText(bg, "BestScore", "BEST: 0", 28);
            SetAnchors(best, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(400, 40));
            int hi = UnityEngine.PlayerPrefs.GetInt("HighScore", 0);
            best.GetComponent<Text>().text = $"BEST: {hi:D6}";

            // MainMenuUI component (wires button callbacks at runtime)
            var menuGO = new GameObject("MainMenuUI");
            Undo.RegisterCreatedObjectUndo(menuGO, "Create MainMenuUI");
            var menuUI = menuGO.AddComponent<ActionGame.MainMenuUI>();

            // Wire buttons at runtime via code (same pattern as GameUI)
            var soStart = new SerializedObject(startBtn.GetComponent<Button>());
            var soQuit  = new SerializedObject(quitBtn.GetComponent<Button>());
            soStart.ApplyModifiedProperties();
            soQuit.ApplyModifiedProperties();

            EditorUtility.SetDirty(canvasGO);
            EditorUtility.SetDirty(menuGO);
            Debug.Log("[UISetupTool] Main Menu UI created. Add onClick listeners at runtime via MainMenuUI.Start().");
        }
    }
}
