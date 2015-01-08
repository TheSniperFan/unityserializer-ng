
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Reflection;

[AddComponentMenu("System/Inheritable RPC Handler")]
public class InheritableRPC : MonoBehaviour
{
	public class CachedRoutine
	{
		public MethodInfo routine;
		public MonoBehaviour behaviour;
	}
	
	private Dictionary<string, List<CachedRoutine>> cache = new Dictionary<string, List<CachedRoutine>>();
	
	[RPC]
	void PerformRPCCall(string routineName, string parameters)
	{
		var b = new BinaryFormatter();
		using(var s = new MemoryStream(Convert.FromBase64String(parameters)))
		{
			var p = (object[])b.Deserialize(s);
			
			if(!cache.ContainsKey(routineName))
			{
				cache[routineName] = GetComponents<MonoBehaviour>()
					.Select(m=> new CachedRoutine { routine = m.GetType().GetMethod(routineName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance), behaviour = m })
					.Where(r=>r.routine != null && r.routine.IsDefined(typeof(RPC), true))
					.ToList();
			}
			foreach(var m in cache[routineName])
			{
				m.routine.Invoke(m.behaviour, p);
			}
		}
	}
}


public static class InheritableRPCExtensions
{
	public class StoredPlayer
	{
		public string ipAddress, guid;
		public int port;
	}
	
	public static void RPCEx(this NetworkView view, string routineName, RPCMode mode, params object[] parameters)
	{
		using(var m = new MemoryStream())
		{
			var b = new BinaryFormatter();
			b.Serialize(m, parameters);
			m.Flush();
			var s = Convert.ToBase64String(m.GetBuffer());
			view.RPC("PerformRPCCall", mode, routineName, s);
		}
	}
	
	public static void RPCEx(this NetworkView view, string routineName, NetworkPlayer player, params object[] parameters)
	{
		using(var m = new MemoryStream())
		{
			var b = new BinaryFormatter();
			b.Serialize(m, parameters);
			m.Flush();
			var s = Convert.ToBase64String(m.GetBuffer());
			view.RPC("PerformRPCCall", player, routineName, s);
		}
	}
}


