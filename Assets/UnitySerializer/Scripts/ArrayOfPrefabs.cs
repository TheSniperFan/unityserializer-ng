using UnityEngine;
using System.Collections;

public class ArrayOfPrefabs : MonoBehaviour {
	
	public GUIStyle style;
	
	public Transform[] prefabs;
	
	public TextAsset aTextAsset;
	
	void Start()
	{
		if(LevelSerializer.IsDeserializing) return;
		style.normal.textColor = Color.red;
	}
	
	void OnGUI()
	{
		GUILayout.FlexibleSpace();
		GUILayout.Label("Hello", style);
		GUILayout.FlexibleSpace();
	}
	
}
