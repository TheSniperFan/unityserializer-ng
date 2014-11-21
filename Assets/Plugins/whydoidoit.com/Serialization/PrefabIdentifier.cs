using UnityEngine;
using System.Collections;
using System.Linq;

[DontStore]
[AddComponentMenu("Storage/Prefab Identifier")]
[ExecuteInEditMode]
public class PrefabIdentifier : StoreInformation
{
	bool inScenePrefab;
	
	public bool IsInScene()
	{
		return inScenePrefab;
	}
	
	protected override void Awake ()
	{
		inScenePrefab = true;
		base.Awake();
		foreach (var c in GetComponents<UniqueIdentifier>().Where(t=>t.GetType() == typeof(UniqueIdentifier) || 
			(t.GetType() == typeof(PrefabIdentifier) && t != this) ||
			t.GetType() == typeof(StoreInformation)
			))
			DestroyImmediate (c);
	}	
}