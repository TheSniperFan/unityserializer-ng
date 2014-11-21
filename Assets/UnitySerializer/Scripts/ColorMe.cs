using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class ColorMe : MonoBehaviourEx {

		// Use this for initialization
	void Start () {
		if(!GetComponent<UniqueIdentifier>().IsDeserializing)
		{
			StartCoroutine("DoColorMe");
		}
	}
	
	IEnumerator DoColorMe()
	{
		var color = renderer.material.color;
		var target = Color.blue;
		while(true)
		{
			var t = 0f;
			while(t < 1)
			{
				
				renderer.material.color = Color.Lerp(color, target, t);
				t += Time.deltaTime/3;
				yield return null;
			}
			t = 0;
			while(t < 1)
			{
				renderer.material.color = Color.Lerp(target, color, t);
				t += Time.deltaTime/3;
				yield return null;
			}
			
		}
	}
}
