// /* ------------------

//       (c) whydoidoit.com 2012
//           by Mike Talbot 
//     ------------------- */
// 
using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public class UnitySerializerWindow : EditorWindow
{
	
	static Texture2D uid,room, manager,materials,store,prefab;
	
	static UnitySerializerWindow ()
	{
		EditorApplication.hierarchyWindowItemOnGUI += DrawThing;
		var path = "Assets/Plugins/Editor/whydoidoit.com/Assets/";
	
		uid = AssetDatabase.LoadAssetAtPath(path + "Id.psd", typeof(Texture2D)) as Texture2D;
		room = AssetDatabase.LoadAssetAtPath(path + "Room.psd", typeof(Texture2D)) as Texture2D;
		materials = AssetDatabase.LoadAssetAtPath(path + "Materials.psd", typeof(Texture2D)) as Texture2D;
		store = AssetDatabase.LoadAssetAtPath(path + "Store.psd", typeof(Texture2D)) as Texture2D;
		prefab = AssetDatabase.LoadAssetAtPath(path + "Prefab.psd", typeof(Texture2D)) as Texture2D;
		manager = AssetDatabase.LoadAssetAtPath(path + "Manager.psd", typeof(Texture2D)) as Texture2D;
		
		if(!EditorPrefs.GetBool("ShowedWarning", false))
		{
			if(EditorUtility.DisplayDialog("Information", "The Unity Serializer wizard menu has moved! It is now under the Window menu on the menu bar to save space.", "Got It", "Remind Me Again"))
			{
				EditorPrefs.SetBool("ShowedWarning", true);
			} 
		} 
		
	}
	
	static bool drawInfo = true;
	static bool showReferences = false;
	static bool showMain = true;
	static bool showRoom = false;
	static float width;
	
	static void DrawThing (int id, Rect area)
	{

		drawInfo = EditorPrefs.GetBool ("UnitySerializer.drawHierarchy", true);
		if (!drawInfo)
			return;

		var go = EditorUtility.InstanceIDToObject (id) as GameObject;
		if (go == null)
			return;
		if(go.transform.parent == null)
			width = area.width;
		
		area.width = 16;
		area.height = 16;
		
		if(go.GetComponent<SaveGameManager>() || go.GetComponent<RoomDataSaveGameStorage>() || go.GetComponent<RadicalRoutineHelper>())
		{
			area.x = width-54;
			GUI.DrawTexture(area, manager);
		}
		
		var c = go.GetComponent<UniqueIdentifier> ();
		if (c != null)
		{
			area.x =  width - 36;
			if (c.GetType () == typeof(UniqueIdentifier) && uid != null) {
				
				GUI.DrawTexture(area, uid);
			}
			if (c.GetType () == typeof(StoreInformation) && store != null) {
				GUI.DrawTexture(area, store);
			}
			if (c.GetType () == typeof(PrefabIdentifier) && prefab != null) {
				GUI.DrawTexture(area, prefab);
			}
			
		}
		
		if(go.GetComponent<DontStoreObjectInRoom>()!= null && room != null)
		{
			area.x = width;
			GUI.DrawTexture(area, room);

		}
		if(go.GetComponent<StoreMaterials>() != null && materials != null)
		{
			area.x =  width - 18;
			GUI.DrawTexture(area, materials);
			area.x+=18;
		}
		
		
	}
	
	[MenuItem("Window/Unity Serializer/Wizard %&0")]
	static void Init ()
	{
		// Get existing open window or if none, make a new one:
		UnitySerializerWindow window = EditorWindow.GetWindow<UnitySerializerWindow> (false, "Unity Serializer");
		window.autoRepaintOnSceneChange = true;
		window.Show ();
	}
	
	[MenuItem("Window/Unity Serializer/Store Information %&s")]
	static void AddStoreInformation ()
	{
		if (Selection.activeGameObject == null)
			return;
		Add (typeof(StoreInformation), Selection.activeGameObject);
	}
	
	[MenuItem("Window/Unity Serializer/Unique Identifier %&u")]
	static void AddUniqueIdentifier ()
	{
		if (Selection.activeGameObject == null)
			return;
		Add (typeof(UniqueIdentifier), Selection.activeGameObject);
	}
	
	[MenuItem("Window/Unity Serializer/Prefab Identifier %&p")]
	static void AddPrefabIdentifier ()
	{
		if (Selection.activeGameObject == null || PrefabUtility.GetPrefabType (Selection.activeGameObject) == PrefabType.None || Has (Selection.activeGameObject, typeof(PrefabIdentifier)))
			return;
		Add (typeof(PrefabIdentifier), Selection.activeGameObject);
	}
	
	[MenuItem("Window/Unity Serializer/Materials %&m")]
	static void AddMaterials ()
	{
		if (Selection.activeGameObject == null || Selection.activeGameObject.GetComponent<StoreInformation>() == null )
			return;
		Selection.activeGameObject.AddComponent<StoreMaterials>();
	}
	
	[MenuItem("Window/Unity Serializer/Remove All %&x")]
	static void Remove ()
	{
		if (Selection.activeGameObject == null)
			return;
		foreach (var c in Selection.activeGameObject.GetAllComponentsInChildren<UniqueIdentifier>())
			DestroyImmediate (c);
	}
	
	private Dictionary<string, bool> open = new Dictionary<string, bool> ();
	Vector2 scroll;
	
	private static Index<string, Index<string, List<object>>> _assetStore = new Index<string, Index<string, List<object>>>();
	
	void Awake()
	{
		BuildLocalAssetStore();
	}
	
	void BuildLocalAssetStore()
	{
		var tmp = Resources.FindObjectsOfTypeAll(typeof(AnimationClip))
			.Concat(Resources.FindObjectsOfTypeAll(typeof(AudioClip)))
			.Concat(Resources.FindObjectsOfTypeAll(typeof(Mesh)))
			.Concat(Resources.FindObjectsOfTypeAll(typeof(Material)))
			.Concat(Resources.FindObjectsOfTypeAll(typeof(Texture)))
			.Distinct()
			.ToList();
		var assets = tmp
			.Where(g=>g!=null && !string.IsNullOrEmpty(g.name) )
			.Where(a=>AssetDatabase.IsMainAsset(a) || AssetDatabase.IsSubAsset(a))
			.Distinct()
			.ToList();
		_assetStore.Clear();
		foreach(var a in assets)
		{
			_assetStore[a.GetType().Name][a.name].Add(a);
			
		}
		foreach(var a in tmp)
		{
			if(_assetStore.ContainsKey(a.GetType().Name) && _assetStore[a.GetType().Name].ContainsKey(a.name) && !_assetStore[a.GetType().Name][a.name].Contains(a))
			{
				_assetStore[a.GetType().Name][a.name].Add(a);
			}
		}
		
	}
	

	void OnGUI ()
	{
		if(_assetStore.Count==0)
		{
			BuildLocalAssetStore();
		}
		var bold = new GUIStyle ("label");
		var boldFold = new GUIStyle ("foldout");
		bold.fontStyle = FontStyle.Bold;
		bold.fontSize = 14;
		boldFold.fontStyle = FontStyle.Bold;
		var someMatched = false;
		
		using (new Horizontal()) {
			
			if (GUILayout.Toggle (showMain, "Save Game", "toolbarbutton", GUILayout.ExpandWidth (false))) {
				showMain = true;
				showReferences = false;
				showRoom = false;
			}
		
			if (GUILayout.Toggle (showRoom, "Rooms", "toolbarbutton", GUILayout.ExpandWidth (false))) {
				showMain = false;
				showRoom = true;
				showReferences = false;
			}
			if (_assetStore.SelectMany (s => s.Value).All (s => s.Value.Count <= 1)) {
				GUI.color = Color.white;
			} else {
				someMatched = true;
				GUI.color = Color.yellow;
			}	
			
			if (GUILayout.Toggle (showReferences, "References", "toolbarbutton", GUILayout.ExpandWidth (false))) {
				showMain = false;
				showRoom = false;
				showReferences = true;
			}
			GUILayout.Label("", "toolbarbutton");
			GUI.color = Color.white;
			
		}
		if (showReferences) {
			using (new ScrollView(ref scroll)) {
				using (new Vertical()) {
					foreach (var tp in _assetStore) {
						if (!open.ContainsKey (tp.Key))
							open [tp.Key] = false;
						
						if(tp.Value.Any(v=>v.Value.Count >1) )
						{
							GUI.color = Color.yellow;
							open [tp.Key] = EditorGUILayout.Foldout (open [tp.Key], tp.Key.ToString (), boldFold);
							GUI.color = Color.white;
						}
						else
						{
							open [tp.Key] = EditorGUILayout.Foldout (open [tp.Key], tp.Key.ToString (), "foldout");
						}
						if (open [tp.Key]) {
							foreach (var n in tp.Value.OrderByDescending(q=>q.Value.Count).ThenBy(q=>q.Key)) {
								using (new Horizontal()) {
									GUILayout.Space (10);
									if (!open.ContainsKey (tp.Key + n.Key))
										open [tp.Key + n.Key] = false;
									if (n.Value.Count <= 1) {
										open[tp.Key + n.Key] = false;
										if(GUILayout.Button (n.Key, "label"))
										{
											Selection.activeObject = n.Value[0] as UnityEngine.Object;
											EditorGUIUtility.PingObject(n.Value[0] as UnityEngine.Object);
										}
									} else {
										open [tp.Key +  n.Key] = EditorGUILayout.Foldout (open [tp.Key +  n.Key], n.Key + " (" + n.Value.Count + ")", boldFold);
									}
								}
								if (open [tp.Key +  n.Key]) {
									using (new Vertical()) {
										foreach (var i in n.Value.Cast<UnityEngine.Object>()) {
											using (new Horizontal()) {
												GUILayout.Space (20);
												var addOn="";
												if(!AssetDatabase.IsMainAsset(i) && !AssetDatabase.IsSubAsset(i))
												{
													GUI.color = Color.red;
													addOn = " (internal to Unity)";
												}
												else
													GUI.color = Color.white;
												if (GUILayout.Button (i.name.ToString () + addOn, "label")) {
													Selection.activeObject = i as UnityEngine.Object;
													EditorGUIUtility.PingObject(i);
												}
												GUI.color = Color.white;
											}
										}
									}
								}
								
							}
						}
					}
				}
			}
			if(someMatched)
			{
				var style = new GUIStyle ("label");
				style.wordWrap = true;

				GUILayout.Space(5);
				GUI.color = Color.yellow;
				GUILayout.Label("Warning", bold);
				GUI.color =Color.white;
				GUILayout.Label("Some referencable items have the same name. This is only a problem if you actually refer to one of the items in a saved variable and then more get added with the same name (existing saved games may load the wrong instance), or if for some other reason Unity reorders the similarly named items.  If you use a highlighted item as a reference it may be worth renaming it to be a unique name if you can.", style);
				GUILayout.Space(5);
				
			}
		}
		
		if (showMain) {
			GUILayout.BeginVertical ();
			if (Selection.gameObjects.Length == 1) {
				if (Selection.activeGameObject != null) {
					GUILayout.Space (8);
					using (new HorizontalCentered()) {
						GUILayout.Label (Selection.activeGameObject.name, bold);
					}
					using (new HorizontalCentered()) {
						GUI.color = new Color (0.8f, 1f, 0.8f, 1);
				
						if (Has (Selection.activeGameObject, typeof(UniqueIdentifier))) {
							GUILayout.Label ("Uniquely Identified");
						} else if (Has (Selection.activeGameObject, typeof(StoreInformation))) {
							GUILayout.Label ("Storing Information");
						} else if (Has (Selection.activeGameObject, typeof(PrefabIdentifier))) {
							GUILayout.Label ("Prefab Identifier");
						} else {
							GUI.color = Color.gray;
							GUILayout.Label ("Nothing stored");
						}
						if(Has(Selection.activeGameObject, typeof(StoreMaterials)))
						{
							GUILayout.Label(" + materials");
						}
						if(Has(Selection.activeGameObject, typeof(StoreMesh)))
						{
							GUILayout.Label(" + mesh");
						}
					}
					GUILayout.Space (4);
					GUI.color = Color.white;
					using (new HorizontalCentered()) {
						using (new Vertical()) {
							using (new Horizontal()) {
								if (!Has (Selection.activeGameObject, typeof(UniqueIdentifier)) && GUILayout.Button ("Unique Identifier")) {
									Undo.RegisterSceneUndo ("Change Identifiers");
									Add (typeof(UniqueIdentifier), Selection.activeGameObject);
								}
								if (!Has (Selection.activeGameObject, typeof(StoreInformation)) && GUILayout.Button ("Store Information")) {
									Undo.RegisterSceneUndo ("Change Identifiers");
									Add (typeof(StoreInformation), Selection.activeGameObject);
								}
								if(Selection.activeGameObject.GetComponent<StoreMaterials>() == null && Selection.activeGameObject.GetComponent<StoreInformation>() != null)
								{
									if(GUILayout.Button("Store Materials"))
									{
										Undo.RegisterSceneUndo ("Change Identifiers");
										Selection.activeGameObject.AddComponent<StoreMaterials>();
									}
								}
								if(Selection.activeGameObject.GetComponent<StoreMesh>() == null && Selection.activeGameObject.GetComponent<StoreInformation>() != null && (Selection.activeGameObject.GetComponent<MeshFilter>() != null || Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() != null))
								{
									if(GUILayout.Button("Store Mesh"))
									{
										Undo.RegisterSceneUndo ("Change Identifiers");
										Selection.activeGameObject.AddComponent<StoreMesh>();
									}
								}
							}
							using (new Horizontal()) {
								if (!Has (Selection.activeGameObject, typeof(PrefabIdentifier)) && PrefabUtility.GetPrefabType (Selection.activeGameObject) != PrefabType.None && GUILayout.Button ("Prefab Identifier")) {
									Undo.RegisterSceneUndo ("Change Identifiers");
									Add (typeof(PrefabIdentifier), Selection.activeGameObject);
									PrefabUtility.ReplacePrefab (Selection.activeGameObject, PrefabUtility.GetPrefabParent (Selection.activeGameObject), ReplacePrefabOptions.ConnectToPrefab);
								}
								GUI.color = new Color (0.9f, 0.5f, 0.5f);
								if (GUILayout.Button ("Remove")) {
									Undo.RegisterSceneUndo ("Change Identifiers");
									foreach (var c in Selection.activeGameObject.GetComponents<UniqueIdentifier>())
										DestroyImmediate (c, true);
									foreach (var c in Selection.activeGameObject.GetComponents<StoreMaterials>())
										DestroyImmediate (c, true);
									foreach (var c in Selection.activeGameObject.GetComponents<StoreMesh>())
										DestroyImmediate (c, true);
									
								}
								GUI.color = Color.white;
							}
						}
					}
					GUILayout.Space (12);
					if (Selection.activeGameObject.transform.Cast<Transform> ().Count () > 0) {
						using (new HorizontalCentered()) {
						
							GUILayout.Label (Selection.activeGameObject.name, bold);
							bold.fontStyle = FontStyle.Italic;
							GUILayout.Label ("+ Children", bold);
						}
						GUILayout.Space (6);
						using (new HorizontalCentered()) {
							using (new Vertical()) {
								using (new Horizontal()) {
									if (GUILayout.Button ("Unique Identifier")) {
										Undo.RegisterSceneUndo ("Change Identifiers");
										foreach (var c in Selection.activeGameObject.GetAllComponentsInChildren<Transform>().Select(t=>t.gameObject).Where(g=>!Has(g, typeof(UniqueIdentifier))))
											Add (typeof(UniqueIdentifier), c);
									}
									if (GUILayout.Button ("Store Information")) {
										Undo.RegisterSceneUndo ("Change Identifiers");
										foreach (var c in Selection.activeGameObject.GetAllComponentsInChildren<Transform>().Select(t=>t.gameObject).Where(g=>!Has(g, typeof(StoreInformation))))
											Add (typeof(StoreInformation), c);
									}
								}
								GUI.color = new Color (0.9f, 0.5f, 0.5f);
								if (GUILayout.Button ("Remove All")) {
									Undo.RegisterSceneUndo ("Change Identifiers");
									foreach (var c in Selection.activeGameObject.GetAllComponentsInChildren<UniqueIdentifier>())
										DestroyImmediate (c, true);
									foreach (var c in Selection.activeGameObject.GetAllComponentsInChildren<StoreMaterials>())
										DestroyImmediate (c, true);
									foreach (var c in Selection.activeGameObject.GetComponents<StoreMesh>())
										DestroyImmediate (c, true);

								}
								GUI.color = Color.white;
							}
						}
					}
				
				}
			} else if (Selection.gameObjects.Length > 0) {
				using (new HorizontalCentered()) {
						
					GUILayout.Label ("Selected Objects", bold);
				}
				GUILayout.Space (6);
				using (new HorizontalCentered()) {
					using (new Vertical()) {
						using (new Horizontal()) {
							if (GUILayout.Button ("Unique Identifier")) {
								Undo.RegisterSceneUndo ("Change Identifiers");
								foreach (var c in Selection.gameObjects)
									Add (typeof(UniqueIdentifier), c);
							}
							if (GUILayout.Button ("Store Information")) {
								Undo.RegisterSceneUndo ("Change Identifiers");
								foreach (var c in Selection.gameObjects)
									Add (typeof(StoreInformation), c);
							}
						}
						using(new Horizontal())
						{
							if(GUILayout.Button("Store Materials"))
							{
								foreach (var c in Selection.gameObjects.Where(g=>g.GetComponent<StoreMaterials>()==null && g.renderer != null))
									c.AddComponent<StoreMaterials>();
							}
							GUI.color = new Color (0.9f, 0.5f, 0.5f);
							if (GUILayout.Button ("Remove All")) {
								Undo.RegisterSceneUndo ("Change Identifiers");
								foreach (var c in Selection.gameObjects.Cast<GameObject>().SelectMany(s=>s.GetAllComponentsInChildren<UniqueIdentifier>()))
									DestroyImmediate (c);
							}
							GUI.color = Color.white;
							
						}
					}
				}
			}
			GUILayout.FlexibleSpace ();
			var items = GameObject.FindObjectsOfType (typeof(SaveGameManager));
			if (items == null || items.Length == 0) {
				GUI.color = new Color (1, 0.6f, 0.6f, 1f);
				var style = new GUIStyle ("label");
				style.wordWrap = true;
				GUILayout.Label ("You need to assign a Save Game Manager script to an object in this scene.  This object should be something that ISN'T loaded and saved with one of the scripts, and NOT set to DontDestroyOnLoad.", style);
				GUILayout.Space (6);
				GUI.color = Color.white;
				using (new Horizontal()) {
					if (Selection.activeGameObject != null && GUILayout.Button ("Assign Save Game Manager")) {
						Undo.RegisterSceneUndo ("Assign save game manager");
						Selection.activeGameObject.AddComponent<SaveGameManager> ();
					} 
					if (GUILayout.Button ("Create a new Save Game Manager")) {
						Undo.RegisterSceneUndo ("Create save game manager");
						var go = new GameObject ();
						go.name = "Save Game Manager";
						go.AddComponent<SaveGameManager> ();
					}
				}
				GUILayout.Space (15);
			} 
			
			
			
			GUILayout.Label ("Maintenance");
			using (new Horizontal()) {
				if (GUILayout.Button ("Delete all saved games")) {
					if (EditorUtility.DisplayDialog ("Delete Saved Games", 
						"Are you sure you want to delete all of the saved games?", "Yes", "No")) {
						LevelSerializer.SavedGames.Clear ();
						LevelSerializer.SaveDataToPlayerPrefs (); 
						JSONLevelSerializer.SavedGames.Clear();
						JSONLevelSerializer.SaveDataToPlayerPrefs();
					}
				}
				if (GameObject.FindObjectsOfType (typeof(SaveGameManager)).Length != 0 && GUILayout.Button ("Capture All Objects")) {
					EditorApplication.isPlaying = true;
					stopWhenReady = 50;
				}
			
			}
			GUILayout.Space (8);
			using (new Horizontal()) {
				GUILayout.Label (string.Format ("Tracked Scene Items: {0:#,0}", UniqueIdentifier.AllIdentifiers.Count));
				var old = drawInfo;
				drawInfo = GUILayout.Toggle (drawInfo, "Hierarchy markers");
				if (old != drawInfo) {
					EditorApplication.RepaintHierarchyWindow ();
					EditorPrefs.SetBool ("UnitySerializer.drawHierarchy", drawInfo);
				}
				
			}
			GUILayout.Space (4);
			GUILayout.EndVertical ();
		}
		
		if (showRoom) 
		{
			if (GameObject.FindObjectsOfType (typeof(Room)).Length > 0) {
				
			GUILayout.BeginVertical ();
			if (Selection.gameObjects.Length == 1) {
				if (Selection.activeGameObject != null) {
					GUILayout.Space (8);
					using (new HorizontalCentered()) {
						GUILayout.Label (Selection.activeGameObject.name, bold);
					}
					using (new HorizontalCentered()) {
						GUI.color = new Color (0.8f, 1f, 0.8f, 1);
				
						
						if (Selection.activeGameObject.GetComponent<DontStoreObjectInRoom>()) {
							GUILayout.Label (  Has(Selection.activeGameObject, typeof(StoreInformation)) ?  "Stored in the save game but not stored in the room state" : "Not saved but flagged for exclusion anyway.");
						} else if( Has(Selection.activeGameObject, typeof(StoreInformation))) 
						{
							GUILayout.Label("Stored in the saved game and the room");
						}
					}
					GUILayout.Space (4);
					GUI.color = Color.white;
					using (new HorizontalCentered()) {
						using (new Vertical()) {
							using (new Horizontal()) {
								if (!Selection.activeGameObject.GetComponent<DontStoreObjectInRoom>() && GUILayout.Button ("Exclude from room")) {
									Undo.RegisterSceneUndo ("Exclude");
									Add (typeof(DontStoreObjectInRoom), Selection.activeGameObject);
								}
								
							}
							GUI.color = new Color (0.9f, 0.5f, 0.5f);
								if (GUILayout.Button ("Remove")) {
									Undo.RegisterSceneUndo ("Change Identifiers");
									foreach (var c in Selection.activeGameObject.GetComponents<DontStoreObjectInRoom>())
										DestroyImmediate (c);
									
								}
								GUI.color = Color.white;
							}
						
					}
					GUILayout.Space (12);
					if (Selection.activeGameObject.transform.Cast<Transform> ().Count () > 0) {
						using (new HorizontalCentered()) {
						
							GUILayout.Label (Selection.activeGameObject.name, bold);
							bold.fontStyle = FontStyle.Italic;
							GUILayout.Label ("+ Children", bold);
						}
						GUILayout.Space (6);
						using (new HorizontalCentered()) {
							using (new Vertical()) {
								using (new Horizontal()) {
									if (GUILayout.Button ("Exclude from room")) {
										Undo.RegisterSceneUndo ("Change Identifiers");
										foreach (var c in Selection.activeGameObject.GetAllComponentsInChildren<Transform>().Select(t=>t.gameObject).Where(g=>!g.GetComponent<DontStoreObjectInRoom>()))
											Add (typeof(DontStoreObjectInRoom), c);
									}
									
								
								GUI.color = new Color (0.9f, 0.5f, 0.5f);
								if (GUILayout.Button ("Remove")) {
									Undo.RegisterSceneUndo ("Change Identifiers");
									foreach (var c in Selection.activeGameObject.GetAllComponentsInChildren<DontStoreObjectInRoom>())
										DestroyImmediate (c);
									}
								GUI.color = Color.white;
							}
							
					}
						}
				}
					}
			} else if (Selection.gameObjects.Length > 0) {
				using (new HorizontalCentered()) {
						
					GUILayout.Label ("Selected Objects", bold);
				}
				GUILayout.Space (6);
				using (new HorizontalCentered()) {
					using (new Vertical()) {
						using (new Horizontal()) {
							if (GUILayout.Button ("Exlude from room")) {
								Undo.RegisterSceneUndo ("Change Identifiers");
								foreach (var c in Selection.gameObjects.Where(g=>g.GetComponent<DontStoreObjectInRoom>()))
									Add (typeof(DontStoreObjectInRoom), c);
							}
							
						}
						using(new Horizontal())
						{
							GUI.color = new Color (0.9f, 0.5f, 0.5f);
							if (GUILayout.Button ("Remove")) {
								Undo.RegisterSceneUndo ("Change Identifiers");
								foreach (var c in Selection.gameObjects.Cast<GameObject>().SelectMany(s=>s.GetAllComponentsInChildren<DontStoreObjectInRoom>()))
									DestroyImmediate (c);
							}
							GUI.color = Color.white;
							
						}
					}
				}
			}
			}
			GUILayout.FlexibleSpace ();
			if (GameObject.FindObjectsOfType (typeof(Room)).Length == 0) {
				GUI.color = new Color (1, 0.6f, 0.6f, 1f);
				var style = new GUIStyle ("label");
				style.wordWrap = true;
				GUILayout.Label ("You need to assign a Room Manager script to an object in this scene.  This object should be something that IS loaded and saved with one of the scripts, and NOT set to DontDestroyOnLoad.", style);
				GUILayout.Space (6);
				GUI.color = Color.white;
				using (new Horizontal()) {
					if (GUILayout.Button ("Create a new Room Manager")) {
						Undo.RegisterSceneUndo ("Create room manager");
						var go = new GameObject ();
						go.name = "Room Manager";
						go.AddComponent<StoreInformation> ();
						go.AddComponent<RoomDataSaveGameStorage>();
						go.AddComponent<Room>();
					}
				}
				GUILayout.Space (15);
			} 
			
			
			
			GUILayout.Label ("Maintenance");
			using (new Horizontal()) {
				if (GUILayout.Button ("Delete all saved games")) {
					if (EditorUtility.DisplayDialog ("Delete Saved Games", 
						"Are you sure you want to delete all of the saved games?", "Yes", "No")) {
						LevelSerializer.SavedGames.Clear ();
						LevelSerializer.SaveDataToPlayerPrefs ();
						JSONLevelSerializer.SavedGames.Clear ();
						JSONLevelSerializer.SaveDataToPlayerPrefs (); 
					}
				}
				if (GameObject.FindObjectsOfType (typeof(SaveGameManager)).Length != 0 && GUILayout.Button ("Capture All Objects")) {
					EditorApplication.isPlaying = true;
					stopWhenReady = 50;
				}
			
			}
			GUILayout.Space (8);
			using (new Horizontal()) {
				GUILayout.Label (string.Format ("Tracked Scene Items: {0:#,0}", UniqueIdentifier.AllIdentifiers.Count));
				var old = drawInfo;
				drawInfo = GUILayout.Toggle (drawInfo, "Hierarchy markers");
				if (old != drawInfo) {
					EditorApplication.RepaintHierarchyWindow ();
					EditorPrefs.SetBool ("UnitySerializer.drawHierarchy", drawInfo);
				}
				
			}
			GUILayout.Space (4);
			GUILayout.EndVertical ();
		}
		Repaint ();
	
	}
	
	int stopWhenReady = 0;
	
	static bool Has (GameObject go, Type tp)
	{
		var c = go.GetComponent (tp);
		return (c != null && c.GetType () == tp);
			
	}
	
	static void Add (Type componentType, GameObject go)
	{
		if(componentType.IsSubclassOf(typeof(UniqueIdentifier)))
		{
			foreach (var c in go.GetComponents(typeof(UniqueIdentifier)).Cast<UniqueIdentifier>())
				DestroyImmediate (c);
		}
		go.AddComponent (componentType);
	}
	
	void Update () //Called 100 times per second on all visible windows.
	{
		if (stopWhenReady != 0) {
			stopWhenReady --;
			if (stopWhenReady == 0) {
				EditorApplication.isPlaying = false;
				EditorApplication.SaveScene ();
			}
		}
	}
//	
//	void OnInspectorUpdate() //OnInspectorUpdate is called at 10 frames per second to give the inspector a chance to update
//	{}
//	
//	void OnFocus() //Called when the window gets keyboard focus.
//	{	Debug.Log(string.Format("EditorWindow[{0}].OnFocus", this.title));	}
//	
//	void OnLostFocus() //Called when the window loses keyboard focus.
//	{	Debug.Log(string.Format("EditorWindow[{0}].OnLostFocus", this.title));	}
//	
	void OnSelectionChange () //Called whenever the selection has changed.
	{	
		Repaint ();
	}
//	
	void OnHierarchyChange () //Called whenever the scene hierarchy has changed.
	{
		Repaint ();
	}
//	
	void OnProjectChange () //Called whenever the project has changed.
	{	
		BuildLocalAssetStore();
	}
	
//	
//	void OnEnable() //This function is called when the object is loaded
//	{	Debug.Log(string.Format("EditorWindow[{0}].OnEnable", this.title));	}
//	
//	void OnDisable() //This function is called when the scriptable object goes out of scope
//	{	Debug.Log(string.Format("EditorWindow[{0}].OnDisable", this.title));	}
//
//	void OnDestroy() //OnDestroy is called when the EditorWindow is closed.
//	{	Debug.Log(string.Format("EditorWindow[{0}].OnDestroy", this.title));	}
	
}

#endif
