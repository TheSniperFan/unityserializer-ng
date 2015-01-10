
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class SomeBaseClass : MonoBehaviour
{
	[RPC]
	protected void PrintThis(string text)
	{
		Debug.Log(text);
	}
}

[AddComponentMenu("Storage/Tests/Inherited")]
public class Inherited : SomeBaseClass
{
	
}


