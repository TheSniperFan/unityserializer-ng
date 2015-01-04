using UnityEngine;
using System.Collections;

public class CentreAndPointMe : MonoBehaviour {
	
	public Transform ball1, ball2;
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
		Vector3 connectingLine = ball1.position - ball2.position;
		transform.position = ball2.position + (0.5f * connectingLine);
		Vector3 perpendicular = Vector3.up;
		transform.rotation = Quaternion.LookRotation(Quaternion.AngleAxis(90, perpendicular) * connectingLine, perpendicular);
		
		
	}
}
