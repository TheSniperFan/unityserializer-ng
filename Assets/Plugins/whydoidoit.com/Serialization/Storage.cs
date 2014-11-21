using System;
using System.IO;
using System.Linq;
using Serialization;



public static class Storage
{
	//Serialize an object to a compressed format in a BASE64 string
	public static string SerializeToString (this object obj)
	{
			return Convert.ToBase64String (UnitySerializer.Serialize(obj));	
	}
	
	//Typed deserialization
	public static T Deserialize<T> (this string data) where T : class
	{
		return Deserialize (data) as T;
	}
	
	//Deserialize a compressed object from a string
	public static object Deserialize (string data)
	{
		return UnitySerializer.Deserialize(Convert.FromBase64String(data));
		
	}
}

