using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Weapons {
public class PlayerInventory : MonoBehaviour {

	
	
	[SerializeField] private int maxShotgunShells = 50;
	public int MaxShotgunShell => maxShotgunShells;
	
	[SerializeField] private int shotgunShells = 50;
	
	[SerializeField] private TextMeshProUGUI ammoReserveDisplay;
	[SerializeField] private TextMeshProUGUI ammoInClipDisplay;
	
	
	public int ShotgunShells {
		get => shotgunShells;
		set {
			shotgunShells = Mathf.Clamp(value, 0, maxShotgunShells);

			ammoReserveDisplay.SetText("" + shotgunShells);
		}
	}


	public void Start() {
		ShotgunShells = shotgunShells;
		
	}
	
	
	public void UpdateAmmoInMag( int ammoInMag ) {
		ammoInClipDisplay.text = "" + ammoInMag;
	}
	
}
}
