using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System;

public static class MaterialDBGenerator {
    public struct ShaderProperty {
        public string name;
        public ShaderUtil.ShaderPropertyType type;
    }

    private static Dictionary<string, List<ShaderProperty>> database = new Dictionary<string, List<ShaderProperty>>();

    private static string currentScene;

    private static void WriteDatabase() {
        string path = Path.GetFullPath(Application.dataPath) + "\\Plugins\\OpenUnityTools\\unityserializer-ng\\Serialization\\GENERATED";
        try {
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            File.WriteAllText(path + "\\GEN_MaterialDB.cs", code);
        }
        catch (IOException e) {
            Debug.LogErrorFormat("IO Exception while writing material database.\n{0}", e);
        }
        catch (Exception e) {
            Debug.LogErrorFormat("Unexpected error while writing material database.\n{0}", e);
        }
        finally {
            code = string.Empty;
        }
    }

    public static void GenerateMaterialDB() {
        currentScene = EditorApplication.currentScene;

        EditorBuildSettingsScene[] scenes = (from s in EditorBuildSettings.scenes
                                             where s.enabled
                                             select s).ToArray();
        foreach (var scene in scenes) {
            ProcessScene(scene);
        }

        EditorApplication.OpenScene(currentScene);
        currentScene = null;

        GenerateClass();
        GenerateMaterials();

        foreach (KeyValuePair<string, List<ShaderProperty>> item in database) {
            GenerateProperties(item.Key);
        }

        WriteDatabase();
    }

    private static void GenerateClass() {
        code = classStub;
        int count = database.Count - 1;
        for (int i = 0; i < count; i++) {
            code = Regex.Replace(code, @"\$0", "$MATERIAL\r\n$0");
        }
        code = Regex.Replace(code, @"\$0", "$MATERIAL");
    }

    private static void GenerateMaterials() {
        code = Regex.Replace(code, @"\$MATERIAL", materialStub);
    }

    private static void GenerateProperties(string materialName) {
        Regex regex = new Regex(Regex.Escape("$MATERIAL"));
        code = regex.Replace(code, materialStub, 1);

        regex = new Regex(Regex.Escape("$MATNAME"));
        code = regex.Replace(code, materialName, 1);

        string s = "";
        for (int i = 0; i < database[materialName].Count; i++) {
            s += "$PROPERTY,\r\n";
        }
        s = s.Substring(0, s.Length - 3);
        regex = new Regex(Regex.Escape("$1"));
        code = regex.Replace(code, s, 1);

        regex = new Regex(Regex.Escape("$PROPERTY"));

        foreach (ShaderProperty prop in database[materialName]) {
            code = regex.Replace(code, propertyStub, 1);
            code = Regex.Replace(code, @"\$PROPNAME", prop.name);
            code = Regex.Replace(code, @"\$PROPTYPE", "MaterialProperty.PropertyType." + prop.type.ToString());
        }
    }

    private const string classStub =
@"using System;
using System.Collections.Generic;

public partial class StoreMaterials {
public StoreMaterials() {

if (MaterialProperties.Count == 0) {
$0
}
}
}
";

    private const string materialStub =
@"MaterialProperties.Add(""$MATNAME"", new List<MaterialProperty> {
$1
});
";

    private const string propertyStub =
@"new MaterialProperty {
name = ""$PROPNAME"",
type = $PROPTYPE
}";

    private static string code = "";

    private static void ProcessScene(EditorBuildSettingsScene scene) {
        EditorApplication.OpenScene(scene.path);

        GameObject[] rootObjects = (from obj in GameObject.FindObjectsOfType<GameObject>()
                                    where !obj.transform.parent
                                    select obj).ToArray();

        foreach (GameObject go in rootObjects) {
            Material[] m2 = (from obj in go.GetComponentsInChildren<StoreMaterials>(true)
                             select obj.GetComponent<Renderer>().sharedMaterials).SelectMany(obj => obj).ToArray();
            foreach (Material mat in m2) {
                Shader currentShader = mat.shader;
                int propertyCount = ShaderUtil.GetPropertyCount(currentShader);

                List<ShaderProperty> properties = new List<ShaderProperty>();

                for (int i = 0; i < propertyCount; i++) {
                    properties.Add(new ShaderProperty {
                        name = ShaderUtil.GetPropertyName(currentShader, i),
                        type = ShaderUtil.GetPropertyType(currentShader, i)
                    });

                    //database[mat].Add(new ShaderProperty {
                    //    name = ShaderUtil.GetPropertyName(currentShader, i),
                    //    type = ShaderUtil.GetPropertyType(currentShader, i)
                    //});

                    //shaderKeywords.Add(new ShaderProperty {
                    //    name = ShaderUtil.GetPropertyName(currentShader, i),
                    //    type = ShaderUtil.GetPropertyType(currentShader, i)
                    //});
                }

                if (database.ContainsKey(mat.name)) {
                    database[mat.name] = properties.Union(database[mat.name]).ToList();
                }
                else {
                    database[mat.name] = properties;
                }
            }
        }
    }
}
