using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(AudioSource))]
public class GunBehaviour : MonoBehaviour {


	[SerializeField] private AudioClip shotSound;
	
	[FormerlySerializedAs("particles")] [SerializeField] private ParticleSystem bulletParticle;
	private AudioSource _audioSource;


	private void Awake() {
		_audioSource = GetComponent<AudioSource>();
	}
	
	
	public void ShootAt( Vector3 target ) {

		Vector3 bulletDir = (target - bulletParticle.transform.position).normalized;
		bulletParticle.transform.rotation = Quaternion.FromToRotation(Vector3.forward, bulletDir);
		
		bulletParticle.Emit(1);
		_audioSource.PlayOneShot(shotSound);
		
	}
	
	
}
