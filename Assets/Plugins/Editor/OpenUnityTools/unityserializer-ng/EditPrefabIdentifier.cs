using UnityEngine;
using System.Collections;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

[ExecuteInEditMode]
[CustomEditor(typeof(PrefabIdentifier))]
public class EditPrefabIdentifier : Editor
{
	public override void OnInspectorGUI()
	{
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel("Current Prefab Identifier");
		EditorGUILayout.LabelField((target as UniqueIdentifier).ClassId);

		if (GUILayout.Button("Create new prefab identifier"))
		{
			if (EditorUtility.DisplayDialog("Create new prefab identifier", 
				"If you change the prefab identifier and then update the prefab or store it over an existing prefab then saved information will no longer work correctly.  You will normally click this button when you intend to create a new prefab out of an existing prefab. Use with caution, ESPECIALLY AFTER YOU HAVE RELEASED YOUR GAME.", 
				"I understand, go ahead and changed it", "Cancel"))
			{
				(target as UniqueIdentifier).ClassId = Guid.NewGuid().ToString();
				EditorUtility.SetDirty(target);
			}
		}
		
		EditorGUILayout.EndHorizontal();
		
	
		DrawDefaultInspector();
		
		var t = target as StoreInformation;
		
		if (!t.StoreAllComponents)
		{ 
			GUILayout.Label("  Store which components");
			var cs = t.GetComponents<Component>().Where(c => !c.GetType().IsDefined(typeof(DontStoreAttribute), false) && (c.hideFlags & HideFlags.HideInInspector) == 0);
			foreach (var c in cs)
			{
				var typeName = c.GetType().FullName;
				GUILayout.BeginHorizontal();
				GUILayout.Label("      >       ");
				if (GUILayout.Toggle(t.Components.Contains(typeName), ObjectNames.NicifyVariableName(typeName)))
				{
					if(!t.Components.Contains(typeName))
						t.Components.Add(typeName);
					EditorUtility.SetDirty(target);
					
				}
				else
				{
					t.Components.Remove(typeName);
					EditorUtility.SetDirty(target);
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
		}
	}
	
}

#endif
