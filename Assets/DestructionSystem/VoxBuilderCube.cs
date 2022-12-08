using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DestructionSystem {
[CreateAssetMenu(menuName = "ScriptableObjects/VoxBuilderCube")]
public class VoxBuilderCube : ScriptableObject, ISerializationCallbackReceiver {
	
	
	// dictionary that maps voxels positions to indexes of their faces in the "faces" list
	private readonly Dictionary<Vector3Int, FaceIds> _positionToFaces;
	
	// lists used to serialize _positionToFaces
	[SerializeField] private List<Vector3Int> positionToFacesSerializedKeys;
	[SerializeField] private List<FaceIds> positionToFacesSerializedValues;
	
	// lists containing the data to build the mesh with
	[SerializeField] private List<int> triangles;
	[SerializeField] private List<Vector3> vertices;
	[SerializeField] private List<Vector3> normals;
	[SerializeField] private List<Vector2> uvs;
	
	// list of faces, on which faces are in the same order as in the previous lists
	[SerializeField] private List<RegisteredFace> faces;
	
	
	/// <summary>
	/// Build an instance of VoxBuilderCube
	/// </summary>
	public VoxBuilderCube() {
		_positionToFaces = new Dictionary<Vector3Int, FaceIds>();
		
		triangles = new List<int>();
		vertices = new List<Vector3>();
		normals = new List<Vector3>();
		uvs = new List<Vector2>();
		
		faces = new List<RegisteredFace>();
		
		positionToFacesSerializedKeys = new List<Vector3Int>();
		positionToFacesSerializedValues = new List<FaceIds>();
	}
	
	
	
	public void OnBeforeSerialize() {
		positionToFacesSerializedKeys = new List<Vector3Int>(_positionToFaces.Keys);
		positionToFacesSerializedValues = new List<FaceIds>(_positionToFaces.Values);
	}

	public void OnAfterDeserialize() {
		for (int i = 0; i < positionToFacesSerializedKeys.Count; i++) {
			_positionToFaces.Add(positionToFacesSerializedKeys[i], positionToFacesSerializedValues[i]);
		}

		positionToFacesSerializedKeys.Clear();
		positionToFacesSerializedValues.Clear();
	}

	
	/// <summary>
	/// Create A new mesh that represents the last model that was passed to the builder
	/// </summary>
	/// <returns>A new mesh</returns>
	public Mesh GetMesh() {
		Mesh mesh = new Mesh { indexFormat = IndexFormat.UInt32 };

		mesh.SetVertices(vertices);
		mesh.SetUVs(0, uvs);
		mesh.SetNormals(normals);

		mesh.SetTriangles(triangles, 0);

		return mesh;
	}
	
	
	
	/// <summary>
	/// Clear the mesh data and rebuilt it according to the specified model
	/// </summary>
	/// <param name="model">The model to build the mesh with</param>
	public void RefreshEntireModel(VoxModel model) {
		ClearMesh();

		LinkedList<VoxModel.Voxel> voxels = model.GetVoxels();

		foreach (VoxModel.Voxel voxel in voxels) {
			for (int dir = 0; dir < 6; dir++) {
				int axis = dir / 2;
				int axisSign = (dir % 2 == 0) ? 1 : -1;
				Vector3 nextPosition = voxel.Position;
				nextPosition[axis] += axisSign * voxel.Size;
				if (model.Get(nextPosition) == 0) {
					AddFace(model, voxel, dir);
				}
			}
		}
	}
	
	
	/// <summary>
	/// Update only a small part of the mesh, according to a specified model. The updated part is a rectangular cuboid.
	/// </summary>
	/// <param name="model">The model to read the updates from</param>
	/// <param name="cornerLow">The lower corner of the area to update, in object coordinates.</param>
	/// <param name="cornerHigh">The upper corner of the area to update, in object coordinates.</param>
	public void RefreshRegion(VoxModel model, Vector3 cornerLow, Vector3 cornerHigh) {
		LinkedList<VoxModel.Voxel> voxels = model.GetVoxelsBetween(cornerLow, cornerHigh, true);

		foreach (VoxModel.Voxel voxel in voxels) {
			if (voxel.Value == 0) {
				RemoveVoxelFaces(model, voxel);
			} else {
				AddVoxelFaces(model, voxel);
			}
		}
	}
	
	
	/// <summary>
	/// Clear the Mesh data, to rebuilt it with an other model for instance
	/// </summary>
	private void ClearMesh() {
		_positionToFaces.Clear();
		triangles.Clear();
		vertices.Clear();
		uvs.Clear();
		normals.Clear();
		faces.Clear();
	}
	
	
	/// <summary>
	/// Remove every faces of a voxel. Does nothing for faces that did not exists.
	/// </summary>
	/// <param name="model">The model of the voxel</param>
	/// <param name="voxel">A voxel</param>
	private void RemoveVoxelFaces(VoxModel model, VoxModel.Voxel voxel) {
		Vector3Int voxelPosition = model.ObjectToVoxelPosition(voxel.Position);
		if (!_positionToFaces.ContainsKey(voxelPosition)) {
			return;
		}

		FaceIds faceId = _positionToFaces[voxelPosition];
		for (int dir = 0; dir < 6; dir++) {
			if (faceId[dir] != -1) {
				RemoveFace(model, voxel, dir);
				faceId[dir] = -1;
			}
		}

		_positionToFaces.Remove(voxelPosition);
	}

	
	/// <summary>
	/// Add every visible faces of a given voxel, and remove invisible faces.
	/// </summary>
	/// <param name="model">The model of the voxel</param>
	/// <param name="voxel">A voxel</param>
	private void AddVoxelFaces(VoxModel model, VoxModel.Voxel voxel) {
		for (int dir = 0; dir < 6; dir++) {
			int axis = dir / 2;
			int axisSign = (dir % 2 == 0) ? 1 : -1;

			Vector3Int nextPosition = model.ObjectToVoxelPosition(voxel.Position);
			nextPosition[axis] += axisSign;

			if (model.Get(nextPosition) == 0) {
				AddFace(model, voxel, dir);
			} else {
				RemoveFace(model, voxel, dir);
			}
		}
	}

	
	/// <summary>
	/// Add a face to a voxel in a specific direction. Does nothing if the face already exists
	/// </summary>
	/// <param name="model">The model of the voxel</param>
	/// <param name="voxel">A voxel</param>
	/// <param name="dir">The index of the direction, between 0 and 5(included)</param>
	private void AddFace(VoxModel model, VoxModel.Voxel voxel, int dir) {
		// check that the voxel position is registered in _positionToFaces
		Vector3Int voxelPosition = model.ObjectToVoxelPosition(voxel.Position);
		if (!_positionToFaces.ContainsKey(voxelPosition)) {
			_positionToFaces.Add(voxelPosition, new FaceIds());
		}
		
		// check that the face does not already exists
		FaceIds faceIds = _positionToFaces[voxelPosition];
		if (faceIds[dir] != -1) {
			return;
		}
		
		// Add the new RegisteredFace instance to faces 
		faceIds[dir] = faces.Count;
		faces.Add(new RegisteredFace { voxelPosition = voxelPosition, dir = dir });
		
		
		// setup some useful variables
		float halfSize = voxel.Size / 2;
		int axis = dir / 2;
		int axisSign = (dir % 2 == 0) ? 1 : -1;

		Vector3 tangent1 = Vector3.zero;
		Vector3 tangent2 = Vector3.zero;
		Vector3 faceDir = Vector3.zero;

		faceDir[axis] = axisSign;
		tangent1[(axis + 1) % 3] = 1;
		tangent2[(axis + 2) % 3] = 1;
		
		Vector3 faceCenter = voxel.Position + faceDir * halfSize;
		
		// create the 4 vertices of the face
		vertices.Add(faceCenter + (tangent1 + tangent2) * halfSize);
		vertices.Add(faceCenter + (-tangent1 + tangent2) * halfSize);
		vertices.Add(faceCenter + (tangent1 - tangent2) * halfSize);
		vertices.Add(faceCenter + (-tangent1 - tangent2) * halfSize);
		
		// create the uv of each corner (assume the face is filled with only one color)
		int colorId = voxel.Value - 1;
		Vector2 uv = new Vector2(colorId % 16, (int)((float)colorId / 16));
		uv += Vector2.one * 0.5f;
		uv /= 16;
		
		// add the normals and uv coords of the 4 vertices of the face
		for (int i = 0; i < 4; i++) {
			normals.Add(faceDir);
			uvs.Add(uv);
		}

		
		// create the 2 triangles of the face
		List<int> triangleSequenceOffset = new List<int> {
			3, 4, 2,
			2, 1, 3,
		};

		List<int> triangleSequenceOffsetReverse = new List<int>(triangleSequenceOffset);
		triangleSequenceOffsetReverse.Reverse();

		List<int> currentSequence = (axisSign == -1) ? triangleSequenceOffset : triangleSequenceOffsetReverse;
		foreach (int offset in currentSequence) {
			triangles.Add(vertices.Count - offset);
		}
	}

	
	/// <summary>
	/// Remove the face of a voxel from a specified direction
	/// </summary>
	/// <param name="model">The model of the voxel</param>
	/// <param name="voxel">A voxel</param>
	/// <param name="dir">The index of the direction, between 0 and 5(included)</param>
	void RemoveFace(VoxModel model, VoxModel.Voxel voxel, int dir) {
		// check that the voxel position is registered in _positionToFaces, which means it may have faces
		Vector3Int voxelPosition = model.ObjectToVoxelPosition(voxel.Position);
		if (!_positionToFaces.ContainsKey(voxelPosition)) {
			return;
		}
		
		// check that the face we want to remove exists
		FaceIds faceIds = _positionToFaces[voxelPosition];
		int faceId = faceIds[dir];
		if (faceId == -1) {
			return;
		}
		
		
		faceIds[dir] = -1;
		
		RegisteredFace replacementFace = faces[faces.Count - 1];
		faces[faceId] = replacementFace;
		faces.RemoveAt(faces.Count - 1);
		_positionToFaces[replacementFace.voxelPosition][replacementFace.dir] = faceId;
		
		int vertexCount = 4;
		int triangleCount = 6;
		int vertexStartIndex = faceId * vertexCount;
		int trianglesStartIndex = faceId * triangleCount;
		int vertexIndexDelta = vertices.Count - vertexCount - vertexStartIndex;
		
		// copy the vertices at the end of the lists over the ones of the face to remove
		for (int i = 0; i < vertexCount; i++) {
			int dstIndex = vertexStartIndex + i;
			int srcIndex = vertices.Count - vertexCount + i;
			vertices[dstIndex] = vertices[srcIndex];
			uvs[dstIndex] = uvs[srcIndex];
			normals[dstIndex] = normals[srcIndex];
		}
		
		// copy the triangles at the end of the triangles list over the triangles of the removed face
		for (int i = 0; i < triangleCount; i++) {
			int dstIndex = trianglesStartIndex + i;
			int srcIndex = triangles.Count - triangleCount + i;

			triangles[dstIndex] = triangles[srcIndex] - vertexIndexDelta;
		}
		
		// remove the face at the end of the list
		uvs.RemoveRange(vertices.Count - vertexCount, vertexCount);
		normals.RemoveRange(vertices.Count - vertexCount, vertexCount);
		vertices.RemoveRange(vertices.Count - vertexCount, vertexCount);
		triangles.RemoveRange(triangles.Count - triangleCount, triangleCount);
	}


	/// <summary>
	/// A small representation of a face. Contains the necessary data to locate the face Index in _positionToFaces
	/// </summary>
	[Serializable]
	private class RegisteredFace {
		// the position of the parent voxel (the face is directed outward)
		public Vector3Int voxelPosition;
		// the index of the face direction (between 0 and 5, included)
		public int dir;
	}
	
	
	/// <summary>
	/// A wrapper class over List<int> to represent a serializable array of face indexes,
	/// for each directions of a voxel.
	/// </summary>
	[Serializable]
	private class FaceIds {
		// the list of face indexes, with 6 elements. -1 means that there is no face in that direction.
		[SerializeField] private List<int> ids;
		
		/// <summary>
		/// Create a new instance of FaceIds, By default it represents a voxel with no faces
		/// </summary>
		public FaceIds() {
			ids = new List<int>(new[] { -1, -1, -1, -1, -1, -1 });
		}
		
		public int this[int index] {
			get { return ids[index]; }
			set { ids[index] = value; }
		}
	}
}
}