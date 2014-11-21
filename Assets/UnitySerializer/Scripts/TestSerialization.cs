using UnityEngine;
using System.Collections;
using Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;


public class TestSerialization : MonoBehaviour
{
	
	void OnEnable()
	{
		//LevelSerializer.Progress += HandleLevelSerializerProgress;
	}
	
	void OnDisable()
	{
		//LevelSerializer.Progress -= HandleLevelSerializerProgress;
	}

	static void HandleLevelSerializerProgress (string section, float complete)
	{
		Debug.Log(string.Format("Progress on {0} = {1:0.00%}", section, complete));
	}
	
	
	void OnGUI()
	{
		
		
		if(GUILayout.Button("Save"))
		{
			//Save the game with a prefix of Game
			var t = DateTime.Now;
			LevelSerializer.SaveGame("Game");
			Radical.CommitLog();
			Debug.Log(string.Format("{0:0.000}", (DateTime.Now - t).TotalSeconds));
		}
		
		//Check to see if there is resume info
		if(LevelSerializer.CanResume)
		{
			if(GUILayout.Button("Resume"))
			{
				LevelSerializer.Resume();
			}
		}
		
		if(LevelSerializer.SavedGames.Count > 0)
		{
			GUILayout.Label("Available saved games");
			//Look for saved games under the given player name
			foreach(var g in LevelSerializer.SavedGames[LevelSerializer.PlayerName])
			{
				if(GUILayout.Button(g.Caption))
				{
					g.Load();
				}
					
			}
		}
	}
	
	// Update is called once per frame
	void Update()
	{

	}
}


