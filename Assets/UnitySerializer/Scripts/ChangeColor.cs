using UnityEngine;
using System.Collections;

public class ChangeColor : MonoBehaviour {
	
	[SerializeThis]
	Color color;
	
	// Use this for initialization
	void Start () {
		color.r = Random.value;
		color.g = Random.value;
		color.b = Random.value;
		color.a = 1;
		
	}
	
	// Update is called once per frame
	void Update () {	
	    
		 color.r += Random.value/100;
		 color.g += Random.value/100;
		 color.b += Random.value/100;
		 renderer.material.color = color;
	}
}
