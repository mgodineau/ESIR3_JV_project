using System;
using TMPro;
using UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Weapons {
public class PlayerInventory : MonoBehaviour {


	[Serializable]
	private class WeaponSlot {

		public bool available;
		public WeaponBehaviour weapon;
		

	}

	public enum AmmoType : int {
		ShotgunShell,
		RifleRound
	}
	
	
	[SerializeField] private Transform head;
	public Transform Head => head;
	
	[SerializeField] private WeaponSlot[] weaponSlots;

	private int _nextWeaponId = -1;
	
	[SerializeField] private int currentWeaponId = 0;
	public int CurrentWeaponId {
		get => currentWeaponId;
		set {
			if ( value == currentWeaponId 
			     || value < 0 
			     || value >= weaponSlots.Length 
			     || !weaponSlots[value].available
				) {
				return;
			}
			
			weaponSlots[currentWeaponId].weapon.Unequip();
			_nextWeaponId = value;
		}
	}
	
	[SerializeField] private int maxShotgunShells = 50;
	public int MaxShotgunShell => maxShotgunShells;
	
	[SerializeField] private int shotgunShells = 50;
	public int ShotgunShells {
		get => shotgunShells;
		set {
			shotgunShells = Mathf.Clamp(value, 0, maxShotgunShells);
			UIManager.UpdateAmmoReserve(shotgunShells);
		}
	}


	[SerializeField] private int maxRifleAmmo = 50;
	public int MaxRifleAmmo => maxRifleAmmo;
	[SerializeField] private int rifleAmmo = 50;
	public int RifleAmmo {
		get => rifleAmmo;
		set {
			rifleAmmo = Mathf.Clamp(value, 0, maxRifleAmmo);
			UIManager.UpdateAmmoReserve(rifleAmmo);
		}
	}
	
	

	public void Start() {
		ShotgunShells = shotgunShells;

		foreach ( WeaponSlot slot in weaponSlots ) {
			slot.weapon.gameObject.SetActive(false);
		}
		
		weaponSlots[currentWeaponId].weapon.gameObject.SetActive(true);
	}
	
	
	public void WeaponUnequiped() {
		currentWeaponId = _nextWeaponId;

		if ( _nextWeaponId != -1 ) {
			weaponSlots[currentWeaponId].weapon.gameObject.SetActive(true);
			_nextWeaponId = -1;
		}
	}
	
	
	private void OnSelectWeapon( InputValue weaponId ) {
		CurrentWeaponId = Mathf.RoundToInt(weaponId.Get<float>());
	}

	private void OnSwitchWeapon( InputValue switchDir ) {
		int dir = switchDir.Get<float>() > 0 ? 1 : -1;
		
		int nextId = CurrentWeaponId + dir;
		if (nextId >= weaponSlots.Length) {
			nextId = 0;
		} else if (nextId < 0) {
			nextId = weaponSlots.Length - 1;
		}
		
		CurrentWeaponId = nextId;
		
	}
	
	
}
}
