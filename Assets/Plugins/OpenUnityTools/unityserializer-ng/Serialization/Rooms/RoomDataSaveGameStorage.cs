// /* ------------------
//       ${Name} 
//       (c)3Radical 2012
//           by Mike Talbot 
//     ------------------- */
// 
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Serialization;

[AddComponentMenu("Storage/Rooms/Room Data Save Game Storage")]
public class RoomDataSaveGameStorage : DontStoreObjectInRoom
{
	
	public Dictionary<string, string> roomData
	{
		get
		{
			return RoomManager.rooms;
		}
		set
		{
			RoomManager.rooms = value;
		}
	}
	
}


