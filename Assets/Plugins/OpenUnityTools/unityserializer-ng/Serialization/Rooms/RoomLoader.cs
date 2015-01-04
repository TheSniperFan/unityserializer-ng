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

[AddComponentMenu("Storage/Internal/Room Loader (Internal use only, do not add this to your scene)")]
public class RoomLoader : MonoBehaviour
{
	void Awake ()
	{
		DontDestroyOnLoad(gameObject);
	}
	
	
	void OnLevelWasLoaded(int level)
	{
		foreach(var go in GameObject.FindObjectsOfType(typeof(GameObject)).Cast<GameObject>())
		{
			go.SendMessage("OnRoomWasLoaded", SendMessageOptions.DontRequireReceiver);
		}
		Destroy(gameObject);
	}
	
}


