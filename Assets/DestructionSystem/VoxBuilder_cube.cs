using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "ScriptableObjects/VoxBuilder_cube")]
public class VoxBuilder_cube : ScriptableObject
{
	[Serializable]
	class DictPositionToFaces : Dictionary<Vector3Int, int[]> {}
	
	[SerializeField] private DictPositionToFaces positionToFaces;

	[SerializeField] private List<int> triangles;
	[SerializeField] private List<Vector3> vertices;
	[SerializeField] private List<Vector3> normals;
	[SerializeField] private List<Vector2> uvs;

	[SerializeField] private List<RegisteredFace> faces;
	
	
	public VoxBuilder_cube() {
		positionToFaces = new DictPositionToFaces();

		triangles = new List<int>();
		vertices = new List<Vector3>();
		normals = new List<Vector3>();
		uvs = new List<Vector2>();

		faces = new List<RegisteredFace>();
	}
	
	
	public Mesh GetMesh() {
		Mesh mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
		
		mesh.SetVertices(vertices);
		mesh.SetUVs(0, uvs);
		mesh.SetNormals(normals);
		
		mesh.SetTriangles(triangles, 0);
		
		return mesh;
	}

	public void ClearMesh() {
		positionToFaces.Clear();
		triangles.Clear();
		vertices.Clear();
		uvs.Clear();
		normals.Clear();
		faces.Clear();
	}
	public void RefreshEntireModel( VoxModel model ) {
		ClearMesh();
		
		LinkedList<VoxModel.Voxel> voxels = model.GetVoxels();

		foreach ( VoxModel.Voxel voxel in voxels )
		{

			for ( int dir=0; dir<6; dir++ ) {
				int axis = dir / 2;
				int axisSign = (dir % 2 == 0) ? 1 : -1;
				Vector3 nextPosition = voxel.position;
				nextPosition[axis] += axisSign * voxel.size;
				if ( model.Get(nextPosition) == 0 ) {
					AddFace(model, voxel, dir);
				}
			}
		}
	}
	
	
	
	private void AddFace( VoxModel model, VoxModel.Voxel voxel, int dir )
	{
		Vector3Int voxelPosition = model.ObjectToVoxelPosition(voxel.position);
		if ( !positionToFaces.ContainsKey(voxelPosition) ) {
			positionToFaces.Add( voxelPosition, new int[6]{-1, -1, -1, -1, -1, -1} );
		}

		int[] facesIds = positionToFaces[voxelPosition];
		if ( facesIds[dir] != -1 ) {
			return;
		}

		facesIds[dir] = faces.Count;
		faces.Add( new RegisteredFace{voxelPosition = voxelPosition, dir = dir} );
		
		float halfSize = voxel.size / 2;
		int axis = dir / 2;
		int axisSign = (dir % 2 == 0) ? 1 : -1;
		
		Vector3 tangent1 = Vector3.zero;
		Vector3 tangent2 = Vector3.zero;
		Vector3 faceDir = Vector3.zero;

		faceDir[axis] = axisSign;
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
		
		
		List<int> triangleSequenceOffset = new List<int>{
			3, 4, 2,
			2, 1, 3,
		};

		List<int> triangleSequenceOffsetReverse = new List<int>(triangleSequenceOffset);
		triangleSequenceOffsetReverse.Reverse();
		
		List<int> currentSequence = (axisSign == -1) ? triangleSequenceOffset : triangleSequenceOffsetReverse;
		foreach ( int offset in currentSequence ) {
			triangles.Add( vertices.Count - offset );
		}
		
	}



	void RemoveFace(VoxModel model, VoxModel.Voxel voxel, int dir)
	{
		Vector3Int voxelPosition = model.ObjectToVoxelPosition(voxel.position);
		if ( !positionToFaces.ContainsKey(voxelPosition) ) {
			return;
		}

		int[] faceIds = positionToFaces[voxelPosition];
		int faceId = faceIds[dir];
		if (faceId == -1) {
			return;
		}
		faceIds[dir] = -1;


		RegisteredFace face = faces[faces.Count - 1];
		faces[faceId] = face;
		positionToFaces[face.voxelPosition][face.dir] = faceId;
		
		int vertexCount = 4;
		int triangleCount = 6;
		int vertexStartIndex = faceId * vertexCount;
		int trianglesStartIndex = faceId * triangleCount;
		int vertexIndexDelta = vertices.Count - vertexCount - vertexStartIndex;
		
		for ( int i=0; i<vertexCount; i++ ) {
			int dstIndex = vertexStartIndex + i;
			int srcIndex = vertices.Count - vertexCount + i;
			vertices[dstIndex] = vertices[srcIndex];
			uvs[dstIndex] = uvs[srcIndex];
			normals[dstIndex] = normals[srcIndex];
		}
		
		for ( int i=0; i<triangleCount; i++ ) {
			int dstIndex = trianglesStartIndex + i;
			int srcIndex = triangles.Count - triangleCount + i;
			
			triangles[dstIndex] = triangles[srcIndex] - vertexIndexDelta;
		}

		vertices.RemoveRange(vertices.Count-vertexCount, vertexCount);
		uvs.RemoveRange(vertices.Count-vertexCount, vertexCount);
		normals.RemoveRange(vertices.Count-vertexCount, vertexCount);
		triangles.RemoveRange(triangles.Count-triangleCount, triangleCount);
	}

	
	[System.Serializable]
	private class RegisteredFace {
		public Vector3Int voxelPosition;
		public int dir;
	}
	
	
}