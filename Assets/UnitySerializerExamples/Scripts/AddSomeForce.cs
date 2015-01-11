using UnityEngine;
using System.Collections;

public class AddSomeForce : MonoBehaviour {
    private bool done;

    [SerializeField]
    private Transform aChild;

    private void Awake() {
        aChild = GetComponentsInChildren<Transform>()[0];
    }

    private void FixedUpdate() {
        if (done)
            return;
        done = true;
        GetComponent<Rigidbody>().angularVelocity = Random.insideUnitSphere * 10.0f;

    }


}
