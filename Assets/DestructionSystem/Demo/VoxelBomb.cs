using UnityEngine;

namespace DestructionSystem {
/// <summary>
/// This Component make the object create a hole when it touches a VoxBehaviour, and destroy itself.
/// </summary>
public class VoxelBomb : MonoBehaviour {
	
	// the radius of the sphere to cut from the voxel model
	public float radius = 5;
	
	
	private void OnCollisionEnter( Collision other ) {
		
		VoxBehaviour voxBehaviour = other.gameObject.GetComponent<VoxBehaviour>();
		if( voxBehaviour != null ) {
			VoxBehaviour.SetSphereAt( transform.position, radius );
			Destroy(gameObject);
		}
		
	}
	
	
}
}
