using UnityEngine;

public class AddSomeForce : MonoBehaviour {
    private bool done;

    [SerializeField]
    private Transform aChild;

    private void Awake() {
        aChild = GetComponentsInChildren<Transform>()[Random.Range(0, transform.childCount)];
    }

    private void FixedUpdate() {
        if (done) {
            return;
        }
        done = true;
        GetComponent<Rigidbody>().angularVelocity = Random.insideUnitSphere * 10.0f;
        aChild.GetComponent<Rigidbody>().angularVelocity = Random.insideUnitSphere * 10.0f;
    }
}
