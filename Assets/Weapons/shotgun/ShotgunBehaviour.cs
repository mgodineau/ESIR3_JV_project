using System;
using System.Collections;
using UI;
using Unity.VisualScripting;
using UnityEngine;

using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Weapons {

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class ShotgunBehaviour : WeaponBehaviour {


	
	private static readonly int Shoot = Animator.StringToHash("shoot");
	private static readonly int Reload = Animator.StringToHash("reload");
	

	[SerializeField] private int magCapacity = 10;

	[SerializeField] private int ammoInReserve = 50;
	private int _ammoInMag;
	
	
	[SerializeField] private ParticleSystem bulletParticles;
	[SerializeField] private int bulletParticlesCount = 10;
	[SerializeField] private int raycastCount = 20;

	
	[SerializeField] private float spreadAngle = 10;
	[SerializeField] private float maxRange = 50;


	[SerializeField] private float bagAnimSpeed = 1.0f;
	
	
	private float _ammoInMagRatio = 0.0f;
	private float _currentAmmoInMagRatio = 0.0f;

	private AudioSource _audioSource;
	[SerializeField] private AudioClip shoot;
	[SerializeField] private AudioClip reload;
	

	private new void Awake() {
		base.Awake();
		
		_audioSource = GetComponent<AudioSource>();
		_ammoInMag = Mathf.Min(magCapacity, ammoInReserve);
		_ammoInMagRatio = (float)_ammoInMag / magCapacity;
	}

	private new void OnEnable() {
		base.OnEnable();
		UIManager.UpdateAmmoInMag(_ammoInMag);
	}


	private void Update() {

		_currentAmmoInMagRatio = Mathf.MoveTowards(_currentAmmoInMagRatio, _ammoInMagRatio, Time.deltaTime * bagAnimSpeed);
		Anim.Play( "AmmoBagEmptying", 1, 1.0f - _currentAmmoInMagRatio );
	}
	

	private void FillMag() {

		int transferredShells = Mathf.Min(magCapacity - _ammoInMag, Inventory.ShotgunShells);
		_ammoInMag += transferredShells;
		Inventory.ShotgunShells -= transferredShells;
		UIManager.UpdateAmmoInMag(_ammoInMag);
		
		_ammoInMagRatio = (float)_ammoInMag / magCapacity;
	}
	
	
	private void OnFire() {

		if ( _ammoInMag == 0 || !IsReadyToShoot ) {
			return;
		}
		_ammoInMag--;
		UIManager.UpdateAmmoInMag(_ammoInMag);
		
		Anim.SetTrigger(Shoot);
		_ammoInMagRatio = (float)_ammoInMag / magCapacity;






		// update the particle direction to make them go to the center of the screen
		Vector3 particleDir = Inventory.Head.forward;
		{
			RaycastHit hit;
			if (Physics.Raycast(Inventory.Head.position, Inventory.Head.forward, out hit)) {
				Vector3 particleDirTmp = (hit.point - bulletParticles.transform.position).normalized;
				if ( Vector3.Dot(particleDir, particleDirTmp) > 0 ) {
					particleDir = particleDirTmp;
				}
				
			}
		}

		Quaternion particleRotation = Quaternion.FromToRotation(Vector3.forward, particleDir);
		bulletParticles.transform.rotation = particleRotation;
		
		
		
		bulletParticles.Emit(bulletParticlesCount);
		
		
		for ( int i=0; i<raycastCount; i++ ) {
			Vector3 rayDir = Vector3.forward;
			
			rayDir = Quaternion.Euler(Random.Range(0, spreadAngle), 0, 0) * rayDir;
			rayDir = Quaternion.Euler(0, 0, Random.Range(0, 360)) * rayDir;
			
			rayDir = Quaternion.FromToRotation(Vector3.forward, Inventory.Head.forward) * rayDir;
			
			
			RaycastHit hit;
			if ( Physics.Raycast( Inventory.Head.position, rayDir, out hit, maxRange ) ) {
				
				ShootableTarget target = hit.transform.GetComponent<ShootableTarget>();
				if ( target != null ) {
					target.TakeDamage( damage / raycastCount, rayDir, ShootableTarget.DamageType.Explosion );
				}
				
			}
			
		}

		_audioSource.PlayOneShot(shoot);
		IsReadyToShoot = false;
	}


	private void OnReload() {

		if ( !IsReadyToShoot || _ammoInMag == magCapacity || Inventory.ShotgunShells == 0 ) {
			return;
		}
		
		
		Anim.SetTrigger(Reload);
		IsReadyToShoot = false;
		
		_audioSource.PlayOneShot(reload);

		}

}
}