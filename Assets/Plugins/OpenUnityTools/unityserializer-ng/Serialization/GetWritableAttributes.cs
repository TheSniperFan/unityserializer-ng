using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Permissions;

namespace Serialization
{

    public class GetWritableAttributes
    {
        private static readonly Dictionary<Type, GetSet[][][]> PropertyAccess = new Dictionary<Type, GetSet[][][]>();
        /// <summary>
        /// Dictionary of all the used objects to check if properties are different
        /// to those set during construction
        /// </summary>
        private static readonly Dictionary<Type, object> Vanilla = new Dictionary<Type, object>();

        public static Entry[] GetProperties(object obj, bool seen)
        {
            var type = obj.GetType();
            var accessors = GetAccessors(type);
			
#if US_LOGGING
			if(Radical.IsLogging())
			{
				Radical.Log ("[Available Properties]");
				Radical.IndentLog();
				accessors[0].Select(a => {  Radical.Log ("{0}",a.Info.Name); return a;}).ToList();
				Radical.OutdentLog();
				Radical.Log ("[/Available Properties]");
			}
			
#endif
			
            return (from a in accessors[seen ? 0 : 2]
                    let value = a.Get(obj)
                    select new Entry()
                               {
                                   PropertyInfo = a.Info,
                                   MustHaveName = true,
                                   Value = value,
				                   IsStatic = a.IsStatic
                               }).ToArray();
        }
		
		
        public static Entry[] GetFields(object obj, bool seen)
        {
            var type = obj.GetType();
            var accessors = GetAccessors(type);
			
#if US_LOGGING
			if(Radical.IsLogging())
			{
				Radical.Log ("[Available Fields]");
				Radical.IndentLog();
				accessors[1].Select(a => {  Radical.Log ("{0}", a.FieldInfo.Name); return a;}).ToList();
				Radical.OutdentLog();
				Radical.Log ("[/Available Fields]");
			}
#endif

            return (from a in accessors[seen ? 1 : 3]
                    let value = a.Get(obj)
                    select new Entry()
                               {
                                   FieldInfo = a.FieldInfo,
                                   MustHaveName = true,
                                   Value = value,
				                   IsStatic = a.IsStatic
                               }).ToArray();
        }

        private static object GetVanilla(Type type)
        {
			try
			{
	            object vanilla = null;
	            lock (Vanilla)
	            {
	                if (!Vanilla.TryGetValue(type, out vanilla))
	                {
	                    vanilla = UnitySerializer.CreateObject(type);
	                    Vanilla[type] = vanilla;
	                }
	            }
	            return vanilla;
			}
			catch
			{
				return null;
			}
        }

        private static GetSet[][] GetAccessors(Type type)
        {
            lock (PropertyAccess)
            {
                var index = (UnitySerializer.IsChecksum ? 1 : 0) + (UnitySerializer.IsChecksum && UnitySerializer.IgnoreIds ? 1 : 0);
                
                GetSet[][][] collection;
                if (!PropertyAccess.TryGetValue(type, out collection))
                {
                    collection = new GetSet[3][][];
                    PropertyAccess[type] = collection;
                }
                var accessors = collection[index];
                if (accessors == null)
                {
					object vanilla = GetVanilla(type);
					bool canGetVanilla = false;
					if(vanilla != null) {
						canGetVanilla = !vanilla.Equals(null);
					} 
                    var acs = new List<GetSet>();
                    var props = UnitySerializer.GetPropertyInfo(type)
						.Where(p=>p.Name != "hideFlags")
					    .Select(p=> new { priority = ((SerializationPriorityAttribute)p.GetCustomAttributes(false).FirstOrDefault(a=>a is SerializationPriorityAttribute)) ?? new SerializationPriorityAttribute(100),
							             info=p})
						.OrderBy(p=>p.priority.Priority).Select(p=>p.info);
                    foreach (var p in props)
                    {
						var getSet = new GetSetGeneric(p);
                        if(!canGetVanilla) 
						{
							getSet.Vanilla = null;
						}
						else
						{
							getSet.Vanilla = getSet.Get(vanilla);
						}
                        acs.Add(getSet);
                    }
                    accessors = new GetSet[4][];
                    accessors[0] = acs.Where(a=>!a.IsStatic).ToArray();
					accessors[2] = acs.ToArray();
                    acs.Clear();
                    var fields = UnitySerializer.GetFieldInfo(type)
						          .Where(f=>f.Name != "hideFlags")
					              .Select(p=> new { priority = ((SerializationPriorityAttribute)p.GetCustomAttributes(false).FirstOrDefault(a=>a is SerializationPriorityAttribute)) ?? new SerializationPriorityAttribute(100),
							             info=p})
						.OrderBy(p=>p.priority.Priority).Select(p=>p.info);
                    foreach (var f in fields)
                    {
                        var getSet = new GetSetGeneric(f);
                        if(!canGetVanilla) 
						{
							getSet.Vanilla = null;
						}
						else
						{
							getSet.Vanilla = getSet.Get(vanilla);
						} 
                        acs.Add(getSet);
                    }
                    accessors[1] = acs.Where(a=>!a.IsStatic).ToArray();
					accessors[3] = acs.ToArray();

                    collection[index] = accessors;
                }
                return accessors;
            }
        }
    }
}