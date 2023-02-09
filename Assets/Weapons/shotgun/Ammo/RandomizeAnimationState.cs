using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Animation))]
public class RandomizeAnimationState : MonoBehaviour
{
	
	[SerializeField] private float minSpeed = 0.8f;
	[SerializeField] private float maxSpeed = 1.5f;
	
	
	private void Awake() {
		Animation anim = GetComponent<Animation>();

		foreach ( AnimationState state in anim ) {
			state.speed = Random.Range(minSpeed, maxSpeed);
			state.time = Random.value * state.clip.length;
		}
		
	}
	
}
