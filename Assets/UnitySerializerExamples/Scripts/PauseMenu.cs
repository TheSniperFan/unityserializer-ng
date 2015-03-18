using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class PauseMenu : MonoBehaviour {
    [SerializeField]
    private bool paused = false;
    [SerializeField]
    private GUITexture pausedGUI;
    [SerializeField]
    private string gameName = "Your Game";
    [SerializeField]
    private static bool logProgress = false;

    private List<Transform> myList = new List<Transform>();


    private void Start() {
        if (pausedGUI)
            pausedGUI.enabled = false;
    }

    private void OnEnable() {
        LevelSerializer.Progress += HandleLevelSerializerProgress;
    }

    private void OnDisable() {
        LevelSerializer.Progress -= HandleLevelSerializerProgress;
    }

    private void Update() {
        if (Input.GetKeyUp(KeyCode.P)) {
            paused = !paused;

            if (paused) {
                Time.timeScale = 0.0f;
                Time.fixedDeltaTime = Time.timeScale * 0.02f;
                if (pausedGUI)
                    pausedGUI.enabled = true;
            }
            else {
                Time.timeScale = 1.0f;
                Time.fixedDeltaTime = Time.timeScale * 0.02f;
                if (pausedGUI)
                    pausedGUI.enabled = false;
            }
        }
    }

    private void OnGUI() {
        if (!paused) {
            GUILayout.BeginArea(new Rect(200.0f, 10.0f, 400.0f, 20.0f));
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Press P to Pause");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
            return;
        }

        GUIStyle box = "box";
        GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 200.0f, Screen.height * 0.5f - 300.0f, 400.0f, 600.0f), box);

        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Save Game")) {
            DateTime t = DateTime.Now;
            LevelSerializer.SaveGame(gameName);
            if (logProgress) {
                Debug.Log(string.Format("Saved in: {0:0.000} seconds", (DateTime.Now - t).TotalSeconds));
            }
        }
        GUILayout.Space(60.0f);
        foreach (LevelSerializer.SaveEntry sg in LevelSerializer.SavedGames[LevelSerializer.PlayerName]) {
            if (GUILayout.Button(sg.Caption)) {
                DateTime t = DateTime.Now;
                LevelSerializer.LoadNow(sg.Data);
                if (logProgress) {
                    Debug.Log(string.Format("Loaded in: {0:0.000} seconds", (DateTime.Now - t).TotalSeconds));
                }
                Time.timeScale = 1.0f;
                Time.fixedDeltaTime = Time.timeScale * 0.02f;
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private static void HandleLevelSerializerProgress(string section, float complete) {
        if (logProgress) {
            Debug.Log(string.Format("Progress on {0} = {1:0.00%}", section, complete));
        }
    }
}
