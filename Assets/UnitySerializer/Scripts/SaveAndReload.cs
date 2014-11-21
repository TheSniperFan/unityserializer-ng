using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Xml.Serialization;
using System.IO;
using Serialization;
using System.Net;

public class SaveAndReload : MonoBehaviour {
	
	static int _id;
	public int id;
	
	void Awake()
	{
		id = _id++;
	}
	
	// Use this for initialization
	void OnMouseDown()
	{
//		var data = JSONLevelSerializer.SaveObjectTree(gameObject);
		JSONLevelSerializer.SaveObjectTreeToServer("ftp://whydoidoit.net/SavedData" + id.ToString() + ".json", gameObject, "testserializer", "T3sts3rializer", (error)=>{
			Debug.Log("Uploaded!" + error);
		});
		//data.WriteToFile("test_json.txt");
		Destroy(gameObject);
		Loom.QueueOnMainThread(()=>{
			Debug.Log("Downloading");
			JSONLevelSerializer.LoadObjectTreeFromServer("http://whydoidoit.net/testserializer/SavedData" + id.ToString() +".json");
		},6f);
	}
	
	
}
