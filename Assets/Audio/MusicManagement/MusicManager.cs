using System;
using System.Collections;
using UnityEngine;

namespace Audio {

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour {


	private static MusicManager _instance;
	public static MusicManager Instance => _instance;

	
	
	[SerializeField] private float transitionDuration = 1.0f;
	
	
	private AudioSource _audioSource;
	private float _maxVolume;

	private Coroutine _currentTransitionCoroutine;
	
	
	private void Awake() {
		if ( _instance != null ) {
			Debug.LogWarning("Warning : Only one MusicManager instance should be present in the scene");
		}
		_instance = this;
		
		
		_audioSource = GetComponent<AudioSource>();
		_maxVolume = _audioSource.volume;
	}
	
	
	public void SetMusic( AudioClip music ) {
		if ( _audioSource.clip != music ) {
			if ( _currentTransitionCoroutine != null ) {
				StopCoroutine(_currentTransitionCoroutine);
			}
			
			_currentTransitionCoroutine = StartCoroutine(MusicTransition(music, transitionDuration));
		}
	}


	private IEnumerator MusicTransition( AudioClip nextMusic, float transitionDuration = 1.0f ) {

		float startingVolume = _audioSource.volume;
		float volumeRatio = 1;
		while (volumeRatio > 0) {
			yield return null;

			volumeRatio = Mathf.Max(0,volumeRatio - Time.deltaTime * 2 / transitionDuration);
			_audioSource.volume = VolumeRatioToRealVolume(volumeRatio, startingVolume);
		}

		volumeRatio = 0;
		_audioSource.clip = nextMusic;
		_audioSource.Play();

		while ( volumeRatio < 1 ) {
			yield return null;
			volumeRatio = Mathf.Min(1, volumeRatio + Time.deltaTime * 2 / transitionDuration);
			_audioSource.volume = VolumeRatioToRealVolume(volumeRatio, _maxVolume);
		}
	}

	private float VolumeRatioToRealVolume( float ratio, float maxVolume ) {
		return (Mathf.Cos((ratio - 1) * Mathf.PI) + 1) * 0.5f * maxVolume;
	}
	
}
}
