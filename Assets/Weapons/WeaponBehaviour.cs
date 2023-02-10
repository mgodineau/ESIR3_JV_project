using System;
using UnityEngine;

namespace Weapons {

[RequireComponent(typeof(Animator))]
public abstract class WeaponBehaviour : MonoBehaviour{
	
	
	
	[SerializeField] protected float damage;
	protected bool IsReadyToShoot;
	
	protected PlayerInventory Inventory;
	
	protected Animator Anim;
	private static readonly int HideTriggerHash = Animator.StringToHash("hide");
	private static readonly int ShowTriggerHash = Animator.StringToHash("show");


	protected void Awake() {
		Anim = GetComponent<Animator>();
		Inventory = GetComponentInParent<PlayerInventory>();
	}
	
	
	protected void OnEnable() {
		Anim.SetTrigger(ShowTriggerHash);
		IsReadyToShoot = false;
	}

	public void Unequip() {
		Anim.SetTrigger(HideTriggerHash);
	}
	
	
	
	private void ReadyToShoot() {
		IsReadyToShoot = true;
	}
	
	private void Hidden() {
		gameObject.SetActive(false);
		Inventory.WeaponUnequiped();
	}
	
}
}