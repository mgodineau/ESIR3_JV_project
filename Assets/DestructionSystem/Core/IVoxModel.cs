using UnityEngine;
using System.Collections.Generic;


namespace DestructionSystem
{
	
	public interface IVoxModel
	{
		
		public Vector3Int BoundingBox { get; }
		public float VoxelSize { get; }
		
		public void SetSize(Vector3Int boundingBox, float size = 1);


		public void Set(Vector3 objectPosition, byte value);

		public byte Get(Vector3Int voxelPosition);

		public byte Get(Vector3 objectPosition);


		public Vector3 VoxelToObjectPosition(Vector3Int voxelPosition);

		public Vector3Int ObjectToVoxelPosition(Vector3 objectPosition);
		
		
		
		public LinkedList<VoxModelOctree.Voxel> GetVoxels();
		
		
		public LinkedList<VoxModelOctree.Voxel> GetVoxelsBetween(Vector3 cornerLow, Vector3 cornerHigh,
			bool includeEmpty = false);
		
		public LinkedList<VoxModelOctree.Voxel> GetVoxelsBetween(Vector3Int cornerLow, Vector3Int cornerHigh,
			bool includeEmpty = false);


	}
}