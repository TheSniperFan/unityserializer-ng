using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

public class Types
{
	
	private static Dictionary<Type, List<Type>> _types = new Dictionary<Type, List<Type>>();
	private static Dictionary<Assembly, bool> _requiredAssemblies = new Dictionary<Assembly, bool>();
	
	public static List<Type> GetTypes (Type attribute)
	{
		if (!_types.ContainsKey (attribute)) {
			_types [attribute] = AppDomain.CurrentDomain.GetAssemblies ()
				//.Where (asm => asm.IsDefined (typeof(RadicalAttribute), false) || _requiredAssemblies.ContainsKey(asm))
				.SelectMany (asm => asm.GetTypes ())
				.Where (tp => tp.IsDefined (attribute, false)).ToList ();
		}
		
		return _types [attribute];
	}
	
	public static void RequireAssembly (Assembly asm)
	{
		_requiredAssemblies [asm] = true;
		Refresh();
	}
	
	public static void Refresh() {
		_types = new Dictionary<Type, List<Type>>();
	}
	
}

public static class TypeHelper
{
	public static T Attribute<T>(this Type tp) where T : Attribute
	{
		return System.Attribute.GetCustomAttribute(tp, typeof(T)) as T;
	}
	public static T Attribute<T>(this object o) where T : Attribute, new()
	{
		if(o is MemberInfo)
		{
			return (T)(o as MemberInfo).GetCustomAttributes(typeof(T), false).FirstOrDefault() ?? new T();
		}
		if(o is ParameterInfo)
		{
			return (T)(o as ParameterInfo).GetCustomAttributes(typeof(T), false).FirstOrDefault() ?? new T();
		}
		
		return o.GetType().Attribute<T>() ?? new T();;
	}
	
	
	
	
}
