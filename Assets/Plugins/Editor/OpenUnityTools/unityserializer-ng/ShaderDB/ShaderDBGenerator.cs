using UnityEngine;
using UnityEditor;

using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;


/// <summary>
/// Generates source code to hard-wire all required shaders and their properties for the StoreMaterials class.
/// </summary>
public static class ShaderDBGenerator {
    #region Stubs
    private const string classStub =
@"using System;
using System.Collections.Generic;

///THIS CODE WAS AUTOMATICALLY GENERATED!
public partial class StoreMaterials {
public StoreMaterials() {

if (ShaderDatabase.Count == 0) {
$0
}
}
}
";

    private const string shaderStub =
@"ShaderDatabase.Add(""$SHADNAME"", new List<MaterialProperty> {
$1
});
";

    private const string propertyStub =
@"new MaterialProperty {
name = ""$PROPNAME"",
type = $PROPTYPE
}";
    #endregion


    /// <summary>
    /// Contains the name of the shaderproperty and its type
    /// </summary>
    public struct ShaderProperty {
        public string name;
        public ShaderUtil.ShaderPropertyType type;
    }

    // Temporarily holds a list of all required shaders and their respecive properties
    private static Dictionary<string, List<ShaderProperty>> tmp_database = new Dictionary<string, List<ShaderProperty>>();

    // Current scene, so we can switch back to it after iterating over all others.
    private static string currentScene;


    /// <summary>
    /// Generates the code for the shader database
    /// </summary>
    public static void GenerateShaderDB() {
        currentScene = EditorApplication.currentScene;

        EditorBuildSettingsScene[] scenes = (from s in EditorBuildSettings.scenes
                                             where s.enabled
                                             select s).ToArray();
        foreach (var scene in scenes) {
            ProcessScene(scene);
        }

        EditorApplication.OpenScene(currentScene);
        currentScene = null;

        string code = string.Empty;

        code = GenerateClass(code);
        code = GenerateShaders(code);

        foreach (KeyValuePair<string, List<ShaderProperty>> item in tmp_database) {
            code = GenerateProperties(code, item.Key);
        }

        WriteDatabase(code);
    }

    /// <summary>
    /// Scans a scene for instances of the StoreMaterials script and adds the shaders and their properties to the
    /// temporary database.
    /// It finds all root objects and uses GetComponentsInChildren to work around the problem that inactive objects
    /// wouldn't be found otherwise. It still doesn't find inactive root objects.
    /// </summary>
    /// <param name="scene">The scene that should be processed</param>
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
                }

                if (tmp_database.ContainsKey(currentShader.name)) {
                    tmp_database[currentShader.name] = properties.Union(tmp_database[currentShader.name]).ToList();
                }
                else {
                    tmp_database[currentShader.name] = properties;
                }
            }
        }
    }

    /// <summary>
    /// Generates the class file and adds the required amount of placeholders for the shaders
    /// </summary>
    /// <param name="code">Input source code</param>
    /// <returns>Output source code</returns>
    private static string GenerateClass(string code) {
        code = classStub;
        int count = tmp_database.Count;
        for (int i = 0; i < count; i++) {
            code = Regex.Replace(code, @"\$0", "$SHADER\r\n$0");
        }

        return Regex.Replace(code, @"\$0", "");
    }

    /// <summary>
    /// Replaces every shader placeholder with a stub for shaders
    /// </summary>
    /// <param name="code">Input source code</param>
    /// <returns>Output source code</returns>
    private static string GenerateShaders(string code) {
        return Regex.Replace(code, @"\$SHADER", shaderStub);
    }

    /// <summary>
    /// Fills the shader stubs with the actual information (shader name, properties, types)
    /// </summary>
    /// <param name="code">Input source code</param>
    /// <param name="shaderName">Name of the shader</param>
    /// <returns>Output source code</returns>
    private static string GenerateProperties(string code, string shaderName) {
        Regex regex = new Regex(Regex.Escape("$SHADER"));
        code = regex.Replace(code, shaderStub, 1);

        regex = new Regex(Regex.Escape("$SHADNAME"));
        code = regex.Replace(code, shaderName, 1);

        string s = "";
        for (int i = 0; i < tmp_database[shaderName].Count; i++) {
            s += "$PROPERTY,\r\n";
        }
        s = s.Substring(0, s.Length - 3);
        regex = new Regex(Regex.Escape("$1"));
        code = regex.Replace(code, s, 1);

        regex = new Regex(Regex.Escape("$PROPERTY"));

        foreach (ShaderProperty prop in tmp_database[shaderName]) {
            code = regex.Replace(code, propertyStub, 1);
            code = Regex.Replace(code, @"\$PROPNAME", prop.name);
            code = Regex.Replace(code, @"\$PROPTYPE", "MaterialProperty.PropertyType." + prop.type.ToString());
        }

        return code;
    }

    /// <summary>
    /// Writes the generated C# code file.
    /// </summary>
    /// <param name="code">Input source code</param>
    private static void WriteDatabase(string code) {
        string path = Path.GetFullPath(Application.dataPath) + "\\Plugins\\OpenUnityTools\\unityserializer-ng\\Serialization\\GENERATED";
        try {
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            File.WriteAllText(path + "\\GEN_ShaderDB.cs", code);
        }
        catch (IOException e) {
            Debug.LogErrorFormat("IO Exception while writing shader database.\n{0}", e);
        }
        catch (System.Exception e) {
            Debug.LogErrorFormat("Unexpected error while writing shader database.\n{0}", e);
        }
    }
}
