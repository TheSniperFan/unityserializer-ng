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

[AddComponentMenu("Storage/Tests/Additional Data Storage")]
public class AdditionalDataStorage : MonoBehaviour
{
	public AdditionalData data;
	
	
	void Start()
	{
		if(LevelSerializer.IsDeserializing)
			return;
		if(data == null)
			data = AdditionalData.CreateInstance<AdditionalData>();
	}
}


