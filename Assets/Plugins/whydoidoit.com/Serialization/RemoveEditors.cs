using UnityEngine;
using System.Collections;
using System.Linq;

[ExecuteInEditMode]
[AddComponentMenu("Storage/Internal/Cleanup Maintenance (Immediately removes itself)")]
public class RemoveEditors : MonoBehaviour
{

	// Use this for initialization
	void Awake()
	{
		LevelSerializer.SavedGames.Clear();
		LevelSerializer.SaveDataToFilePrefs();
		
		
		Component.DestroyImmediate(this);
	}
	
}

