using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxModel : ScriptableObject
{
	
	
	private VoxOctree root;
	
	private byte _depth;
	public byte Depth {
		get {return _depth;}
	}
	private float _voxelSize;
	public float VoxelSize {
		get {return _voxelSize;}
	}
	
	
	private Vector3 objectCenterOffset;
	private int sizeInVoxels;


	public VoxModel( Vector3 size, float voxelSize = 1 ) {
		_voxelSize = voxelSize;
		
		sizeInVoxels = (int)Mathf.Max(size.x, size.y, size.z);
		_depth = (byte)Mathf.CeilToInt( Mathf.Log(sizeInVoxels, 2) );
		sizeInVoxels = (int)Mathf.Pow(2, _depth);

		objectCenterOffset = size * _voxelSize * 0.5f;
		objectCenterOffset.y = 0;
		root = new VoxOctreeLeaf();
	}
	
	
	public void Set( Vector3 objectPosition, byte value ) {
		if ( _depth != 0 && root is VoxOctreeLeaf )
        {
			root = new VoxOctreeNode(root);
        }
		root.Set(ObjectToNormalizedPosition(objectPosition), _depth, value);
	}
	
	public byte Get(Vector3 objectPosition) {
		Vector3 normalizedPosition = ObjectToNormalizedPosition(objectPosition);
		return InUnitCube(normalizedPosition) ? root.Get(normalizedPosition) : (byte)0;
	}
	
	
	public Vector3 VoxelToObjectPosition(Vector3 voxelPosition) {
		return voxelPosition * _voxelSize - objectCenterOffset;
	}
	
	private Vector3 ObjectToNormalizedPosition(Vector3 objectPosition) {
		return (objectPosition + objectCenterOffset) / (sizeInVoxels * _voxelSize);
	}
	
	private Vector3 NormalizedToObjectPosition(Vector3 normalizedPosition) {
		return normalizedPosition * sizeInVoxels * _voxelSize - objectCenterOffset;
	}
	
	private bool InUnitCube(Vector3 position)
    {
		for(int i=0; i<3; i++)
        {
			if( position[i] < 0 || position[i] > 1 )
            {
				return false;
            }
        }
		return true;
    }


	internal void Set(VoxReader.Vector3 position, int v)
	{
		throw new NotImplementedException();
	}
	
	public LinkedList<Voxel> GetVoxels() {
		
		LinkedList<Voxel> voxels = new LinkedList<Voxel>();
		
		float objectSize = sizeInVoxels * _voxelSize;
		root.FillVoxelCollection( voxels, NormalizedToObjectPosition(Vector3.one * 0.5f), objectSize, _depth);
		return voxels;
	}


	public struct Voxel {
		
		public Vector3 position;
		public float size;
		public byte depth;
		public byte value;
		
		public Voxel(Vector3 position, float size, byte depth, byte value) {
			this.position = position;
			this.size = size;
			this.depth = depth;
			this.value = value;
		}
	}


	abstract class VoxOctree : ICloneable {
		
		
		public abstract byte Get( Vector3 normalizedPosition );
		
		public abstract void Set( Vector3 normalizedPosition, byte depth, byte value );
		
		public abstract void FillVoxelCollection( System.Collections.Generic.ICollection<Voxel> voxels, Vector3 objectPosition, float voxelSize, byte depth );

		public abstract object Clone();
    }


	class VoxOctreeNode : VoxOctree
	{
		public VoxOctree[] children;
		
		public VoxOctreeNode( VoxOctree[] children ) {
			this.children = children;
		}
		
		public VoxOctreeNode( VoxOctree child ) {
			children = new VoxOctree[8];
			for( int i=0; i<children.Length; i++ )
            {
				children[i] = (VoxOctree)child.Clone();
            }
		}
		
		public VoxOctreeNode( byte value ) : this(new VoxOctreeLeaf(value)) {}
		
		
		
		public override byte Get(Vector3 normalizedPosition)
		{
			byte childId = GetchildId(ref normalizedPosition);
			return children[childId].Get(normalizedPosition);
		}

		public override void Set(Vector3 normalizedPosition, byte depth, byte value)
		{
			byte childId = GetchildId(ref normalizedPosition);
			
			if(depth == 1) {
				children[childId] = new VoxOctreeLeaf(value);
				return;
			}
			
			if( children[childId] is VoxOctreeLeaf ) {
				children[childId] = new VoxOctreeNode(children[childId]);
			}
			
			depth--;
			children[childId].Set(normalizedPosition, depth, value);
		}





		public override void FillVoxelCollection( System.Collections.Generic.ICollection<Voxel> voxels, Vector3 objectPosition, float voxelSize, byte depth ) {
			
			byte nextDepth = (byte)(depth - 1);
			float nextVoxelSize = voxelSize / 2;
			float positionOffset = nextVoxelSize / 2;
			
			int[] offsets = {-1, 1};
			for( int i=0; i<8; i++ ) {
				int xOffset = offsets[i & 1];
				int yOffset = offsets[(i>>1) & 1];
				int zOffset = offsets[(i>>2) & 1];
				
				Vector3 nextPosition = objectPosition;
				nextPosition += Vector3.right * xOffset * positionOffset;
				nextPosition += Vector3.up * yOffset * positionOffset;
				nextPosition += Vector3.forward * zOffset * positionOffset;
				
				children[i].FillVoxelCollection(voxels, nextPosition, nextVoxelSize, nextDepth);
			}
			
		}
		
		public override object Clone()
		{
			return new VoxOctreeNode((VoxOctree[])children.Clone());
		}
		
		private byte GetchildId(ref Vector3 normalizedPosition) {
			byte id = 0;
			byte powerOf2 = 1;
			for( int i=0; i<3; i++ ) {
				if (normalizedPosition[i] >= 0.5f) {
					id += powerOf2;
					normalizedPosition[i] -= 0.5f;
				}
				powerOf2 *= 2;
			}
			normalizedPosition *= 2;
			
			return id;
		}

        
    }


	class VoxOctreeLeaf : VoxOctree
	{
		public byte value;
		
		public VoxOctreeLeaf(byte value = 0) {
			this.value = value;
		}
		
		
		public override byte Get(Vector3 normalizedPosition)
		{
			return value;
		}

		public override void Set(Vector3 normalizedPosition, byte depth, byte value)
		{
			this.value = value;
		}
		
		
		public override void FillVoxelCollection( System.Collections.Generic.ICollection<Voxel> voxels, Vector3 objectPosition, float voxelSize, byte depth ) {
			if (value == 0)
            {
				return;
            }
			
			Voxel voxel = new Voxel();
			voxel.depth = depth;
			voxel.value = value;
			voxel.size = voxelSize;
			voxel.position =  objectPosition;
			
			voxels.Add(voxel);
		}

        public override object Clone()
        {
			return new VoxOctreeLeaf(value);
        }
    }

}
