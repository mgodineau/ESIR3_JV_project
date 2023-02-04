using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviour
{

[Header("Movement")]
public float moveSpeed;   
public float groundDrag;

public float jumpForce;
public float jumpCooldown;
public float airMultiplier;
public float gravity;
bool readyToJump;

[Header("KeyBindinds")]
public KeyCode jumpKey = KeyCode.Space;

[Header("Ground Check")]
public float playerHeight;
public LayerMask whatIsGround;
public bool grounded;

public Transform orientation;

float horizontalInput;
float verticalInput;

Vector3 moveDirection;

Rigidbody rb; 

private void Start()
{
	rb = this.GetComponent<Rigidbody>();
	rb.freezeRotation = true;

	readyToJump = true;

}

private void Update()
{
	grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
	MyInput();
	SpeedControl();

	//handle drag
	if(grounded)
		rb.drag = groundDrag;
	else
		rb.drag = 0;
}

private void FixedUpdate()
{
	MovePlayer();
}

private void MyInput()
{
	horizontalInput = Input.GetAxisRaw("Horizontal");
	verticalInput = Input.GetAxisRaw("Vertical");   

	//when to jump
	if(Input.GetKey(jumpKey) && readyToJump && grounded)
	{
		readyToJump = false;
			Jump();
			Invoke(nameof(ResetJump), jumpCooldown);
	}

	// if (horizontalInput == -1 || horizontalInput == 1)
	//     Debug.Log("horizontal " + horizontalInput);
	// if (verticalInput == -1 || verticalInput == 1)
	//     Debug.Log("vertical " + verticalInput);
}

private void MovePlayer()
{
	//calculate movement direction
	moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
	//this.transform.rotation = Quaternion.LookRotation(moveDirection);

	//on ground
	if(grounded)
		rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.VelocityChange); 
	
	//in air
	else if (!grounded)
			rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.VelocityChange); 
			rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration); 
}

private void SpeedControl()
{
	Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

	// limit velocity if needed
	if(flatVel.magnitude > moveSpeed)
	{
		Vector3 limitedVel = flatVel.normalized * moveSpeed;
		rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.y);
	}
}

private void Jump()
{
	//reset y velocity
	rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

	rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);       
}

private void ResetJump()
{
	readyToJump = true;
}
}
