using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons;

public class HealthOrbeBehaviour : MonoBehaviour {

	[SerializeField] private int healthBonus = 25;
	
	
	private void OnTriggerEnter(Collider other) {
		
		Player_hp health = other.GetComponentInChildren<Player_hp>();
		if ( health == null || health.Health == health.MaxHealth ) {
			return;
		}


		health.Health += healthBonus;
		Destroy(gameObject);
		
	}
}
