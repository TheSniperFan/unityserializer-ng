using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


[CustomEditor(typeof(StoreMaterials))]
public class StoreMaterialsEditor : Editor {
    private StoreMaterials script;


    public override void OnInspectorGUI() {
        script = (StoreMaterials)target;

        if (!EditorApplication.isPlaying) {
            Renderer renderer = script.GetComponent<Renderer>();

            foreach (Material mat in renderer.sharedMaterials) {
                if (mat) {
                    if (StoreMaterials.ShaderDatabase.ContainsKey(mat.shader.name)) {
                        using (new Horizontal()) {
                            using (new Vertical()) {
                                EditorGUILayout.LabelField("Status:");
                                EditorGUILayout.LabelField("Material name:");
                                EditorGUILayout.LabelField("Shader name:");
                            }
                            using (new Vertical()) {
                                Color c = GUI.color;
                                GUI.color = Color.green;
                                EditorGUILayout.LabelField("Storing", EditorStyles.boldLabel);
                                GUI.color = c;

                                EditorGUILayout.LabelField(mat.name);
                                string[] s = mat.shader.name.Split('/');
                                EditorGUILayout.LabelField(s[s.Length - 1]);
                            }
                        }
                    }
                    else {
                        EditorGUILayout.HelpBox(string.Format("Shader \"{0}\" wasn't found in the shader database!\nMake sure you open the Unity Serializer wizard, go to the Materials tab and rebuild it.", mat.shader.name)
                            , MessageType.Warning);

                        if (!StoreMaterials.Dirty) {
                            StoreMaterials.Dirty = true;
                        }
                    }
                }
            }
        }
        else {
            EditorGUILayout.HelpBox("Not available in play mode.", MessageType.Info);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh")) {
            script.SendMessage("OnEnable");
        }
    }
}