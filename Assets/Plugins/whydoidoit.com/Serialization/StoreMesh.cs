using UnityEngine;
using System.Collections;

public class StoreMesh : MonoBehaviour {
	
	[HideInInspector]
	public Vector3[] vertices;
	[HideInInspector]
	public Vector3[] normals;
	[HideInInspector]
	public Vector2[] uv;
	[HideInInspector]
	public Vector2[] uv1;
	[HideInInspector]
	public Vector2[] uv2;
	[HideInInspector]
	public Color[] colors;
	[HideInInspector]
	public int[][] triangles;
	[HideInInspector]
	public Vector4[] tangents;
	[HideInInspector]
	public int subMeshCount;
	
	MeshFilter filter;
	SkinnedMeshRenderer skinnedMeshRenderer;
	
	void Awake()
	{
		filter = GetComponent<MeshFilter>();
		skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
		if(filter==null && skinnedMeshRenderer == null)
			Destroy(this);
	}
	
	void OnSerializing()
	{
		var mesh = filter != null ? filter.mesh : skinnedMeshRenderer.sharedMesh;
		vertices = mesh.vertices;
		normals = mesh.normals;
		uv = mesh.uv;
		uv1 = mesh.uv1;
		uv2 = mesh.uv2;
		colors = mesh.colors;
		triangles = new int[subMeshCount = mesh.subMeshCount][];
		for(var i = 0; i < mesh.subMeshCount; i++)
		{
			triangles[i] = mesh.GetTriangles(i);
		}
		tangents = mesh.tangents;
	}
	
	void OnDeserialized()
	{
		var mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv  = uv;
		mesh.uv1 = uv1;
		mesh.uv2 = uv2;
		mesh.colors = colors;
		mesh.tangents = tangents;
		mesh.subMeshCount = subMeshCount;
		for(var i = 0; i < subMeshCount; i++)
		{
			mesh.SetTriangles(triangles[i], i);
		}
		mesh.RecalculateBounds();
		if(filter != null)
			filter.mesh = mesh;
		else
			skinnedMeshRenderer.sharedMesh = mesh;
	}
	
	
}
