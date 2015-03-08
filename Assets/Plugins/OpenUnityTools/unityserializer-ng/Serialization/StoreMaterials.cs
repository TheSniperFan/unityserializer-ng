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

#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("Storage/Store Materials")]
[ExecuteInEditMode]
[DontStore]
public class StoreMaterials : MonoBehaviour {
    public Index<string, List<MaterialProperty>> MaterialProperties = new Index<string, List<MaterialProperty>>();
    static Index<string, List<MaterialProperty>> cache = new Index<string, List<MaterialProperty>>();

    public enum ShaderPropertyType {
        Color = 0,
        Vector = 1,
        Float = 2,
        Range = 3,
        TexEnv = 4,
    }

    [Serializable]
    public class MaterialProperty {
        public string name;
        public string description;
        public ShaderPropertyType type;
    }

    public class StoredValue {
        public MaterialProperty property;
        public object value;
    }


    static StoreMaterials() {
        DelegateSupport.RegisterFunctionType<Texture2D, int>();
        DelegateSupport.RegisterFunctionType<StoreMaterials, List<MaterialProperty>>();
        DelegateSupport.RegisterFunctionType<MaterialProperty, ShaderPropertyType>();
        DelegateSupport.RegisterFunctionType<MaterialProperty, string>();
        DelegateSupport.RegisterFunctionType<StoredValue, MaterialProperty>();
        DelegateSupport.RegisterFunctionType<StoredValue, object>();
    }

    private void Awake() {
        OnEnable();
    }

    private void OnEnable() {
        MaterialProperties.Clear();
        cache.Clear();
        foreach (Material mat in GetComponent<Renderer>().sharedMaterials) {
            if (!MaterialProperties.ContainsKey(mat.name))
                MaterialProperties.Add(mat.name, GetShaderProperties(mat));
        }
    }

    private void OnDisable() {
        cache.Clear();
    }

    internal void ForceRefresh() {
        OnEnable();
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
                case ShaderPropertyType.Color:
                    o.value = m.GetColor(p.name);
                    break;
                case ShaderPropertyType.Float:
                    o.value = m.GetFloat(p.name);
                    break;
                case ShaderPropertyType.Range:
                    o.value = m.GetFloat(p.name);
                    break;
                case ShaderPropertyType.TexEnv:
                    o.value = m.GetTexture(p.name);
                    break;
                case ShaderPropertyType.Vector:
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
                case ShaderPropertyType.Color:
                    m.SetColor(v.property.name, (Color)v.value);
                    break;
                case ShaderPropertyType.Float:
                    m.SetFloat(v.property.name, (float)v.value);
                    break;
                case ShaderPropertyType.Range:
                    m.SetFloat(v.property.name, (float)v.value);
                    break;
                case ShaderPropertyType.TexEnv:
                    m.SetTexture(v.property.name, (Texture)v.value);
                    break;
                case ShaderPropertyType.Vector:
                    m.SetVector(v.property.name, (Vector4)v.value);
                    break;
                default:
                    Debug.LogError("Unsupported type: " + v.property.type.ToString());
                    break;
            }
        }
    }

    public List<MaterialProperty> GetShaderProperties(Material material) {
        if (cache.ContainsKey(material.shader.name))
            return cache[material.shader.name];

        var list = new List<MaterialProperty>();
        Shader s = material.shader;
#if UNITY_EDITOR
        int count = ShaderUtil.GetPropertyCount(s);

        for (int i = 0; i < count; i++) {
            list.Add(new MaterialProperty() {
                type = (ShaderPropertyType)ShaderUtil.GetPropertyType(s, i),
                description = ShaderUtil.GetPropertyDescription(s, i),
                name = ShaderUtil.GetPropertyName(s, i)
            });
        }
#endif

        cache[material.shader.name] = list;
        return list;
    }
}