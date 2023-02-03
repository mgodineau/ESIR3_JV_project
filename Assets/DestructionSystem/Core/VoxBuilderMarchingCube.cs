using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using DestructionSystem.Utils;
using Unity.VisualScripting;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;


namespace DestructionSystem {

[Serializable]
public class VoxBuilderMarchingCube : IVoxBuilder {
	
	
	[Serializable]
	private struct VertexLayout {
		public static readonly VertexAttributeDescriptor[] Layout = {
			new(VertexAttribute.Position),
			new(VertexAttribute.Normal),
			new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
		};
		
		public Vector3 position;
		public Vector3 normal;
		public Vector2 uv;
	}


	[Serializable]
	private class VertexBuildData {

		public List<int> triangleIds;
		public int edgeDir;
		public Vector3Int metaEdgePos;
		
		public VertexBuildData( int edgeDir, Vector3Int metaEdgePos ) {
			triangleIds = new List<int>();
			this.edgeDir = edgeDir;
			this.metaEdgePos = metaEdgePos;
		}
	}

	[Serializable]
	private struct MetaMesh {
		public byte pattern;
		public List<int> trianglesIds;
	}
	
	
	[Serializable]
	private class MonitoredVertexList : Utils.MonitoredList<VertexLayout> {}
	
	[Serializable]
	private class MonitoredIndexList : Utils.MonitoredList<int> {}

	[Serializable]
	private class Serializable3DMetaMeshArray : Utils.Serializable3DArray<MetaMesh> {
		
		[SerializeField] private List<MetaMesh> _data; 
		protected override List<MetaMesh> Data {
			get => _data;
			set => _data = value;
		}
		
		public Serializable3DMetaMeshArray(Vector3Int dim) : this(dim, new MetaMesh(){pattern = 0, trianglesIds = new List<int>()} ) {}
		public Serializable3DMetaMeshArray(Vector3Int dim, MetaMesh defaultValue) : base(dim, defaultValue) {}
	}
	
	
	// lists containing the data to build the mesh with
	[SerializeReference] private MonitoredIndexList triangles;
	[SerializeReference] private MonitoredVertexList vertices;
	[SerializeField] private List<VertexBuildData> verticesToTriangles;

	[SerializeField] public int metaVoxelSize;
	[SerializeField] private Serializable3DIntArray metaVoxels;
	
	[SerializeField] private Serializable3DMetaMeshArray metaMeshToTriangles;

	[SerializeField] private Serializable3DIntArray[] vertexIndexes;
	
	
	
	public VoxBuilderMarchingCube( int metaVoxelSize = 4 ) {
		
		triangles = new MonitoredIndexList();
		vertices = new MonitoredVertexList();
		verticesToTriangles = new List<VertexBuildData>();

		this.metaVoxelSize = metaVoxelSize;
		metaVoxels = new Serializable3DIntArray(Vector3Int.zero);
		metaMeshToTriangles = new Serializable3DMetaMeshArray(Vector3Int.zero);
		
		vertexIndexes = new Serializable3DIntArray[3];
	}

	
	
	public Mesh GetMesh() {
		Mesh mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
		mesh.MarkDynamic();
		
		mesh.SetVertexBufferParams(vertices.Count, VertexLayout.Layout);
		mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count);
		
		mesh.SetIndexBufferParams( triangles.Count, IndexFormat.UInt32 );
		mesh.SetIndexBufferData( triangles, 0, 0, triangles.Count );
		
		mesh.subMeshCount = 1;
		mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Count));
		
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}
	
	// TODO refactor
	public void UpdateMesh(Mesh mesh) {
		MeshUpdateFlags flags =
			MeshUpdateFlags.DontNotifyMeshUsers
			| MeshUpdateFlags.DontRecalculateBounds
			| MeshUpdateFlags.DontValidateIndices;
		
		mesh.SetVertexBufferParams(vertices.Count, VertexLayout.Layout);
		foreach ( KeyValuePair<int, int> chunkLocationAndSize in vertices.GetUpdatedChunks())
		{
			int startIndex = chunkLocationAndSize.Key * vertices.chunkSize;
			int currentChunkSize = chunkLocationAndSize.Value * vertices.chunkSize;
			currentChunkSize = Mathf.Min(currentChunkSize, vertices.Count - startIndex);
			if (currentChunkSize <= 0) { 
				break;
			}
			
			mesh.SetVertexBufferData(vertices, startIndex, startIndex, currentChunkSize, 0, flags);
		}
		vertices.ClearUpdatedChunks();

		
		//TODO update indexes by chunks
		mesh.SetIndexBufferParams( triangles.Count, IndexFormat.UInt32 );
		foreach (KeyValuePair<int, int> chunkLocationAndSize in triangles.GetUpdatedChunks()) {
			int startIndex = chunkLocationAndSize.Key * triangles.chunkSize;
			int currentChunkSize = chunkLocationAndSize.Value * triangles.chunkSize;
			currentChunkSize = Mathf.Min(currentChunkSize, triangles.Count - startIndex);
			if (currentChunkSize <= 0) { 
				break;
			}
			
			mesh.SetIndexBufferData(triangles, startIndex, startIndex, currentChunkSize, flags);
		}
		triangles.ClearUpdatedChunks();
		
		
		mesh.subMeshCount = 1;
		mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Count), flags);
		
		mesh.MarkModified();
		mesh.RecalculateNormals();
	}
	
	
	
	public void RefreshEntireModel(IVoxModel model) {
		
		Vector3 modelSize = model.BoundingBox;
		Vector3Int metaVoxelBoundingBox = Vector3Int.CeilToInt(modelSize / metaVoxelSize);
		metaVoxels = new Serializable3DIntArray(metaVoxelBoundingBox);
		metaMeshToTriangles = new Serializable3DMetaMeshArray( metaVoxelBoundingBox + Vector3Int.one );
		verticesToTriangles = new List<VertexBuildData>();
		
		vertexIndexes = new Serializable3DIntArray[3]{ 
			new (metaVoxelBoundingBox+Vector3Int.one*2, -1),
			new (metaVoxelBoundingBox+Vector3Int.one*2, -1),
			new (metaVoxelBoundingBox+Vector3Int.one*2, -1)
		};
		
		
		// build the array of meta voxels
		Vector3Int voxelPos = Vector3Int.zero;
		for (voxelPos.x = 0; voxelPos.x < model.BoundingBox.x; voxelPos.x++) {
			for (voxelPos.y = 0; voxelPos.y < model.BoundingBox.y; voxelPos.y++) {
				for (voxelPos.z = 0; voxelPos.z < model.BoundingBox.z; voxelPos.z++) {
					if ( model.Get(voxelPos) != 0 ) {
						Vector3Int metaPos = voxelPos / metaVoxelSize;
						metaVoxels[metaPos]++;
					}
				}
			}
		}
		
		vertices.Clear();
		triangles.Clear();
		
		
		// build every meta mesh
		Vector3Int metaMeshPos = Vector3Int.zero;
		for (metaMeshPos.x = 0; metaMeshPos.x < metaMeshToTriangles.Dim.x; metaMeshPos.x++) {
			for (metaMeshPos.y = 0; metaMeshPos.y < metaMeshToTriangles.Dim.y; metaMeshPos.y++) {
				for (metaMeshPos.z = 0; metaMeshPos.z < metaMeshToTriangles.Dim.z; metaMeshPos.z++) {
					RefreshMetaMesh(model, metaMeshPos);
				}
			}
		}

	}
	
	public void RefreshRegion(IVoxModel model, Vector3 cornerLow, Vector3 cornerHigh) {

		Vector3Int voxelCornerLow = model.ObjectToVoxelPosition(cornerLow);
		Vector3Int voxelCornerHigh = model.ObjectToVoxelPosition(cornerHigh);
		
		Vector3Int metaCornerLow = Vector3Int.FloorToInt( voxelCornerLow / metaVoxelSize );
		Vector3Int metaCornerHigh = Vector3Int.CeilToInt( voxelCornerHigh / metaVoxelSize );
		metaCornerLow = Vector3Int.Max(metaCornerLow, Vector3Int.zero);
		metaCornerHigh = Vector3Int.Min( metaCornerHigh, metaVoxels.Dim-Vector3Int.one );
		
		// refersh metaVoxels
		Vector3Int metaVoxelPos = Vector3Int.zero;
		for ( metaVoxelPos.x = metaCornerLow.x; metaVoxelPos.x <= metaCornerHigh.x; metaVoxelPos.x++ ) {
			for (metaVoxelPos.y = metaCornerLow.y; metaVoxelPos.y <= metaCornerHigh.y; metaVoxelPos.y++) {
				for (metaVoxelPos.z = metaCornerLow.z; metaVoxelPos.z <= metaCornerHigh.z; metaVoxelPos.z++) {
					metaVoxels[metaVoxelPos] = CountVoxels(model, metaVoxelPos);
				}
			}
		}
		
		
		
		// build every meta mesh
		Vector3Int metaMeshPos = Vector3Int.zero;
		for (metaMeshPos.x = metaCornerLow.x; metaMeshPos.x <= metaCornerHigh.x+1; metaMeshPos.x++) {
			for (metaMeshPos.y = metaCornerLow.y; metaMeshPos.y <= metaCornerHigh.y+1; metaMeshPos.y++) {
				for (metaMeshPos.z = metaCornerLow.z; metaMeshPos.z <= metaCornerHigh.z+1; metaMeshPos.z++) {
					RefreshMetaMesh(model, metaMeshPos);
				}
			}
		}
		
	}
	
	
	
	private byte CountVoxels( IVoxModel model, Vector3Int metaVoxelPos ) {
		
		byte voxelCount = 0;

		Vector3Int voxelBasePos = metaVoxelPos * metaVoxelSize;
		Vector3Int offset = Vector3Int.zero;
		for ( offset.x = 0; offset.x < metaVoxelSize; offset.x++ ) {
			for (offset.y = 0; offset.y < metaVoxelSize; offset.y++) {
				for (offset.z = 0; offset.z < metaVoxelSize; offset.z++) {
					if ( model.Get( voxelBasePos + offset ) != 0 ) {
						voxelCount++;
					}
				}
			}
		}
		
		return voxelCount;
	}
	
	
	
	private void RefreshMetaMesh( IVoxModel model, Vector3Int metaMeshPos ) {
		
		// compute the pattern index
		byte patternIndex = 0;
		Vector3Int indexOffset = Vector3Int.zero;
		for ( indexOffset.x=0; indexOffset.x <= 1; indexOffset.x++ ) {
			for (indexOffset.y = 0; indexOffset.y <= 1; indexOffset.y++) {
				for (indexOffset.z = 0; indexOffset.z <= 1; indexOffset.z++) {

					Vector3Int metaVoxelPos = metaMeshPos + indexOffset - Vector3Int.one;
					if ( !metaVoxels.AreIndexesValid(metaVoxelPos) || metaVoxels[metaVoxelPos] == 0 ) {
						int currentFlag = 1 << (indexOffset.x ^ indexOffset.y + 2 * indexOffset.y + 4 * indexOffset.z);
						patternIndex |= (byte)currentFlag;
					}
				}
			}
		}

		
		
		// update vertices
		byte[] currentTriangles = Utils.MarchingCubeTables.TriTable[patternIndex];
		Dictionary<byte, int> vertexLocalToGlobal = RefreshVertices(model, currentTriangles, metaMeshPos);





		// update triangles
		if ( patternIndex == metaMeshToTriangles[metaMeshPos].pattern ) {
			return;
		}

		foreach ( int triId in metaMeshToTriangles[metaMeshPos].trianglesIds ) {
			RemoveTriangle(triId);
		}

		int[] currentTrianglesGlobal = Array.ConvertAll(currentTriangles, i => vertexLocalToGlobal[i]);
		for( int i=0; i<currentTrianglesGlobal.Length; i+=3 ) {
			AddTriangle( new int[]{currentTrianglesGlobal[i], currentTrianglesGlobal[i+1], currentTrianglesGlobal[i+2]} );
		}
		
		
		List<int> trianglesIds = new List<int>();
		for ( int i=0; i<currentTriangles.Length; i+=3 ) {
			trianglesIds.Add(triangles.Count-currentTriangles.Length + i );
		}
		metaMeshToTriangles[metaMeshPos] = new MetaMesh() { pattern = patternIndex, trianglesIds = trianglesIds };

	}
	
	
	Dictionary<byte, int> RefreshVertices(IVoxModel model, byte[] localIndexes, Vector3Int metaMeshPos) {

		Dictionary<byte, int> vertexLocalToGlobal = new Dictionary<byte, int>();
		
		for ( byte i=0; i<12; i++ ) {
			var (edgeDir, metaEdgePos) = GetVertexLocation(metaMeshPos, i);

			if ( !Array.Exists(localIndexes, x => x == i) ) {
				RemoveVertex(metaEdgePos, edgeDir);
			}
		}

		for ( byte i=0; i<12; i++ ) {
			var (edgeDir, metaEdgePos) = GetVertexLocation(metaMeshPos, i);

			if (Array.Exists(localIndexes, x => x == i)) {
				vertexLocalToGlobal[i] = AddVertex(model, metaEdgePos, edgeDir);
			}
		}

		return vertexLocalToGlobal;
	}

	private static (int edgeDir, Vector3Int metaEdgePos) GetVertexLocation(Vector3Int metaMeshPos, byte i) {
		Vector3 localOffset = Utils.MarchingCubeTables.VertexOffsets[i];
		int edgeDir = 0;
		for (int j = 0; j < 3; j++) {
			if (Mathf.RoundToInt(localOffset[j] * 2) == 1) {
				edgeDir = j;
			}
		}

		Vector3Int metaEdgePos = metaMeshPos;
		for (int j = 0; j < 3; j++) {
			if (Mathf.RoundToInt(localOffset[j]) == 1) {
				metaEdgePos[j]++;
			}
		}

		return (edgeDir, metaEdgePos);
	}


	int AddVertex( IVoxModel model, Vector3Int metaPos, int edgeDir ) {
		
		if ( vertexIndexes[edgeDir][metaPos] == -1 ) {

			vertexIndexes[edgeDir][metaPos] = vertices.Count;
			vertices.Add( new VertexLayout() );
			verticesToTriangles.Add(new VertexBuildData(edgeDir, metaPos));
		}
		
		int vertexId = vertexIndexes[edgeDir][metaPos];
		
		Vector3 offset = Vector3.zero;
		offset[edgeDir] = 0.5f;
		
		Vector3 vertexPosition = model.VoxelToObjectPosition(metaPos * metaVoxelSize) + offset * metaVoxelSize * model.VoxelSize;
		VertexLayout vertex = new VertexLayout() { position = vertexPosition, normal = Vector3.up, uv=Vector2.zero};
		vertices[vertexId] = vertex;
		
		return vertexId;
	}


	private void RemoveVertex(Vector3Int metaMeshPos, int edgeDir) {
		
		
		int vertexId = vertexIndexes[edgeDir][metaMeshPos];
		
		if (vertexId == -1) {
			return;
		}
		
		vertices[vertexId] = vertices[^1];
		verticesToTriangles[vertexId] = verticesToTriangles[^1];
		vertexIndexes[verticesToTriangles[vertexId].edgeDir][verticesToTriangles[vertexId].metaEdgePos] = vertexId;

		
		foreach ( int triId in verticesToTriangles[vertexId].triangleIds ) {
			
			for ( int i=triId; i<triId+3; i++ ) {
				if (triangles[i] == vertices.Count-1 ) {
					triangles[i] = vertexId;
				}
			}
		}
		
		vertexIndexes[edgeDir][metaMeshPos] = -1;
		vertices.RemoveAt(vertices.Count-1);
		verticesToTriangles.RemoveAt(verticesToTriangles.Count-1);
	}
	
	
	
	private void AddTriangle( int[] verticesIds ) {
		
		triangles.AddRange( verticesIds );
		

		int triId = triangles.Count - 3;
		foreach ( int vertexId in verticesIds ) {
			verticesToTriangles[vertexId].triangleIds.Add(triId);
		}
	}
	
	private void RemoveTriangle( int triIndex ) {

		for ( int i=0; i<3; i++ ) {
			verticesToTriangles[triangles[triIndex + i]].triangleIds.Remove(triIndex);
			triangles[triIndex + i] = triangles[triangles.Count - 3 + i];

			int tmp = verticesToTriangles[triangles[triIndex + i]].triangleIds.IndexOf(triangles.Count - 3);
			verticesToTriangles[triangles[triIndex + i]].triangleIds[tmp] = triIndex;
		}
		
		triangles.RemoveRange(triangles.Count-3, 3);
	}
	

}
}