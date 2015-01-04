using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using Serialization;

public class TestJSON : MonoBehaviour {

	void Awake()
	{
		Loom.Initialize();
	}
	public class TestClass
	{
		public int variable;
		public int property {get;set;}
		public List<int> ints = new List<int>();
		public Dictionary<string, int> dic = new Dictionary<string, int>();
		public int[] intar = new  [] { 1,2};
		public object[] ar = new object[2];
		public int[,] md = new int [2,2];
	}
	
	void OnGUI()
	{
		var tc = new TestClass { variable = 1, property = 2 };
		tc.ints.Add(1);
		tc.ints.Add(2);
		tc.dic["hello"] = 1;
		tc.dic["mum"] = 2;
		tc.intar[0] = 99;
		tc.ar[0] = tc.ints;
		tc.ar[1] = tc.dic;
		tc.md[1,1]=1000;
		
		
		if(GUILayout.Button("Press me"))
		{
			JSONLevelSerializer.SerializeLevelToServer("ftp://whydoidoit.net/testIt.json", "testserializer", "T3sts3rializer", (error)=>{
				Debug.Log(transform.position.ToString());
			});
			
		}
		if(GUILayout.Button("Or me"))
		{
			LevelSerializer.LoadSavedLevelFromFile("test.data");
		}
	}
}
