using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class SaveAndLoadToServer : MonoBehaviour {
	public GameObject targetGameObject;
	void OnGUI()
	{
		using( new VerticalCentered())
		{
			if(targetGameObject && GUILayout.Button("Save to server JSON"))
			{
				JSONLevelSerializer.SaveObjectTreeToServer("ftp://whydoidoit.net/testme.json", targetGameObject,"testserializer","T3sts3rializer", Completed);
				Destroy(targetGameObject);
			}
			if(!targetGameObject && GUILayout.Button("Load from server JSON"))
			{
				JSONLevelSerializer.LoadObjectTreeFromServer("http://whydoidoit.net/testserializer/testme.json", CompletedJSONLoad);
			}
			if(targetGameObject && GUILayout.Button("Save to server Binary"))
			{
				LevelSerializer.SaveObjectTreeToServer("ftp://whydoidoit.net/testme.dat", targetGameObject, "testserializer", "T3sts3rializer", Completed);
				Destroy(targetGameObject);
			}
			if(!targetGameObject && GUILayout.Button("Load from server Binary"))
			{
				LevelSerializer.LoadObjectTreeFromServer("http://whydoidoit.net/testserializer/testme.dat", CompletedLoad);
			}
			if(GUILayout.Button("Save scene to server JSON"))
			{
				JSONLevelSerializer.SerializeLevelToServer("ftp://whydoidoit.net/testscene.json", "testserializer", "T3sts3rializer", Completed);
			}
			if(GUILayout.Button("Load scene from server JSON"))
			{
				JSONLevelSerializer.LoadSavedLevelFromServer("http://whydoidoit.net/testserializer/testscene.json");
			}
			if(GUILayout.Button("Save scene to server Binary"))
			{
				JSONLevelSerializer.SerializeLevelToServer("ftp://whydoidoit.net/testscene.data", "testserializer", "T3sts3rializer", Completed);
			}
			if(GUILayout.Button("Load scene from server Binary"))
			{
				JSONLevelSerializer.LoadSavedLevelFromServer("http://whydoidoit.net/testserializer/testscene.data");
			}
		}
	}
	
	void CompletedLoad(LevelLoader loader)
	{
		targetGameObject = loader.Last;
	}
	
	void CompletedJSONLoad(JSONLevelLoader loader)
	{
		targetGameObject = loader.Last;
	}
	
	void Completed(Exception e)
	{
		if(e != null)
		{
			Debug.Log("Error");
			Debug.Log(e.ToString());
			Debug.Log(transform.position.ToString());
		}
		else
		{
			Debug.Log("Succeeded");
		}
	}
}
