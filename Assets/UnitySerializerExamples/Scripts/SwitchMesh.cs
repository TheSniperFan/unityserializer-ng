using UnityEngine;
using System.Collections;

public class SwitchMesh : MonoBehaviour {
    [SerializeField]
	private Mesh mesh;
	
	private void OnMouseDown()  {
		GetComponent<MeshFilter>().mesh = mesh;
	}
}
