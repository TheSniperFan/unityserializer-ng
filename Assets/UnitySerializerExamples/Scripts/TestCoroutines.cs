using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Serialization;

[AddComponentMenu("Storage/Tests/Coroutines")]
public class TestCoroutines : MonoBehaviour
{
	
	void Start()
	{
		if(!LevelSerializer.IsDeserializing)
		{
			gameObject.StartExtendedCoroutine(MyCoroutine());
		}
		StartCoroutine("Hello");
	}
	
	IEnumerator Hello()
	{
		var a = 1000;
		while(true)
		{
			a+=1;
			yield return StartCoroutine(WaitSeconds(10));

		}
	}
	
	IEnumerator MyCoroutine()
	{
		var a = 0;
		while(true)
		{
			a+=1;
			yield return WaitSeconds(1);
		}
		
	}
	
	IEnumerator WaitSeconds(float time)
	{
		var t= 0f;
		while(t < time)
		{
			yield return null;
			t+= Time.deltaTime;
		}
	}
	
}


