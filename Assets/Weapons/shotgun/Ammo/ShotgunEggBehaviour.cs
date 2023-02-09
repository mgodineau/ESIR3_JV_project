using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Weapons;


public class ShotgunEggBehaviour : MonoBehaviour {

	[SerializeField] private int ammoCount = 10;
	
	
	private Animation _anim;

	private void Awake() {
		_anim = GetComponent<Animation>();
	}
	
	private void OnTriggerEnter(Collider other) {

		PlayerInventory inventory = other.GetComponent<PlayerInventory>();
		if ( inventory == null || inventory.ShotgunShells == inventory.MaxShotgunShell ) {
			return;
		}
		
		
		inventory.ShotgunShells += ammoCount;
		
		
		for ( int i=0; i<transform.childCount; i++ ) {
			transform.GetChild(i).gameObject.SetActive(false);
		}
		
		_anim.enabled = false;
		enabled = false;
	}
}
