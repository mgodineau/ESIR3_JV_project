using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;


[RequireComponent(typeof(CharacterController))]
public class PlayerController_arm : MonoBehaviour {


	[SerializeField] private float walkingSpeed = 5.0f;
	[SerializeField] private float jumpSpeed = 1.0f;
	[SerializeField] private float gravityMultiplier = 1.0f;
	
	[SerializeField] private float mouseSensitivity = 1.0f;
	[SerializeField] private float camMaxAngle = 90;
	[SerializeField] private float camMinAngle = -90;
	
	
	[SerializeField] private Transform cam;
	private CharacterController _controller;


	[SerializeField] private Animator _anim;
	private static readonly int Jump = Animator.StringToHash("jump");
	private static readonly int Run = Animator.StringToHash("run");
	private static readonly int Stop = Animator.StringToHash("stop");

	private Vector2 _moveInput;
	private Vector2 _rotationInput;

	private float _camAngle = 0;
	private Vector3 _currentVelocity;
	public Vector3 CurrentVelocity => _controller.velocity;

	public AudioSource AudioSource;

	public AudioClip footstep;
	public AudioClip jump;



	private void Awake() {
		if (cam == null) {
			Debug.LogError("attribute \"cam\" of PlayerController is not set");
		}
		
		_controller = GetComponent<CharacterController>();

		_moveInput = Vector2.zero;
		_rotationInput = Vector2.zero;
		
		_currentVelocity = Vector3.zero;
	}


	private void OnEnable() {
		Cursor.lockState = CursorLockMode.Locked;
	}
	
	private void Update() {
		
		// translate body
		Vector2 moveSpeed = _moveInput * walkingSpeed;
		_currentVelocity = transform.forward * moveSpeed.y + transform.right * moveSpeed.x + Vector3.up * _currentVelocity.y;
		if (!_controller.isGrounded) {
			_currentVelocity.y += Physics.gravity.y * Time.deltaTime * gravityMultiplier;
		} else if( _currentVelocity.y <= 0 ) {
			_currentVelocity.y = Physics.gravity.y;
		}

		_controller.Move( _currentVelocity * Time.deltaTime );
		
		
		//rotate body yaw
		transform.Rotate(Vector3.up, _rotationInput.x * mouseSensitivity);
		
		// rotate camera pitch
		_camAngle = Mathf.Clamp(_camAngle - _rotationInput.y * mouseSensitivity, camMinAngle, camMaxAngle);
		cam.localRotation = Quaternion.Euler(_camAngle, 0, 0);

		//Audio manager

		if (_controller.isGrounded && moveSpeed.sqrMagnitude > walkingSpeed - 1)
		{
			PlayFootStep(footstep);
		}

		if(moveSpeed.sqrMagnitude > walkingSpeed - 1)
        {
			_anim.SetTrigger(Run);
		}
		else
        {
			_anim.SetTrigger(Stop);
		}






	}
	
	
	
	
	private void OnMove(InputValue movementValue) {
		_moveInput = movementValue.Get<Vector2>();
	}
	
	private void OnRotate(InputValue rotationValue) {
		_rotationInput = rotationValue.Get<Vector2>();
	}

	private void OnJump() {
		if ( _controller.isGrounded ) {
			_currentVelocity.y = jumpSpeed * gravityMultiplier;
		}
		_anim.SetTrigger(Jump);
		AudioSource.volume = 0.5f;
		AudioSource.PlayOneShot(jump);
	}

	void PlayFootStep(AudioClip audio)
    {
		if (!AudioSource.isPlaying)
        {
			AudioSource.pitch = Random.Range(0.8f, 1.2f);
			AudioSource.volume = Random.Range(0.15f, 0.30f);
			AudioSource.PlayOneShot(audio);
        }

    }
	
}
