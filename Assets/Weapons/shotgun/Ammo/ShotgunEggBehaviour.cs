
using UnityEngine;
using Weapons;

[RequireComponent(typeof(Collider))]
public class ShotgunEggBehaviour : MonoBehaviour {

	[SerializeField] private int ammoCount = 10;
	
	
	private Animation _anim;
	private Collider _collider;
	
	private void Awake() {
		_anim = GetComponent<Animation>();
		_collider = GetComponent<Collider>();
	}
	
	private void OnTriggerEnter(Collider other) {

		PlayerInventory inventory = other.GetComponentInChildren<PlayerInventory>();
		if ( inventory == null || inventory.ShotgunShells == inventory.MaxShotgunShell ) {
			return;
		}
		
		
		inventory.ShotgunShells += ammoCount;
		
		
		for ( int i=0; i<transform.childCount; i++ ) {
			transform.GetChild(i).gameObject.SetActive(false);
		}
		
		_anim.enabled = false;
		_collider.enabled = false;
	}
}
