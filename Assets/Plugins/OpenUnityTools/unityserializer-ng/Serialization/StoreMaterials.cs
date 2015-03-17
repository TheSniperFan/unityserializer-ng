// /* ------------------
//
//       (c) whydoidoit.com 2012
//           by Mike Talbot 
//     ------------------- */
// 
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Serialization;

[AddComponentMenu("Storage/Store Materials")]
[ExecuteInEditMode]
[DontStore]
public partial class StoreMaterials : MonoBehaviour {
    public static Index<string, List<MaterialProperty>> MaterialProperties = new Index<string, List<MaterialProperty>>();
    static Index<string, List<MaterialProperty>> cache = new Index<string, List<MaterialProperty>>();

    public static bool Dirty {
        get;
        set;
    }

    public static int MaterialCount {
        get {
            return MaterialProperties.Count;
        }
    }

    public static int PropertyCount {
        get {
            int count = 0;
            foreach (List<MaterialProperty> list in MaterialProperties.Values) {
                count += list.Count;
            }
            return count;
        }
    }

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

    public class StoredValue {
        public MaterialProperty property;
        public object value;
    }

    static StoreMaterials() {
        DelegateSupport.RegisterFunctionType<Texture2D, int>();
        DelegateSupport.RegisterFunctionType<StoreMaterials, List<MaterialProperty>>();
        DelegateSupport.RegisterFunctionType<MaterialProperty, ShaderUtil.ShaderPropertyType>();
        DelegateSupport.RegisterFunctionType<MaterialProperty, string>();
        DelegateSupport.RegisterFunctionType<StoredValue, MaterialProperty>();
        DelegateSupport.RegisterFunctionType<StoredValue, object>();
    }

    private void Awake() {
        OnEnable();
    }

    private void OnEnable() {
        cache.Clear();

        if (!StoreMaterials.Dirty) {
            Renderer renderer = GetComponent<Renderer>();
            foreach (Material mat in renderer.sharedMaterials) {
                if (!MaterialProperties.ContainsKey(mat.name)) {
                    Dirty = true;
                    break;
                }
            }
        }
    }

    private void OnDisable() {
        cache.Clear();
    }

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
                    o.value = m.GetColor(p.name);
                    break;
                case MaterialProperty.PropertyType.Float:
                    o.value = m.GetFloat(p.name);
                    break;
                case MaterialProperty.PropertyType.Range:
                    o.value = m.GetFloat(p.name);
                    break;
                case MaterialProperty.PropertyType.TexEnv:
                    o.value = m.GetTexture(p.name);
                    break;
                case MaterialProperty.PropertyType.Vector:
                    o.value = m.GetVector(p.name);
                    break;
                default:
                    Debug.LogError("Unsupported type: " + p.type.ToString());
                    break;
            }
        }
        return output;
    }

    public void SetValues(Material m, IEnumerable<StoredValue> values) {
        foreach (var v in values) {
            switch (v.property.type) {
                case MaterialProperty.PropertyType.Color:
                    m.SetColor(v.property.name, (Color)v.value);
                    break;
                case MaterialProperty.PropertyType.Float:
                    m.SetFloat(v.property.name, (float)v.value);
                    break;
                case MaterialProperty.PropertyType.Range:
                    m.SetFloat(v.property.name, (float)v.value);
                    break;
                case MaterialProperty.PropertyType.TexEnv:
                    m.SetTexture(v.property.name, (Texture)v.value);
                    break;
                case MaterialProperty.PropertyType.Vector:
                    m.SetVector(v.property.name, (Vector4)v.value);
                    break;
                default:
                    Debug.LogError("Unsupported type: " + v.property.type.ToString());
                    break;
            }
        }
    }

    public List<MaterialProperty> GetShaderProperties(Material material) {
        if (cache.ContainsKey(material.shader.name)) {
            return cache[material.shader.name];
        }

        var list = new List<MaterialProperty>();
        foreach (KeyValuePair<string, List<MaterialProperty>> material_list in MaterialProperties) {
            foreach (var m in material_list.Value) {
                if (material.HasProperty(m.name)) {
                    list.Add(m);
                }
            }
        }
        cache[material.shader.name] = list;
        return list;
    }
}