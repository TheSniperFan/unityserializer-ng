using UnityEngine;
using System.Collections;

public class ArrayOfPrefabs : MonoBehaviour {
    [SerializeField]
    private GUIStyle style;
    [SerializeField]
    private Transform[] prefabs;
    [SerializeField]
    private TextAsset aTextAsset;

    void Start() {
        if (LevelSerializer.IsDeserializing) return;
        style.normal.textColor = Color.red;
    }
}
