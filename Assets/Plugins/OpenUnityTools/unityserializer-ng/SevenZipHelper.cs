using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;


public static class CompressionHelper
{
	
	/*
	public static void PrewarmFileCreator()
	{
		var f = File.CreateText("prewarm.cs");
		f.WriteLine("using System;");
		f.WriteLine("public static partial class PreWarm {");
		f.WriteLine(" public static string [] PrewarmNames = new string [] {");
		List<string> names = new List<string>();
		Prewarm(names, typeof(UnityEngine.Object).Assembly);
		Prewarm(names, typeof(System.Object).Assembly);
		var useNames = names.GroupBy(n=>n).OrderByDescending(n=>n.Count()).Select(n=>n.Key).Take(10000).ToList();
		foreach(var name in useNames)
		{
			f.WriteLine("      \"" + name + "\", ");
		}
		f.WriteLine("       \"END OF LIST\"");
		f.WriteLine("     };");
		f.WriteLine("}");
		f.Close();
		
	}
	
	static void Prewarm(List<string> names, Assembly assembly)
	{
		
		foreach(var type in assembly.GetTypes())
		{
			Prewarm(names, type.GetFields());
			Prewarm(names, type.GetProperties());
			
		}
	}
	
	static void Prewarm(List<string> names, IEnumerable<MemberInfo> members)
	{
		names.AddRange(members.Select(m=>m.Name));
	}
	*/
	
	
	public static string technique = "ZipStream";
	
	public static string Compress(byte[] data)
	{
		using(var m = new MemoryStream())
		{
			switch(technique)
			{
			case "ZipStream":
				
				var br = new BinaryWriter(m);
				var z = new DeflaterOutputStream(m);
				br.Write(data.Length);
				z.Write(data, 0, data.Length);
				z.Flush();
				z.Close();
				break;
			}
			return technique + ":" + Convert.ToBase64String(m.GetBuffer());
		}
	}
	
	public static byte[] Decompress(string data)
	{
		byte[] output = null;
		if(data.StartsWith("ZipStream:"))
		{
			var m = new MemoryStream(Convert.FromBase64String(data.Substring(10)));
			var z = new InflaterInputStream(m);
			var br = new BinaryReader(m);
			var length = br.ReadInt32();
		    output = new byte[length];
			z.Read(output, 0, length);
			z.Close();
			m.Close();
			
		}
		return output;
	}
}
