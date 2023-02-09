using UnityEngine;

namespace Weapons {
public class WeaponsHandlerMovements : MonoBehaviour {


	[SerializeField] private Vector3 maxOffset = Vector3.one * 0.1f;
	[SerializeField] private Vector3 maxSpeed = Vector3.one * 10;
	[SerializeField] private float lerpSpeed = 1;
	
	private PlayerController _player;
	
	private Vector3 _centerPosition;
	private Vector3 _currentOffset = Vector3.zero;
	
	private void Awake() {
		_centerPosition = transform.localPosition;
		_player = GetComponentInParent<PlayerController>();
	}
	
	private void Update() {
		
/*		
		Vector3 currentSpeed = _player.CurrentVelocity;

		Vector3 desiredOffset = transform.InverseTransformVector( -currentSpeed);
		for (int i=0; i<3; i++) {
			desiredOffset[i] = Mathf.Clamp(desiredOffset[i], -maxSpeed[i], maxSpeed[i]) / maxSpeed[i] * maxOffset[i];
		}


		_currentOffset = Vector3.Lerp( _currentOffset, desiredOffset, Time.deltaTime * lerpSpeed);
		
		transform.localPosition = _centerPosition + _currentOffset; z*/
	}
	
	
}
}
