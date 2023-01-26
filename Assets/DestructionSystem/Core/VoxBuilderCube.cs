using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

namespace DestructionSystem {
[CreateAssetMenu(menuName = "ScriptableObjects/VoxBuilderCube")]
public class VoxBuilderCube : ScriptableObject, ISerializationCallbackReceiver {
	
	[Serializable]
	private class MonitoredVertexList : Utils.MonitoredList<VertexLayout> {}

	// dictionary that maps voxels positions to indexes of their faces in the "faces" list
	private readonly Dictionary<Vector3Int, FaceIds> _positionToFaces;
	
	// lists used to serialize _positionToFaces
	[SerializeField] private List<Vector3Int> positionToFacesSerializedKeys;
	[SerializeField] private List<FaceIds> positionToFacesSerializedValues;
	
	// lists containing the data to build the mesh with
	[SerializeField] private List<int> triangles;
	// [SerializeField] private List<VertexLayout> vertices;
	[SerializeReference] private MonitoredVertexList vertices;
	
	// list of faces, on which faces are in the same order as in the previous lists
	[SerializeField] private List<RegisteredFace> faces;

	[SerializeField] public UVMappingStrategy uvMapping;
	
	
	
	/// <summary>
	/// Build an instance of VoxBuilderCube
	/// </summary>
	public VoxBuilderCube() {
		_positionToFaces = new Dictionary<Vector3Int, FaceIds>();
		
		triangles = new List<int>();
		vertices = new MonitoredVertexList();
		
		faces = new List<RegisteredFace>();
		
		positionToFacesSerializedKeys = new List<Vector3Int>();
		positionToFacesSerializedValues = new List<FaceIds>();

		uvMapping = UVMappingStrategy.UsePalette;
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
		mesh.MarkDynamic();
		
		mesh.SetVertexBufferParams(vertices.Count, VertexLayout.layout);
		mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count);
		
		mesh.SetIndexBufferParams( triangles.Count, IndexFormat.UInt32 );
		mesh.SetIndexBufferData( triangles, 0, 0, triangles.Count );
		
		mesh.subMeshCount = 1;
		mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Count));
		
		mesh.RecalculateBounds();
		return mesh;
	}

	public void UpdateMesh(Mesh mesh)
	{

		MeshUpdateFlags flags =
			MeshUpdateFlags.DontNotifyMeshUsers
			| MeshUpdateFlags.DontRecalculateBounds
			| MeshUpdateFlags.DontValidateIndices;
		
		mesh.SetVertexBufferParams(vertices.Count, VertexLayout.layout);
		foreach ( KeyValuePair<int, int> chunkLocationAndSize in vertices.GetUpdatedChunks())
		{
			int startIndex = chunkLocationAndSize.Key * vertices.chunkSize;
			int currentChunkSize = chunkLocationAndSize.Value * vertices.chunkSize;
			currentChunkSize = Mathf.Min(currentChunkSize, vertices.Count - startIndex);
			if (currentChunkSize <= 0) { break;
			}
			
			mesh.SetVertexBufferData(vertices, startIndex, startIndex, currentChunkSize, 0, flags);
		}
		vertices.ClearUpdatedChunks();

		int previousIndexCount = (int)mesh.GetIndexCount(0);
		mesh.SetIndexBufferParams( triangles.Count, IndexFormat.UInt32 );
		if ( previousIndexCount < triangles.Count )
		{
			mesh.SetIndexBufferData(triangles, previousIndexCount, previousIndexCount, triangles.Count - previousIndexCount, flags);
		}
		
		mesh.subMeshCount = 1;
		mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Count), flags);
		
		mesh.MarkModified();
	}
	
	
	
	/// <summary>
	/// Clear the mesh data and rebuilt it according to the specified model
	/// </summary>
	/// <param name="modelOctree">The model to build the mesh with</param>
	public void RefreshEntireModel(IVoxModel modelOctree) {
		ClearMesh();

		LinkedList<VoxModelOctree.Voxel> voxels = modelOctree.GetVoxels();

		foreach (VoxModelOctree.Voxel voxel in voxels) {
			for (int dir = 0; dir < 6; dir++) {
				int axis = dir / 2;
				int axisSign = (dir % 2 == 0) ? 1 : -1;
				Vector3 nextPosition = voxel.Position;
				nextPosition[axis] += axisSign * voxel.Size;
				if (modelOctree.Get(nextPosition) == 0) {
					AddFace(modelOctree, voxel, dir);
				}
			}
		}
	}
	
	
	/// <summary>
	/// Update only a small part of the mesh, according to a specified model. The updated part is a rectangular cuboid.
	/// </summary>
	/// <param name="modelOctree">The model to read the updates from</param>
	/// <param name="cornerLow">The lower corner of the area to update, in object coordinates.</param>
	/// <param name="cornerHigh">The upper corner of the area to update, in object coordinates.</param>
	public void RefreshRegion(IVoxModel modelOctree, Vector3 cornerLow, Vector3 cornerHigh) {
		LinkedList<VoxModelOctree.Voxel> voxels = modelOctree.GetVoxelsBetween(cornerLow, cornerHigh, true);

		foreach (VoxModelOctree.Voxel voxel in voxels)
		{
			if (voxel.Value == 0)
			{
				RemoveVoxelFaces(modelOctree, voxel);
			}
			else
			{
				AddVoxelFaces(modelOctree, voxel);
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
		faces.Clear();
	}
	
	
	/// <summary>
	/// Remove every faces of a voxel. Does nothing for faces that did not exists.
	/// </summary>
	/// <param name="modelOctree">The model of the voxel</param>
	/// <param name="voxel">A voxel</param>
	private void RemoveVoxelFaces(IVoxModel modelOctree, VoxModelOctree.Voxel voxel) {
		Vector3Int voxelPosition = modelOctree.ObjectToVoxelPosition(voxel.Position);
		if (!_positionToFaces.ContainsKey(voxelPosition)) {
			return;
		}

		FaceIds faceId = _positionToFaces[voxelPosition];
		for (int dir = 0; dir < 6; dir++) {
			if (faceId[dir] != -1) {
				RemoveFace(modelOctree, voxel, dir);
				faceId[dir] = -1;
			}
		}

		_positionToFaces.Remove(voxelPosition);
	}

	
	/// <summary>
	/// Add every visible faces of a given voxel, and remove invisible faces.
	/// </summary>
	/// <param name="modelOctree">The model of the voxel</param>
	/// <param name="voxel">A voxel</param>
	private void AddVoxelFaces(IVoxModel modelOctree, VoxModelOctree.Voxel voxel) {
		for (int dir = 0; dir < 6; dir++) {
			int axis = dir / 2;
			int axisSign = (dir % 2 == 0) ? 1 : -1;

			Vector3Int nextPosition = modelOctree.ObjectToVoxelPosition(voxel.Position);
			nextPosition[axis] += axisSign;

			if (modelOctree.Get(nextPosition) == 0) {
				AddFace(modelOctree, voxel, dir);
			} else {
				RemoveFace(modelOctree, voxel, dir);
			}
		}
	}

	
	/// <summary>
	/// Add a face to a voxel in a specific direction. Does nothing if the face already exists
	/// </summary>
	/// <param name="modelOctree">The model of the voxel</param>
	/// <param name="voxel">A voxel</param>
	/// <param name="dir">The index of the direction, between 0 and 5(included)</param>
	private void AddFace(IVoxModel modelOctree, VoxModelOctree.Voxel voxel, int dir) {
		// check that the voxel position is registered in _positionToFaces
		Vector3Int voxelPosition = modelOctree.ObjectToVoxelPosition(voxel.Position);
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
		List<Vector3> positions = new List<Vector3>();
		positions.Add(faceCenter + (tangent1 + tangent2) * halfSize);
		positions.Add(faceCenter + axisSign * (tangent1 - tangent2) * halfSize);
		positions.Add(faceCenter + axisSign * (-tangent1 + tangent2) * halfSize);
		positions.Add(faceCenter + (-tangent1 - tangent2) * halfSize);
		
		// create the uv of each corner (assume the face is filled with only one color)
		Vector2[] uvs = CreateFaceUVs(modelOctree, voxel, positions);

		// add the normals and uv coords of the 4 vertices of the face
		for (int i = 0; i<positions.Count; i++) {
			vertices.Add( new VertexLayout(){position = positions[i], normal = faceDir, uv = uvs[i]} );
		}

		
		// create the 2 triangles of the face
		List<int> triangleSequenceOffset = new List<int> {
			3, 4, 2,
			2, 1, 3,
		};
		
		foreach (int offset in triangleSequenceOffset) {
			triangles.Add(vertices.Count - offset);
		}
	}

	private Vector2[] CreateFaceUVs( IVoxModel modelOctree, VoxModelOctree.Voxel voxel, List<Vector3> positions)
	{
		switch (uvMapping)
		{
			case UVMappingStrategy.UsePalette:
				int colorId = voxel.Value - 1;
				Vector2 uvPal = new Vector2(colorId % 16, (int)((float)colorId / 16));
				uvPal += Vector2.one * 0.5f;
				uvPal /= 16;
				return new Vector2[] { uvPal, uvPal, uvPal, uvPal};
			
			case UVMappingStrategy.ProjectionXZ:
				Vector3Int positionVox = modelOctree.ObjectToVoxelPosition(voxel.Position);
				Vector2 uvProj = new Vector2( 
					(float)positionVox.x / modelOctree.BoundingBox.x,
					(float)positionVox.z / modelOctree.BoundingBox.z);
				return new Vector2[] { uvProj, uvProj, uvProj, uvProj};

			case UVMappingStrategy.UseFullTexture:
				return new Vector2[]
				{
					Vector2.zero,
					Vector2.right,
					Vector2.up, 
					Vector2.one
				};
		}

		return new Vector2[]{};
	}
	
	
	/// <summary>
	/// Remove the face of a voxel from a specified direction
	/// </summary>
	/// <param name="modelOctree">The model of the voxel</param>
	/// <param name="voxel">A voxel</param>
	/// <param name="dir">The index of the direction, between 0 and 5(included)</param>
	private void RemoveFace(IVoxModel modelOctree, VoxModelOctree.Voxel voxel, int dir) {
		// check that the voxel position is registered in _positionToFaces, which means it may have faces
		Vector3Int voxelPosition = modelOctree.ObjectToVoxelPosition(voxel.Position);
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
			// updatedChunksIndexes.Add( dstIndex / chunkSize );
		}
		
		// copy the triangles at the end of the triangles list over the triangles of the removed face
		for (int i = 0; i < triangleCount; i++) {
			int dstIndex = trianglesStartIndex + i;
			int srcIndex = triangles.Count - triangleCount + i;

			triangles[dstIndex] = triangles[srcIndex] - vertexIndexDelta;
		}
		
		// remove the face at the end of the list
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

	[Serializable]
	private struct VertexLayout
	{
		public static readonly VertexAttributeDescriptor[] layout = new VertexAttributeDescriptor[]
		{
			new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
			new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
		};
		
		public Vector3 position;
		public Vector3 normal;
		public Vector2 uv;
	}

	[Serializable]
	public enum UVMappingStrategy
	{
		UsePalette,
		UseFullTexture,
		ProjectionXZ
	}
	
	
}
}