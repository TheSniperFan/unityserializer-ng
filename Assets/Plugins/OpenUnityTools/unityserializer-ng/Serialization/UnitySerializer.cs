#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

public class Timing : IDisposable
{
    private readonly string _caption;
    private readonly DateTime _start;

    public Timing(string caption)
    {
        _caption = caption;
        _start = DateTime.Now;
    }

    #region IDisposable Members

    public void Dispose()
    {
        Radical.LogNow("{0} - {1:0.000}", _caption, (DateTime.Now - _start).TotalSeconds);
    }

    #endregion
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class SerializeAll : Attribute
{
}

/// <summary>
///   Indicates that a no public members should be saved by default
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DoNotSerializePublic : Attribute
{
}

/// <summary>
///   Indicates that a property or field should be serialized
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SerializeThis : Attribute
{
}

/// <summary>
///   Indicates that a property or field should not be serialized
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Event)]
public class DoNotSerialize : Attribute
{
}

/// <summary>
///   Used in checksum mode to flag a property as not being part
///   of the "meaning" of an object - i.e. two objects with the
///   same checksum "mean" the same thing, even if some of the
///   properties are different, those properties would not be
///   relevant to the purpose of the object
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DoNotChecksum : Attribute
{
}

public interface IDeserialized
{
    void Deserialized();
}

namespace Serialization
{
    public class SerializePrivateFieldOfType
    {
        private static readonly Index<string, List<SerializePrivateFieldOfType>> privateFields =
            new Index<string, List<SerializePrivateFieldOfType>>();

        private readonly string _fieldName;

        public SerializePrivateFieldOfType(string typeName, string fieldName)
        {
            _fieldName = fieldName;
            privateFields[typeName].Add(this);
        }

        public static IEnumerable<FieldInfo> GetFields(Type type)
        {
            var name = "";
            if (privateFields.ContainsKey(type.Name))
            {
                name = type.Name;
            }
            if (privateFields.ContainsKey(type.FullName))
            {
                name = type.FullName;
            }
            if (!string.IsNullOrEmpty(name))
            {
                var fields = privateFields[name];
                return
                    type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField |
                                   BindingFlags.Static).Where(f => fields.Any(fld => fld._fieldName == f.Name));
            }
            return new FieldInfo[0];
        }
    }

    /// <summary>
    ///   Attribute used to flag IDs this can be useful for check object
    ///   consistence when the serializer is in a mode that does not 
    ///   serialize identifiers
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SerializerId : Attribute
    {
    }

    /// <summary>
    ///   Always use an event to create instances of this type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CreateUsingEvent : Attribute
    {
    }

    /// <summary>
    ///   This class can create a type of object
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CreatorFor : Attribute
    {
        public readonly Type CreatesType;

        public CreatorFor(Type createsType)
        {
            CreatesType = createsType;
        }
    }

    /// <summary>
    ///   Used to set an order for deserialiation
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SerializationPriorityAttribute : Attribute
    {
        public readonly int Priority;

        public SerializationPriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }


    public interface IProvideAttributeList
    {
        bool AllowAllSimple(Type tp);
        IEnumerable<string> GetAttributeList(Type tp);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AttributeListProvider : Attribute
    {
        public readonly Type AttributeListType;

        public AttributeListProvider(Type attributeListType)
        {
            AttributeListType = attributeListType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DeferredAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class Specialist : Attribute
    {
        public readonly Type Type;

        public Specialist(Type type)
        {
            Type = type;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SpecialistProvider : Attribute
    {
    }

    public interface ISerializeObjectEx : ISerializeObject
    {
        bool CanSerialize(Type targetType, object instance);
    }

    public interface ISpecialist
    {
        object Serialize(object value);
        object Deserialize(object value);
    }

    public interface ISerializeObject
    {
        object[] Serialize(object target);
        object Deserialize(object[] data, object instance);
    }

    public interface ICreateObject
    {
        object Create(Type itemType);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SerializerAttribute : Attribute
    {
        internal readonly Type SerializesType;

        public SerializerAttribute(Type serializesType)
        {
            SerializesType = serializesType;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class OnlyInterfaces : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SubTypeSerializerAttribute : Attribute
    {
        internal readonly Type SerializesType;

        public SubTypeSerializerAttribute(Type serializesType)
        {
            SerializesType = serializesType;
        }
    }

    /// <summary>
    ///   .NET compatible binary serializer with suppression support
    ///   produces compact representations, suitable for further compression
    /// </summary>
    //
    public static class UnitySerializer
    {
		public static Encoding TextEncoding = Encoding.Default;
		
        private static readonly string DataPath;

        internal static List<Action> FinalDeserialization = new List<Action>();

        private static readonly Dictionary<Type, IEnumerable<FieldInfo>> FieldLists =
            new Dictionary<Type, IEnumerable<FieldInfo>>();

        private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> PropertyLists =
            new Dictionary<Type, IEnumerable<PropertyInfo>>();

        private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> ChecksumLists =
            new Dictionary<Type, IEnumerable<PropertyInfo>>();

        internal static List<Type> _knownTypesList;
        internal static Dictionary<Type, ushort> _knownTypesLookup;
        private static Dictionary<object, int> _seenObjects;
        private static Dictionary<Type, bool> _seenTypes;
        private static Dictionary<int, object> _loadedObjects;
        internal static List<string> _propertyList;
        internal static Dictionary<string, ushort> _propertyLookup;


        private static Stack<List<DeferredSetter>> _deferredStack;
        private static Stack<List<Action>> _finalActions;
        private static Stack<Dictionary<int, object>> _loadedObjectStack;
        private static Stack<Dictionary<Type, bool>> _seenTypesStack;
        private static Stack<Dictionary<object, int>> _storedObjectsStack;
        private static Stack<KnownTypesStackEntry> _knownTypesStack;
        private static Stack<PropertyNameStackEntry> _propertyNamesStack;
        private static Stack<int> _idStack;
        public static bool IgnoreIds;
        private static int _nextId;
        //Holds a reference to the custom serializers
        private static readonly Dictionary<Type, ISerializeObject> Serializers =
            new Dictionary<Type, ISerializeObject>();

        //Specialist serializers
        internal static readonly Dictionary<Type, ISpecialist> Specialists = new Dictionary<Type, ISpecialist>();
        //Holds a reference to the custom serializers
        private static readonly Dictionary<Type, ISerializeObject> SubTypeSerializers =
            new Dictionary<Type, ISerializeObject>();

        //Holds a reference to the custom object creators
        private static readonly Dictionary<Type, ICreateObject> Creators = new Dictionary<Type, ICreateObject>();
        //Holds a reference to the custom object attribute list providers
        private static readonly Dictionary<Type, IProvideAttributeList> AttributeLists =
            new Dictionary<Type, IProvideAttributeList>();

        //Holds a list of the deferred types
        public static readonly Dictionary<Type, bool> DEFERRED = new Dictionary<Type, bool>();
        //Dictionary to ensure we only scan an assembly once
        private static readonly Dictionary<Assembly, bool> Assemblies = new Dictionary<Assembly, bool>();

        /// <summary>
        ///   Write all types, even if they are known, often used with Loud mode
        /// </summary>
        public static bool Verbose;

        private static readonly Dictionary<Type, ushort> PrewarmedTypes =
            new Dictionary<Type, ushort>();

        public static readonly List<Type> PrewarmLookup = new List<Type>();

        private static readonly Dictionary<string, ushort> PrewarmedNames = new Dictionary<string, ushort>();
        private static readonly HashSet<Type> privateTypes = new HashSet<Type>();
        private static readonly Stack<Type> currentTypes = new Stack<Type>();
        public static int currentVersion;
		

        /// <summary>
        ///   Cache for property name to item lookups
        /// </summary>
        private static readonly Dictionary<Type, Dictionary<string, EntryConfiguration>> StoredTypes =
            new Dictionary<Type, Dictionary<string, EntryConfiguration>>();

        /// <summary>
        ///   Put the serializer into Checksum mode
        /// </summary>
        public static bool IsChecksum { get; private set; }
		
		public static T Copy<T>(T original) where T : class
		{
			return Deserialize<T>(Serialize(original));
		}

        public static Type GetTypeEx(object fullTypeName)
        {
            var typeName = fullTypeName as string;
			
            if(typeName != null)
			{
				while(typeName.Contains("Version"))
				{
					typeName = typeName.Substring(0,typeName.IndexOf("Version")) + typeName.Substring(typeName.IndexOf(",", typeName.IndexOf("Version"))+2);
				}
				var type = Type.GetType(typeName);
	            if (type != null)
	            {
	                return type;
	            }
	            
				
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetType(typeName) != null);
                return assembly != null ? assembly.GetType(typeName) : null;
			}
            if(fullTypeName is ushort)
            {
                if((ushort)fullTypeName >= 60000)
                    return PrewarmLookup[(ushort)fullTypeName-60000];
                return  _knownTypesList[(ushort)fullTypeName];
            }
            return null;
        }


        public static void SerializeToFile(object obj, string fileName)
        {
            using (var file = File.Create(DataPath + "/" + fileName))
            {
                Serialize(obj, file, false);
            }
        }

        public static T DeserializeFromFile<T>(string fileName) where T : class
        {
            if (!File.Exists(DataPath + "/" + fileName))
            {
                return null;
            }
            using (var file = File.Open(DataPath + "/" + fileName, FileMode.Open))
            {
                return Deserialize(file) as T;
            }
        }
		
		public static void InformDeserializedObjects(FinalProcess process)
		{
			foreach(var o in process.deserializedObjects)
			{
				o.Deserialized();
			}
		}
		
        public static void InformDeserializedObjects()
        {
            foreach (var d in DeserializedObject)
            {
                try
                {
                    d.Deserialized();
                }
                catch
                {
                }
            }
            DeserializedObject.Clear();
        }

        public static void AddFinalAction(Action a)
        {
            FinalDeserialization.Add(a);
        }
		
		public struct FinalProcess
		{
			public List<Action> deferredActions;
			public List<DeferredSetter> deferredSetters;
			public List<IDeserialized> deserializedObjects;
		}
		
		public static FinalProcess TakeOwnershipOfFinalization()
		{
			var result = new FinalProcess();
			lock(FixupFunctions)
			{
				result.deserializedObjects = DeserializedObject;
				result.deferredActions = FinalDeserialization;
				result.deferredSetters = FixupFunctions;
				FinalDeserialization = new List<Action>();
				FixupFunctions = new List<DeferredSetter>();
				DeserializedObject = new List<IDeserialized>();
			}
			return result;
		}
		
        public static void RunDeferredActions(int count = 1, bool clear = true)
        {
			lock (FixupFunctions)
            {
	            for(var i = 0; i < count; i++)
				{
	                foreach (var action in FixupFunctions.Where(f => f.enabled))
	                {
	                    try
	                    {
	                        action.deferredRetrievalFunction(action.parameters);
	                        //Radical.Log ("Deferred {0}", d.GetType().FullName);
	                        action._setAction();
	                    }
	                    catch (Exception e)
	                    {
	                        Radical.LogError("Failed deferred deserialization with error " + e.GetType().FullName + "'" +
	                                         e.Message + "' @ " + e.StackTrace);
	                    }
	                }
				    foreach (var action in FinalDeserialization)
		            {
		                action();
		            }
				}
				if(clear)
				{
				   	FixupFunctions.Clear();
			   		FinalDeserialization.Clear();        
				}
			
			}
		}
		
        
		public static void RunDeferredActions(FinalProcess process, int count = 1, bool clear = true)
        {
			lock(FixupFunctions)
			{
				
				var tempFixup = FixupFunctions;
				var tempFinal = FinalDeserialization;
				
				FixupFunctions = process.deferredSetters;
				FinalDeserialization = process.deferredActions;
				
			    for(var i = 0; i < count; i++)
				{
	                foreach (var action in process.deferredSetters.Where(f => f.enabled))
	                {
	                    try
	                    {
	                        action.deferredRetrievalFunction(action.parameters);
	                        //Radical.Log ("Deferred {0}", d.GetType().FullName);
	                        action._setAction();
	                    }
	                    catch (Exception e)
	                    {
	                        Radical.LogError("Failed deferred deserialization with error " + e.GetType().FullName + "'" +
	                                         e.Message + "' @ " + e.StackTrace);
	                    }
	                }
				    foreach (var action in process.deferredActions)
		            {
		                action();
		            }
				}
				if(clear)
				{
				   	process.deferredActions.Clear();
			   		process.deferredSetters.Clear();        
				}
				
				FixupFunctions = tempFixup;
				FinalDeserialization = tempFinal;
			
			}
		}
		
		
        

        public static void AddFixup(DeferredSetter setter)
        {
            lock (FixupFunctions)
            {
                FixupFunctions.Add(setter);
            }
        }


        public static event Func<Type, bool> CanSerialize;

        internal static bool CanSerializeType(Type tp)
        {
            if (CanSerialize != null)
            {
                return CanSerialize(tp);
            }
            else
            {
                return true;
            }
        }

        internal static void PushPropertyNames(bool clear)
        {
			if(SerializationScope.IsPrimaryScope)
			{
	            _propertyNamesStack.Push(new PropertyNameStackEntry {propertyList = _propertyList, propertyLookup = _propertyLookup});
	            if (clear)
	            {
	                _propertyList = new List<string>();
	                _propertyLookup = new Dictionary<string, ushort>();
	            }
			}
			else
			{
				_propertyList = _propertyList ?? new List<string>();
				_propertyLookup = _propertyLookup ?? new Dictionary<string, ushort>();
			}
        }

        internal static void PushPropertyNames()
        {
            PushPropertyNames(true);
        }

        internal static void PopPropertyNames()
        {
			if(SerializationScope.IsPrimaryScope)
			{
	            var stackEntry = _propertyNamesStack.Pop();
	            _propertyList = stackEntry.propertyList;
	            _propertyLookup = stackEntry.propertyLookup;
			}
        }


        /// <summary>
        ///   Event that is fired if a particular type cannot be instantiated
        /// </summary>
        public static event EventHandler<ObjectMappingEventArgs> CreateType;


        private static void InvokeCreateType(ObjectMappingEventArgs e)
        {
            var handler = CreateType;
            if (handler != null)
            {
                handler(null, e);
            }
        }


        /// <summary>
        ///   Event that is fired if a particular type cannot be found
        /// </summary>
        public static event EventHandler<TypeMappingEventArgs> MapMissingType;


        internal static void InvokeMapMissingType(TypeMappingEventArgs e)
        {
            var handler = MapMissingType;
            if (handler != null)
            {
                handler(null, e);
            }
        }

        /// <summary>
        ///   Deserialize to a type
        /// </summary>
        /// <param name="array"> </param>
        /// <returns> </returns>
        public static T Deserialize<T>(byte[] array) where T : class
        {
            return Deserialize(array) as T;
        }
		
				/// <summary>
		/// Writes a byte array to a file.
		/// </summary>
		/// <param name='data'>
		/// The bytes to write
		/// </param>
		/// <param name='filename'>
		/// The filename to save them to
		/// </param>
		public static void WriteToFile(this byte[] data, string filename = null)
		{
			var f = File.Create(filename ?? "test_output.data");
			var w = new BinaryWriter(f);
			w.Write(data);
			w.Flush();
			f.Close();
		
		}
		
		/// <summary>
		/// Writes a string to file.
		/// </summary>
		/// <param name='str'>
		/// The string to write
		/// </param>
		/// <param name='filename'>
		/// Filename for the output
		/// </param>/
		public static void WriteToFile(this string str, string filename  = null)
		{
			var f = File.Create(filename ?? "test_output.txt");
			var w = new StreamWriter(f);
			w.Write(str);
			w.Flush();
			f.Close();
		}


        /// <summary>
        ///   Deserialize from a stream to a type
        /// </summary>
        /// <param name="stream"> </param>
        /// <returns> </returns>
        public static T Deserialize<T>(Stream stream) where T : class
        {
            return Deserialize(stream) as T;
        }

        /// <summary>
        ///   Get a checksum for an item.  Checksums "should" be different 
        ///   for every object that has a different "meaning".  You can
        ///   flag properties as DoNotChecksum if that helps to keep decorative
        ///   properties away from the checksum whilst including meaningful ones
        /// </summary>
        /// <param name="item"> The object to checksum </param>
        /// <returns> A checksum string, this includes no illegal characters and can be used as a file name </returns>
        public static string GetChecksum(object item)
        {
            if (item == null)
            {
                return "";
            }
            var checksum = new byte[17];
            checksum.Initialize();
            var isChecksum = IsChecksum;
            IsChecksum = true;
            var toBytes = Serialize(item);
            IsChecksum = isChecksum;

            for (var i = 0; i < toBytes.Length; i++)
            {
                checksum[i & 15] ^= toBytes[i];
            }
            return item.GetType().Name + "-" + toBytes.Count() + "-" + Encode(checksum);
        }

        private static string Encode(byte[] checksum)
        {
            var s = Convert.ToBase64String(checksum);
            return s.Aggregate("", (current, c) => current + (Char.IsLetterOrDigit(c)
                                                                  ? c
                                                                  : Char.GetNumericValue(c)));
        }


        /// <summary>
        ///   Register all of the custom serializers in an assembly
        /// </summary>
        /// <param name="assembly"> Leave blank to register the assembly that the method is called from, or pass an assembly </param>
        public static void RegisterSerializationAssembly()
        {
            RegisterSerializationAssembly(null);
        }

        public static void RegisterSerializationAssembly(Assembly assembly)
        {
            if (assembly == null) 
            {
                assembly = Assembly.GetCallingAssembly();
            }
            if (Assemblies.ContainsKey(assembly))
            {
                return;
            }
            Assemblies[assembly] = true;
            ScanAllTypesForAttribute(
                (tp, attr) =>
                    {
                        Serializers[((SerializerAttribute) attr).SerializesType] =
                            Activator.CreateInstance(tp) as ISerializeObject;
                    }, assembly, typeof (SerializerAttribute));
            ScanAllTypesForAttribute(
                (tp, attr) =>
                    {
                        AttributeLists[((AttributeListProvider) attr).AttributeListType] =
                            Activator.CreateInstance(tp) as IProvideAttributeList;
                    }, assembly,
                typeof (AttributeListProvider));

            ScanAllTypesForAttribute(
                (tp, attr) =>
                    {
                        SubTypeSerializers[((SubTypeSerializerAttribute) attr).SerializesType] =
                            Activator.CreateInstance(tp) as ISerializeObject;
                    }, assembly,
                typeof (SubTypeSerializerAttribute));
            ScanAllTypesForAttribute((tp, attr) => { Specialists[tp] = Activator.CreateInstance(tp) as ISpecialist; },
                                     assembly, typeof (SpecialistProvider));
            ScanAllTypesForAttribute(
                (tp, attr) => { Creators[((CreatorFor) attr).CreatesType] = Activator.CreateInstance(tp) as ICreateObject; },
                assembly, typeof (CreatorFor));
        }

        //Function to be called when scanning types

        /// <summary>
        ///   Scan all of the types in an assembly for a particular attribute
        /// </summary>
        /// <param name="function"> The function to call </param>
        /// <param name="assembly"> The assembly to scan </param>
        internal static void ScanAllTypesForAttribute(ScanTypeFunction function, Assembly assembly)
        {
            ScanAllTypesForAttribute(function, assembly, null);
        }

        internal static void ScanAllTypesForAttribute(ScanTypeFunction function, Assembly assembly, Type attribute)
        {
            try
            {
                foreach (var tp in assembly.GetTypes())
                {
                    if (attribute != null)
                    {
                        var attrs = Attribute.GetCustomAttributes(tp, attribute, false);
                        foreach (var attr in attrs)
                        {
                            function(tp, attr);
                        }
                    }
                    else
                    {
                        function(tp, null);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        ///   Caches and returns property info for a type
        /// </summary>
        /// <param name="itm"> The type that should have its property info returned </param>
        /// <returns> An enumeration of PropertyInfo objects </returns>
        /// <remarks>
        ///   It should be noted that the implementation converts the enumeration returned from reflection to an array as this more than double the speed of subsequent reads
        /// </remarks>
        internal static IEnumerable<PropertyInfo> GetPropertyInfo(Type itm)
        {
            lock (PropertyLists)
            {
                string[] validNames;
                IEnumerable<PropertyInfo> ret;
                if (!IsChecksum)
                {
                    if (!PropertyLists.TryGetValue(itm, out ret))
                    {
                        var tp = itm;
                        var allowSimple = true;
                        validNames = AttributeLists
                            .Where(p => p.Key.IsAssignableFrom(tp))
                            .SelectMany(p =>
                                            {
                                                allowSimple = allowSimple && p.Value.AllowAllSimple(tp);
                                                return p.Value.GetAttributeList(tp);
                                            }).ToArray();
                        if (validNames.FirstOrDefault() == null)
                        {
                            validNames = null;
                        }
                        var containingType = itm;

                        ret = containingType
                            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static)
                            .Where(
                                p =>
                                !typeof (Component).IsAssignableFrom(tp) || tp == typeof (Component) || !componentNames.ContainsKey(p.Name))
                            .Where(
                                p => p.PropertyType.GetCustomAttributes(typeof (DoNotSerialize), true).Count() == 0 &&
                                     p.GetGetMethod() != null &&
                                     (!containingType.IsDefined(typeof (DoNotSerializePublic), true) ||
                                      p.IsDefined(typeof (SerializeThis), true)) &&
                                     !p.GetCustomAttributes(typeof (DoNotSerialize), true).Any() &&
                                     !p.GetCustomAttributes(typeof (ObsoleteAttribute), true).Any() &&
                                     !(p.GetIndexParameters().Any()) &&
                                     (p.GetSetMethod() != null &&
                                      CanSerializeType(p.PropertyType)) &&
                                     ((p.PropertyType.IsValueType && allowSimple) || validNames == null ||
                                      validNames.Any(n => n == p.Name))
                            ).ToArray();
                        PropertyLists[itm] = ret;
                    }
                }
                else
                {
                    if (!ChecksumLists.TryGetValue(itm, out ret))
                    {
                        var tp = itm;
                        var allowSimple = true;
                        validNames = AttributeLists
                            .Where(p => p.Key.IsAssignableFrom(tp))
                            .SelectMany(p =>
                                            {
                                                allowSimple = allowSimple & p.Value.AllowAllSimple(tp);
                                                return p.Value.GetAttributeList(tp);
                                            }).ToArray();
                        var availableNames = validNames as string[];
                        if (availableNames.FirstOrDefault() == null)
                        {
                            validNames = null;
                        }

                        ret = tp
                            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static)
                            .Where(
                                p =>
                                !typeof (Component).IsAssignableFrom(tp) || tp == typeof (Component) || !componentNames.ContainsKey(p.Name))
                            .Where(
                                p => !p.PropertyType.GetCustomAttributes(typeof (DoNotSerialize), true).Any() &&
                                     p.GetGetMethod() != null &&
                                     (!tp.IsDefined(typeof (DoNotSerializePublic), true) ||
                                      p.IsDefined(typeof (SerializeThis), true)) &&
                                     !p.GetCustomAttributes(typeof (DoNotSerialize), true).Any() &&
                                     !p.GetCustomAttributes(typeof (DoNotChecksum), true).Any() &&
                                     !p.GetCustomAttributes(typeof (ObsoleteAttribute), true).Any() &&
                                     !(p.GetIndexParameters().Any()) &&
                                     (p.GetSetMethod() != null &&
                                      CanSerializeType(p.PropertyType)) &&
                                     ((allowSimple && p.PropertyType.IsValueType) || validNames == null ||
                                      availableNames.Any(n => n == p.Name))
                            ).ToArray();
                        ChecksumLists[itm] = ret;
                    }
                }
                var propertyInfos = ret as PropertyInfo[] ?? ret.ToArray();
                return IgnoreIds && ret != null
                           ? propertyInfos.Where(p => !p.GetCustomAttributes(typeof (SerializerId), true).Any())
                           : propertyInfos;
            }
        }


        public static IEnumerable<PropertyInfo> GetProperties(Type item)
        {
            var tempChecksum = IsChecksum;
            var tempIgnoreIds = IgnoreIds;
            IsChecksum = false;
            IgnoreIds = false;

            var result = GetPropertyInfo(item);

            IsChecksum = tempChecksum;
            IgnoreIds = tempIgnoreIds;

            return result;
        }

        public static IEnumerable<FieldInfo> GetFields(Type item)
        {
            var tempChecksum = IsChecksum;
            var tempIgnoreIds = IgnoreIds;
            IsChecksum = false;
            IgnoreIds = false;

            var result = GetFieldInfo(item);

            IsChecksum = tempChecksum;
            IgnoreIds = tempIgnoreIds;

            return result;
        }


        public static void AddPrivateType(Type tp)
        {
            privateTypes.Add(tp);
        }

        /// <summary>
        ///   Caches and returns field info for a type
        /// </summary>
        /// <param name="itm"> The type that should have its field info returned </param>
        /// <returns> An enumeration of FieldInfo objects </returns>
        /// <remarks>
        ///   It should be noted that the implementation converts the enumeration returned from reflection to an array as this more than double the speed of subsequent reads
        /// </remarks>
        internal static IEnumerable<FieldInfo> GetFieldInfo(Type itm)
        {
            lock (FieldLists)
            {
                IEnumerable<FieldInfo> ret;


                if (FieldLists.ContainsKey(itm))
                {
                    ret = FieldLists[itm];
                }
                else
                {
                    var tp = itm;
                    var allowSimple = true;
                    var validNames = AttributeLists
                        .Where(p => p.Key.IsAssignableFrom(tp))
                        .SelectMany(p =>
                                        {
                                            allowSimple = allowSimple && p.Value.AllowAllSimple(tp);
                                            return p.Value.GetAttributeList(tp);
                                        }).ToList();

                    if (validNames.FirstOrDefault() == null)
                    {
                        validNames = null;
                    }
                    var parents = new List<Type>();
                    var allParents = new List<Type>();
                    var scan = tp;
                    var addAll = false;
                    do
                    {
                        if (privateTypes.Any(currentTypes.Contains) || addAll ||
                            scan.IsDefined(typeof (SerializeAll), false) || (scan.GetInterface("IEnumerator") != null))
                        {
                            if ((scan.GetInterface("IEnumerator") != null))
                            {
                                if (!addAll)
                                {
                                    addAll = true;
                                    privateTypes.Add(scan);
                                }
                            }
                            parents.Add(scan);
                        }
                        //So we can check for SerializeThis
                        allParents.Add(scan);
                        scan = scan.BaseType;
                    } while (scan != null);


                    ret = FieldLists[itm] = tp
                                                .GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                           BindingFlags.SetField | BindingFlags.Static)
							                    .Where(p=> (!tp.IsDefined(typeof (DoNotSerializePublic), true) ||
							             	          p.IsDefined(typeof (SerializeThis), true)))
                                                .Concat(
                                                    allParents.SelectMany(
                                                        type =>
                                                        type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic |
                                                                       BindingFlags.SetField | BindingFlags.Static).
                                                            Where(f => f.IsDefined(typeof (SerializeThis), true))))
                                                .Concat(
                                                    parents.SelectMany(
                                                        p =>
                                                        p.GetFields(BindingFlags.Instance | BindingFlags.NonPublic |
                                                                    BindingFlags.SetField)))
                                                .Concat(SerializePrivateFieldOfType.GetFields(tp))
                                                .Where(
                                                    p =>
                                                    !p.IsLiteral &&
                                                    !p.FieldType.IsDefined(typeof (DoNotSerialize), false) &&
								                                    

                                                    !p.IsDefined(typeof (DoNotSerialize), false) &&
                                                    CanSerializeType(p.FieldType) &&
                                                    (
                                                        (p.FieldType.IsValueType && allowSimple) ||
                                                        validNames == null ||
                                                        (validNames.Any(n => n == p.Name))
                                                    ))
                                                .Where(
                                                    p =>
                                                    !typeof (Component).IsAssignableFrom(tp) || tp == typeof (Component) || !componentNames.ContainsKey(p.Name))
                                                .ToArray();
                }

                return IsChecksum
                           ? ret.Where(p => !p.GetCustomAttributes(typeof (DoNotChecksum), true).Any())
                           : ret;
            }
        }

        /// <summary>
        ///   Returns a token that represents the name of the property
        /// </summary>
        /// <param name="name"> The name for which to return a token </param>
        /// <returns> A 2 byte token representing the name </returns>
        internal static ushort GetPropertyDefinitionId(string name)
        {
            ushort id;
            if (!PrewarmedNames.TryGetValue(name, out id))
            {
#if US_LOGGING
				Radical.Log("Prewarm miss on {0}", name);
#endif
                if (!_propertyLookup.TryGetValue(name, out id))
                {
                    id = (ushort) _propertyLookup.Count;
                    _propertyLookup[name] = id;
                }
            }
            return id;
        }


        public static object Deserialize(IStorage storage)
        {
            using (new SerializationScope())
            {
                var v = Verbose;
                Verbose = false;
                CreateStacks();
                try
                {
                    PushKnownTypes(false);
                    PushPropertyNames(false);

                    var serializer = storage;
                    serializer.StartDeserializing();
                    var ob = DeserializeObject(new Entry()
                                                   {
                                                       Name = "root"
                                                   }, serializer);
                    serializer.FinishedDeserializing();
                    return ob;
                }

                finally
                {
                    PopKnownTypes();
                    PopPropertyNames();
                    Verbose = v;
                }
            }
        }


        /// <summary>
        ///   Deserializes from a stream, potentially into an existing instance
        /// </summary>
        /// <param name="inputStream"> Stream to deserialize from </param>
        /// <param name="instance"> Instance to use </param>
        /// <returns> </returns>
        public static object Deserialize(Stream inputStream)
        {
            return Deserialize(inputStream, null);
        }

        public static object Deserialize(Stream inputStream, object instance)
        {
            // this version always uses the BinarySerializer
            using (new SerializationScope())
            {
                var v = Verbose;
                CreateStacks();
                try
                {
                    PushKnownTypes();
                    PushPropertyNames();

                    var rw = new BinaryReader(inputStream);
                    var version = rw.ReadString();
                    currentVersion = Int32.Parse(version.Substring(4));
                    if (currentVersion >= 5)
                    {
                        inputStream.Position = 0;
                        var serializer = new BinarySerializer(rw.ReadBytes((int) inputStream.Length));
                        serializer.StartDeserializing();
                        var ob = DeserializeObject(new Entry()
                                                       {
                                                           Name = "root",
                                                           Value = instance
                                                       }, serializer);
                        serializer.FinishedDeserializing();
                        return ob;
                    }

                    return null;
                }
                catch (Exception e)
                {
                    Radical.LogError("Serialization error: " + e.ToString());
                    return null;
                }
                finally
                {
                    PopKnownTypes();
					PopPropertyNames();
                    Verbose = v;
                }
            }
        }

        internal static void PopKnownTypes()
        {
			if(SerializationScope.IsPrimaryScope)
			{
	            var stackEntry = _knownTypesStack.Pop();
	            _knownTypesList = stackEntry.knownTypesList;
	            _knownTypesLookup = stackEntry.knownTypesLookup;
			}
        }

        private static void PushKnownTypes(bool clear)
        {
			if(SerializationScope.IsPrimaryScope)
			{
	            _knownTypesStack.Push(new KnownTypesStackEntry {knownTypesList = _knownTypesList, knownTypesLookup = _knownTypesLookup});
	            if (!clear)
	            {
	                return;
	            }
	            _knownTypesList = new List<Type>();
	            _knownTypesLookup = new Dictionary<Type, ushort>();
			}
			else
			{
	            _knownTypesList = _knownTypesList ?? new List<Type>();
	            _knownTypesLookup = _knownTypesLookup ?? new Dictionary<Type, ushort>();
			}
        }

        internal static void PushKnownTypes()
        {
            PushKnownTypes(true);
        }

        /// <summary>
        ///   Convert a previously serialized object from a byte array 
        ///   back into a .NET object
        /// </summary>
        /// <param name="bytes"> The data stream for the object </param>
        /// <returns> The rehydrated object represented by the data supplied </returns>
        public static object Deserialize(byte[] bytes)
        {
            using (new SerializationScope())
            {
                using (var inputStream = new MemoryStream(bytes))
                {
                    return Deserialize(inputStream);
                }
            }
        }

        /// <summary>
        ///   Convert a previously serialized object from a byte array 
        ///   back into a .NET object
        /// </summary>
        /// <param name="bytes"> The data stream for the object </param>
        /// <returns> The rehydrated object represented by the data supplied </returns>
        public static void DeserializeInto(byte[] bytes, object instance)
        {
            using (new SerializationScope())
            {
                using (var inputStream = new MemoryStream(bytes))
                {
                    Deserialize(inputStream, instance);
                }
            }
        }

        /// <summary>
        ///   Creates a set of stacks on the current thread
        /// </summary>
        private static void CreateStacks()
        {
            if (_propertyNamesStack == null)
            {
                _propertyNamesStack = new Stack<PropertyNameStackEntry>();
            }
            if (_knownTypesStack == null)
            {
                _knownTypesStack = new Stack<KnownTypesStackEntry>();
            }
            if (_loadedObjectStack == null)
            {
                _loadedObjectStack = new Stack<Dictionary<int, object>>();
            }
            if (_storedObjectsStack == null)
            {
                _storedObjectsStack = new Stack<Dictionary<object, int>>();
            }
            if (_seenTypesStack == null)
            {
                _seenTypesStack = new Stack<Dictionary<Type, bool>>();
            }
            if (_deferredStack == null)
            {
                _deferredStack = new Stack<List<DeferredSetter>>();
            }
            if (_finalActions == null)
            {
                _finalActions = new Stack<List<Action>>();
            }
            if (_idStack == null)
            {
                _idStack = new Stack<int>();
            }
        }


        public static void Serialize(object item, IStorage storage)
        {
            Serialize(item, storage, false);
        }

        public static void Serialize(object item, IStorage storage, bool forDeserializeInto)
        {
            var verbose = Verbose;
            Verbose = false;
            CreateStacks();
		
            using (new SerializationScope())
            {
				SerializationScope.SetPrimaryScope();
                try
                {
                    storage.StartSerializing();
                    SerializeObject(new Entry()
                                        {
                                            Name = "root",
                                            Value = item
                                        }, storage, forDeserializeInto);
                    storage.FinishedSerializing();
                }
                finally
                {
                    Verbose = verbose;
                }
            }
        }


        public static void Serialize(object item, Stream outputStream)
        {
            Serialize(item, outputStream, false);
        }

        public static void Serialize(object item, Stream outputStream, bool forDeserializeInto)
        {
            CreateStacks();


            using (new SerializationScope())
            {
				SerializationScope.SetPrimaryScope();

                //var serializer = Activator.CreateInstance(SerializerType) as IStorage;
                var serializer = new BinarySerializer();
                serializer.StartSerializing();
                SerializeObject(new Entry()
                                    {
                                        Name = "root",
                                        Value = item
                                    }, serializer, forDeserializeInto);
                serializer.FinishedSerializing();
                var outputWr = new BinaryWriter(outputStream);
                outputWr.Write(serializer.Data);
                outputWr.Flush();
                
            }
        }

        /// <summary>
        ///   Serialize an object into an array of bytes
        /// </summary>
        /// <param name="item"> The object to serialize </param>
        /// <returns> A byte array representation of the item </returns>
        public static byte[] Serialize(object item)
        {
            using (new SerializationScope())
            {
                using (var outputStream = new MemoryStream())
                {
                    Serialize(item, outputStream);
                    //Reset the verbose mode
                    return outputStream.ToArray();
                }
            }
        }


        public static byte[] SerializeForDeserializeInto(object item)
        {
            using (new SerializationScope())
            {
				using (var outputStream = new MemoryStream())
                {
                    Serialize(item, outputStream, true);
                    return outputStream.ToArray();
                }
			
            }
        }

        /// <summary>
        ///   Serialize an object into an array of bytes
        /// </summary>
        /// <param name="item"> The object to serialize </param>
        /// <param name="makeVerbose"> Whether the object should be serialized for forwards compatibility </param>
        /// <returns> A byte array representation of the item </returns>
        public static byte[] Serialize(object item, bool makeVerbose)
        {
            using (new SerializationScope())
            {
                using (var outputStream = new MemoryStream())
                {
                    var v = Verbose;
                    Verbose = makeVerbose;
                    Serialize(item, outputStream);
                    Verbose = v;
                    //Reset the verbose mode
                    return outputStream.ToArray();
                }
            }
        }

        /// <summary>
        ///   Return whether the type specified is a simple type that can be serialized fast
        /// </summary>
        /// <param name="tp"> The type to check </param>
        /// <returns> True if the type is a simple one and can be serialized directly </returns>
        private static bool IsSimpleType(Type tp)
        {
            return tp.IsPrimitive || tp == typeof (string) || tp.IsEnum || tp == typeof (DateTime) ||
                   tp == typeof (TimeSpan) || tp == typeof (Guid) || tp == typeof (decimal);
        }
		
		public static object currentlySerializingObject;
		
        private static void SerializeObjectAndProperties(object item, Type itemType, IStorage storage)
        {
#if US_LOGGING
			if(Radical.IsLogging())
			{
				Radical.Log ("[! {0}]", item.GetType().FullName);
				Radical.IndentLog();
			}
#endif
			var last = currentlySerializingObject;
			currentlySerializingObject = item;
            WriteFields(itemType, item, storage);
            WriteProperties(itemType, item, storage);
			currentlySerializingObject = last;
#if US_LOGGING
            if(Radical.IsLogging())
			{
				Radical.OutdentLog();
				Radical.Log ("[/! {0}]", item.GetType().FullName);
			}
#endif
        }
		
		private static GameObject _componentHelper;

        /// <summary>
        ///   Create an instance of a type
        /// </summary>
        /// <param name="itemType"> The type to construct </param>
        /// <returns> </returns>
        internal static object CreateObject(Type itemType)
        {
            try
            {
                if (Creators.ContainsKey(itemType))
                {
                    return Creators[itemType].Create(itemType);
                }
                if (typeof (Component).IsAssignableFrom(itemType))
                {
					if(typeof(Transform).IsAssignableFrom(itemType))
						return null;
					if(_componentHelper == null)
					{
						_componentHelper = new GameObject("Component Helper") { hideFlags = HideFlags.HideAndDontSave };
                        _componentHelper.SetActive(false);
					}
					try
					{
						var component = _componentHelper.GetComponent(itemType);
						if(component == null)
							component = _componentHelper.AddComponent(itemType);
						
	                    return component;
					}
					catch
					{
						return null;
					}
                }
                if (itemType.IsSubclassOf(typeof (ScriptableObject)))
                {
                    return ScriptableObject.CreateInstance(itemType);
                }
                return itemType.IsDefined(typeof (CreateUsingEvent), false)
                           ? CreateInstance(itemType)
                           : Activator.CreateInstance(itemType);
            }
            catch (Exception)
            {
                try
                {
                    var constructorInfo =
                        itemType.GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic,
                                                null, new Type[] {}, null);
                    return constructorInfo != null ? constructorInfo.Invoke(new object[] {}) : CreateInstance(itemType);
                }
                catch
                {
                    return CreateInstance(itemType);
                }
            }
        }

        public static void DummyAction()
        {
        }

        private static object CreateInstance(Type itemType)
        {
            //Raise an event to construct the object
            var ct = new ObjectMappingEventArgs {TypeToConstruct = itemType};
            if (typeof (MulticastDelegate).IsAssignableFrom(itemType))
            {
                return
                    (object)
                    Delegate.CreateDelegate(typeof (Action),
                                            typeof (UnitySerializer).GetMethod("DummyAction",
                                                                               BindingFlags.Public | BindingFlags.Static));
            }
            InvokeCreateType(ct);
            //Check if we created the right thing
            if (ct.Instance != null &&
                (ct.Instance.GetType() == itemType || ct.Instance.GetType().IsSubclassOf(itemType)))
            {
                return ct.Instance;
            }

            var error =
                string.Format(
                    "Could not construct an object of type '{0}', it must be creatable in this scope and have a default parameterless constructor or you should handle the CreateType event on UnitySerializer to construct the object",
                    itemType.FullName);
            throw new MissingConstructorException(error);
        }


		
        /// <summary>
        ///   Logs a type and returns a unique token for it
        /// </summary>
        /// <param name="tp"> The type to retrieve a token for </param>
        /// <returns> A 2 byte token representing the type </returns>
        internal static ushort GetTypeId(Type tp)
        {
            ushort tpId;
            if (!PrewarmedTypes.TryGetValue(tp, out tpId))
            {
                if (!_knownTypesLookup.TryGetValue(tp, out tpId))
                {
                    tpId = (ushort) _knownTypesLookup.Count;
                    _knownTypesLookup[tp] = tpId;
                }
            }
            return tpId;
        }

        /// <summary>
        ///   Gets a property setter and a standard default type for an entry
        /// </summary>
        /// <param name="entry"> </param>
        private static void UpdateEntryWithName(Entry entry)
        {
            if (entry.Name == null)
            {
                Radical.Log("Invalid Entry");
            }
            Dictionary<string, EntryConfiguration> configurations;
            if (!StoredTypes.TryGetValue(entry.OwningType, out configurations))
            {
                configurations = new Dictionary<string, EntryConfiguration>();
                StoredTypes[entry.OwningType] = configurations;
            }

            EntryConfiguration entryConfiguration;
            if (!configurations.TryGetValue(entry.Name, out entryConfiguration))
            {
                entryConfiguration = new EntryConfiguration();

                var pi = entry.OwningType.GetProperty(entry.Name,
                                                      BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (pi != null)
                {
                    entryConfiguration.Type = pi.PropertyType;
                    entryConfiguration.Setter = new GetSetGeneric(pi);
                }
                else
                {
                    var fi = GetField(entry.OwningType, entry.Name);
                    if (fi != null)
                    {
                        entryConfiguration.Type = fi.FieldType;
                        entryConfiguration.Setter = new GetSetGeneric(fi);
                    }
                }
                configurations[entry.Name] = entryConfiguration;
            }
            entry.StoredType = entryConfiguration.Type;
            entry.Setter = entryConfiguration.Setter;
        }

        private static FieldInfo GetField(Type tp, string name)
        {
            FieldInfo fi = null;
            while (tp != null &&
                   (fi =
                    tp.GetField(name,
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                BindingFlags.Static)) == null)
            {
                tp = tp.BaseType;
            }
            return fi;
        }

        #region Serialization

        private static readonly Dictionary<Type, ISerializeObject> cachedSerializers =
            new Dictionary<Type, ISerializeObject>();

        private static void SerializeObject(Entry entry, IStorage storage)
        {
            SerializeObject(entry, storage, false);
        }

        private static bool CompareToNull(object o)
        {
            return (o is Object) ? !(bool) ((Object) o) : o == null;
        }

        private static void SerializeObject(Entry entry, IStorage storage, bool first)
        {
#if US_LOGGING
			if(Radical.IsLogging())
			{
				Radical.Log ("{1} - {0}", entry.Name, _nextId.ToString ());
			}
#endif
            var item = entry.Value;
            var objectId = _nextId++;
            if (CompareToNull(item))
            {
                entry.Value = new Nuller();
                item = entry.Value;
            }

            if (storage.StartSerializing(entry, objectId))
            {
                _seenObjects[item] = objectId;
                return;
            }

            var itemType = item.GetType();
            using (new TypePusher(itemType))
            {
                //Check for simple types again
                if (IsSimpleType(itemType))
                {
                    storage.WriteSimpleValue(itemType.IsEnum
                                                 ? Convert.ChangeType(item, Enum.GetUnderlyingType(itemType),
                                                                      CultureInfo.InvariantCulture)
                                                 : item);
                    return;
                }

                //Check whether this object has been seen
                if (!(itemType.IsValueType) && _seenObjects.ContainsKey(item) && !first)
                {
                    storage.BeginWriteObject(_seenObjects[item], item.GetType(), true);
                    storage.EndWriteObject();
                    return;
                }


                var skipRecord = false;

                if (!first)
                {
                    //Check for custom serialization
                    if (Serializers.ContainsKey(itemType))
                    {
                        //If we have a custom serializer then use it!
                        storage.BeginWriteObject(objectId, itemType, false);
                        storage.BeginWriteProperty("data", typeof (object[]));
                        var serializer = Serializers[itemType];
                        var data = serializer.Serialize(item);
						using(new SerializationSplitScope())
						{
	                        SerializeObject(new Entry()
	                                            {
	                                                Name = "data",
	                                                Value = data,
	                                                StoredType = typeof (object[])
	                                            }, storage);
						}
                        storage.EndWriteProperty();
                        storage.EndWriteObject();
						_seenObjects[item] = objectId;
                        return;
                    }

                    ISerializeObject serializeObject;
                    if (!cachedSerializers.TryGetValue(itemType, out serializeObject))
                    {
                        foreach (var tp in SubTypeSerializers)
                        {
                            if (!tp.Key.IsAssignableFrom(itemType) ||
                                (tp.Value.GetType().IsDefined(typeof (OnlyInterfaces), false) && !itemType.IsInterface))
                            {
                                continue;
                            }
                            serializeObject = tp.Value;
                            break;
                        }
                        cachedSerializers[itemType] = serializeObject;
                    }
                    if (serializeObject != null)
                    {
                        if (!(serializeObject is ISerializeObjectEx) || (serializeObject as ISerializeObjectEx).CanSerialize(itemType, entry.Value))
                        {
                            //If we have a custom serializer then use it!
                            storage.BeginWriteObject(objectId, itemType, false);
                            storage.BeginWriteProperty("data", typeof (object[]));
                            var data = serializeObject.Serialize(item);
							using(new SerializationSplitScope())
							{
	                            SerializeObject(new Entry()
	                                                {
	                                                    Name = "data",
	                                                    Value = data,
	                                                    StoredType = typeof (object[])
	                                                }, storage);
							}
                            storage.EndWriteProperty();
                            storage.EndWriteObject();
							_seenObjects[item] = objectId;
                        }
                        return;
                    }
                }
                else
                {
                    skipRecord = true;
                }


                //We are going to serialize an object
                if (!skipRecord && !itemType.IsValueType)
                {
					//Debug.Log(objectId.ToString() + " : " + item.ToString());
                    _seenObjects[item] = objectId;
                }
                storage.BeginWriteObject(objectId, itemType, false);

                //Check for collection types)
                if (item is Array)
                {
                    if (((Array) item).Rank == 1)
                    {
                        SerializeArray(item as Array, itemType, storage);
                    }
                    else
                    {
                        SerializeMultiDimensionArray(item as Array, itemType, storage);
                    }
                    storage.EndWriteObject();
                    return;
                }
                else if (item is IDictionary)
                {
                    SerializeDictionary(item as IDictionary, itemType, storage);
                }
                else if (item is IList)
                {
                    SerializeList(item as IList, itemType, storage);
                }

                //storageOtherwise we are serializing an object
                SerializeObjectAndProperties(item, itemType, storage);
                storage.EndWriteObject();
            }
        }

        private static void SerializeList(ICollection item, Type tp, IStorage storage)
        {
            Type valueType = null;
            //Try to optimize the storage of types based on the type of list
            if (tp.IsGenericType)
            {
                var types = tp.GetGenericArguments();
                valueType = types[0];
            }

            //storage.WriteValue("no_items", item.Count);
            storage.BeginWriteList(item.Count, item.GetType());
            var entry = new Entry();

            var id = 0;
            foreach (var val in item)
            {
                entry.Value = val;
                entry.StoredType = valueType;
                if(!storage.BeginWriteListItem(id++, val))
                    SerializeObject(entry, storage);
                storage.EndWriteListItem();
            }
            storage.EndWriteList();
        }

        private static void SerializeDictionary(IDictionary item, Type tp, IStorage storage)
        {
            Type keyType = null;
            Type valueType = null;
            //Try to optimise storage based on the type of dictionary
            if (tp.IsGenericType)
            {
                var types = tp.GetGenericArguments();
                keyType = types[0];
                valueType = types[1];
            }

            //storage.WriteValue("no_items", item.Count);
            storage.BeginWriteDictionary(item.Count, item.GetType());
			storage.BeginWriteDictionaryKeys();
            //Serialize the pairs
            var id = 0;
            foreach (var key in item.Keys)
            {
                if(!storage.BeginWriteDictionaryKey(id++,key))
                    SerializeObject(new Entry
                                    {
                                        StoredType = keyType,
                                        Value = key
                                    }, storage);
                storage.EndWriteDictionaryKey();
            }
			storage.EndWriteDictionaryKeys();
			storage.BeginWriteDictionaryValues();
            id = 0;
            foreach (var val in item.Values)
            {
                if(!storage.BeginWriteDictionaryValue(id++, val))
                    SerializeObject(new Entry
                                    {
                                        StoredType = valueType,
                                        Value = val
                                    }, storage);
                storage.EndWriteDictionaryValue();
            }
			storage.EndWriteDictionaryValues();
            storage.EndWriteDictionary();
        }

        private static void SerializeArray(Array item, Type tp, IStorage storage)
        {
            var elementType = tp.GetElementType();
            if (IsSimpleType(elementType))
            {
                storage.WriteSimpleArray(item.Length, item);
            }
            else
            {
                var length = item.Length;
                storage.BeginWriteObjectArray(length, item.GetType());
                for (var l = 0; l < length; l++)
                {
                    var val = item.GetValue(l);
                    if(!storage.BeginWriteObjectArrayItem(l, val))
                        SerializeObject(new Entry()
                                        {
                                            Value = item.GetValue(l),
                                            StoredType = elementType
                                        }, storage);
                    storage.EndWriteObjectArrayItem();
                }
                storage.EndWriteObjectArray();
            }
        }

        private static void SerializeMultiDimensionArray(Array item, Type tp, IStorage storage)
        {
            // Multi-dimension serializer data is:
            // Int32: Ranks
            // Int32 (x number of ranks): length of array dimension 

            var dimensions = item.Rank;

            var length = item.GetLength(0);

            // Determine the number of cols being populated
            //var cols = item.GetLength(item.Rank - 1);

            // Explicitly write this value, to denote that this is a multi-dimensional array
            // so it doesn't break the deserializer when reading values for existing arrays

            storage.BeginMultiDimensionArray(item.GetType(), dimensions, length);


            var indicies = new int[dimensions];

            // Write out the length of each array, if we are dealing with the first array
            for (var arrayStartIndex = 0; arrayStartIndex < dimensions; arrayStartIndex++)
            {
                indicies[arrayStartIndex] = 0;
                //storage.WriteValue("dim_len" + arrayStartIndex, item.GetLength(arrayStartIndex));
                storage.WriteArrayDimension(arrayStartIndex, item.GetLength(arrayStartIndex));
            }

            SerializeArrayPart(item, 0, indicies, storage);

            storage.EndMultiDimensionArray();
        }

        private static void SerializeArrayPart(Array item, int i, int[] indices, IStorage storage)
        {
            var length = item.GetLength(i);
            for (var l = 0; l < length; l++)
            {
                indices[i] = l;
                if (i != item.Rank - 2)
                {
                    SerializeArrayPart(item, i + 1, indices, storage);
                }
                else
                {
                    var arrayType = item.GetType().GetElementType();
                    var cols = item.GetLength(i + 1);

                    var baseArray = Array.CreateInstance(arrayType, cols);

                    // Convert the whole multi-dimensional array to be 'row' based
                    // and serialize using the existing code
                    for (var arrayStartIndex = 0; arrayStartIndex < cols; arrayStartIndex++)
                    {
                        indices[i + 1] = arrayStartIndex;
                        baseArray.SetValue(item.GetValue(indices), arrayStartIndex);
                    }

                    SerializeArray(baseArray, baseArray.GetType(), storage);
                }
            }
        }


        private static void WriteProperties(Type itemType, object item, IStorage storage)
        {
            var seen = _seenTypes.ContainsKey(itemType) && _seenTypes[itemType];
            _seenTypes[itemType] = true;
            var propList = GetWritableAttributes.GetProperties(item, seen);
            storage.BeginWriteProperties(propList.Length);

            foreach (var entry in propList)
            {
                storage.BeginWriteProperty(entry.Name, entry.PropertyInfo.PropertyType);
                SerializeObject(entry, storage, false);
                storage.EndWriteProperty();
            }
            storage.EndWriteProperties();
        }


        private static void WriteFields(Type itemType, object item, IStorage storage)
        {
            var seen = _seenTypes.ContainsKey(itemType);
            if (!seen)
            {
                _seenTypes[itemType] = false;
            }
            var fieldList = GetWritableAttributes.GetFields(item, seen);
            storage.BeginWriteFields(fieldList.Length);
            foreach (var entry in fieldList)
            {
                storage.BeginWriteField(entry.Name, entry.FieldInfo.FieldType);
                SerializeObject(entry, storage, false);
                storage.EndWriteField();
            }
            storage.EndWriteFields();
        }

        #region Nested type: Nuller

        public class Nuller
        {
        }

        #endregion

        #region Nested type: TypePusher

        private class TypePusher : IDisposable
        {
            public TypePusher(Type t)
            {
                currentTypes.Push(t);
            }

            #region IDisposable Members

            public void Dispose()
            {
                currentTypes.Pop();
            }

            #endregion
        }

        #endregion

        #endregion

        #region New Deserialization

        #region Delegates

        public delegate object GetData(Dictionary<string, object> parameters);

        #endregion

        public static object DeserializingObject;
        private static readonly Stack<object> DeserializingStack = new Stack<object>();
        internal static  List<DeferredSetter> FixupFunctions = new List<DeferredSetter>();
        internal static  List<IDeserialized> DeserializedObject = new List<IDeserialized>();

        /// <summary>
        ///   Deserializes an object or primitive from the stream
        /// </summary>
        /// <param name="entry"> </param>
        /// <param name="storage"> </param>
        /// <returns> The value read from the file </returns>
        /// <remarks>
        ///   The function is supplied with the type of the property that the object was stored in (if known) this enables
        ///   a compact format where types only have to be specified if they differ from the expected one
        /// </remarks>
        internal static object DeserializeObject(Entry entry, IStorage storage)
        {
            try
            {
                var objectID = _nextId++;
                //Get a name for the item
                storage.DeserializeGetName(entry);
                //Update the core info including a property getter
                if (entry.MustHaveName)
                {
                    UpdateEntryWithName(entry);
                }
#if US_LOGGING
				if(Radical.IsLogging())
				{
					Radical.Log ("{1} - {0}", entry.Name, objectID.ToString ());
					Radical.Log ("<Object {0}>", entry.Name);
					Radical.IndentLog ();
				}
#endif
                //Start to deserialize
                var candidate = storage.StartDeserializing(entry);
                if (candidate != null)
                {
                    storage.FinishDeserializing(entry);
                    return candidate;
                }


                var itemType = entry.StoredType;

                if (itemType == null)
                {
                    return null;
                }

                object obj = null, result2 = null;

                //Check if this is a simple value and read it if so
                if (IsSimpleType(itemType))
                {
                    if (itemType.IsEnum)
                    {
                        return Enum.Parse(itemType, storage.ReadSimpleValue(Enum.GetUnderlyingType(itemType)).ToString(),
                                          true);
                    }
                    return storage.ReadSimpleValue(itemType);
                }

                //See if we should lookup this object or create a new one
                bool isReference;
                var existingId = storage.BeginReadObject(out isReference);
                if (existingId != -1 && entry.Value == null)
                {
                    if (isReference)
                    {
                        try
                        {
                            var o = _loadedObjects[existingId];
							storage.EndReadObject();
							return o;
                        }
                        catch
                        {
                            throw new SerializationException(
                                "Error when trying to link to a previously seen object. The stream gave an object id of " +
                                existingId + " but that was not found.  It is possible that an" +
                                "error has caused the data stream to become corrupt and that this id is wildly out of range.  Ids should be sequential numbers starting at 1 for the first object or value seen and then incrementing thereafter.");
                        }
                    }
                }
				if(entry.Value!=null)
                	_loadedObjects[objectID] = entry.Value;

                //Only custom serialize if the object hasn't already been created
                //this is normally only tr
                if (entry.Value == null)
                {
                    //Check for custom serialization
                    if (Serializers.ContainsKey(itemType))
                    {
                        //Read the serializer and its data
                        var serializer = Serializers[itemType];
                        var nentry = new Entry
                                         {
                                             Name = "data",
                                             StoredType = typeof (object[])
                                         };
                        storage.BeginReadProperty(nentry);
						object[] data = null;
						using(new SerializationSplitScope())
						{
	                        data =
	                            (object[])
	                            DeserializeObject(nentry, storage);
						}
                        var result = serializer.Deserialize(data, entry.Value);
                        storage.EndReadProperty();
                        storage.EndReadObject();
                        _loadedObjects[objectID] = result;
                        storage.FinishDeserializing(entry);
                        return result;
                    }
                    ISerializeObject serializeObject;
                    if (!cachedSerializers.TryGetValue(itemType, out serializeObject))
                    {
                        serializeObject = null;
                        foreach (var tp in SubTypeSerializers)
                        {
                            if (!tp.Key.IsAssignableFrom(itemType) ||
                                (tp.Value.GetType().IsDefined(typeof (OnlyInterfaces), false) && !itemType.IsInterface))
                            {
                                continue;
                            }
                            serializeObject = tp.Value;
                            break;
                        }
                        
                        cachedSerializers[itemType] = serializeObject;
                    }
                    if (serializeObject != null)
                    {
                        if (
                            !(serializeObject is ISerializeObjectEx) || (serializeObject as ISerializeObjectEx).CanSerialize(itemType, entry.Value))
                        {
                            var nentry = new Entry
                                             {
                                                 Name = "data",
                                                 StoredType = typeof (object[])
                                             };
                            storage.BeginReadProperty(nentry);
                            //If we have a custom serializer then use it!
							object[] data;
							using(new SerializationSplitScope())
							{
	                            data =
	                                (object[])
	                                DeserializeObject(nentry, storage);
	
							}
                            var result = serializeObject.Deserialize(data, entry.Value);
                            storage.EndReadProperty();
                            storage.EndReadObject();
                            _loadedObjects[objectID] = result;
                            storage.FinishDeserializing(entry);
                            return result;
                        }
                    }
                }

                //Otherwise create the object
                if (itemType.IsArray)
                {
                    int baseCount;
                    var isMultiDimensionArray = storage.IsMultiDimensionalArray(out baseCount);

                    if (isMultiDimensionArray)
                    {
                        var result = DeserializeMultiDimensionArray(itemType, storage, objectID);
                        storage.EndReadObject();
                        _loadedObjects[objectID] = result;
                        storage.FinishDeserializing(entry);
                        return result;
                    }
                    else
                    {
                        var result = DeserializeArray(itemType, storage, baseCount, objectID);
                        storage.EndReadObject();
                        _loadedObjects[objectID] = result;
                        storage.FinishDeserializing(entry);
                        return result;
                    }
                }

                obj = entry.Value ?? CreateObject(itemType);
                if (itemType.IsValueType)
                {
                    obj = RuntimeHelpers.GetObjectValue(obj);
                }
                _loadedObjects[objectID] = obj;

                //Check for collection types)
                if (obj is IDictionary)
                {
                    DeserializeDictionary(obj as IDictionary, itemType, storage);
                }
                if (obj is IList)
                {
                    DeserializeList(obj as IList, itemType, storage);
                }

                //Otherwise we are serializing an object
                result2 = DeserializeObjectAndProperties(obj, itemType, storage);
                storage.EndReadObject();
                if (result2 is IDeserialized)
                {
                    DeserializedObject.Add(result2 as IDeserialized);
                }

                //Check for null
                if (obj is Nuller)
                {
                    return null;
                }
                return result2;
            }
            finally
            {
#if US_LOGGING
				if(Radical.IsLogging())
				{
					Radical.OutdentLog ();
					Radical.Log ("</Object {0}>", entry.Name);
				}
#endif
            }
        }

        /// <summary>
        ///   Deserializes an array of values
        /// </summary>
        /// <param name="itemType"> The type of the array </param>
        /// <param name="storage"> </param>
        /// <param name="count"> </param>
        /// <returns> The deserialized array </returns>
        /// <remarks>
        ///   This routine optimizes for arrays of primitives and bytes
        /// </remarks>
        private static object DeserializeArray(Type itemType, IStorage storage, int count, int objectID)
        {
            var elementType = itemType.GetElementType();
            Array result = null;

            if (IsSimpleType(elementType))
            {
                result = storage.ReadSimpleArray(elementType, count);
                _loadedObjects[objectID] = result;
            }
            else
            {
                if (count == -1)
                {
                    count = storage.BeginReadObjectArray(itemType);
                }
                result = Array.CreateInstance(elementType, count);
                _loadedObjects[objectID] = result;

                for (var l = 0; count==-1 ? storage.HasMore() : l < count; l++)
                {
					var entry = new Entry()
                                                      {
                                                          StoredType = elementType
                                                      };
                    var value = storage.BeginReadObjectArrayItem(l, entry);
                    value = value ?? DeserializeObject(entry, storage);
                    if (value != null && value.GetType().IsDefined(typeof (DeferredAttribute), true))
                    {
                        var toSet = value;
                        value = new DeferredSetter(d => toSet);
                    }

                    if (value is DeferredSetter)
                    {
                        var st = value as DeferredSetter;
                        var pos = l;
                        var nd = new DeferredSetter(st.deferredRetrievalFunction) {enabled = st.enabled};
                        nd._setAction = () =>
                                           {
                                               if (result != null)
                                               {
                                                   result.SetValue(nd.deferredRetrievalFunction(st.parameters), pos);
                                               }
                                           };
                        AddFixup(nd);
                    }
                    else
                    {
                        result.SetValue(value, l);
                    }

                    storage.EndReadObjectArrayItem();
                }
                if (count != -1)
                {
                    storage.EndReadObjectArray();
                }
            }


            return result;
        }


        /// <summary>
        ///   Deserializes a multi-dimensional array of values
        /// </summary>
        /// <param name="itemType"> The type of the array </param>
        /// <param name="storage"> </param>
        /// <param name="objectID"> </param>
        /// <returns> The deserialized array </returns>
        /// <remarks>
        ///   This routine deserializes values serialized on a 'row by row' basis, and
        ///   calls into DeserializeArray to do this
        /// </remarks>
        private static object DeserializeMultiDimensionArray(Type itemType, IStorage storage, int objectID)
        {
            //Read the number of dimensions the array has
            //var dimensions = storage.ReadValue<int>("dimensions");
            //var totalLength = storage.ReadValue<int>("length");
            int dimensions, totalLength;
            storage.BeginReadMultiDimensionalArray(out dimensions, out totalLength);

            // Establish the length of each array element
            // and get the total 'row size'
            var lengths = new int[dimensions];
            var indices = new int[dimensions];

            for (var item = 0; item < dimensions; item++)
            {
                lengths[item] = storage.ReadArrayDimension(item); //.ReadValue<int>("dim_len" + item);
                indices[item] = 0;
            }
            //Get the expected element type
            var elementType = itemType.GetElementType();

            var sourceArrays = Array.CreateInstance(elementType, lengths);
            DeserializeArrayPart(sourceArrays, 0, indices, itemType, storage, objectID);
            return sourceArrays;
        }

        private static void DeserializeArrayPart(Array sourceArrays, int i, int[] indices, Type itemType,
                                                 IStorage storage, int objectID)
        {
            var length = sourceArrays.GetLength(i);
            for (var l = 0; l < length; l++)
            {
                indices[i] = l;
                if (i != sourceArrays.Rank - 2)
                {
                    DeserializeArrayPart(sourceArrays, i + 1, indices, itemType, storage, objectID);
                }
                else
                {
                    var sourceArray = (Array) DeserializeArray(itemType, storage, -1, objectID);
                    var cols = sourceArrays.GetLength(i + 1);
                    for (var arrayStartIndex = 0; arrayStartIndex < cols; arrayStartIndex++)
                    {
                        indices[i + 1] = arrayStartIndex;
                        sourceArrays.SetValue(sourceArray.GetValue(arrayStartIndex), indices);
                    }
                }
            }
        }

        /// <summary>
        ///   Deserializes a dictionary from storage, handles generic types with storage optimization
        /// </summary>
        /// <param name="o"> The newly created dictionary </param>
        /// <param name="itemType"> The type of the dictionary </param>
        /// <param name="storage"> </param>
        /// <returns> The dictionary object updated with the values from storage </returns>
        private static object DeserializeDictionary(IDictionary o, Type itemType, IStorage storage)
        {
            Type keyType = null;
            Type valueType = null;
            if (itemType.IsGenericType)
            {
                var types = itemType.GetGenericArguments();
                keyType = types[0];
                valueType = types[1];
            }

            var count = storage.BeginReadDictionary(keyType, valueType);
			storage.BeginReadDictionaryKeys();
            var list = new List<object>();
            for (var i = 0; count==-1 ? storage.HasMore() : i < count; i++)
            {
				var entry = new Entry()
                                                  {
                                                      StoredType = keyType
                                                  };
                var value = storage.BeginReadDictionaryKeyItem(i, entry) ??
                    DeserializeObject(entry, storage);
                if (value.GetType().IsDefined(typeof (DeferredAttribute), true))
                {
                    var toSet = value;
                    value = new DeferredSetter(d => toSet);
                }

                if (value is DeferredSetter)
                {
                    var st = value as DeferredSetter;
                    var nd = new DeferredSetter(st.deferredRetrievalFunction) {enabled = st.enabled};
                    list.Add(null);
                    var c = list.Count - 1;
                    nd._setAction = () =>
                                       {
                                           if (list.Count > c)
                                           {
                                               list[c] = nd.deferredRetrievalFunction(st.parameters);
                                           }
                                       };
                    AddFixup(nd);
                }
                else
                {
                    list.Add(value);
                }
                storage.EndReadDictionaryKeyItem();
            }
			storage.EndReadDictionaryKeys();
			storage.BeginReadDictionaryValues();
            for (var i = 0; count==-1 ? storage.HasMore() : i < count; i++)
            {
				var entry = new Entry()
                                                  {
                                                      StoredType = valueType
                                                  };
                var value = storage.BeginReadDictionaryValueItem(i, entry) ??
                    DeserializeObject(entry, storage);
                if (value != null && value.GetType().IsDefined(typeof (DeferredAttribute), true) || list[i] == null)
                {
                    var toSet = value;
                    value = new DeferredSetter(d => toSet);
                }

                if (value is DeferredSetter)
                {
                    var st = value as DeferredSetter;
                    var nd = new DeferredSetter(st.deferredRetrievalFunction) {enabled = st.enabled};
                    var index = i;
                    nd._setAction = () =>
                                       {
                                           if (o != null && list != null)
                                           {
                                               o[list[index]] = nd.deferredRetrievalFunction(st.parameters);
                                           }
                                       };
                    AddFixup(nd);
                }
                else
                {
                    o[list[i]] = value;
                }
                storage.EndReadDictionaryValueItem();
            }
			storage.EndReadDictionaryValues();
            storage.EndReadDictionary();

            if (currentVersion >= 7 && currentVersion < 9)
            {
                DeserializeObjectAndProperties(o, itemType, storage);
            }

            return o;
        }

        /// <summary>
        ///   Deserialize a list from the data stream
        /// </summary>
        /// <param name="o"> The newly created list </param>
        /// <param name="itemType"> The type of the list </param>
        /// <param name="storage"> </param>
        /// <returns> The list updated with values from the stream </returns>
        private static object DeserializeList(IList o, Type itemType, IStorage storage)
        {
            Type valueType = null;
            if (itemType.IsGenericType)
            {
                var types = itemType.GetGenericArguments();
                valueType = types[0];
            }

            var count = storage.BeginReadList(valueType);
            for (var i = 0; count==-1 ? storage.HasMore() : i < count; i++)
            {
				var entry = new Entry()
                                                  {
                                                      StoredType = valueType,
                                                  };
                var value = storage.BeginReadListItem(i, entry) ?? 
                     DeserializeObject(entry, storage);
                if (value != null && value.GetType().IsDefined(typeof (DeferredAttribute), true))
                {
                    var toSet = value;
                    value = new DeferredSetter(d => toSet);
                }

                if (value is DeferredSetter)
                {
                    var st = value as DeferredSetter;
                    var nd = new DeferredSetter(st.deferredRetrievalFunction) {enabled = st.enabled};
                    nd._setAction = () =>
                                       {
                                           if (o != null)
                                           {
                                               o.Add(nd.deferredRetrievalFunction(st.parameters));
                                           }
                                       };
                    AddFixup(nd);
                }
                else
                {
                    o.Add(value);
                }
                storage.EndReadListItem();
            }
            if (currentVersion >= 7 && currentVersion < 9)
            {
                DeserializeObjectAndProperties(o, itemType, storage);
            }

            storage.EndReadList();
            return o;
        }

        /// <summary>
        ///   Deserializes a class based object that is not a collection, looks for both public properties and fields
        /// </summary>
        /// <param name="o"> The object being deserialized </param>
        /// <param name="itemType"> The type of the object </param>
        /// <param name="storage"> </param>
        /// <returns> The object updated with values from the stream </returns>
        private static object DeserializeObjectAndProperties(object o, Type itemType, IStorage storage)
        {
            DeserializingStack.Push(DeserializingObject);
            try
            {
				var last = currentlySerializingObject;
				currentlySerializingObject = o;
                DeserializingObject = o;
#if US_LOGGING
				Radical.Log ("[! {0}]", o.GetType().FullName);
				Radical.IndentLog();
#endif
                DeserializeFields(storage, itemType, o);
                DeserializeProperties(storage, itemType, o);
				currentlySerializingObject = last;
#if US_LOGGING
	            Radical.OutdentLog();
				Radical.Log ("[/! {0}]", o.GetType().FullName);
#endif
                return o;
            }
            finally
            {
                DeserializingObject = DeserializingStack.Pop();
            }
        }

        /// <summary>
        ///   Deserializes the properties of an object from the stream
        /// </summary>
        /// <param name="storage"> </param>
        /// <param name="itemType"> The type of the object </param>
        /// <param name="o"> The object to deserialize </param>
        private static void DeserializeProperties(IStorage storage, Type itemType, object o)
        {
            //Get the number of properties
            //var propCount = storage.ReadValue<byte>("property_count");
            var propCount = storage.BeginReadProperties();

            for (var i = 0; propCount !=-1 ? i < propCount : storage.HasMore(); i++)
            {
                //Deserialize the value
                var entry = storage.BeginReadProperty(new Entry
                                                          {
                                                              OwningType = itemType,
                                                              MustHaveName = true
                                                          });
                var value = DeserializeObject(entry, storage);
#if US_LOGGING
				if(Radical.IsLogging())
				{
					Radical.Log (string.Format("Property {0} : {1}", entry.Name, value.GetType().FullName));
				}
#endif
                if (entry.Setter != null && value != null)
                {
                    try
                    {
                        if (value.GetType().IsDefined(typeof (DeferredAttribute), true))
                        {
                            var toSet = value;
                            value = new DeferredSetter(d => toSet);
                        }
                        if (value is DeferredSetter)
                        {
                            //We need to account for there being multiple items with the same 
                            var setter = value as DeferredSetter;
                            var ns = new DeferredSetter(setter.deferredRetrievalFunction) {enabled = setter.enabled};
                            ns._setAction = () => entry.Setter.Set(o,
                                                                  setter.deferredRetrievalFunction(
                                                                      setter.parameters));
                            if (entry.OwningType.IsValueType)
                            {
                                ns._setAction();
                            }
                            else
                            {
                                AddFixup(ns);
                            }
                        } 
                        else
                        {
                            entry.Setter.Set(o, value);
                        }
                    }
                    catch (ArgumentException)
                    {
                        try
                        {
                            // if the property is nullable enum we need to handle it differently because a straight ChangeType doesn't work
                            // TODO maybe adjust persistence to have a nullable bit in propertyindex?
                            var type = Nullable.GetUnderlyingType(entry.Setter.Info.PropertyType);
                            if (type != null && type.IsEnum)
                            {
                                entry.Setter.Info.SetValue(o, Enum.Parse(type, value.ToString(), true), null);
                            }
                            else
                            {
                                entry.Setter.Info.SetValue(o,
                                                           Convert.ChangeType(value, entry.Setter.Info.PropertyType,
                                                                              null), null);
                            }
                        }
                        catch (Exception e)
                        {
                            Radical.LogError("Serialization error: " + e.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        //Suppress cases where the old value is no longer compatible with the new property type
                        Radical.LogError("Serialization error: " + e.ToString());
                    }
                }
                storage.EndReadProperty();
            }
            storage.EndReadProperties();
        }

        /// <summary>
        ///   Deserializes the fields of an object from the stream
        /// </summary>
        /// <param name="storage"> </param>
        /// <param name="itemType"> The type of the object </param>
        /// <param name="o"> The object to deserialize </param>
        private static void DeserializeFields(IStorage storage, Type itemType, object o)
        {
            var fieldCount = storage.BeginReadFields();

            for (var i = 0; fieldCount==-1 ?  storage.HasMore() : i < fieldCount; i++)
            {
                var entry = storage.BeginReadField(new Entry()
                                                       {
                                                           OwningType = itemType,
                                                           MustHaveName = true
                                                       });
                var value = DeserializeObject(entry, storage);
#if US_LOGGING
				if(Radical.IsLogging())
				{
					Radical.Log (string.Format("Field {0} : {1}", entry.Name, value == null ? "null" : value.GetType().FullName));
				}
#endif
                if (entry.Setter != null && value != null)
                {
                    try
                    {
                        if (value.GetType().IsDefined(typeof (DeferredAttribute), true))
                        {
                            var toSet = value;
                            value = new DeferredSetter(d => toSet);
                        }

                        if (value is DeferredSetter)
                        {
                            //We need to account for there being multiple items with the same 
                            var setter = value as DeferredSetter;
                            var ns = new DeferredSetter(setter.deferredRetrievalFunction)
                                         {
                                             enabled = setter.enabled,
                                             _setAction = () =>
                                                             {
                                                                 if (entry.Setter != null)
                                                                 {
                                                                     entry.Setter.Set(o,
                                                                                      setter.deferredRetrievalFunction(
                                                                                          setter.parameters));
                                                                 }
                                                             }
                                         };
                            if (entry.OwningType.IsValueType)
                            {
                                ns._setAction();
                            }
                            else
                            {
                                AddFixup(ns);
                            }
                        }
                        else
                        {
                            entry.Setter.Set(o, value);
                        }
                    }
                    catch (ArgumentException)
                    {
                        try
                        {
                            // if the property is nullable enum we need to handle it differently because a straight ChangeType doesn't work
                            var type = Nullable.GetUnderlyingType(entry.Setter.FieldInfo.FieldType);
                            if (type != null && type.IsEnum)
                            {
                                entry.Setter.FieldInfo.SetValue(o, Enum.Parse(type, value.ToString(), true));
                            }
                            else
                            {
                                entry.Setter.FieldInfo.SetValue(o,
                                                                Convert.ChangeType(value,
                                                                                   entry.Setter.FieldInfo.FieldType,
                                                                                   null));
                            }
                        }
                        catch (Exception e)
                        {
                            Radical.LogError("Serialization error: " + e.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        //Suppress cases where the old value is no longer compatible with the new property type
                        Radical.LogError("Serialization error: " + e.ToString());
                    }
                }
                storage.EndReadField();
            }
            storage.EndReadFields();
        }

        public class DeferredSetter
        {
			public int priority = 0;
            public readonly GetData deferredRetrievalFunction;
            public bool enabled = true;
            internal readonly Dictionary<string, object> parameters = new Dictionary<string, object>();
            internal Action _setAction;

            public DeferredSetter(GetData retrievalFunction)
            {
                deferredRetrievalFunction = retrievalFunction;
            }
        }

        #endregion

        #region Nested type: EntryConfiguration

        /// <summary>
        ///   Stores configurations for entries
        /// </summary>
        private class EntryConfiguration
        {
            public GetSet Setter;
            public Type Type;
        }

        #endregion

        #region Nested type: KnownTypesStackEntry

        private class KnownTypesStackEntry
        {
            public List<Type> knownTypesList;
            public Dictionary<Type, ushort> knownTypesLookup;
        }

        #endregion

        #region Nested type: MissingConstructorException

        public class MissingConstructorException : Exception
        {
            public MissingConstructorException(string message)
                : base(message)
            {
            }
        }

        #endregion

        #region Basic IO

        #region Delegates

        public delegate object ReadAValue(BinaryReader reader);

        #endregion

        private static readonly Dictionary<Type, WriteAValue> Writers = new Dictionary<Type, WriteAValue>();
        public static readonly Dictionary<Type, ReadAValue> Readers = new Dictionary<Type, ReadAValue>();
        private static readonly Dictionary<string, bool> componentNames = new Dictionary<string, bool>();


        static UnitySerializer()
        {
            componentNames = typeof (Component).GetFields().Cast<MemberInfo>()
                .Concat(typeof (Component).GetProperties().Cast<MemberInfo>())
                .Select(m => m.Name)
                .ToDictionary(m => m, m => true);


            var index = (ushort) 60000;
            foreach (var tp in PreWarm.PrewarmTypes)
            {
                var type = Type.GetType(tp);
                if (type == null)
                {
					index++;
					PrewarmLookup.Add(null);
                    continue;
                }
                PrewarmedTypes[type] = index++;
                PrewarmLookup.Add(type);
            }
            index = 50000;
            foreach (var name in PreWarm.PrewarmNames)
            {
                PrewarmedNames[name] = index++;
            }


            DataPath = Application.persistentDataPath;
            RegisterSerializationAssembly();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                RegisterSerializationAssembly(asm);
            }
            Writers[typeof (string)] = StringWriter;
            Writers[typeof (Decimal)] = DecimalWriter;
            Writers[typeof (float)] = FloatWriter;
            Writers[typeof (byte[])] = ByteArrayWriter;
            Writers[typeof (bool)] = BoolWriter;
            Writers[typeof (Guid)] = GuidWriter;
            Writers[typeof (DateTime)] = DateTimeWriter;
            Writers[typeof (TimeSpan)] = TimeSpanWriter;
            Writers[typeof (char)] = CharWriter;
            Writers[typeof (ushort)] = UShortWriter;
            Writers[typeof (double)] = DoubleWriter;
            Writers[typeof (ulong)] = ULongWriter;
            Writers[typeof (int)] = IntWriter;
            Writers[typeof (uint)] = UIntWriter;
            Writers[typeof (byte)] = ByteWriter;
            Writers[typeof (long)] = LongWriter;
            Writers[typeof (short)] = ShortWriter;
            Writers[typeof (sbyte)] = SByteWriter;

            Readers[typeof (string)] = AStringReader;
            Readers[typeof (Decimal)] = DecimalReader;
            Readers[typeof (float)] = FloatReader;
            Readers[typeof (byte[])] = ByteArrayReader;
            Readers[typeof (bool)] = BoolReader;
            Readers[typeof (Guid)] = GuidReader;
            Readers[typeof (DateTime)] = DateTimeReader;
            Readers[typeof (TimeSpan)] = TimeSpanReader;
            Readers[typeof (char)] = CharReader;
            Readers[typeof (ushort)] = UShortReader;
            Readers[typeof (double)] = DoubleReader;
            Readers[typeof (ulong)] = ULongReader;
            Readers[typeof (int)] = IntReader;
            Readers[typeof (uint)] = UIntReader;
            Readers[typeof (byte)] = ByteReader;
            Readers[typeof (long)] = LongReader;
            Readers[typeof (short)] = ShortReader;
            Readers[typeof (sbyte)] = SByteReader;
        }

        private static object ShortReader(BinaryReader reader)
        {
            return reader.ReadInt16();
        }

        private static object LongReader(BinaryReader reader)
        {
            return reader.ReadInt64();
        }

        private static object GuidReader(BinaryReader reader)
        {
			if(currentVersion >=10)
			{
				return new Guid(reader.ReadBytes(16));
			}
            return new Guid(reader.ReadString());
        }

        private static object SByteReader(BinaryReader reader)
        {
            return reader.ReadSByte();
        }

        private static object ByteReader(BinaryReader reader)
        {
            return reader.ReadByte();
        }

        private static object UIntReader(BinaryReader reader)
        {
            return reader.ReadUInt32();
        }

        private static object IntReader(BinaryReader reader)
        {
            return reader.ReadInt32();
        }

        private static object ULongReader(BinaryReader reader)
        {
            return reader.ReadUInt64();
        }

        private static object DoubleReader(BinaryReader reader)
        {
            return reader.ReadDouble();
        }

        private static object UShortReader(BinaryReader reader)
        {
            return reader.ReadUInt16();
        }

        private static object CharReader(BinaryReader reader)
        {
            return reader.ReadChar();
        }

        private static object FloatReader(BinaryReader reader)
        {
            return reader.ReadSingle();
        }

        private static object TimeSpanReader(BinaryReader reader)
        {
            return new TimeSpan(reader.ReadInt64());
        }

        private static object DateTimeReader(BinaryReader reader)
        {
            return new DateTime(reader.ReadInt64());
        }

        private static object ByteArrayReader(BinaryReader reader)
        {
            var len = reader.ReadInt32();
            return reader.ReadBytes(len);
        }

        private static object DecimalReader(BinaryReader reader)
        {
            var array = new int[4];
            array[0] = (int) reader.ReadInt32();
            array[1] = (int) reader.ReadInt32();
            array[2] = (int) reader.ReadInt32();
            array[3] = (int) reader.ReadInt32();

            return new Decimal(array);
        }

        private static object BoolReader(BinaryReader reader)
        {
            return reader.ReadChar() == 'Y';
        }

        private static object AStringReader(BinaryReader reader)
        {
            var retString = reader.ReadString();

            return retString == "~~NULL~~"
                       ? null
                       : retString;
        }

        private static void SByteWriter(BinaryWriter writer, object value)
        {
            writer.Write((sbyte) value);
        }

        private static void ShortWriter(BinaryWriter writer, object value)
        {
            writer.Write((short) value);
        }

        private static void LongWriter(BinaryWriter writer, object value)
        {
            writer.Write((long) value);
        }

        private static void ByteWriter(BinaryWriter writer, object value)
        {
            writer.Write((byte) value);
        }

        private static void UIntWriter(BinaryWriter writer, object value)
        {
            writer.Write((uint) value);
        }

        private static void IntWriter(BinaryWriter writer, object value)
        {
            writer.Write((int) value);
        }

        private static void ULongWriter(BinaryWriter writer, object value)
        {
            writer.Write((ulong) value);
        }

        private static void DoubleWriter(BinaryWriter writer, object value)
        {
            writer.Write((double) value);
        }

        private static void UShortWriter(BinaryWriter writer, object value)
        {
            writer.Write((ushort) value);
        }

        private static void CharWriter(BinaryWriter writer, object value)
        {
            writer.Write((char) value);
        }

        private static void TimeSpanWriter(BinaryWriter writer, object value)
        {
            writer.Write(((TimeSpan) value).Ticks);
		}

        private static void DateTimeWriter(BinaryWriter writer, object value)
        {
            writer.Write(((DateTime) value).Ticks);
        }

        private static void GuidWriter(BinaryWriter writer, object value)
        {
            writer.Write(((Guid)value).ToByteArray());
        }

        private static void BoolWriter(BinaryWriter writer, object value)
        {
            writer.Write((bool) value
                             ? 'Y'
                             : 'N');
        }

        private static void ByteArrayWriter(BinaryWriter writer, object value)
        {
            var array = value as byte[];
            writer.Write((int) array.Length);
            writer.Write(array);
        }

        private static void FloatWriter(BinaryWriter writer, object value)
        {
            writer.Write((float) value);
        }

        private static void DecimalWriter(BinaryWriter writer, object value)
        {
            var array = Decimal.GetBits((Decimal) value);
            writer.Write(array[0]);
            writer.Write(array[1]);
            writer.Write(array[2]);
            writer.Write(array[3]);
        }

        private static void StringWriter(BinaryWriter writer, object value)
        {
            writer.Write((string) value);
        }


        /// <summary>
        ///   Write a basic untyped value
        /// </summary>
        /// <param name="writer"> The writer to commit byte to </param>
        /// <param name="value"> The value to write </param>
        internal static void WriteValue(BinaryWriter writer, object value)
        {
            WriteAValue write;

            if (!Writers.TryGetValue(value.GetType(), out write))
            {
                writer.Write((int) value);
                return;
            }
            write(writer, value);
        }

        /// <summary>
        ///   Read a basic value from the stream
        /// </summary>
        /// <param name="reader"> The reader with the stream </param>
        /// <param name="tp"> The type to read </param>
        /// <returns> The hydrated value </returns>
        internal static object ReadValue(BinaryReader reader, Type tp)
        {
            ReadAValue read;
            if (!Readers.TryGetValue(tp, out read))
            {
                return reader.ReadInt32();
            }
            return read(reader);
        }

        private delegate void WriteAValue(BinaryWriter writer, object value);

        #endregion

        #region Nested type: ObjectMappingEventArgs

        /// <summary>
        ///   Arguments for object creation event
        /// </summary>
        public class ObjectMappingEventArgs : EventArgs
        {
            /// <summary>
            ///   Supply a type to use instead
            /// </summary>
            public object Instance = null;

            /// <summary>
            ///   The type that cannot be
            /// </summary>
            public Type TypeToConstruct;
        }

        #endregion

        #region Nested type: PropertyNameStackEntry

        internal class PropertyNameStackEntry
        {
            public List<string> propertyList;
            public Dictionary<string, ushort> propertyLookup;
        }

        #endregion

        #region Nested type: ScanTypeFunction

        internal delegate void ScanTypeFunction(Type type, Attribute attribute);

        #endregion

        #region Nested type: SerializationScope
		
		
		
        public class SerializationScope : IDisposable
        {
			static Stack<bool> _primaryScopeStack = new Stack<bool>();
			static bool _hasSetPrimaryScope;
			static bool _primaryScope;
            
			//[ThreadStatic]
            internal static int _counter = 0;

            public static bool IsInScope
            {
                get
                {
                    return _counter != 0;
                }
            }

            public static bool IsPrimaryScope
            {
                get
                {
                    return _primaryScope || true;
                }
            }
			
			public static void SetPrimaryScope()
			{
				if(_hasSetPrimaryScope)
					return;
				_primaryScope = true;
				_hasSetPrimaryScope = true;
			}
			
			
			

#if US_LOGGING
			DateTime startTime;
#endif

            readonly List<DeferredSetter> _fixupFunctions;
            readonly List<Action> _finalDeserialization;

            public SerializationScope()
            {
				
				_primaryScopeStack.Push(_primaryScope);
				_primaryScope = false;
                if (_seenObjects == null)
                {
                    _seenObjects = new Dictionary<object, int>();
                }
                if (_loadedObjects == null)
                {
                    _loadedObjects = new Dictionary<int, object>();
                }
                if (_seenTypes == null)
                {
                    _seenTypes = new Dictionary<Type, bool>();
                }
                if (_counter == 0)
                {
					_fixupFunctions = FixupFunctions;
					FixupFunctions = new List<DeferredSetter>();
					_finalDeserialization = FinalDeserialization;
					FinalDeserialization = new List<Action>();
                    _seenObjects.Clear();
                    _loadedObjects.Clear();
                    _seenTypes.Clear();
#if US_LOGGING
					startTime = DateTime.Now;
#endif
                    _nextId = 0;
                }
                _counter++;
            }

            public void Dispose()
            {

				_primaryScope = _primaryScopeStack.Pop();
                if (--_counter != 0)
                {
                    return;
                }
				_hasSetPrimaryScope = false;
                RunDeferredActions();
                FinalDeserialization = _finalDeserialization;
                FixupFunctions = _fixupFunctions;

#if US_LOGGING
					Radical.LogNow("Saving {0:#,0} in {1:0.000} @ {2:0.000}", _nextId, (DateTime.Now - startTime).TotalSeconds, (DateTime.Now - startTime).TotalMilliseconds/_nextId);
#endif
                _nextId = 0;
                _seenObjects.Clear();
                _loadedObjects.Clear();
                _seenTypes.Clear();
				if(_knownTypesLookup != null) _knownTypesLookup.Clear();
				if(_knownTypesList != null) _knownTypesList.Clear();
				if(_propertyLookup != null) _propertyLookup.Clear();
				if(_propertyList != null) _propertyList.Clear();
				
            }
        }

        #endregion

        #region Nested type: SerializationSplitScope

        public class SerializationSplitScope : IDisposable
        {
			readonly List<DeferredSetter> _fixupFunctions;
            readonly List<Action> _finalDeserialization;
			int _previousCounter = 0;

            public SerializationSplitScope()
            {
				_previousCounter = SerializationScope._counter;
                CreateStacks();
                if (_seenObjects == null)
                {
                    _seenObjects = new Dictionary<object, int>();
                }
                if (_loadedObjects == null)
                {
                    _loadedObjects = new Dictionary<int, object>();
                }
                if (_seenTypes == null)
                {
                    _seenTypes = new Dictionary<Type, bool>();
                }
                _seenTypesStack.Push(_seenTypes);
                _storedObjectsStack.Push(_seenObjects);
                _loadedObjectStack.Push(_loadedObjects);
                _idStack.Push(_nextId);
                _nextId = 0;
				SerializationScope._counter = 0;
                _seenObjects = new Dictionary<object, int>();
                _loadedObjects = new Dictionary<int, object>();
                _seenTypes = new Dictionary<Type, bool>();
				_fixupFunctions = FixupFunctions;
				FixupFunctions = new List<DeferredSetter>();
				_finalDeserialization = FinalDeserialization;
				FinalDeserialization = new List<Action>();
            }

            #region IDisposable Members

            public void Dispose()
            {
                _seenObjects = _storedObjectsStack.Pop();
                _loadedObjects = _loadedObjectStack.Pop();
                _seenTypes = _seenTypesStack.Pop();
                _nextId = _idStack.Pop();
				SerializationScope._counter = _previousCounter;
				RunDeferredActions();
                FinalDeserialization = _finalDeserialization;
                FixupFunctions = _fixupFunctions;
            }

            #endregion
        }

        #endregion

        #region Nested type: TypeMappingEventArgs

        /// <summary>
        ///   Arguments for a missing type event
        /// </summary>
        public class TypeMappingEventArgs : EventArgs
        {
            /// <summary>
            ///   The missing types name
            /// </summary>
            public string TypeName = String.Empty;

            /// <summary>
            ///   Supply a type to use instead
            /// </summary>
            public Type UseType = null;
        }

        #endregion
    }
}