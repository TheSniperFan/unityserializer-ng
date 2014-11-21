using UnityEngine;
using System.Collections;

public class AddSomeForce : MonoBehaviour {
	
	bool done;
	
	public Transform aChild;
	
	void Awake()
	{
		aChild = GetComponentsInChildren<Transform>()[1];
	}
	
	// Use this for initialization
	void FixedUpdate () {
		if(done) 
			return;
		done = true;
		rigidbody.angularVelocity = Random.insideUnitSphere * 10;
		
	}
	

}
