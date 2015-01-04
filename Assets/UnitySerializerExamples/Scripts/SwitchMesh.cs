using UnityEngine;
using System.Collections;

public class SwitchMesh : MonoBehaviour {
	
	
	public Mesh mesh;
	
	void OnMouseDown()
	{
		GetComponent<MeshFilter>().mesh = mesh;
	}
}
