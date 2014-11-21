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

[AddComponentMenu("Storage/Store Materials")]
[ExecuteInEditMode]
[DontStore]
public class StoreMaterials : MonoBehaviour
{
	
	static StoreMaterials()
	{
		DelegateSupport.RegisterFunctionType<Texture2D, int>();
		DelegateSupport.RegisterFunctionType<StoreMaterials, List<MaterialProperty>>();
		DelegateSupport.RegisterFunctionType<MaterialProperty, MaterialProperty.PropertyType>();
		DelegateSupport.RegisterFunctionType<MaterialProperty, string>();
		DelegateSupport.RegisterFunctionType<StoredValue, MaterialProperty>();
		DelegateSupport.RegisterFunctionType<StoredValue, object>();
	}
	
	public List<MaterialProperty> MaterialProperties = new List<MaterialProperty>();
	
	[Serializable]
	public class MaterialProperty
	{
		public enum PropertyType
		{
			unknown = -1,
			color = 0,
			vector = 1,
			texture = 2,
			textureOffset = 3,
			textureScale = 4,
			matrix = 5,
			real = 6
		}
		public string name;
		public PropertyType type; 
	}
	
	public class StoredValue
	{
		public MaterialProperty Property;
		public object Value;
	}
	
	
	public List<StoredValue> GetValues(Material m)
	{
		var list = GetShaderProperties(m);
		var output = new List<StoredValue>();
		foreach(var p in list)
		{
			var o = new StoredValue { Property = p };
			output.Add(o);
			switch(p.type)
			{
			case MaterialProperty.PropertyType.color:
				o.Value = m.GetColor(p.name);
				break;
			case MaterialProperty.PropertyType.real:
				o.Value = m.GetFloat(p.name);
				break;
			case MaterialProperty.PropertyType.texture:
				o.Value = m.GetTexture(p.name);
				break;
			case MaterialProperty.PropertyType.vector:
				o.Value = m.GetVector(p.name);
				break;
			case MaterialProperty.PropertyType.textureOffset:
				o.Value = m.GetTextureOffset(p.name);
				break;
			case MaterialProperty.PropertyType.textureScale:
				o.Value = m.GetTextureScale(p.name);
				break;
			case MaterialProperty.PropertyType.matrix:
				o.Value = m.GetMatrix(p.name);
				break;
			}
		}
		return output;
	}
	
	public void SetValues(Material m, IEnumerable<StoredValue> values)
	{
		foreach(var v in values)
		{
			switch(v.Property.type)
			{
			case MaterialProperty.PropertyType.color:
				m.SetColor(v.Property.name, (Color)v.Value);
				break;
			case MaterialProperty.PropertyType.real:
				m.SetFloat(v.Property.name, (float)v.Value);
				break;
			case MaterialProperty.PropertyType.texture:
				m.SetTexture(v.Property.name, (Texture)v.Value);
				break;
			case MaterialProperty.PropertyType.vector:
				m.SetVector(v.Property.name, (Vector4)v.Value);
				break;
			case MaterialProperty.PropertyType.textureOffset:
				m.SetTextureOffset(v.Property.name, (Vector2)v.Value);
				break;
			case MaterialProperty.PropertyType.textureScale:
				m.SetTextureScale(v.Property.name, (Vector2)v.Value);
				break;
			case MaterialProperty.PropertyType.matrix:
				m.SetMatrix(v.Property.name, (Matrix4x4)v.Value);
				break;
			}
		}
	}
	
	
	static Index<string, List<MaterialProperty>> cache = new Index<string, List<MaterialProperty>>();
	Color transparent = new Color(0,0,0,0);
	public List<MaterialProperty> GetShaderProperties(Material material)
	{
		if(cache.ContainsKey(material.shader.name))
			return cache[material.shader.name];
		
		var list = new List<MaterialProperty>();
		foreach(var m in MaterialProperties)
		{
			if(material.HasProperty(m.name))
			{
				if(m.type == MaterialProperty.PropertyType.unknown)
				{
					try
					{
						var p = material.GetColor(m.name);
						if(p != transparent)
						    list.Add( new MaterialProperty { name = m.name, type = MaterialProperty.PropertyType.color });
					}
					catch
					{
					}
					try
					{
						var p = material.GetFloat(m.name);
						if(p != 0)
						    list.Add( new MaterialProperty { name = m.name, type = MaterialProperty.PropertyType.real });
					}
					catch
					{
					}
					try
					{
						var p = material.GetTexture(m.name);
						if(p!=null)
						    list.Add( new MaterialProperty { name = m.name, type = MaterialProperty.PropertyType.texture });
					}
					catch
					{
					}
					try
					{
						var p = material.GetVector(m.name);
						if(p != Vector4.zero)
						    list.Add( new MaterialProperty { name = m.name, type = MaterialProperty.PropertyType.vector });
					}
					catch
					{
						
					}
					try
					{
						var p = material.GetMatrix(m.name);
						if(p != Matrix4x4.identity)
						     list.Add( new MaterialProperty { name = m.name, type = MaterialProperty.PropertyType.matrix });
					}
					catch
					{
					}
					try
					{
						var p = material.GetTextureOffset(m.name);
						if(p != Vector2.zero)
						     list.Add( new MaterialProperty { name = m.name, type = MaterialProperty.PropertyType.textureOffset });
					}
					catch
					{
					}
					try
					{
						var p = material.GetTextureScale(m.name);
						if(p != Vector2.zero)
						     list.Add( new MaterialProperty { name = m.name, type = MaterialProperty.PropertyType.textureScale });
					}
					catch
					{
					}
				}
				else
				{
					list.Add(m);
				}

					
			}
			
		}
		cache[material.shader.name] = list;
		return list;
	}
	
	public StoreMaterials()
	{
		if(MaterialProperties.Count==0)
		{
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.color,  name = "_Color"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.texture,  name = "_MainTex"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.texture,  name = "_Cube"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.real,  name = "_SelfIllumStrength"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.texture,  name = "_ReflectionTex"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.texture,  name = "_Normals"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.texture,  name = "_Normal"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.unknown,  name = "_SelfIllumination"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.texture,  name = "_NoiseTex"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.color,  name = "_TintColor"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.unknown,  name = "_Illum"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.unknown,  name = "_EmissionLM"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.unknown,  name = "_InterlacePattern"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.unknown,  name = "_Intensity"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.unknown,  name = "_Distort"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.unknown,  name = "_ShimmerDistort"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.color,  name = "_FogColor"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.color,  name = "_SpecColor"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.real,  name = "_Shininess"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.color,  name = "_ReflectColor"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.texture,  name = "_BumpMap"});
			MaterialProperties.Add(new MaterialProperty { type = MaterialProperty.PropertyType.unknown,  name = "_Parallax"});
			
		}
	}
	void Awake()
	{
		OnEnable();
	}
	void OnEnable()
	{
		if(renderer != null)
		{
			MaterialProperties =  renderer.sharedMaterials.Where(m=>m).SelectMany(m=>GetShaderProperties(m)).Discrete(m=>m.name).ToList();
		}

	}
	
	
	
}


