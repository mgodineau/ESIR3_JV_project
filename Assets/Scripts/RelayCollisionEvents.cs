using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RelayCollisionEvents : MonoBehaviour {

	[SerializeField] private UnityEvent collisionEnterEvent;
	
	
	private void OnCollisionEnter(Collision collision) {
		if ( !collision.transform.IsChildOf(transform) ) {
			collisionEnterEvent.Invoke();
		}
	}
}
