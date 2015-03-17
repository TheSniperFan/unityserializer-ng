using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


[CustomEditor(typeof(StoreMaterials))]
public class StoreMaterialsEditor : Editor {
    List<bool> show = new List<bool>();
    StoreMaterials script;

    private void Rebuild() {
        show.Clear();

        for (int i = 0; i < StoreMaterials.MaterialProperties.Count + 1; i++)
            show.Add(true);
    }

    public override void OnInspectorGUI() {
        script = (StoreMaterials)target;

        if ((show.Count - 1 - StoreMaterials.MaterialProperties.Count) != 0)
            Rebuild();

        show[0] = EditorGUILayout.Foldout(show[0], "Material properties");
        if (show[0]) {
            if (!EditorApplication.isPlaying) {
                Renderer renderer = script.GetComponent<Renderer>();

                foreach (Material mat in renderer.sharedMaterials) {
                    if (mat) {
                        if (StoreMaterials.MaterialProperties.ContainsKey(mat.name)) {
                            GUILayout.Label(mat.name);
                            foreach (StoreMaterials.MaterialProperty item in StoreMaterials.MaterialProperties[mat.name]) {
                                GUILayout.Label(item.name);
                            }
                        }
                        else {
                            EditorGUILayout.HelpBox(string.Format("Material \"{0}\" wasn't found in the material database!\nMake sure you open the Unity Serializer wizard, go to the Materials tab and rebuild it.", mat.name)
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
}