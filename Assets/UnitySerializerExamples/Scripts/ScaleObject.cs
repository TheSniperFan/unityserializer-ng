using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class ScaleObject : MonoBehaviour {

	// Use this for initialization
	void Start () {
		if(!GetComponent<UniqueIdentifier>().IsDeserializing)
		{
			this.StartExtendedCoroutine(ScaleMe());
		}
	}
	
	IEnumerator ScaleMe()
	{
		var scale = transform.localScale;
		var newScale = scale * 5;
		while(true)
		{
			var t = 0f;
			while(t < 1)
			{
				
				transform.localScale = Vector3.Lerp(scale, newScale, t);
				t += Time.deltaTime/3;
				yield return null;
			}
			t = 0;
			while(t < 1)
			{
				
				transform.localScale = Vector3.Lerp(newScale, scale, t);
				t += Time.deltaTime/3;
				yield return null;
			}
			
		}
	}
}