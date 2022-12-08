using UnityEngine;

namespace DestructionSystem {
/// <summary>
/// A MonoBehaviour that rotates it's gameobject wit a constant speed around a specific axis
/// </summary>
public class RotationTest : MonoBehaviour  {
    
	public float rotationSpeed = 90;
	public Vector3 rotationAxis = Vector3.forward;
    
	void Update() {
		transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
	}
}
}
