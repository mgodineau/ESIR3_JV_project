using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VoxBehaviour : MonoBehaviour
{
	
	
	private VoxModel _model;
	
	
	private MeshFilter _meshComponent;
	public MeshFilter MeshComponent {
		get {
			if( _meshComponent == null ) {
				_meshComponent = GetComponent<MeshFilter>();
			}
			return _meshComponent;
		}
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
