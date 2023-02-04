using System;
using System.Collections;
using UnityEngine;

namespace Audio {

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour {


	private static MusicManager _instance;
	public static MusicManager Instance => _instance;

	
	
	[SerializeField] private float transitionDuration = 1.0f;
	
	
	private AudioSource audioSource;
	private float maxVolume;
	
	
	private void Awake() {
		if ( _instance != null ) {
			Debug.LogWarning("Warning : Only one MusicManager instance should be present in the scene");
		}
		_instance = this;
		
		
		audioSource = GetComponent<AudioSource>();
		maxVolume = audioSource.volume;
	}
	
	
	public void SetMusic( AudioClip music ) {
		if ( audioSource.clip != music ) {
			StartCoroutine(MusicTransition(music, transitionDuration));
		}
	}


	private IEnumerator MusicTransition( AudioClip nextMusic, float transitionDuration = 1.0f ) {

		float volumeRatio = 1;
		while (volumeRatio != 0) {
			yield return null;

			volumeRatio = Mathf.Max( 0, volumeRatio - Time.deltaTime * 2 / transitionDuration );
			audioSource.volume = VolumeRatioToRealVolume(volumeRatio);
		}

		audioSource.clip = nextMusic;
		audioSource.Play();

		while ( volumeRatio != 1 ) {
			yield return null;
			volumeRatio = Mathf.Min( 1, volumeRatio + Time.deltaTime * 2 / transitionDuration );
			audioSource.volume = VolumeRatioToRealVolume(volumeRatio);
		}

	}

	private float VolumeRatioToRealVolume( float ratio ) {
		return (Mathf.Cos((ratio - 1) * Mathf.PI) + 1) * 0.5f * maxVolume;
	}
	
}
}
