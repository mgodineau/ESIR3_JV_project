using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Weapons {
public class PlayerInventory : MonoBehaviour {

	
	
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


	public void Start() {
		ShotgunShells = shotgunShells;
		
	}
	
	
	
}
}
