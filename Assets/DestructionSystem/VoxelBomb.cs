using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelBomb : MonoBehaviour
{
	
	public float radius = 5;
	
	
	private void OnCollisionEnter( Collision other ) {
		
		VoxBehaviour voxBehaviour = other.gameObject.GetComponent<VoxBehaviour>();
		if( voxBehaviour != null ) {
			VoxBehaviour.SetSphereAt( transform.position, 0 );
			Destroy(gameObject);
		}
		
	}
	
	
}
