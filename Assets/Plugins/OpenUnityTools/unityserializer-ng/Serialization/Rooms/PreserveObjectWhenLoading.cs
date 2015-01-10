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

[AddComponentMenu("Components/Storage/Preserve Object When Loading")]
public class PreserveObjectWhenLoading : MonoBehaviour
{
	void Awake()
	{
		LevelLoader.OnDestroyObject += HandleLevelLoaderOnDestroyObject;
	}

	void HandleLevelLoaderOnDestroyObject (GameObject toBeDestroyed, ref bool cancel)
	{
		
		cancel = true;
	}
	
	void OnDestroy()
	{
		LevelLoader.OnDestroyObject -= HandleLevelLoaderOnDestroyObject;
	}
	
	
}


