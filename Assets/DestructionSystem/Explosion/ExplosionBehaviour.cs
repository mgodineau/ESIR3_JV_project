using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionBehaviour : MonoBehaviour {
	
	
	[SerializeField] private float holeRadius = 5.0f;
	[SerializeField] private float explosionLifetime = 1.0f;
	
	private void OnEnable() {
		DestructionSystem.VoxBehaviour.SetSphereAt( transform.position, holeRadius );
		StartCoroutine(DelayedDestruction(explosionLifetime));
	}


	private IEnumerator DelayedDestruction( float delay ) {
		yield return new WaitForSeconds(delay);
		Destroy(gameObject);
	}
	
}
