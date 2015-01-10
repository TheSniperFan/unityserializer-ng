
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

[AddComponentMenu("Storage/Tests/Simple Caller")]
public class SimpleCaller : MonoBehaviour
{
	public NetworkView otherView;
	
	void Start ()
	{
		Network.InitializeServer(200,8081,true);
	}

	void Update ()
	{
    
	}
	
	void OnGUI()
	{
		if(GUILayout.Button("Call Print"))
		{
			otherView.RPCEx("PrintThis", RPCMode.All, "Hello World");
		}
	}
}


