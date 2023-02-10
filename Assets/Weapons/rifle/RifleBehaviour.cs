using System;
using UI;
using Unity.VisualScripting;
using UnityEngine;

namespace Weapons {
[RequireComponent(typeof(Animator))]
public class RifleBehaviour : WeaponBehaviour {

	
	
	private static readonly int Shoot = Animator.StringToHash("shoot");
	
	


	private new void Awake() {
		base.Awake();
	}
	
	private new void OnEnable() {
		base.OnEnable();
		UIManager.UpdateAmmoInMag(0);
	}
	
	
	private void OnFire() {
		
		if ( !IsReadyToShoot || Inventory.RifleAmmo <= 0 ) {
			return;
		}


		RaycastHit hit;
		if (Physics.Raycast( Inventory.Head.position, Inventory.Head.forward, out hit )) {

			ShootableTarget target = hit.transform.GetComponent<ShootableTarget>();
			if ( target != null ) {
				target.TakeDamage(damage , Inventory.Head.forward, ShootableTarget.DamageType.Petrifaction );
			}
			
		}
		
		Anim.SetTrigger(Shoot);
		Inventory.RifleAmmo--;
		IsReadyToShoot = false;
	}
	
	
}
}
