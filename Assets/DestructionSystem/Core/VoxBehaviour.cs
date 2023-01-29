using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DestructionSystem {
/// <summary>
/// A behaviour with an attached voxel model and mesh builder. This is required to update the mesh.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VoxBehaviour : MonoBehaviour {
	
	
	// a set of every voxel models active in the scene
	private static readonly HashSet<VoxBehaviour> RegisteredVoxBehaviour = new HashSet<VoxBehaviour>();

	
	[SerializeField] public VoxModelOctree model;
	[SerializeField] public SerializableVoxBuilder builder;
	
	private MeshFilter _meshComponent;
	private MeshCollider _meshCollider;


	
	// a list of thread that contains model updates. none of them is started
	private readonly Queue<System.Threading.Thread> _pendingModelUpdates = new Queue<System.Threading.Thread>();
	// a running thread that updates the model. Can be null
	private System.Threading.Thread _runningThread;
	private bool _isMeshColliderNull;

	private void Awake() {
		// get some components. the collider can be null
		_meshComponent = GetComponent<MeshFilter>();
		_meshCollider = GetComponent<MeshCollider>();
		_isMeshColliderNull = _meshCollider == null;
		
		
		// duplicate the model and it's builder to not change any assets
		model = Instantiate(model);
		builder = Instantiate(builder);
		
		// duplicate the mesh, to not modify the asset
		SetMesh( _meshComponent.mesh );
	}


	private void LateUpdate() {
		// when the current thread is done updating the model, display the new mesh
		if (_runningThread?.IsAlive == false) {
			builder.UpdateMesh( _isMeshColliderNull ? _meshComponent.sharedMesh : _meshCollider.sharedMesh);
			_runningThread = null;
		}
		
		// if no thread are running, start one of the pending thread
		if (_runningThread == null && _pendingModelUpdates.Count != 0) {
			_runningThread = _pendingModelUpdates.Dequeue();
			_runningThread.Start();
		}
	}


	private void OnEnable() {
		RegisteredVoxBehaviour.Add(this);
	}

	private void OnDisable() {
		RegisteredVoxBehaviour.Remove(this);
	}
	
	
	/// <summary>
	/// Set a sphere of voxels to a specific value, among every voxel models in the scene
	/// </summary>
	/// <param name="worldCenter">The center of the sphere, in world coordinates</param>
	/// <param name="radius">The radius of the sphere</param>
	/// <param name="value">The new value of voxels in the sphere</param>
	public static void SetSphereAt(Vector3 worldCenter, float radius, byte value = 0) {
		foreach (VoxBehaviour behaviour in RegisteredVoxBehaviour) {
			behaviour.SetSphere(worldCenter, radius, value);
		}
	}
	
	/// <summary>
	/// Set a sphere of voxels to a specific value, only in this model
	/// </summary>
	/// <param name="worldCenter">The center of the sphere, in world coordinates</param>
	/// <param name="radius">The radius of the sphere</param>
	/// <param name="value">The new value of voxels in the sphere</param>
	private void SetSphere(Vector3 worldCenter, float radius, byte value = 0) {
		Transform tr = transform;
		Vector3 objectCenter = tr.InverseTransformPoint(worldCenter);
		float objectRadius = radius / tr.localScale.x;

		List<Vector3> voxels = CreateSphereVoxels(objectCenter, objectRadius);
		ScheduleModelUpdate(voxels, value);
	}

	
	/// <summary>
	/// Set the new mesh to display. Update the meshFilter and the meshCollider, if present
	/// </summary>
	/// <param name="mesh">The new mesh to display</param>
	private void SetMesh(Mesh mesh) {
		_meshComponent.mesh = mesh;
		if (_meshCollider != null) {
			_meshCollider.sharedMesh = mesh;
		}
	}
	
	
	/// <summary>
	/// Plan a new update on the model
	/// </summary>
	/// <param name="voxelPositions">The object positions of the voxels to update</param>
	/// <param name="value">the new value of the voxels to update</param>
	private void ScheduleModelUpdate(List<Vector3> voxelPositions, byte value) {
		System.Threading.Thread thread = new System.Threading.Thread(() => {
			Vector3 cornerHigh = model.VoxelToObjectPosition(Vector3Int.zero);
			Vector3 cornerLow = model.VoxelToObjectPosition(model.BoundingBox);
			
			foreach (Vector3 voxelPosition in voxelPositions) {
				model.Set(voxelPosition, value);
				cornerHigh = Vector3.Max(cornerHigh, voxelPosition);
				cornerLow = Vector3.Min(cornerLow, voxelPosition);
			}
			
			cornerLow -= Vector3.one * model.VoxelSize;
			cornerHigh += Vector3.one * model.VoxelSize;
			builder.RefreshRegion(model, cornerLow, cornerHigh);
		});
		_pendingModelUpdates.Enqueue(thread);
	}
	
	
	/// <summary>
	/// </summary>
	/// <param name="center">The center of the sphere in object coordinates</param>
	/// <param name="radius">The radius of the sphere</param>
	/// <returns>The positions of every voxels inside a specified sphere. The positions are expressed in world coordinates</returns>
	private List<Vector3> CreateSphereVoxels(Vector3 center, float radius) {
		List<Vector3> voxels = new List<Vector3>();
		
		// make sure the center of the sphere is exactly the center of a voxel, so that every other positions are.
		Vector3 centerSnap = center / model.VoxelSize;
		for (int i = 0; i < 3; i++) {
			centerSnap[i] = Mathf.Round(centerSnap[i]);
		}
		centerSnap *= model.VoxelSize;
		
		// loop through every voxel positions in the cube around the sphere, and keep only the ones in the sphere
		float sqrRadius = radius * radius;
		int radiusInVoxels = Mathf.RoundToInt(radius / model.VoxelSize);
		for (int x = -radiusInVoxels; x < radiusInVoxels; x++) {
			for (int y = -radiusInVoxels; y < radiusInVoxels; y++) {
				for (int z = -radiusInVoxels; z < radiusInVoxels; z++) {
					Vector3 offset = new Vector3(x, y, z) * model.VoxelSize;
					if (offset.sqrMagnitude <= sqrRadius) {
						voxels.Add(centerSnap + offset);
					}
				}
			}
		}
		
		
		return voxels;
	}
}
}