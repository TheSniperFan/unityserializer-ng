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

[AddComponentMenu("Storage/Tests/Test Loading")]
public class TestLoading : MonoBehaviour
{
	
	
	void Start ()
	{
		DontDestroyOnLoad(this);
		Invoke("LoadLevel",0.3f);
	}

	void LoadLevel()
	{
		
		Application.LoadLevel("Example");
	}
}


