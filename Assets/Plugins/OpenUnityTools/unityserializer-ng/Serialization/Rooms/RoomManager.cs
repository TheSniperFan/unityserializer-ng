// /* ------------------
//       ${Name} 
//       (c)3Radical 2012
//           by Mike Talbot 
//     ------------------- */
// 
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class RoomManager
{
	public static bool savingRoom;
	public static bool loadingRoom;
	
	public static Dictionary<string, string> rooms = new Dictionary<string, string>();
	
	public static void SaveCurrentRoom()
	{
		savingRoom = true;
		rooms[SceneManager.GetActiveScene().name] = LevelSerializer.SerializeLevel();
		savingRoom = false;
	}
	
	public static void LoadRoom(string name)
	{
		LoadRoom(name, true);
	}
	
	public static void LoadRoom(string name, bool showGUI)
	{
		if(Room.Current)
			Room.Current.Save();
		if(rooms.ContainsKey(name))
		{
			loadingRoom = true;
			var loader = LevelSerializer.LoadSavedLevel(rooms[name]);
			loader.showGUI = showGUI;
			loader.whenCompleted = (obj, list)=>{
				foreach(var gameObject in list)
				{
					gameObject.SendMessage("OnRoomWasLoaded", SendMessageOptions.DontRequireReceiver);
				}
			};
		}
		else
		{
			var go = new GameObject("RoomLoader");
			go.AddComponent<RoomLoader>();
            SceneManager.LoadScene(name, LoadSceneMode.Single);
		}
	}
	
	
}


