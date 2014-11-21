using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class RenameObject : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
		if(!LevelSerializer.IsDeserializing)
			name = UnityEngine.Random.Range(0,100000).ToString();
		
	}
	
	
}
