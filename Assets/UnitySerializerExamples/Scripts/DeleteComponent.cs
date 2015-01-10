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

[AddComponentMenu("Storage/Tests/Delete Component")]
public class DeleteComponent : MonoBehaviour
{
	void Start ()
	{
		if(!LevelSerializer.IsDeserializing)
		{
			Destroy(GetComponent<DeletedComponent>());
		}
	}

	
}


