using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VoxBehaviour : MonoBehaviour
{
	
	private static HashSet<VoxBehaviour> registeredVoxBehaviour = new HashSet<VoxBehaviour>();
	
	
	[SerializeField]
	private VoxModel _model;
	public VoxModel Model {
		set {_model = value;}
	}
	
	private MeshFilter _meshComponent;
	public MeshFilter MeshComponent {
		get {
			if( _meshComponent == null ) {
				_meshComponent = GetComponent<MeshFilter>();
			}
			return _meshComponent;
		}
	}
	
	
	public void OnEnable() {
		registeredVoxBehaviour.Add(this);
	}
	
	public void OnDisable() {
		registeredVoxBehaviour.Remove(this);
	}
	
	
	
	
	public static void SetSphereAt(Vector3 worldCenter, float radius, byte value = 0) {
		foreach( VoxBehaviour behaviour in registeredVoxBehaviour ) {
			behaviour.SetSphere(worldCenter, radius, value);
		}
	}
	
	private void SetSphere(Vector3 worldCenter, float radius, byte value = 0) {
		Vector3 objectCenter = transform.InverseTransformPoint(worldCenter);
		float objectRadius = radius / transform.localScale.x;
		
		List<Vector3> voxels = CreateSphereVoxels(objectCenter, objectRadius);
		SetVoxelsValue(voxels, value);
	}
	
	
	private void SetVoxelsValue( List<Vector3> voxelPositions, byte value ) {
		foreach( Vector3 voxel in voxelPositions ) {
			_model.Set(voxel, value);
		}
		
		MeshComponent.mesh = CreateMeshFromModel(_model);
	}
	
	
	private List<Vector3> CreateSphereVoxels( Vector3 center, float radius ) {
		List<Vector3> voxels = new List<Vector3>();
		
		Vector3 centerSnap = center / _model.VoxelSize;
		for( int i=0; i<3; i++ ) {
			centerSnap[i] = Mathf.Round(centerSnap[i]);
		}
		centerSnap *= _model.VoxelSize;
		
		int sizeInVoxels = Mathf.RoundToInt(radius / _model.VoxelSize);
		int halfSize = sizeInVoxels / 2;
		for( int x=-halfSize; x<halfSize; x++ ) {
			for( int y=-halfSize; y<halfSize; y++ ) {
				for( int z=-halfSize; z<halfSize; z++ ) {
					Vector3 offset = new Vector3(x, y, z) * _model.VoxelSize;
					voxels.Add( centerSnap + offset );
				}
			}
		}
		
		
		return voxels;
	}
	
	public Mesh CreateMeshFromModel( VoxModel model ) {
		
		Mesh mesh = new Mesh();
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		
		LinkedList<VoxModel.Voxel> voxels = model.GetVoxels();
		//IEnumerable<VoxModel.Voxel> filteredVoxels = System.Linq.Enumerable.Where<VoxModel.Voxel>(voxels, (VoxModel.Voxel voxel ) => voxel.value != 0);
		
		
		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		List<int> triangles = new List<int>();
		
		foreach(VoxModel.Voxel voxel in voxels) {
			if( voxel.depth != 0 ) {
				//continue;
			}
			CreateCubeAt( voxel, model, vertices, normals, triangles );

		}
		mesh.SetVertices(vertices);
		mesh.SetNormals(normals);
		mesh.SetTriangles(triangles, 0);
		
		return mesh;
	}
	
	
	private void CreateCubeAt( VoxModel.Voxel voxel, VoxModel model, List<Vector3> vertices, List<Vector3> normals, List<int> triangles ) {
		
		float halfSize = voxel.size / 2;
		
		List<int> triangleSequenceOffset = new List<int>{
			3, 4, 2,
			2, 1, 3,
		};
		
		List<int> triangleSequenceOffsetReverse = new List<int>(triangleSequenceOffset);
		triangleSequenceOffsetReverse.Reverse();
		
		for( int axis=0; axis<3; axis++ ) {
			Vector3 tangent1 = Vector3.zero;
			Vector3 tangent2 = Vector3.zero;
			Vector3 faceDir = Vector3.zero;
			
			for( int dir = -1; dir<=1; dir+=2 ) {
				
				faceDir[axis] = dir;

				if( model.Get(voxel.position + faceDir * voxel.size ) != 0 )
                {
					continue;
                }

				tangent1[(axis+1)%3] = 1;
				tangent2[(axis+2)%3] = 1;
				
				
				Vector3 faceCenter = voxel.position + faceDir * halfSize;
				
				vertices.Add(faceCenter + (tangent1 + tangent2) * halfSize );
				vertices.Add(faceCenter + (-tangent1 + tangent2) * halfSize );
				vertices.Add(faceCenter + (tangent1 - tangent2) * halfSize );
				vertices.Add(faceCenter + (-tangent1 - tangent2) * halfSize );
				
				for(int i=0; i<4; i++) {
					normals.Add(faceDir);
				}


				List<int> currentSequence = (dir == -1) ? triangleSequenceOffset : triangleSequenceOffsetReverse;
				foreach( int offset in currentSequence) {
					triangles.Add(vertices.Count - offset);
				}

			}
			
			
			
		}
		
		
		
	}
	
	
}
