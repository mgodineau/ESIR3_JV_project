using System;
using System.Collections.Generic;
using System.Dynamic;
using DestructionSystem.Utils;
using UnityEngine;
using UnityEngine.Rendering;


namespace DestructionSystem {

[Serializable]
public class VoxBuilderMarchingCube : IVoxBuilder {
	
	
	[Serializable]
	private struct VertexLayout
	{
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
	private class MonitoredVertexList : Utils.MonitoredList<VertexLayout> {}
	
	
	// lists containing the data to build the mesh with
	[SerializeField] private List<int> triangles;
	[SerializeReference] private MonitoredVertexList vertices;

	[SerializeField] public int metaVoxelSize;
	[SerializeField] private Serializable3DByteArray metaVoxels;
	
	
	public VoxBuilderMarchingCube( int metaVoxelSize = 4 ) {
		
		triangles = new List<int>();
		vertices = new MonitoredVertexList();

		this.metaVoxelSize = metaVoxelSize;
		metaVoxels = new Serializable3DByteArray(Vector3Int.zero);
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
			if (currentChunkSize <= 0) { break;
			}
			
			mesh.SetVertexBufferData(vertices, startIndex, startIndex, currentChunkSize, 0, flags);
		}
		vertices.ClearUpdatedChunks();

		// int previousIndexCount = (int)mesh.GetIndexCount(0);
		mesh.SetIndexBufferParams( triangles.Count, IndexFormat.UInt32 );
		// if ( previousIndexCount < triangles.Count )
		// {
		// 	mesh.SetIndexBufferData(triangles, previousIndexCount, previousIndexCount, triangles.Count - previousIndexCount, flags);
		// }
		mesh.SetIndexBufferData( triangles, 0, 0, triangles.Count, flags );
		
		mesh.subMeshCount = 1;
		mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Count), flags);
		
		mesh.MarkModified();
		mesh.RecalculateNormals();
	}
	
	
	
	public void RefreshEntireModel(IVoxModel model) {
		
		Vector3 modelSize = model.BoundingBox;
		Vector3Int metaVoxelBoundingBox = Vector3Int.CeilToInt(modelSize / metaVoxelSize);
		metaVoxels = new Serializable3DByteArray(metaVoxelBoundingBox);

		
		
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

		vertices = new MonitoredVertexList();
		triangles.Clear();
		
		
		// build every meta mesh
		Vector3Int metaMeshPos = Vector3Int.zero;
		for (metaMeshPos.x = -1; metaMeshPos.x < metaVoxels.Dim.x; metaMeshPos.x++) {
			for (metaMeshPos.y = -1; metaMeshPos.y < metaVoxels.Dim.y; metaMeshPos.y++) {
				for (metaMeshPos.z = -1; metaMeshPos.z < metaVoxels.Dim.z; metaMeshPos.z++) {
					RefreshMetaMesh(model, metaMeshPos);
				}
			}
		}

	}
	
	public void RefreshRegion(IVoxModel model, Vector3 cornerLow, Vector3 cornerHigh) {
		RefreshEntireModel(model);
		// throw new System.NotImplementedException();
	}
	
	
	
	public void RefreshMetaMesh( IVoxModel model, Vector3Int metaMeshPos ) {
		
		// compute the pattern index
		byte patternIndex = 0;

		Vector3Int indexOffset = Vector3Int.zero;
		for ( indexOffset.x=0; indexOffset.x<=1; indexOffset.x++ ) {
			for (indexOffset.y = 0; indexOffset.y <= 1; indexOffset.y++) {
				for (indexOffset.z = 0; indexOffset.z <= 1; indexOffset.z++) {
					
					if ( !metaVoxels.AreIndexesValid(metaMeshPos + indexOffset) || metaVoxels[metaMeshPos + indexOffset] == 0 ) {
						int currentFlag = 1 << (indexOffset.x ^ indexOffset.y + 2 * indexOffset.y + 4 * indexOffset.z);
						patternIndex |= (byte)currentFlag;
					}
				}
			}
		}
		
		
		// update vertices
		byte[] currentTriangles = Utils.MarchingCubeTables.TriTable[patternIndex];
		
		// Debug.Log("case : " + patternIndex);
		
		Vector3 metaVoxelObjectPosition = model.VoxelToObjectPosition(metaMeshPos * metaVoxelSize);
		foreach ( byte vertexId in currentTriangles ) {
			VertexLayout currentVertex = new VertexLayout() {
				position = Utils.MarchingCubeTables.VertexOffsets[vertexId] * metaVoxelSize * model.VoxelSize + metaVoxelObjectPosition,
				normal = Vector3.up,
				uv = Vector2.zero
			};
			
			triangles.Add( vertices.Count );
			vertices.Add( currentVertex );
		}
		
		
		// update mesh
		
		
	}
	
	
}
}