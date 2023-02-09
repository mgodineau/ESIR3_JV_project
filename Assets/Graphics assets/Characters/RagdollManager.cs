using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollManager : MonoBehaviour {

	[SerializeField] private Component[] componentsToDisable = Array.Empty<Component>();
	[SerializeField] private Rigidbody mainRigidbody;
	public Rigidbody MainRigidbody => mainRigidbody;
	
	private Rigidbody[] _ragdollRigidbodies;


	private void Awake() {
		_ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
		SetRagdoll(false);
	}


	public void EnableRagdoll() {
		SetRagdoll(true);
	}
	
	
	private void SetRagdoll(bool enable) {
		
		foreach ( Component component in componentsToDisable ) {
			if ( component is Collider col ) {
				col.enabled = !enable;
			} else if ( component is Behaviour behaviour ) {
				behaviour.enabled = !enable;
			}
		}

		foreach ( Rigidbody rb in _ragdollRigidbodies ) {
			rb.isKinematic = !enable;
		}
	}
	
	
}
