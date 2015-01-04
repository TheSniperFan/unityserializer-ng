using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Permissions;
using System.Collections.Generic;
using System.Linq;

namespace Serialization
{

	//Using reflection to get and set properties so there is no
	//JIT compiliation
	public class GetSetGeneric : GetSet
	{
    	
		public GetSetGeneric (PropertyInfo info)
		{
			Name = info.Name;
			Info = info;
			CollectionType = Info.PropertyType.GetInterface ("IEnumerable", true) != null;
			var customAttrs = info.GetCustomAttributes(typeof(Specialist),true);
			if(customAttrs.Length>0)
			{
				var specialist = (Specialist)customAttrs[0];
				Get = (o)=>UnitySerializer.Specialists[specialist.Type].Serialize(info.GetValue(o, null));
				Set = (o,v)=>info.SetValue( o, UnitySerializer.Specialists[specialist.Type].Deserialize(v), null);
			}
			else
			{
				var getMethod = info.GetGetMethod (true);
				var setMethod = info.GetSetMethod (true);
				if(getMethod == null)
				{
					
					Get = (o)=> {
						return info.GetValue(o, null);
					};
					Set = (o,v) => {
						info.SetValue(o, v, null);
						
					};
					return;
				}
				
				IsStatic = getMethod.IsStatic;
				Get = (o) => {
				
						return getMethod.FastInvoke(o, null);
						//return getMethod.Invoke (o, null);
				};
				Set = (o,v) => {
					try {
						setMethod.FastInvoke (o, new [] {v});
					} catch (Exception e) {
						Radical.LogWarning (string.Format("When setting {0} to {1} found {2}:", o != null ? o.ToString() : "null", v != null ? v.ToString() : "null", e.ToString ()));
					}
				};
			}
		}

		public GetSetGeneric (FieldInfo info)
		{
			Name = info.Name;
			FieldInfo = info;
			var customAttrs = info.GetCustomAttributes(typeof(Specialist),true);
			if(customAttrs.Length>0)
			{
				var specialist = (Specialist)customAttrs[0];
				Get = (o)=>UnitySerializer.Specialists[specialist.Type].Serialize(info.GetValue(o));
				Set = (o,v)=>info.SetValue( o, UnitySerializer.Specialists[specialist.Type].Deserialize(v));
			}
			else
			{
				Get = info.GetValue;
				Set = info.SetValue;
			}
			IsStatic = info.IsStatic;
			CollectionType = FieldInfo.FieldType.GetInterface ("IEnumerable", true) != null;
			return;
		}

		public GetSetGeneric (Type t, string name)
		{
			Name = name;
			var p = t.GetProperty (name);
			if (p == null) {
				FieldInfo = t.GetField (Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				Get = FieldInfo.GetValue;
				Set = FieldInfo.SetValue;
				IsStatic = FieldInfo.IsStatic;
				CollectionType = FieldInfo.FieldType.GetInterface ("IEnumerable", true) != null;
				return;
			}
			Info = p;
			CollectionType = Info.PropertyType.GetInterface ("IEnumerable", true) != null;
			var getMethod = p.GetGetMethod (true);
			var setMethod = p.GetSetMethod (true);
			IsStatic = getMethod.IsStatic;
			Get = (o) => getMethod.Invoke ( !IsStatic ? o : null, null);
			Set = (o,v) => setMethod.Invoke ( !IsStatic ? o : null, new [] {v});
			
			
			
		}

	}
	
	
	

}