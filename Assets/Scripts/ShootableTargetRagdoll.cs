using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RagdollManager))]
public class ShootableTargetRagdoll : ShootableTarget {

	[SerializeField] private GameObject explosionPref;
	
	private RagdollManager _ragdoll;
	private bool alive = true;
	// private bool deathCollision = false;
	private DamageType _damageType;


	private void Awake() {
		_ragdoll = GetComponent<RagdollManager>();
	}


	protected override void KillTarget(Vector3 direction, DamageType damageType) {
		
		alive = false;
		_damageType = damageType;


		_ragdoll.EnableRagdoll();
		_ragdoll.MainRigidbody.AddForce( direction*500 + Vector3.up * 500, ForceMode.Impulse );

	}


	public void NotifyCollision() {
		if ( alive) {
			return;
		}

		if ( _damageType == DamageType.Explosion ) {
			Instantiate( explosionPref, _ragdoll.MainRigidbody.position, Quaternion.identity );
		}
		
		
		if ( _damageType != DamageType.Standard ) {
			Destroy(gameObject);
		}
	}
	
}
