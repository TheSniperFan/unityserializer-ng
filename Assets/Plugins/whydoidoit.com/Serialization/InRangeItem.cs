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

[AddComponentMenu("Storage/Advanced/In Range Item")]
public class InRangeItem : MonoBehaviour
{
	void Start ()
	{
		if(OnlyInRangeManager.Instance != null)
		{
			OnlyInRangeManager.Instance.AddRangedItem(gameObject);
		}
	}
	
	void OnDestroy()
	{
		if(OnlyInRangeManager.Instance != null)
		{
			OnlyInRangeManager.Instance.DestroyRangedItem(gameObject);
		}
		
	}
	
	
}


