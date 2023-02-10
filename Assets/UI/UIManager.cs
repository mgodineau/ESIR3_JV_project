using System;
using TMPro;
using UnityEngine;

namespace UI {
public class UIManager : MonoBehaviour {


	private static UIManager _instance;
	
	
	
	[SerializeField] private TextMeshProUGUI ammoInMagDisplay;
	[SerializeField] private TextMeshProUGUI ammoReserveDisplay;
	[SerializeField] private TextMeshProUGUI healthDisplay;
	
	
	
	
	private void Awake() {
		_instance = this;
	}
	
	
	public static void UpdateHealth( int health ) {
		if ( IsInstantiated() ) {
			_instance.healthDisplay.SetText("" + health);
		}
	}
	
	
	public static void UpdateAmmoInMag( int ammo ) {
		if (IsInstantiated()) {
			_instance.ammoInMagDisplay.SetText("" + ammo);
		}
	}


	public static void UpdateAmmoReserve( int ammo ) {
		if (IsInstantiated()) {
			_instance.ammoReserveDisplay.SetText("" + ammo);
		}
	}

	public static bool IsInstantiated() {
		if ( _instance == null ) {
			Debug.LogError("[Error] There are no instance of UIManager in the scene, but a script is trying to access it");
		}

		return _instance != null;
	}


	
}
}
