using UnityEngine;
using System.Collections;

public class OpenDoor : MonoBehaviour {

	void Update()
	{
		if(Input.GetKeyDown(KeyCode.B))
		{
			rigidbody.AddForceAtPosition(Vector3.forward * 10, transform.position + new Vector3(0.3f,0,0), ForceMode.Impulse);
		}
	}
}
