// /* ------------------
//
//       (c) whydoidoit.com 2012
//           by Mike Talbot 
//     ------------------- */
// 
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Serialization;


/// <summary>
/// Used to store material and shader information
/// </summary>
[AddComponentMenu("Storage/Store Materials")]
[ExecuteInEditMode]
[DontStore]
public partial class StoreMaterials : MonoBehaviour {
    /// <summary>
    /// Contains all shaders and properties that are used with all instances of this script in the entire project.
    /// The initialization happens in the constructor which is created using code generation.
    /// </summary>
    public static Index<string, List<MaterialProperty>> ShaderDatabase = new Index<string, List<MaterialProperty>>();

    /// <summary>
    /// Caches shaders we already searched for
    /// </summary>
    static Index<string, List<MaterialProperty>> cache = new Index<string, List<MaterialProperty>>();

    /// <summary>
    /// Stores whether all shaders are in the shader database
    /// </summary>
    public static bool Dirty {
        get;
        set;
    }

    /// <summary>
    /// The amount of shaders in the database
    /// </summary>
    public static int ShaderCount {
        get {
            return ShaderDatabase.Count;
        }
    }

    /// <summary>
    /// The amount of properties of all shaders in the database
    /// </summary>
    public static int PropertyCount {
        get {
            int count = 0;
            foreach (List<MaterialProperty> list in ShaderDatabase.Values) {
                count += list.Count;
            }
            return count;
        }
    }

    /// <summary>
    /// Contains a copy of the ShaderPropertyType enum from the ShaderUtil class, because it's not available in player builds
    /// </summary>
    [Serializable]
    public class MaterialProperty {
        [Serializable]
        public enum PropertyType {
            Color = 0,
            Vector = 1,
            Float = 2,
            Range = 3,
            TexEnv = 4,
        }

        public string name;
        public string description;
        public PropertyType type;
    }

    /// <summary>
    /// Container for the stored information
    /// </summary>
    public class StoredValue {
        public MaterialProperty property;
        public object[] value;
    }

    static StoreMaterials() {
        DelegateSupport.RegisterFunctionType<Texture2D, int>();
        DelegateSupport.RegisterFunctionType<StoreMaterials, List<MaterialProperty>>();
        DelegateSupport.RegisterFunctionType<MaterialProperty, MaterialProperty.PropertyType>();
        DelegateSupport.RegisterFunctionType<MaterialProperty, string>();
        DelegateSupport.RegisterFunctionType<StoredValue, MaterialProperty>();
        DelegateSupport.RegisterFunctionType<StoredValue, object>();
    }

    private void Awake() {
        OnEnable();
    }

    private void OnEnable() {
        cache.Clear();

#if UNITY_EDITOR
        if (!StoreMaterials.Dirty) {
            Renderer renderer = GetComponent<Renderer>();
            foreach (Material mat in renderer.sharedMaterials) {
                if (!ShaderDatabase.ContainsKey(mat.shader.name)) {
                    Dirty = true;
                    break;
                }
            }
        }
#endif
    }

    private void OnDisable() {
        cache.Clear();
    }

    /// <summary>
    /// Gets the values given a material
    /// </summary>
    /// <param name="m">The material</param>
    /// <returns>A StoredValue containing value and type information</returns>
    public List<StoredValue> GetValues(Material m) {
        var list = GetShaderProperties(m);
        var output = new List<StoredValue>();
        foreach (var p in list) {
            var o = new StoredValue {
                property = p
            };
            output.Add(o);
            switch (p.type) {
                case MaterialProperty.PropertyType.Color:
                    o.value = new object[1];
                    o.value[0] = m.GetColor(p.name);
                    break;
                case MaterialProperty.PropertyType.Float:
                    o.value = new object[1];
                    o.value[0] = m.GetFloat(p.name);
                    break;
                case MaterialProperty.PropertyType.Range:
                    o.value = new object[1];
                    o.value[0] = m.GetFloat(p.name);
                    break;
                case MaterialProperty.PropertyType.TexEnv:
                    o.value = new object[3];
                    o.value[0] = m.GetTexture(p.name);
                    o.value[1] = m.GetTextureOffset(p.name);
                    o.value[2] = m.GetTextureScale(p.name);
                    break;
                case MaterialProperty.PropertyType.Vector:
                    o.value = new object[1];
                    o.value[0] = m.GetVector(p.name);
                    break;
                default:
                    Debug.LogError("Unsupported type: " + p.type.ToString());
                    break;
            }
        }
        return output;
    }

    /// <summary>
    /// Restores the material values
    /// </summary>
    /// <param name="m">Material</param>
    /// <param name="values">Set of values</param>
    public void SetValues(Material m, IEnumerable<StoredValue> values) {
        foreach (var v in values) {
            switch (v.property.type) {
                case MaterialProperty.PropertyType.Color:
                    m.SetColor(v.property.name, (Color)v.value[0]);
                    break;
                case MaterialProperty.PropertyType.Float:
                    m.SetFloat(v.property.name, (float)v.value[0]);
                    break;
                case MaterialProperty.PropertyType.Range:
                    m.SetFloat(v.property.name, (float)v.value[0]);
                    break;
                case MaterialProperty.PropertyType.TexEnv:
                    m.SetTexture(v.property.name, (Texture)v.value[0]);
                    m.SetTextureOffset(v.property.name, (Vector2)v.value[1]);
                    m.SetTextureScale(v.property.name, (Vector2)v.value[2]);
                    break;
                case MaterialProperty.PropertyType.Vector:
                    m.SetVector(v.property.name, (Vector4)v.value[0]);
                    break;
                default:
                    Debug.LogError("Unsupported type: " + v.property.type.ToString());
                    break;
            }
        }
    }

    /// <summary>
    /// Finds the shader's properties in the shader database and caches them
    /// </summary>
    /// <param name="material">Material</param>
    /// <returns>List of properties</returns>
    public List<MaterialProperty> GetShaderProperties(Material material) {
        if (cache.ContainsKey(material.shader.name)) {
            return cache[material.shader.name];
        }

        var list = new List<MaterialProperty>();
        List<MaterialProperty> material_list = ShaderDatabase[material.shader.name];
        foreach (MaterialProperty prop in material_list) {
            if (material.HasProperty(prop.name)) {
                list.Add(prop);
            }
        }
        cache[material.shader.name] = list;
        return list;
    }
}