using System;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class SlaveNavMeshAgentToAnimator : MonoBehaviour {

	private static readonly int SpeedXHash = Animator.StringToHash("speedX");
	private static readonly int SpeedYHash = Animator.StringToHash("speedY");
	
	
	private Animator _animator;
	private NavMeshAgent _agent;

	private Vector3 currentLocalSpeed = Vector3.zero;
	
	
	
	private void Awake() {
		_agent = GetComponent<NavMeshAgent>();
		_animator = GetComponent<Animator>();
		
		_agent.updatePosition = false;
	}

	private void Start() {
		transform.position = _agent.nextPosition;
	}


	private void Update() {
		Vector3 desiredLocalSpeed =  transform.InverseTransformVector(_agent.desiredVelocity);
		currentLocalSpeed = Vector3.MoveTowards(currentLocalSpeed, desiredLocalSpeed, Time.deltaTime * _agent.acceleration);
		
		_animator.SetFloat(SpeedXHash, currentLocalSpeed.x);
		_animator.SetFloat(SpeedYHash, currentLocalSpeed.z);
		
		
		Vector3 position = transform.position;
		position.y = _agent.nextPosition.y;
		transform.position = position + Vector3.up * _agent.baseOffset;
		
		_agent.nextPosition = position;
		
		
	}

}
