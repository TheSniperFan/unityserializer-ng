using UnityEngine;
using System.Collections;
using System.Linq;
using System;

#if UNITY_EDITOR

using UnityEditor;

[CustomEditor(typeof(StoreInformation))]
public class EditStoreInformation : Editor
{
	public override void OnInspectorGUI()
	{
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



