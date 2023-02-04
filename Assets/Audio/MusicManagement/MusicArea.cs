using UnityEngine;

namespace Audio {
public class MusicArea : MonoBehaviour {
	
	
	[SerializeField] private AudioClip areaMusic;
	
	
	private void OnTriggerEnter(Collider other) {

		if ( other.CompareTag("Player") ) {
			MusicManager.Instance.SetMusic(areaMusic);
		}
		
	}
}
}