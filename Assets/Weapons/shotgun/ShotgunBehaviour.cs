using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

using UnityEngine.Serialization;

namespace Weapons {

[RequireComponent(typeof(Animator))]
public class ShotgunBehaviour : MonoBehaviour {


	private Animator _anim;
	private static readonly int Shoot = Animator.StringToHash("shoot");
	private static readonly int Reload = Animator.StringToHash("reload");
	

	[SerializeField] private int magCapacity = 10;

	[SerializeField] private int ammoInReserve = 50;
	private int _ammoInMag;
	
	
	[SerializeField] private Transform canonEnd;
	[SerializeField] private ParticleSystem bulletParticles;
	[SerializeField] private int bulletParticlesCount = 10;
	[SerializeField] private int raycastCount = 20;

	
	[SerializeField] private float totalDamage = 1.0f;
	[SerializeField] private float spreadAngle = 10;
	[SerializeField] private float maxRange = 50;


	[SerializeField] private float bagAnimSpeed = 1.0f;
	
	private bool _readyToShoot = true;
	private float _ammoInMagRatio = 0.0f;
	private float _currentAmmoInMagRatio = 0.0f;
	

	private void Awake() {
		_anim = GetComponent<Animator>();
		_ammoInMag = Mathf.Min(magCapacity, ammoInReserve);

		_currentAmmoInMagRatio = _ammoInMag;
		
	}

	private void Update() {

		_currentAmmoInMagRatio = Mathf.MoveTowards(_currentAmmoInMagRatio, _ammoInMagRatio, Time.deltaTime * bagAnimSpeed);
		_anim.Play( "AmmoBagEmptying", 1, 1.0f - _currentAmmoInMagRatio );
	}


	private void ReadyToShoot() {
		_readyToShoot = true;
	}

	private void FillMag() {
		_ammoInMag = magCapacity;
		_ammoInMagRatio = (float)_ammoInMag / magCapacity;
	}
	
	
	private void OnFire() {

		if ( _ammoInMag == 0 || !_readyToShoot ) {
			return;
		}
		_ammoInMag--;
		
		
		_anim.SetTrigger(Shoot);
		_ammoInMagRatio = (float)_ammoInMag / magCapacity;
		
		
		
		
		bulletParticles.Emit(bulletParticlesCount);
		

		for ( int i=0; i<raycastCount; i++ ) {
			Vector3 rayDir = Vector3.forward;
			
			rayDir = Quaternion.Euler(Random.Range(0, spreadAngle), 0, 0) * rayDir;
			rayDir = Quaternion.Euler(0, 0, Random.Range(0, 360)) * rayDir;
			
			rayDir = Quaternion.FromToRotation(Vector3.forward, transform.forward) * rayDir;
			
			
			RaycastHit hit;
			if ( Physics.Raycast( canonEnd.position, rayDir, out hit, maxRange ) ) {
				
				ShootableTarget target = hit.transform.GetComponent<ShootableTarget>();
				if ( target != null ) {
					target.TakeDamage( totalDamage / raycastCount );
				}
				
			}
			
		}
		
		
		_readyToShoot = false;
	}


	private void OnReload() {

		if ( !_readyToShoot || _ammoInMag == magCapacity ) {
			return;
		}
		
		
		_anim.SetTrigger(Reload);
		_readyToShoot = false;

	}

}
}