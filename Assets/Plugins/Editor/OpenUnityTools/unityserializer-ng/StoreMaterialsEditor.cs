//using UnityEngine;
//using UnityEditor;
//using System.Collections;
//using System.Collections.Generic;

//[ExecuteInEditMode]
//[CustomEditor(typeof(StoreMaterials))]
//public class StoreMaterialsEditor : Editor {
//    List<bool> show = new List<bool>();
//    StoreMaterials script;

//    private void Rebuild() {
//        show.Clear();

//        for (int i = 0; i < script.MaterialProperties.Count + 1; i++)
//            show.Add(false);
//    }

//    public override void OnInspectorGUI() {
//        script = (StoreMaterials)target;

//        if ((show.Count - 1 - script.MaterialProperties.Count) != 0)
//            Rebuild();

//        show[0] = EditorGUILayout.Foldout(show[0], "Material properties");
//        if (show[0]) {
//            int i = 1;
//            foreach (string mat in script.MaterialProperties.Keys) {
//                show[i] = EditorGUILayout.Foldout(show[i], mat);
//                if (show[i]) {
//                    foreach (StoreMaterials.MaterialProperty property in script.MaterialProperties[mat]) {
//                        string type = "";

//                        switch ((ShaderUtil.ShaderPropertyType)property.type) {
//                            case ShaderUtil.ShaderPropertyType.Color:
//                                type = "COLOR";
//                                break;
//                            case ShaderUtil.ShaderPropertyType.Float:
//                                type = "FLOAT";
//                                break;
//                            case ShaderUtil.ShaderPropertyType.Range:
//                                type = "FLOAT";
//                                break;
//                            case ShaderUtil.ShaderPropertyType.TexEnv:
//                                type = "TEXTURE";
//                                break;
//                            case ShaderUtil.ShaderPropertyType.Vector:
//                                type = "VECTOR";
//                                break;
//                        }

//                        GUILayout.Label("[" + type + "]\t" + property.description);
//                    }
//                }
//                i++;
//            }

//            EditorGUILayout.Space();
//            if (GUILayout.Button("Refresh")) {
//                script.ForceRefresh();
//            }
//        }
//    }
//}