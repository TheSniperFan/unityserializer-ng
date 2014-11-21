using UnityEngine;
using System.Collections;

public class CreateTexture : MonoBehaviour {
	
	[SerializeThis]
	Texture2D texture;
	
	public Texture2D referenceTexture;
	
	
	// Use this for initialization
	void Start () {
		if(LevelSerializer.IsDeserializing)
			return;
		var m = new Material(Shader.Find("Transparent/Diffuse"));
		if(Random.value <0.8f)
		{
			texture = new Texture2D(2,2);
			var color1 = new Color(Random.value, Random.value, Random.value, 1f);
			var color2 = new Color(Random.value, Random.value, Random.value, 1f);
			texture.SetPixel(0,0,color1);
			texture.SetPixel(0,1,color2);
			texture.SetPixel(1,1,color1);
			texture.SetPixel(1,0,color2);
			texture.Apply();
		}
		else
			texture = referenceTexture;
		m.mainTexture = texture;
		renderer.material = m;
			
	
	}
}
