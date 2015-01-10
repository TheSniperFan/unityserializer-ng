using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[DontStore]
[ExecuteInEditMode]
[AddComponentMenu("Storage/Store Information")]
public class StoreInformation : UniqueIdentifier
{
	public bool StoreAllComponents = true;
	[HideInInspector]
	public List<string> Components = new List<string>();
	
	protected override void Awake()
	{
		base.Awake();
		foreach(var c in GetComponents<UniqueIdentifier>().Where(t=>t.GetType() == typeof(UniqueIdentifier) || 
			(t.GetType() == typeof(StoreInformation) && t != this)))
			DestroyImmediate(c);
	}
	
}

