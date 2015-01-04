// /* ------------------
//       ${Name} 
//       (c)3Radical 2012
//           by Mike Talbot 
//     ------------------- */
// 
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Serialization;

[AddComponentMenu("Storage/Tests/AdditionalData")]
public class AdditionalData : ScriptableObject
{
	public float value = UnityEngine.Random.value;
	
#if UNITY_EDITOR
	
	public static void CreateAdditionalData()
	{
		var data = ScriptableObject.CreateInstance<AdditionalData>();
		AssetDatabase.CreateAsset(data, "Assets/additionalData.asset");
	}
	
#endif
	
}


