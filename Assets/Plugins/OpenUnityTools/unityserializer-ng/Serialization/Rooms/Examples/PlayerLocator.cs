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

[AddComponentMenu("Storage/Rooms/Examples/Player Locator")]
public class PlayerLocator : MonoBehaviour
{
	public static PlayerLocator Current;
	public static GameObject PlayerGameObject;
	
	void Awake()
	{
		DontDestroyOnLoad(gameObject);
		Current = this;
		PlayerGameObject = gameObject;
	}

}


