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

[AddComponentMenu("Storage/Rooms/Dont Store Object In Room")]
public class DontStoreObjectInRoom : MonoBehaviour, IControlSerializationEx
{
	public bool preserveThisObjectWhenLoading = true;
	
	void Awake()
	{
		LevelLoader.OnDestroyObject += HandleLevelLoaderOnDestroyObject;
	}

	void HandleLevelLoaderOnDestroyObject (GameObject toBeDestroyed, ref bool cancel)
	{
		if(toBeDestroyed == gameObject)
			cancel = preserveThisObjectWhenLoading;
	}
	
	void OnDestroy()
	{
		LevelLoader.OnDestroyObject -= HandleLevelLoaderOnDestroyObject;
	}
	
	public bool ShouldSaveWholeObject ()
	{
		return !RoomManager.savingRoom;
	}
	
	public bool ShouldSave ()
	{
		return !RoomManager.savingRoom;
	}
	
}


