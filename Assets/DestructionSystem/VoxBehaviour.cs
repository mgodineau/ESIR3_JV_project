using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VoxBehaviour : MonoBehaviour
{

	private static HashSet<VoxBehaviour> registeredVoxBehaviour = new HashSet<VoxBehaviour>();


	[SerializeField] public VoxModel model;
	[SerializeField] public VoxBuilder_cube builder;
	
	private MeshFilter meshComponent;
	private MeshCollider meshCollider;


	//private Queue<ModelUpdateThread> pendingModelUpdates = new Queue<ModelUpdateThread>();
	private Queue<System.Threading.Thread> pendingModelUpdates = new Queue<System.Threading.Thread>();
	private System.Threading.Thread runningThread = null;

	private void Awake()
	{
		model = (VoxModel)model.Clone();
		//builder = (VoxBuilder_cube)builder.Clone();
		
		meshComponent = GetComponent<MeshFilter>();
		meshCollider = GetComponent<MeshCollider>();

		// VoxBuilder_cube builder = new VoxBuilder_cube(_model);
		// SetMesh(builder.GetMesh());
	}


    private void LateUpdate()
    {
        if ( runningThread != null && !runningThread.IsAlive ) {
			SetMesh( builder.GetMesh() );
			runningThread = null;
        }

		if( runningThread == null && pendingModelUpdates.Count != 0 ) {
			runningThread = pendingModelUpdates.Dequeue();
			runningThread.Start();
        }

    }


    private void OnEnable() {
		registeredVoxBehaviour.Add(this);
	}

	private void OnDisable() {
		registeredVoxBehaviour.Remove(this);
	}



	public static void SetSphereAt(Vector3 worldCenter, float radius, byte value = 0) {
		foreach (VoxBehaviour behaviour in registeredVoxBehaviour) {
			behaviour.SetSphere(worldCenter, radius, value);
		}
	}

	private void SetSphere(Vector3 worldCenter, float radius, byte value = 0) {
		Vector3 objectCenter = transform.InverseTransformPoint(worldCenter);
		float objectRadius = radius / transform.localScale.x;

		List<Vector3> voxels = CreateSphereVoxels(objectCenter, objectRadius);
		ScheduleModelUpdate(voxels, value);
	}


	private void SetMesh( Mesh mesh )
    {
		meshComponent.mesh = mesh;
		if( meshCollider != null )
        {
			meshCollider.sharedMesh = mesh;
        }
    }


	private void ScheduleModelUpdate(List<Vector3> voxelPositions, byte value)
	{
		System.Threading.Thread thread = new System.Threading.Thread(() =>
		{
			foreach (Vector3 voxel in voxelPositions) {
				model.Set(voxel, value);
			}
			builder.RefreshEntireModel(model);
		});
		pendingModelUpdates.Enqueue(thread);
		// pendingModelUpdates.Enqueue( new ModelUpdateThread(model, voxelPositions, value) );
	}

	private static void SetVoxelsValue(VoxModel model, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles, List<Vector3> voxelPositions, byte value) {
		foreach (Vector3 voxel in voxelPositions)
		{
			model.Set(voxel, value);
		}

		LinkedList<VoxModel.Voxel> voxels = model.GetVoxels();
		foreach( VoxModel.Voxel voxel in voxels ) {
			CreateCubeAt(model, voxel, vertices, normals, uvs, triangles);
        }
	}


	private List<Vector3> CreateSphereVoxels(Vector3 center, float radius) {
		List<Vector3> voxels = new List<Vector3>();

		Vector3 centerSnap = center / model.VoxelSize;
		for (int i = 0; i < 3; i++) {
			centerSnap[i] = Mathf.Round(centerSnap[i]);
		}
		centerSnap *= model.VoxelSize;
		float sqrRadius = radius * radius;

		int radiusInVoxels = Mathf.RoundToInt(radius / model.VoxelSize);
		for (int x = -radiusInVoxels; x < radiusInVoxels; x++) {
			for (int y = -radiusInVoxels; y < radiusInVoxels; y++) {
				for (int z = -radiusInVoxels; z < radiusInVoxels; z++) {
					Vector3 offset = new Vector3(x, y, z) * model.VoxelSize;
					if (offset.sqrMagnitude <= sqrRadius)
					{
						voxels.Add(centerSnap + offset);
					}
				}
			}
		}


		return voxels;
	}


	public Mesh CreateMeshFromModel(VoxModel model) {

		Mesh mesh = new Mesh();
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

		LinkedList<VoxModel.Voxel> voxels = model.GetVoxels();


		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> triangles = new List<int>();

		foreach (VoxModel.Voxel voxel in voxels) {
			if (voxel.depth != 0) {
				//continue;
			}
			CreateCubeAt(model, voxel, vertices, normals, uvs, triangles);

		}
		mesh.SetVertices(vertices);
		mesh.SetNormals(normals);
		mesh.SetUVs(0, uvs);
		mesh.SetTriangles(triangles, 0);

		return mesh;
	}




	private static void CreateCubeAt(VoxModel model, VoxModel.Voxel voxel, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles) {

		float halfSize = voxel.size / 2;

		List<int> triangleSequenceOffset = new List<int>{
			3, 4, 2,
			2, 1, 3,
		};

		List<int> triangleSequenceOffsetReverse = new List<int>(triangleSequenceOffset);
		triangleSequenceOffsetReverse.Reverse();

		for (int axis = 0; axis < 3; axis++) {
			Vector3 tangent1 = Vector3.zero;
			Vector3 tangent2 = Vector3.zero;
			Vector3 faceDir = Vector3.zero;

			for (int dir = -1; dir <= 1; dir += 2) {

				faceDir[axis] = dir;

				if (model.Get(voxel.position + faceDir * voxel.size) != 0)
				{
					continue;
				}

				tangent1[(axis + 1) % 3] = 1;
				tangent2[(axis + 2) % 3] = 1;


				Vector3 faceCenter = voxel.position + faceDir * halfSize;

				vertices.Add(faceCenter + (tangent1 + tangent2) * halfSize);
				vertices.Add(faceCenter + (-tangent1 + tangent2) * halfSize);
				vertices.Add(faceCenter + (tangent1 - tangent2) * halfSize);
				vertices.Add(faceCenter + (-tangent1 - tangent2) * halfSize);

				int colorId = voxel.value - 1;
				Vector2 uv = new Vector2(colorId % 16, colorId / 16);
				uv += Vector2.one * 0.5f;
				uv /= 16;

				for (int i = 0; i < 4; i++) {
					normals.Add(faceDir);
					uvs.Add(uv);
				}


				List<int> currentSequence = (dir == -1) ? triangleSequenceOffset : triangleSequenceOffsetReverse;
				foreach (int offset in currentSequence) {
					triangles.Add(vertices.Count - offset);
				}

			}



		}


	}



	private class ModelUpdateThread {

		private List<Vector3> vertices;
		private List<Vector3> normals;
		private List<Vector2> uvs;
		private List<int> triangles;
		private System.Threading.Thread thread;
		
		public bool IsDone {
			get {return !thread.IsAlive;}
		}


		public ModelUpdateThread( VoxModel model, List<Vector3> voxelPositions, byte value ) {
			vertices = new List<Vector3>();
			normals = new List<Vector3>();
			uvs = new List<Vector2>();
			triangles = new List<int>();

			thread = new System.Threading.Thread( () => { SetVoxelsValue(model, vertices, normals, uvs, triangles, voxelPositions, value);  } );
        }


		public void Start()
        {
			thread.Start();
        }

		public Mesh GetUpdatedMesh() {
			Mesh mesh = new Mesh();
			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

			mesh.SetVertices(vertices);
			mesh.SetNormals(normals);
			mesh.SetUVs(0, uvs);
			mesh.SetTriangles(triangles, 0);

			return mesh;
        }

	}


	
}
