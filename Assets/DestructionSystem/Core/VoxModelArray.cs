using System;
using System.Collections.Generic;
using UnityEngine;

namespace DestructionSystem
{
	[Serializable]
	public class VoxModelArray : ScriptableObject, IVoxModel
	{
		
		
		
		public Vector3Int BoundingBox => data.Dim;
		
		[SerializeField] private float voxelSize;
		public float VoxelSize => voxelSize;
		
		[SerializeField] private Vector3 objectCenterOffset;
		
		
		[SerializeField] Utils.Serializable3DByteArray data;
		
		
		public VoxModelArray()
		{
			data = new Utils.Serializable3DByteArray(Vector3Int.zero);
		}
		
		public void SetSize(Vector3Int boundingBox, float size = 1)
		{
			voxelSize = size;

			data = new Utils.Serializable3DByteArray(boundingBox);
			
			objectCenterOffset = (Vector3)boundingBox * voxelSize * 0.5f;
			objectCenterOffset.y = 0;

		}
		
		
		public void Set(Vector3Int voxelPosition, byte value)
		{
			if ( data.AreIndexesValid(voxelPosition) ) {
				data[voxelPosition] = value;
			}
			
		}
		
		public void Set(Vector3 objectPosition, byte value)
		{
			Set( ObjectToVoxelPosition(objectPosition), value );
		}

		public byte Get(Vector3Int voxelPosition)
		{
			return data.AreIndexesValid(voxelPosition) 
				? data[voxelPosition.x, voxelPosition.y, voxelPosition.z] 
				: (byte)0;
		}

		public byte Get(Vector3 objectPosition)
		{
			return Get(ObjectToVoxelPosition(objectPosition));
		}

		public Vector3 VoxelToObjectPosition(Vector3Int voxelPosition)
		{
			return ((Vector3)voxelPosition) * voxelSize - objectCenterOffset;
		}

		public Vector3Int ObjectToVoxelPosition(Vector3 objectPosition)
		{
			Vector3 voxelPosition = (objectPosition + objectCenterOffset) / voxelSize;
			return Vector3Int.FloorToInt(voxelPosition);
		}

		public LinkedList<VoxModelOctree.Voxel> GetVoxels()
		{
			return GetVoxelsBetween( Vector3Int.zero, BoundingBox-Vector3Int.one );
		}


		public LinkedList<VoxModelOctree.Voxel> GetVoxelsBetween(Vector3 cornerLow, Vector3 cornerHigh,
			bool includeEmpty = false)
		{
			return GetVoxelsBetween(
				ObjectToVoxelPosition(cornerLow),
				ObjectToVoxelPosition(cornerHigh),
				includeEmpty
				);
		}
		
		public LinkedList<VoxModelOctree.Voxel> GetVoxelsBetween(Vector3Int cornerLow, Vector3Int cornerHigh, bool includeEmpty = false)
		{
			LinkedList<VoxModelOctree.Voxel> voxels = new LinkedList<VoxModelOctree.Voxel>();
			Vector3Int pos = Vector3Int.zero;
			
			for ( pos.x=cornerLow.x; pos.x<=cornerHigh.x; pos.x++ )
			{
				for (pos.y = cornerLow.y; pos.y <= cornerHigh.y; pos.y++)
				{
					for (pos.z = cornerLow.z; pos.z <= cornerHigh.z; pos.z++)
					{
						byte value = Get(pos);
						if ( value != 0 || includeEmpty )
						{
							voxels.AddLast(new VoxModelOctree.Voxel(position:VoxelToObjectPosition(pos), size:voxelSize, depth:0, value:value) );
						}
					}
				}
			}


			return voxels;
		}
	}
}