using UnityEngine;

public class Movement : MonoBehaviour {
    private CharacterController controller;

    private Vector3 moveDirection = Vector3.zero;
    private Vector3 forward = Vector3.zero;
    private Vector3 right = Vector3.zero;

    private void Start() {
        controller = GetComponent<CharacterController>();
    }

    private void Update() {
        forward = GetComponent<Transform>().forward;
        right = new Vector3(forward.z, 0.0f, -forward.x);

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector3 targetDirection = horizontalInput * right + verticalInput * forward;

        moveDirection = Vector3.RotateTowards(moveDirection, targetDirection, 200.0f * Mathf.Deg2Rad * Time.deltaTime, 1000.0f);

        Vector3 movement = moveDirection * Time.deltaTime * 10.0f;
        controller.Move(movement);
    }
}
