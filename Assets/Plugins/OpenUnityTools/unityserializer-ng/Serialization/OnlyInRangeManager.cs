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
using System.IO;

[AddComponentMenu("Storage/Advanced/Only In Range Manager")]
public class OnlyInRangeManager : MonoBehaviour
{
	//Class to hold information about tracked items
	public class InRange
	{
		public Transform transform;
		public Vector3 lastPosition;
		bool _inprogress;
		//Inprogress flag indicates that it is being loaded or saved
		public bool inprogress
		{
			get
			{
				return _inprogress;
			}
			
			set
			{
				_inprogress = value;
				count =0;
			}
		}
		//Unique identifier
		public string id;
		//Number of frames since coming into or going out of range
		public int count;
		
		//Test function to see if we should save this item
		public void Test(OnlyInRangeManager manager, Vector3 position, float sqrRange)
		{
			if(inprogress)
				return;
			if(transform != null)
			{
				
				if((transform.position - position).sqrMagnitude < sqrRange)
				{
					count++;
					if(count > 3)
						manager.hideList.Remove(this);
				}
				else
				{
					count = 0;
					manager.hideList.Add(this);
				}
			}
			else
			{
				if((lastPosition - position).sqrMagnitude < sqrRange)
				{
					count++;
					if(count > 3)
						manager.viewList.Add(this);
				}
				else
				{
					count = 0;
					manager.viewList.Remove(this);
				}
			}
		}
		
	}
	
	//Singleton instance
	public static OnlyInRangeManager Instance;
	
	//Add an item to be tracked
	public void AddRangedItem(GameObject go)
	{
		var ui = go.GetComponent<PrefabIdentifier>();
		if(ui==null)
		{
			return;
		}
		if(!rangedItems.Any(i=>i.id == ui.Id))
		{
			rangedItems.Add(new InRange { id = ui.Id, transform = go.transform});
		}
		
	}
	
	//Remove an item from tracking
	public void DestroyRangedItem(GameObject go)
	{
		var ui = go.GetComponent<PrefabIdentifier>();
		if(ui==null)
		{
			return;
		}
		var item = rangedItems.FirstOrDefault(i=>i.id == ui.Id);
		if(item == null || item.inprogress)
		{
			return;
		}
		if(File.Exists(Application.persistentDataPath + "/" + item.id + ".dat"))
		{
			File.Delete(Application.persistentDataPath + "/" + item.id + ".dat");
		}
		rangedItems.Remove(item);
	}
	
	//All of the items currently tracked
	[SerializeThis]
	HashSet<InRange> rangedItems = new HashSet<InRange>();
	//Items being hidden
	[SerializeThis]
	HashSet<InRange> hideList = new HashSet<InRange>();
	//Items being shown
	[SerializeThis]
	HashSet<InRange> viewList = new HashSet<InRange>();
	
	//Range
	public float range = 5;
	
	
	
	void Awake()
	{
		Instance = this;
	}
	
	void LateUpdate()
	{
		if(LevelSerializer.IsDeserializing)
			return;
		
		var sqrRange = range * range;
		var p = transform.position;
		//Test all of the items
		foreach(var r in rangedItems)
		{
			r.Test(this, p, sqrRange);
		}
		
		//Hide items on odd frames
		if(hideList.Count > 0 && (Time.frameCount & 1) != 0)
		{
			var h = hideList.First();
			hideList.Remove(h);
			h.inprogress = true;
			StartCoroutine(HideItem(h));
		}
		//Show items on even frames
		if(viewList.Count >0 && (Time.frameCount & 1)==0)
		{
			var v = viewList.First();
			viewList.Remove(v);
			v.inprogress = true;
			StartCoroutine(ViewItem(v));
		}
		
	}
	
	//Hide an item
	IEnumerator HideItem(InRange item)
	{
		LevelSerializer.DontCollect();
		//Save the data
		var data = LevelSerializer.SerializeLevel(false, item.transform.GetComponent<UniqueIdentifier>().Id);
		yield return new WaitForEndOfFrame();
		LevelSerializer.Collect();
		//Write it to a file
		var f = File.Create(Application.persistentDataPath + "/" + item.id + ".dat");
		f.Write(data,0,data.Length);
		yield return null;
		f.Close();
		yield return new WaitForEndOfFrame();
		//Destroy the game object
		item.lastPosition = item.transform.position;
		Destroy(item.transform.gameObject);
		yield return new WaitForEndOfFrame();
		item.transform = null;
		item.inprogress = false;
	}
	
	
	//Load an item
	IEnumerator ViewItem(InRange item)
	{
		//Check for the file
		if(!File.Exists(Application.persistentDataPath + "/" + item.id + ".dat"))
		{
			yield break;
		}
		yield return new WaitForEndOfFrame();
		//Load the data
		var f = File.Open(Application.persistentDataPath + "/" + item.id + ".dat", FileMode.Open);
		var d = new byte[f.Length];
		f.Read(d, 0, (int)f.Length);
		f.Close();
		yield return new WaitForEndOfFrame();
		//Deserialize it
		var complete = false;
		LevelLoader loader = null;
		LevelSerializer.DontCollect();
		LevelSerializer.LoadNow(d, true, false, (usedLevelLoader)=>{
			complete = true;
			loader = usedLevelLoader;
			LevelSerializer.Collect();
		});
		while(!complete)
		{
			yield return null;
		}
		item.transform = loader.Last.transform;
		item.inprogress = false;

	}
	
	
}


