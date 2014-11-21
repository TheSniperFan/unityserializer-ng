using UnityEngine;
using System.Collections;
using System.Linq;

using UnityEditor;


[ExecuteInEditMode]
[CustomEditor(typeof(SaveGameManager))]
public class EditSaveGameManager : Editor
{
	bool foldout;
	public override void OnInspectorGUI()
	{
		var t = target as SaveGameManager;
		DrawDefaultInspector();
		if(t.Reference != null)
		{
			foldout = EditorGUILayout.Foldout(foldout,"Tracked Objects: " + t.Reference.Count);
			if(foldout)
			{
				foreach(var g in t.Reference.AllReferences.OrderBy(go=>go.name))
				{
					using(new Horizontal())
					{
						GUILayout.Space(25);
						if(GUILayout.Button(g.name, GUILayout.Width(120)))
						{
							EditorGUIUtility.PingObject(g);
						}
						GUILayout.Label(g.GetComponent<UniqueIdentifier>().Id + " > " + g.GetInstanceID());
					}
				}
			}
		}
		
	}
}