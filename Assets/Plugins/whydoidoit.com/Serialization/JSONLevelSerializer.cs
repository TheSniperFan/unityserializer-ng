using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using Serialization;
using System.Reflection;
using System.Text;
using System.IO;
using System.Net;


/// <summary>
/// JSON level serializer.  This is the main interaction point for saving
/// and loading Unity objects and scenes to JSON.
/// </summary>
//
public static class JSONLevelSerializer
{
    #region Delegates
	
	/// <summary>
	/// Delegate for querying storage
	/// </summary>
    public delegate void StoreQuery(GameObject go, ref bool store);
	public delegate void StoreComponentQuery(Component component, ref bool store);

    #endregion

    #region SerializationModes enum
	
	/// <summary>
	/// Serialization modes.
	/// </summary>
    public enum SerializationModes
    {
		/// <summary>
		/// Serialize when the system is free
		/// </summary>
        SerializeWhenFree,
		/// <summary>
		/// Cache serialization when suspending
		/// </summary>
        CacheSerialization
    }

    #endregion

    private static Dictionary<string, GameObject> allPrefabs = new Dictionary<string, GameObject>();
	/// <summary>
	/// The names of types that should be ignored
	/// </summary>
    public static HashSet<string> IgnoreTypes = new HashSet<string>();

    public static Dictionary<Type, IComponentSerializer> CustomSerializers =
        new Dictionary<Type, IComponentSerializer>();

    private static int lastFrame;

    /// <summary>
    ///   The name of the player.
    /// </summary>
    public static string PlayerName = string.Empty;
	
	/// <summary>
	/// Whether resume information should be saved when the
	/// game is saved
	/// </summary>
    public static bool SaveResumeInformation = true;
    private static int _suspensionCount;
    private static SaveEntry _cachedState;

    /// <summary>
    ///   The serialization caching mode
    /// </summary>
    public static SerializationModes SerializationMode = SerializationModes.CacheSerialization;

    /// <summary>
    ///   The max games that will be stored.
    /// </summary>
    public static int MaxGames = 20;

    /// <summary>
    ///   The saved games.
    /// </summary>
    public static Lookup<string, List<SaveEntry>> SavedGames = new Index<string, List<SaveEntry>>();

    //Stop cases
    private static readonly List<Type> _stopCases = new List<Type>();
	/// <summary>
	/// Is the level being loaded
	/// </summary>
	/// <value>
	/// <c>true</c> if this instance is deserializing; otherwise, <c>false</c>.
	/// </value>
    public static bool IsDeserializing
    {
        get
        {
            return LevelSerializer.IsDeserializing;
        }
        set
        {
            LevelSerializer.IsDeserializing = value;
        }
    }
    private static readonly List<object> createdPlugins = new List<object>();

    static JSONLevelSerializer()
    {
		webClient.UploadDataCompleted += HandleWebClientUploadDataCompleted;
		webClient.UploadStringCompleted += HandleWebClientUploadStringCompleted;
		//Basic plug in configuration and special cases
        _stopCases.Add(typeof (PrefabIdentifier));
        UnitySerializer.AddPrivateType(typeof (AnimationClip));

        //Other initialization
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            UnitySerializer.ScanAllTypesForAttribute(
                (tp, attr) => createdPlugins.Add(Activator.CreateInstance(tp)), asm, typeof (SerializerPlugIn));
            UnitySerializer.ScanAllTypesForAttribute(
                (tp, attr) =>
                    {
                        CustomSerializers[((ComponentSerializerFor) attr).SerializesType] =
                            Activator.CreateInstance(tp) as IComponentSerializer;
                    }, asm,
                typeof (ComponentSerializerFor));
        }


        AllPrefabs =
            Resources.FindObjectsOfTypeAll(typeof (GameObject)).Cast<GameObject>()
                .Where(go =>
                           {
                               var pf = go.GetComponent<PrefabIdentifier>();
                               return pf != null && !pf.IsInScene();
                           })
                .Distinct(CompareGameObjects.Instance)
                .ToDictionary(go => go.GetComponent<PrefabIdentifier>().ClassId, go => go);


        try
        {
            var stored = PlayerPrefs.GetString("JSON_Save_Game_Data_");
            if (!string.IsNullOrEmpty(stored))
            {
                SavedGames =
                    UnitySerializer.JSONDeserialize<Lookup<string, List<SaveEntry>>>(stored);
            }
            if (SavedGames == null)
            {
                SavedGames = new Index<string, List<SaveEntry>>();
            }
        }
        catch
        {
            SavedGames = new Index<string, List<SaveEntry>>();
        }
    }
	
	#region Extended methods
	
	
    static void HandleWebClientUploadStringCompleted (object sender, UploadStringCompletedEventArgs e)
    {
		lock(Guard)
		{
			uploadCount--;
		}
		Loom.QueueOnMainThread(()=>{
		if(e.UserState is Action<Exception>)
			(e.UserState as Action<Exception>)(e.Error);
		});
    }
	
	static readonly object Guard = new object();

    static void HandleWebClientUploadDataCompleted (object sender, UploadDataCompletedEventArgs e)
    {
		lock(Guard)
		{
			uploadCount--;
		}
		Loom.QueueOnMainThread(()=>{
		if(e.UserState is Action<Exception>)
			(e.UserState as Action<Exception>)(e.Error);
		});
    }
	
	static WebClient webClient = new WebClient();
	
	/// <summary>
	/// Saves a particular object tree to a file.  The file will be
	/// saved beneath Application.persistentDataPath
	/// </summary>
	/// <param name='filename'>
	/// The filename to save the object tree into
	/// </param>
	/// <param name='rootOfTree'>
	/// The root of the tree
	/// </param>
	public static void SaveObjectTreeToFile(string filename, GameObject rootOfTree)
	{
		var data = SaveObjectTree(rootOfTree);
		data.WriteToFile(Application.persistentDataPath + "/" + filename);
	}
	
	/// <summary>
	/// Loads an object tree into the current scene from a file
	/// </summary>
	/// <param name='filename'>
	/// The file that should be loaded (from within Application.persistentDataPath)
	/// </param>
	/// <param name='onComplete'>
	/// A method call to make when loading is complete
	/// </param>
	public static void LoadObjectTreeFromFile(string filename, Action<JSONLevelLoader> onComplete = null)
	{
		var x= File.OpenText(Application.persistentDataPath + "/" + filename);
		var data = x.ReadToEnd();
		x.Close();
		LoadObjectTree(data, onComplete);
	}
	
	/// <summary>
	/// Serializes the level to a file beneath Application.persistentDataPath
	/// </summary>
	/// <param name='filename'>
	/// The filename to use
	/// </param>
	public static void SerializeLevelToFile(string filename)
	{
		var data = SerializeLevel();
		data.WriteToFile(Application.persistentDataPath + "/" + filename);
	}
	
	/// <summary>
	/// Loads a level from a file within Application.persistentDataPath
	/// </summary>
	/// <param name='filename'>
	/// The filename to use
	/// </param>
	public static void LoadSavedLevelFromFile(string filename)
	{
		var x = File.OpenText(Application.persistentDataPath + "/" + filename);
		var data = x.ReadToEnd();
		x.Close();
		LoadSavedLevel(data);
	}
	
	/// <summary>
	/// Saves an object tree to a server using POST or STOR
	/// </summary>
	/// <param name='uri'>
	/// The url to save the tree to e.g. ftp://whydoidoit.net/Downloads/someFile.txt
	/// </param>
	/// <param name='rootOfTree'>
	/// The object to be saved
	/// </param>
	/// <param name='userName'>
	/// The user name (if required)
	/// </param>
	/// <param name='password'>
	/// The password (if required)
	/// </param>
	/// <param name='onComplete'>
	/// A function to call when the upload is complete
	/// </param>
	public static void SaveObjectTreeToServer(string uri, GameObject rootOfTree, string userName = "", string password = "", Action<Exception> onComplete =null)
	{
		onComplete = onComplete ?? delegate {};
		Action execute = ()=>{
			var data = SaveObjectTree(rootOfTree);
			Action doIt = ()=> {
				uploadCount++;
				webClient.Credentials = new NetworkCredential(userName, password);
				webClient.UploadStringAsync(new Uri(uri), null, data, onComplete);
			};
	
			DoWhenReady(doIt);	
		};
		execute();
		
		
	}
	
	static void DoWhenReady(Action upload)
	{
		lock(Guard)
		{
	
			if(uploadCount > 0)
			{
				Loom.QueueOnMainThread(()=>DoWhenReady(upload), 0.4f);
			}
			else
			{
				upload();
			}
		}
	
	}
	
	static int uploadCount;
	
	/// <summary>
	/// Loads an object tree from a server
	/// </summary>
	/// <param name='uri'>
	/// The url to load the object tree from
	/// </param>
	/// <param name='onComplete'>
	/// A method to call when the load is complete
	/// </param>
	/// <param name='onComplete'>
	/// A method to call when there is an error using load
	/// </param>
	public static void LoadObjectTreeFromServer(string uri, Action<JSONLevelLoader> onComplete = null, Action<string> onError = null)
	{
		onComplete = onComplete ?? delegate {};
		onError = onError ?? delegate {};
		RadicalRoutineHelper.Current.StartCoroutine(DownloadFromServer(uri, onComplete, onError));
	}
	
	/// <summary>
	/// Serializes the level to a server.
	/// </summary>
	/// <param name='uri'>
	/// The url of the location for the stored data.  ftp://whydoidoit.net/Downloads/someFile.dat
	/// </param>
	/// <param name='userName'>
	/// User name if required
	/// </param>
	/// <param name='password'>
	/// Password if required
	/// </param>
	/// <param name='onComplete'>
	/// A method to call when the serialization is complete
	/// </param>
	public static void SerializeLevelToServer(string uri, string userName = "", string password = "", Action<Exception> onComplete = null)
	{
		lock(Guard)
		{
			if(uploadCount > 0)
			{
				Loom.QueueOnMainThread(()=>SerializeLevelToServer(uri, userName, password, onComplete), 0.5f);
				return;
			}
			uploadCount++;
			onComplete = onComplete ?? delegate {};
			var data = SerializeLevel();
			webClient.Credentials = new NetworkCredential(userName, password);
			webClient.UploadStringAsync(new Uri(uri), null, data, onComplete);
		}
	}
	
	/// <summary>
	/// Loads the saved level from a server url.
	/// </summary>
	/// <param name='uri'>
	/// The url of the server to load the data from
	/// </param>
	/// <param name="onError">
	/// A function to call when there is an error finding the file
	/// </param>
	public static void LoadSavedLevelFromServer(string uri, Action<string> onError = null)
	{
		onError = onError ?? delegate {};
		RadicalRoutineHelper.Current.StartCoroutine(DownloadLevelFromServer(uri, onError));
	}
	
	static IEnumerator DownloadFromServer(string uri, Action<JSONLevelLoader> onComplete, Action<string> onError)
	{
		var www = new WWW(uri);
		yield return www;
		if(!string.IsNullOrEmpty(www.error))
		{
			onError(www.error);
			yield break;
		}
			
		LoadObjectTree(www.text, onComplete);
	}
			
	static IEnumerator DownloadLevelFromServer(string uri, Action<string> onError)
	{
		var www = new WWW(uri);
		yield return www;
		if(!string.IsNullOrEmpty(www.error))
		{
			onError(www.error);
			yield break;
		}
		
		
		LoadSavedLevel(www.text);
	}
	
	#endregion

	
	public static Dictionary<string, GameObject> AllPrefabs
    {
        get
        {
            if (Time.frameCount != lastFrame)
            {
                allPrefabs = allPrefabs.Where(p => p.Value).ToDictionary(p => p.Key, p => p.Value);
                lastFrame = Time.frameCount;
            }
            return allPrefabs;
        }
        set { allPrefabs = value; }
    }
	
	/// <summary>
	/// Gets a value indicating whether this instance can resume.
	/// </summary>
	/// <value>
	/// <c>true</c> if this instance can resume; otherwise, <c>false</c>.
	/// </value>
    public static bool CanResume
    {
        get { return !string.IsNullOrEmpty(PlayerPrefs.GetString(PlayerName + "JSON__RESUME__")); }
    }

    /// <summary>
    ///   Gets a value indicating whether this instance is suspended.
    /// </summary>
    /// <value> <c>true</c> if this instance is suspended; otherwise, <c>false</c> . </value>
    public static bool IsSuspended
    {
        get { return _suspensionCount > 0; }
    }

    /// <summary>
    ///   Gets the serialization suspension count.
    /// </summary>
    /// <value> The suspension count. </value>
    public static int SuspensionCount
    {
        get { return _suspensionCount; }
    }


    /// <summary>
    ///   Occurs when the level was deserialized
    /// </summary>
    public static event Action Deserialized = delegate { };

    /// <summary>
    ///   Occurs when the level was serialized.
    /// </summary>
    public static event Action GameSaved = delegate { };

    /// <summary>
    ///   Occurs when suspending serialization.
    /// </summary>
    public static event Action SuspendingSerialization = delegate { };

    /// <summary>
    ///   Occurs when resuming serialization.
    /// </summary>
    public static event Action ResumingSerialization = delegate { };

    internal static void InvokeDeserialized()
    {
        _suspensionCount = 0;
        if (Deserialized != null)
        {
            Deserialized();
        }
        foreach (var go in UnityEngine.Object.FindObjectsOfType(typeof (GameObject)).Cast<GameObject>())
        {
            go.SendMessage("OnDeserialized", null, SendMessageOptions.DontRequireReceiver);
        }
    }
	
	/// <summary>
	/// Handle this event to indicate if specific items should be stored
	/// </summary>
    public static event StoreQuery Store;
	public static event StoreComponentQuery StoreComponent = delegate  {};

    /// <summary>
    ///   Resume for a stored game state that wasn't directly saved
    /// </summary>
    public static void Resume()
    {
        var data = PlayerPrefs.GetString(PlayerName + "JSON__RESUME__");
        if (!string.IsNullOrEmpty(data))
        {
            var se = UnitySerializer.JSONDeserialize<SaveEntry>(data);
            se.Load();
        }
    }

    /// <summary>
    ///   Create a resumption checkpoint
    /// </summary>
    public static void Checkpoint()
    {
        SaveGame("Resume", false, PerformSaveCheckPoint);
    }

    private static void PerformSaveCheckPoint(string name, bool urgent)
    {
        var newGame = CreateSaveEntry(name, urgent);
        PlayerPrefs.SetString(PlayerName + "JSON__RESUME__", UnitySerializer.JSONSerialize(newGame));
		PlayerPrefs.Save();
    }

    /// <summary>
    ///   Suspends the serialization. Must resume as many times as you suspend
    /// </summary>
    public static void SuspendSerialization()
    {
        if (_suspensionCount == 0)
        {
            SuspendingSerialization();
            if (SerializationMode == SerializationModes.CacheSerialization)
            {
                _cachedState = CreateSaveEntry("resume", true);
                if (SaveResumeInformation)
                {
                    PlayerPrefs.SetString(PlayerName + "JSON__RESUME__",
                                          UnitySerializer.JSONSerialize(_cachedState));
					PlayerPrefs.Save();
                }
            }
        }
        _suspensionCount++;
    }

    /// <summary>
    ///   Resumes the serialization. Must be balanced with calls to SuspendSerialization
    /// </summary>
    public static void ResumeSerialization()
    {
        _suspensionCount--;
        if (_suspensionCount == 0)
        {
            ResumingSerialization();
        }
    }

    /// <summary>
    ///   Ignores the type of component when saving games.
    /// </summary>
    /// <param name='typename'> Typename of the component to ignore </param>
    public static void IgnoreType(string typename)
    {
        IgnoreTypes.Add(typename);
    }

    public static void UnIgnoreType(string typename)
    {
        IgnoreTypes.Remove(typename);
    }

    /// <summary>
    ///   Ignores the type of component when saving games.
    /// </summary>
    /// <param name='tp'> The type of the component to ignore </param>
    public static void IgnoreType(Type tp)
    {
        if (tp.FullName != null)
        {
            IgnoreTypes.Add(tp.FullName);
        }
    }

    /// <summary>
    ///   Creates a saved game for the current position
    /// </summary>
    /// <returns> The new save entry. </returns>
    /// <param name='name'> A name for the save entry </param>
    /// <param name='urgent'> An urgent save will store the current state, even if suspended. In this case it is likely that clean up will be necessary by handing Deserialized messages or responding to the LevelSerializer.Deserialized event </param>
    public static SaveEntry CreateSaveEntry(string name, bool urgent)
    {
        return new SaveEntry()
                   {
                       Name = name,
                       When = DateTime.Now,
                       Level = Application.loadedLevelName,
                       Data = SerializeLevel(urgent)
                   };
    }

    /// <summary>
    ///   Saves the game.
    /// </summary>
    /// <param name='name'> The name to use for the game </param>
    public static void SaveGame(string name)
    {
        SaveGame(name, false, null);
    }
	
	/// <summary>
	/// Saves the game.
	/// </summary>
	/// <param name='name'>
	/// The name of the game to save
	/// </param>
	/// <param name='urgent'>
	/// Whether suspension should be ignored
	/// </param>
	/// <param name='perform'>
	/// The method used to perform the save
	/// </param>
    public static void SaveGame(string name, bool urgent, Action<string, bool> perform)
    {
        perform = perform ?? PerformSave;
        //See if we need to serialize later
        if (!urgent && (IsSuspended && SerializationMode == SerializationModes.SerializeWhenFree))
        {
            //Are we already waiting for serialization to occur
            if (GameObject.Find("/SerializationHelper") != null)
            {
                return;
            }
            //Create a helper
            var go = new GameObject("SerializationHelper");
            var helper = go.AddComponent(typeof (SerializationHelper)) as SerializationHelper;
            helper.gameName = name;
            helper.perform = perform;
            return;
        }

        perform(name, urgent);
    }

    private static void PerformSave(string name, bool urgent)
    {
        var newGame = CreateSaveEntry(name, urgent);
        SavedGames[PlayerName].Insert(0, newGame);


        while (SavedGames[PlayerName].Count > MaxGames)
        {
            SavedGames[PlayerName].RemoveAt(SavedGames.Count - 1);
        }

        SaveDataToPlayerPrefs();

        PlayerPrefs.SetString(PlayerName + "JSON__RESUME__", UnitySerializer.JSONSerialize(newGame));
		PlayerPrefs.Save();
        GameSaved();
    }


    /// <summary>
    ///   Saves the stored game data to player prefs.
    /// </summary>
    public static void SaveDataToPlayerPrefs()
    {
        PlayerPrefs.SetString("JSON_Save_Game_Data_", UnitySerializer.JSONSerialize(SavedGames));
		PlayerPrefs.Save();
    }

    /// <summary>
    ///   Registers the calling assembly as one providing serialization extensions.
    /// </summary>
    public static void RegisterAssembly()
    {
        UnitySerializer.ScanAllTypesForAttribute(
            (tp, attr) =>
                {
                    CustomSerializers[((ComponentSerializerFor) attr).SerializesType] =
                        Activator.CreateInstance(tp) as IComponentSerializer;
                }, Assembly.GetCallingAssembly(),
            typeof (ComponentSerializerFor));
    }

    /// <summary>
    ///   Adds the prefab path.
    /// </summary>
    /// <param name='path'> A resource path that contains prefabs to be created for the game </param>
    public static void AddPrefabPath(string path)
    {
        foreach (var pair in Resources.LoadAll(path, typeof (GameObject))
            .Cast<GameObject>()
            .Where(go => go.GetComponent<UniqueIdentifier>() != null)
            .ToDictionary(go => go.GetComponent<UniqueIdentifier>().ClassId, go => go).Where(
                pair => !AllPrefabs.ContainsKey(pair.Key)))
        {
            AllPrefabs.Add(pair.Key, pair.Value);
        }
    }
	
	/// <summary>
	/// Indicates that the storage system should not garbage collect
	/// </summary>
	public static void DontCollect()
	{
		_collectionCount++;
	}
	
	/// <summary>
	/// Indicates that the system can garbage collect
	/// </summary>
	public static void Collect()
	{
		_collectionCount--;
	}
	
	static int _collectionCount = 0;
	
	/// <summary>
	/// Gets a value indicating whether this <see cref="JSONLevelSerializer"/> should garbage collect.
	/// </summary>
	/// <value>
	/// <c>true</c> if should collect; otherwise, <c>false</c>.
	/// </value>
	public static bool ShouldCollect
	{
		get
		{
			return _collectionCount <= 0;
		}
	}

    /// <summary>
    ///   Serializes the level to a string
    /// </summary>
    /// <returns> The level data as a string </returns>
    /// <exception>Is thrown when the serization was suspended
    ///   <cref>SerizationSuspendedException</cref>
    /// </exception>
    public static string SerializeLevel()
    {
        return SerializeLevel(false);
    }

    /// <summary>
    ///   Serializes the level.
    /// </summary>
    /// <returns> The level stored as a string. </returns>
    /// <param name='urgent'> Whether to ignore an suspension of serialization </param>
    /// <exception cref='SerializationSuspendedException'>Is thrown when the serialization was suspended and urgent was not specified</exception>
    public static string SerializeLevel(bool urgent)
    {
		//using(new Timing("Save Level"))
		{
			if (IsSuspended && !urgent)
	        {
	            if (SerializationMode == SerializationModes.CacheSerialization)
	            {
	                return _cachedState.Data;
	            }
	            else
	            {
	                throw new SerializationSuspendedException();
	            }
	        }
	        //Try to get as much memory as possible
			Resources.UnloadUnusedAssets();
	        if(ShouldCollect) GC.Collect();
	        var data = SerializeLevel(false, null);
	        //Free up memory that has been used during serialization
	        if(ShouldCollect) GC.Collect();
	        return data;
		}
    }

    public static void RaiseProgress(string section, float complete)
    {
        Progress(section, complete);
    }

    public static bool HasParent(UniqueIdentifier i, string id)
    {
        var scan = UniqueIdentifier.GetByName(i.Id).transform;
        while (scan != null)
        {
            UniqueIdentifier ui;
            if ((ui = scan.GetComponent<UniqueIdentifier>()) != null)
            {
                if (id == ui.Id)
                {
                    return true;
                }
            }
            scan = scan.parent;
        }
        return false;
    }

    private static void GetComponentsInChildrenWithClause(Transform t, List<StoreInformation> components)
    {
        foreach (var c in t.Cast<Transform>())
        {
            var s = c.GetComponent<StoreInformation>();
            if (s != null)
            {
                if (!(s is PrefabIdentifier))
                {
                    components.Add(s);
                    GetComponentsInChildrenWithClause(c, components);
                }
            }
            else
            {
                GetComponentsInChildrenWithClause(c, components);
            }
        }
    }

    /// <summary>
    /// Save an objects tree so it can be reloaded later
    /// </summary>
    /// <param name="rootOfTree">The object at the root of the tree</param>
    /// <returns></returns>
    public static string SaveObjectTree(this GameObject rootOfTree)
    {
        if (!rootOfTree.GetComponent<UniqueIdentifier>())
		{
            EmptyObjectIdentifier.FlagAll(rootOfTree);
		}
        return SerializeLevel(false, rootOfTree.GetComponent<UniqueIdentifier>().Id);
    }

    /// <summary>
    /// Reload an object tree
    /// </summary>
    /// <param name="data">The data for the tree to be loaded</param>
    /// <param name="onComplete">A function to call when the load is complete</param>
    public static void LoadObjectTree(string data, Action<JSONLevelLoader> onComplete = null)
    {
        onComplete = onComplete ?? delegate { };
        LoadNow(data, true, false, onComplete);
    }


    /// <summary>
    ///   Internal function
    /// </summary>
    public static List<StoreInformation> GetComponentsInChildrenWithClause(GameObject go)
    {
        var components = new List<StoreInformation>();
        GetComponentsInChildrenWithClause(go.transform, components);
        return components;
    }

    public static event Action<string, float> Progress = delegate { };

    /// <summary>
    ///   Serializes the level to a byte array, with an optional root item. The root item
    ///   and its children, if specified, will be the only things saved
    /// </summary>
    /// <returns> The level data as a byte array </returns>
    /// <param name='urgent'> Whether to save even if serialization is suspended </param>
    /// <param name='id'> Identifier (or null) of an object to be the root of the data serialization </param>
    public static string SerializeLevel(bool urgent, string id)
    {
        LevelData ld;


        using (new Radical.Logging())
        {
			using(new UnitySerializer.ForceJSON())
			{
	            //First we need to know the name of the last level loaded
	            using (new UnitySerializer.SerializationScope())
	            {
	                ld = new LevelData()
	                         {
	                             //The level to reload
	                             Name = Application.loadedLevelName
	                         };
	                //All of the currently active uniquely identified objects
	                ld.StoredObjectNames = UniqueIdentifier
	                    .AllIdentifiers
	                    .Where(i => string.IsNullOrEmpty(id) || i.Id == id || HasParent(i, id))
	                    .Select(i => i.gameObject)
	                    .Where(go => go != null)
	                    .Where(go =>
	                               {
	                                   var shouldSerialize = go.FindInterface<IControlSerializationEx>();
	                                   return shouldSerialize == null || shouldSerialize.ShouldSaveWholeObject();
	                               })
	                    .Where(go =>
	                               {
	                                   if (Store == null)
	                                   {
	                                       return true;
	                                   }
	                                   var result = true;
	                                   Store(go, ref result);
	                                   return result;
	                               })
	                    .Select(n =>
	                                {
	                                    try
	                                    {
	                                        var si = new StoredItem()
	                                                     {
                                                             createEmptyObject = n.GetComponent<EmptyObjectIdentifier>() != null,
	                                                         Active = n.active,
										                     layer = n.layer,
									                         tag = n.tag,
									                         setExtraData = true,
                                                         
	                                                         Components =
	                                                             n.GetComponents<Component>().Where(c=>c!=null).Select(
	                                                                 c => c.GetType().FullName).Distinct().
	                                                             ToDictionary(v => v, v => true),
	                                                         Name = n.GetComponent<UniqueIdentifier>().Id,
	                                                         GameObjectName = n.name,
	                                                         ParentName =
	                                                             (n.transform.parent == null ||
	                                                              n.transform.parent.GetComponent<UniqueIdentifier>() ==
	                                                              null)
	                                                                 ? null
	                                                                 : (n.transform.parent.GetComponent<UniqueIdentifier>().
	                                                                       Id),
	                                                         ClassId = n.GetComponent<PrefabIdentifier>() != null
	                                                                       ? n.GetComponent<PrefabIdentifier>().ClassId
	                                                                       : string.Empty
	                                                     };
	                                        if (n.GetComponent<StoreInformation>())
	                                        {
	                                            n.SendMessage("OnSerializing", SendMessageOptions.DontRequireReceiver);
	                                        }
	                                        var pf = n.GetComponent<PrefabIdentifier>();
	                                        if (pf != null)
	                                        {
	                                            var components = GetComponentsInChildrenWithClause(n);
	                                            si.Children = components.GroupBy(c => c.ClassId).ToDictionary(c => c.Key,
	                                                                                                          c =>
	                                                                                                          c.Select(
	                                                                                                              i => i.Id)
	                                                                                                              .ToList());
	                                        }
	                                        return si;
	                                    }
	                                    catch (Exception e)
	                                    {
	                                        Debug.LogWarning("Failed to serialize status of " + n.name + " with error " +
	                                                         e.ToString());
	                                        return null;
	                                    }
	                                })
	                    .Where(si => si != null)
	                    .ToList();
	
	                //All of the data for the items to be stored
	
	                var toBeProcessed = UniqueIdentifier
	                    .AllIdentifiers
	                    .Where(o => o.GetComponent<StoreInformation>() != null || o.GetComponent<PrefabIdentifier>() != null)
	                    .Where(i => string.IsNullOrEmpty(id) || i.Id == id  || HasParent(i, id))
	                    .Where(i => i != null)
	                    .Select(i => i.gameObject)
	                    .Where(i => i != null)
	                    .Where(go =>
	                               {
	                                   var shouldSerialize = go.FindInterface<IControlSerializationEx>();
	                                   return shouldSerialize == null || shouldSerialize.ShouldSaveWholeObject();
	                               })
	                    .Distinct()
	                    .Where(go =>
	                               {
	                                   if (Store == null)
	                                   {
	                                       return true;
	                                   }
	                                   var result = true;
	                                   Store(go, ref result);
	
	                                   return result;
	                               })
	                    .SelectMany(o => o.GetComponents<Component>())
	                    .Where(c =>
	                               {
	                                   if (c == null)
	                                   {
	                                       return false;
	                                   }
	                                   var tp = c.GetType();
								       var store = true;
								       StoreComponent(c, ref store);
	                                   return store && (!(c is IControlSerialization) || (c as IControlSerialization).ShouldSave()) &&
	                                          !tp.IsDefined(typeof (DontStoreAttribute), true)
	                                          && !IgnoreTypes.Contains(tp.FullName);
	                               })
	                    .Select(c => new
	                                     {
	                                         Identifier =
	                                     (StoreInformation) c.gameObject.GetComponent(typeof (StoreInformation)),
	                                         Component = c
	                                     })
	                    .Where(cp =>
	                           (cp.Identifier.StoreAllComponents ||
	                            cp.Identifier.Components.Contains(cp.Component.GetType().FullName)))
	                    .OrderBy(cp => cp.Identifier.Id)
	                    .ThenBy(cp => cp.Component.GetType().FullName).ToList();
	                var processed = 0;
	                ld.StoredItems = toBeProcessed
	                    .Select(cp =>
	                                {
	                                    try
	                                    {
	                                        if (Radical.IsLogging())
	                                        {
	                                            Radical.Log("<{0} : {1} - {2}>", cp.Component.gameObject.GetFullName(),
	                                                        cp.Component.GetType().Name,
	                                                        cp.Component.GetComponent<UniqueIdentifier>().Id);
	                                            Radical.IndentLog();
	                                        }
	                                        var sd = new StoredData()
	                                                     {
	                                                         Type = cp.Component.GetType().FullName,
	                                                         ClassId = cp.Identifier.ClassId,
	                                                         Name = cp.Component.GetComponent<UniqueIdentifier>().Id
	                                                     };
	
	                                        if (CustomSerializers.ContainsKey(cp.Component.GetType()))
	                                        {
	                                            sd.Data = UnitySerializer.TextEncoding.GetString(CustomSerializers[cp.Component.GetType()].Serialize(cp.Component));
	                                        }
	                                        else
	                                        {
	                                            sd.Data = UnitySerializer.JSONSerializeForDeserializeInto(cp.Component);
	                                        }
											if (Radical.IsLogging())
	                                        {
	                                            Radical.OutdentLog();
	                                            Radical.Log("</{0} : {1}>", cp.Component.gameObject.GetFullName(),
	                                                        cp.Component.GetType().Name);
	                                        }
	                                        processed++;
	                                        Progress("Storing", (float) processed/(float) toBeProcessed.Count);
	                                        return sd;
	                                    }
	                                    catch (Exception e)
	                                    {
	                                        processed++;
	                                        Debug.LogWarning("Failed to serialize data (" +
	                                                         cp.Component.GetType().AssemblyQualifiedName + ") of " +
	                                                         cp.Component.name + " with error " + e.ToString());
	                                        return null;
	                                    }
	                                })
	                    .Where(s => s != null)
	                    .ToList();
	            }
	        }
		}
        var data = UnitySerializer.JSONSerialize(ld);
		return data;
    }
	
	
	/// <summary>
	/// Loads the specified data immediately
	/// </summary>
	/// <param name='data'>
	/// The data to load
	/// </param>
	public static void LoadNow(string data)
	{
		LoadNow(data, false, true, null);
	}
	
	/// <summary>
	/// Loads the specified data immediately
	/// </summary>
	/// <param name='data'>
	/// The data to load
	/// </param>
	/// <param name='dontDeleteExistingItems'>
	/// Whether items that are not in the file should be deleted
	/// </param>
	public static void LoadNow(string data, bool dontDeleteExistingItems)
	{
		LoadNow(data, dontDeleteExistingItems, true, null);
	}
	
	/// <summary>
	/// Loads the specified data immediately
	/// </summary>
	/// <param name='data'>
	/// Data to load
	/// </param>
	/// <param name='dontDeleteExistingItems'>
	/// Whether items that are not in the file should be deleted
	/// </param>
	/// <param name='showLoadingGUI'>
	/// Show the white flash
	/// </param>
	public static void LoadNow(string data, bool dontDeleteExistingItems, bool showLoadingGUI)
	{
		LoadNow(data, dontDeleteExistingItems, showLoadingGUI, null);
	}
	
	/// <summary>
	/// Loads the specified data immediately
	/// </summary>
	/// <param name='data'>
	/// Data to load
	/// </param>
	/// <param name='dontDeleteExistingItems'>
	/// Whether items that are not in the file should be deleted
	/// </param>
	/// <param name='showLoadingGUI'>
	/// Show the white flash
	/// </param>
	/// <param name='complete'>
	/// Function to call when complete
	/// </param>
	/// <exception cref='ArgumentException'>
	/// Is thrown when an argument passed to a method is invalid.
	/// </exception>
	public static void LoadNow(string data, bool dontDeleteExistingItems, bool showLoadingGUI, Action<JSONLevelLoader> complete)
	{
		
		if(data == null)
		{
			throw new ArgumentException("data parameter must be provided");
		}
		//Create a level loader
		var l = new GameObject();
		var loader = l.AddComponent<JSONLevelLoader>();
		loader.showGUI = showLoadingGUI;
		var ld = UnitySerializer.JSONDeserialize<JSONLevelSerializer.LevelData> (data);
		loader.Data = ld;
		loader.DontDelete = dontDeleteExistingItems;
		//Get the loader to do its job
		loader.StartCoroutine(PerformLoad(loader, complete));
	}
	
	static IEnumerator PerformLoad(JSONLevelLoader loader, Action<JSONLevelLoader> complete)
	{
		yield return loader.StartCoroutine(loader.Load(0, Time.timeScale));
		if(complete != null)
			complete(loader);
	}
	
	
    /// <summary>
    ///   Loads the saved level.
    /// </summary>
    /// <param name='data'> The data describing the level to load </param>
    public static JSONLevelLoader LoadSavedLevel(string data)
    {
        IsDeserializing = true;
		LevelSerializer.IsDeserializing = true;
        SaveGameManager.Loaded();
        var go = new GameObject();
        UnityEngine.Object.DontDestroyOnLoad(go);
        var loader = go.AddComponent<JSONLevelLoader>();
		loader.Data = UnitySerializer.JSONDeserialize<LevelData>(UnitySerializer.UnEscape(data));

		Application.LoadLevel(loader.Data.Name);
        return loader;
    }

    #region Nested type: CompareGameObjects

    private class CompareGameObjects : IEqualityComparer<GameObject>
    {
        #region IEqualityComparer[GameObject] implementation

        public bool Equals(GameObject x, GameObject y)
        {
            return System.String.Compare(x.GetComponent<PrefabIdentifier>().ClassId, y.GetComponent<PrefabIdentifier>().ClassId, System.StringComparison.Ordinal) == 0;
        }

        public int GetHashCode(GameObject obj)
        {
            return obj.GetComponent<PrefabIdentifier>().ClassId.GetHashCode();
        }

        #endregion

        public static readonly CompareGameObjects Instance = new CompareGameObjects();
    }

    #endregion

    #region Nested type: LevelData

    public class LevelData
    {
        //The name of the level that was saved
        public string Name;
        //A set of all of the unique object names on the level
        //The data that was saved for the level
        public List<StoredData> StoredItems;
        public List<StoredItem> StoredObjectNames;
		public string rootObject;
    }

    #endregion

    #region Nested type: ProgressHelper

    private class ProgressHelper 
    {
        #region ICodeProgress implementation

        public void SetProgress(long inSize, long outSize)
        {
            RaiseProgress("Compression",
                          0.5f);
        }

        #endregion
    }

    #endregion

    #region Nested type: SaveEntry

    /// <summary>
    ///   A saved game entry
    /// </summary>
    public class SaveEntry
    {
        /// <summary>
        ///   The data about the saved game
        /// </summary>
        public string Data;

        /// <summary>
        ///   The name of the unity scene
        /// </summary>
        public string Level;

        /// <summary>
        ///   The name provided for the saved game.
        /// </summary>
        public string Name;

        /// <summary>
        ///   The time that the game was saved
        /// </summary>
        public DateTime When;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JSONLevelSerializer.SaveEntry" /> class.
        /// </summary>
        /// <param name='contents'> The string representing the data of the saved game (use .ToString()) </param>
        public SaveEntry(string contents)
        {
            UnitySerializer.JSONDeserializeInto(contents, this);
        }

        public SaveEntry()
        {
        }

        /// <summary>
        ///   Gets the caption.
        /// </summary>
        /// <value> The caption which is a combination of the name, the level and the time that the game was saved </value>
        public string Caption
        {
            get { return string.Format("{0} - {1} - {2:g}", Name, Level, When); }
        }

        /// <summary>
        ///   Load this saved game
        /// </summary>
        public void Load()
        {
            LoadSavedLevel(Data);
        }

        /// <summary>
        ///   Delete this saved game
        /// </summary>
        public void Delete()
        {
            var owner = SavedGames.FirstOrDefault(p => p.Value.Contains(this));
            if (owner.Value != null)
            {
                owner.Value.Remove(this);
                SaveDataToPlayerPrefs();
            }
        }

        /// <summary>
        ///   Returns a <see cref="System.String" /> that represents the current <see cref="JSONLevelSerializer.SaveEntry" />.
        /// </summary>
        /// <returns> A <see cref="System.String" /> that represents the current <see cref="JSONLevelSerializer.SaveEntry" /> . </returns>
        public override string ToString()
        {
            return UnitySerializer.JSONSerialize(this);
        }
    }

    #endregion

    #region Nested type: SerializationHelper

    /// <summary>
    ///   Checks for the ability to serialize
    /// </summary>
    public class SerializationHelper : MonoBehaviour
    {
        public string gameName;
        public Action<string, bool> perform;

        private void Update()
        {
            //Check to see if we are still suspended
            if (IsSuspended == false)
            {
                if (perform != null)
                {
                    perform(gameName, false);
                }
                DestroyImmediate(gameObject);
            }
        }
    }

    #endregion

    #region Nested type: SerializationSuspendedException

    public class SerializationSuspendedException : Exception
    {
        public SerializationSuspendedException() : base("Serialization was suspended: " + _suspensionCount + " times")
        {
        }
    }

    #endregion

    #region Nested type: StoredData
	
	/// <summary>
	/// Stored data for a saved object
	/// </summary>
    public class StoredData
    {
		/// <summary>
		/// The class identifier if a prefab
		/// </summary>
        public string ClassId;
		/// <summary>
		/// The stored data for the item
		/// </summary>
        public string Data;
		/// <summary>
		/// The id of the item
		/// </summary>
        public string Name;
		/// <summary>
		/// The type of the stored item
		/// </summary>
        public string Type;
    }

    #endregion

    #region Nested type: StoredItem
	
	
    public class StoredItem
    {
        public bool Active;
        public bool createEmptyObject;
		public int layer;
		public string tag;
		public bool setExtraData;
        
        public List<string> ChildIds = new List<string>();
        public Dictionary<string, List<string>> Children = new Dictionary<string, List<string>>();
        public string ClassId;
        public Dictionary<string, bool> Components;

        [DoNotSerialize] public GameObject GameObject;
        public string GameObjectName;
        public string Name;
        public string ParentName;

        public override string ToString()
        {
            return string.Format("{0}  child of {2} - ({1})", Name, ClassId, ParentName);
        }
    }

    #endregion
}
