// /* ------------------
//       ${Name} 
//       (c)3Radical 2012
//           by Mike Talbot 
//     ------------------- */
// 
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Serialization;
using System.Runtime.Serialization;

[Serializable]
public class StoredReferences : ScriptableObject
{
	public void Clear ()
	{
		ById.Clear ();
		ByObject.Clear ();
		entries.Clear();
	}
		
	public int Count {
		get {
			FixEntries();
			return entries.Count;
		}
	}
			
	public GameObject[] AllReferences {
		get {
			FixEntries();
			return entries.Select(g=>g.gameObject).ToArray();
		}
	}
	
	private static List<SaveGameManager.StoredEntry> betweenSceneReferences = new List<SaveGameManager.StoredEntry>();
	
	
	void OnDisable()
	{
		if(Application.isPlaying)
		{
			betweenSceneReferences = entries.Where(g=>g.gameObject != null).ToList();
		}
	}
	
	void OnEnable()
	{
		if(Application.isPlaying)
		{
			entries = entries.Concat(betweenSceneReferences.Where(g=>g.gameObject != null)).Where(g=>g.gameObject != null).ToList();
			FixEntries();
			ById.Clear();
			ByObject.Clear();
			betweenSceneReferences = entries.Where(g=>g.gameObject != null).ToList();
		}
	}
		
	public List<SaveGameManager.StoredEntry> entries = new List<SaveGameManager.StoredEntry>();
	Dictionary<string, int> ById = new Dictionary<string, int> ();
	Dictionary<GameObject, int> ByObject = new Dictionary<GameObject, int> ();

	public SaveGameManager.StoredEntry this [string id] {
		get {
			EnsureDictionaries();
			if (ById.ContainsKey (id)) {
				var se = entries[ById [id]];
				if (se.gameObject == null) {
					ById.Remove (id);
					return null;
				}
				return se;
			}
			return null;
		}
		set {
			var index =0;
			if(!ById.TryGetValue(id, out index))
			{
				index = entries.Count;
				ById[id] = index;
				entries.Add(value);
			}
			entries[ById [id]] = value;
			ByObject [value.gameObject] = index;
			
		}
	}
	
	void EnsureDictionaries()
	{
		if(ById.Count ==0 && entries.Count > 0)
		{
			FixEntries();
			var i =0;
			foreach(var e in entries)
			{
				ById[e.Id] = i;
				ByObject[e.gameObject] = i++;
			}
		}
	}

	public SaveGameManager.StoredEntry this [GameObject id] {
		get {
			EnsureDictionaries();
			if(ByObject.ContainsKey(id))
			{
				return entries[ByObject[id]];
			}
			return null;
		}
			
		set {
			var index = 0;
			if(!ByObject.TryGetValue(id, out index))
			{
				index = entries.Count;
				ByObject[id] = index;
				entries.Add(value);
			}
			entries[ByObject[id]] = value;
			ById[value.Id] = index;
		}
	}

	public void Remove (GameObject go)
	{
		var data = this [go];
		if (data != null) {
			ById.Remove (data.Id);
			ByObject.Remove (data.gameObject);
		}
	}

	public StoredReferences Alive ()
	{
		var ret = ScriptableObject.CreateInstance<StoredReferences>();
		foreach (var e in entries) {
			if (e.gameObject != null) {
				ret [e.Id] = e;
			}
		}
		return ret;
	}
	
	void FixEntries()
	{
		entries = entries.Where(g=> g != null && g.gameObject != null && g.gameObject.GetComponent<UniqueIdentifier>() != null).ToList();
	}
	

}


