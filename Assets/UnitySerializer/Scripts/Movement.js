#pragma strict


private var controller :CharacterController;
controller = gameObject.GetComponent(CharacterController);

public var texture : Texture2D;
 
private var moveDirection = Vector3.zero;
private var forward = Vector3.zero;
private var right = Vector3.zero;

function Start () {
	texture = null;
	
}

function Update () {
    forward = transform.forward;
	right = Vector3(forward.z, 0, -forward.x);
 
	var horizontalInput = Input.GetAxisRaw("Horizontal");
	var verticalInput = Input.GetAxisRaw("Vertical");
	var targetDirection = horizontalInput * right + verticalInput * forward;	
 
	 moveDirection = Vector3.RotateTowards(moveDirection, targetDirection, 200 * Mathf.Deg2Rad * Time.deltaTime, 1000);
   
	var movement = moveDirection  * Time.deltaTime * 10;
	controller.Move(movement);
}