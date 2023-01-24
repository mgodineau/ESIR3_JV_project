using System;
using System.Collections.Generic;
using UnityEngine;

namespace DestructionSystem {
public class VoxModel : ScriptableObject, ISerializationCallbackReceiver
{
	private VoxOctree _root;
	[SerializeField] private List<SerializableVoxOctree> voxOctreeSerialized;
	
	public string content = "default content";
	
	[SerializeField] private byte depth;
	public byte Depth => depth;


	[SerializeField] private Vector3Int boundingBox;
	public Vector3Int BoundingBox => boundingBox;
	
	[SerializeField] private float voxelSize;
	public float VoxelSize => voxelSize;
	
	[SerializeField] private Vector3 objectCenterOffset;
	
	[SerializeField] private int sizeInVoxels;
	public int SizeInVoxels => sizeInVoxels;
	
	public VoxModel() {
		voxOctreeSerialized = new List<SerializableVoxOctree>();
	}
	
	public void SetSize( Vector3Int boundingBox, float size = 1 ) {
		content = "des trucs et des machins";

		this.boundingBox = boundingBox;
		voxelSize = size;
		
		sizeInVoxels = (int)Mathf.Max(boundingBox.x, boundingBox.y, boundingBox.z);
		depth = (byte)Mathf.CeilToInt( Mathf.Log(sizeInVoxels, 2) );
		sizeInVoxels = (int)Mathf.Pow(2, depth);

		objectCenterOffset = (Vector3)boundingBox * voxelSize * 0.5f;
		objectCenterOffset.y = 0;
		_root = new VoxOctreeLeaf();
	}
	
	
	public void Set( Vector3 objectPosition, byte value ) {
		if ( depth != 0 && _root is VoxOctreeLeaf )
		{
			_root = new VoxOctreeNode(_root);
		}
		_root.Set(ObjectToNormalizedPosition(objectPosition), depth, value);
	}


	public byte Get( Vector3Int voxelPosition )
	{
		return Get( VoxelToObjectPosition(voxelPosition) );
	}
	
	public byte Get(Vector3 objectPosition)
	{
		Vector3 normalizedPosition = ObjectToNormalizedPosition(objectPosition);
		return InUnitCube(normalizedPosition) ? _root.Get(normalizedPosition) : (byte)0;
	}

	
	
	public Vector3 VoxelToObjectPosition(Vector3Int voxelPosition) {
		return ((Vector3)voxelPosition) * voxelSize - objectCenterOffset;
	}
	
	public Vector3Int ObjectToVoxelPosition(Vector3 objectPosition)
	{
		Vector3 voxelPosition = (objectPosition + objectCenterOffset) / voxelSize;
		return Vector3Int.FloorToInt(voxelPosition);
	}
	
	private Vector3 ObjectToNormalizedPosition(Vector3 objectPosition) {
		return (objectPosition + objectCenterOffset) / (sizeInVoxels * voxelSize);
	}
	
	private Vector3 NormalizedToObjectPosition(Vector3 normalizedPosition) {
		return normalizedPosition * sizeInVoxels * voxelSize - objectCenterOffset;
	}
	
	private static bool InUnitCube(Vector3 position)
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

	private static bool IsPositionBetween( Vector3 position, Vector3 cornerLow, Vector3 cornerHigh ) {
		for ( int i=0; i<3; i++ ) {
			if ( position[i] < cornerLow[i] || position[i] > cornerHigh[i] ) {
				return false;
			}
		}
		return true;
	}
	
	
	
	
	public LinkedList<Voxel> GetVoxels() {
		return GetVoxelsBetween( NormalizedToObjectPosition(Vector3.zero), NormalizedToObjectPosition(Vector3.one) );
		// LinkedList<Voxel> voxels = new LinkedList<Voxel>();
		//
		// float objectSize = sizeInVoxels * _voxelSize;
		// root.FillVoxelCollection( voxels, NormalizedToObjectPosition(Vector3.one * 0.5f), objectSize, _depth);
		// return voxels;
	}


	public LinkedList<Voxel> GetVoxelsBetween(Vector3Int cornerLow, Vector3Int cornerHight, bool includeEmpty = false) {
		return GetVoxelsBetween(VoxelToObjectPosition(cornerLow), VoxelToObjectPosition(cornerHight), includeEmpty);
	}

	public LinkedList<Voxel> GetVoxelsBetween(Vector3 cornerLow, Vector3 cornerHight, bool includeEmpty = false) {
		
		LinkedList<Voxel> voxels = new LinkedList<Voxel>();
		if (_root != null)
		{
			_root.GetVoxelsBetween(voxels, cornerLow, cornerHight, NormalizedToObjectPosition(Vector3.one * 0.5f),
				depth, voxelSize * sizeInVoxels, includeEmpty);
		}

		return voxels;
	}

	public void OnBeforeSerialize()
	{
		voxOctreeSerialized.Clear();
		if (_root != null)
		{
			SerializeVoxOctree(_root);
		}
	}

	public void OnAfterDeserialize()
	{
		if (voxOctreeSerialized.Count != 0)
		{
			_root = DeserializeVoxOctree(0, out int _);
		}
		voxOctreeSerialized.Clear();
	}
	
	
	private void SerializeVoxOctree( VoxOctree tree ) {
		bool isLeaf = tree is VoxOctreeLeaf;
		SerializableVoxOctree serializedRoot = new SerializableVoxOctree(){isLeaf = isLeaf};
		if(isLeaf) {
			serializedRoot.value = ((VoxOctreeLeaf)tree).Value;
		}
		voxOctreeSerialized.Add(serializedRoot);
		if( !isLeaf ) {
			foreach( VoxOctree child in ((VoxOctreeNode)tree).Children ) {
				SerializeVoxOctree(child);
			}
		}
		
	}
	
	VoxOctree DeserializeVoxOctree( int rootId, out int nodeCount ) {
		nodeCount = 1;
		SerializableVoxOctree serializedRoot = voxOctreeSerialized[rootId];
		
		if( serializedRoot.isLeaf ) {
			return new VoxOctreeLeaf(serializedRoot.value);
		}
		
		VoxOctree[] children = new VoxOctree[8];
		for( int i=0; i<8; i++ ) {
			children[i] = DeserializeVoxOctree(rootId + nodeCount, out int childNodeCount);
			nodeCount += childNodeCount;
		}
		return new VoxOctreeNode(children);
	}
	
	
	public struct Voxel {
		
		public Vector3 Position;
		public float Size;
		public byte Depth;
		public byte Value;
		
		public Voxel(Vector3 position, float size, byte depth, byte value) {
			Position = position;
			Size = size;
			Depth = depth;
			Value = value;
		}
	}
	
	
	[Serializable]
	struct SerializableVoxOctree {
		public bool isLeaf;
		public byte value;
	}
	
	
	
	abstract class VoxOctree : ICloneable {
		
		
		public abstract byte Get( Vector3 normalizedPosition );
		
		public abstract void Set( Vector3 normalizedPosition, byte depth, byte value );
		
		public abstract void FillVoxelCollection( ICollection<Voxel> voxels, Vector3 objectPosition, float voxelSize, byte depth );
		
		public abstract void GetVoxelsBetween(ICollection<Voxel> voxels, Vector3 cornerLow, Vector3 cornerHigh, Vector3 objectPosition, byte depth, float size, bool includeEmpty);
		
		public abstract object Clone();
	}
	
	
	class VoxOctreeNode : VoxOctree
	{
		public VoxOctree[] Children;
		
		public VoxOctreeNode( VoxOctree[] children ) {
			Children = children;
		}
		
		public VoxOctreeNode( VoxOctree child ) {
			Children = new VoxOctree[8];
			for( int i=0; i<Children.Length; i++ )
			{
				Children[i] = (VoxOctree)child.Clone();
			}
		}
		
		
		
		public override byte Get(Vector3 normalizedPosition)
		{
			byte childId = GetchildId(ref normalizedPosition);
			return Children[childId].Get(normalizedPosition);
		}

		public override void Set(Vector3 normalizedPosition, byte depth, byte value)
		{
			byte childId = GetchildId(ref normalizedPosition);
			
			if(depth == 1) {
				Children[childId] = new VoxOctreeLeaf(value);
				return;
			}
			
			if( Children[childId] is VoxOctreeLeaf ) {
				Children[childId] = new VoxOctreeNode(Children[childId]);
			}
			
			depth--;
			Children[childId].Set(normalizedPosition, depth, value);
		}
		
		
		
		public override void FillVoxelCollection( ICollection<Voxel> voxels, Vector3 objectPosition, float voxelSize, byte depth) {
			
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
				
				Children[i].FillVoxelCollection(voxels, nextPosition, nextVoxelSize, nextDepth);
			}
			
		}

		public override void GetVoxelsBetween(ICollection<Voxel> voxels, Vector3 cornerLow, Vector3 cornerHigh, Vector3 objectPosition, byte depth, float size, bool includeEmpty) {
			byte nextDepth = (byte)(depth - 1);
			float nextSize = size / 2;
			float positionOffset = nextSize / 2;
			
			int[] offsets = {-1, 1};
			for( int i=0; i<8; i++ ) {
				Vector3 offset = new Vector3(
					offsets[i & 1], 
					offsets[(i>>1) & 1], 
					offsets[(i>>2) & 1]);
				
				
				
				Vector3 nextPosition = objectPosition + offset * positionOffset;

				if (IsPositionBetween(nextPosition, cornerLow - Vector3.one * positionOffset, cornerHigh + Vector3.one * positionOffset)) {
					Children[i].GetVoxelsBetween(voxels, cornerLow, cornerHigh, nextPosition, nextDepth, nextSize, includeEmpty);
				}
			}
		}

		public override object Clone() {
			VoxOctree[] clonedChildren = new VoxOctree[8];
			for( int i=0; i<8; i++ ) {
				clonedChildren[i] = (VoxOctree)Children[i].Clone();
			}

			return new VoxOctreeNode(clonedChildren);
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
		public byte Value;
		
		public VoxOctreeLeaf(byte value = 0) {
			Value = value;
		}
		
		
		public override byte Get(Vector3 normalizedPosition)
		{
			return Value;
		}

		public override void Set(Vector3 normalizedPosition, byte depth, byte value)
		{
			Value = value;
		}
		
		
		public override void FillVoxelCollection( ICollection<Voxel> voxels, Vector3 objectPosition, float voxelSize, byte depth ) {
			if (Value == 0)
			{
				return;
			}
			
			Voxel voxel = new Voxel();
			voxel.Depth = depth;
			voxel.Value = Value;
			voxel.Size = voxelSize;
			voxel.Position =  objectPosition;
			
			voxels.Add(voxel);
		}
		
		public override void GetVoxelsBetween(ICollection<Voxel> voxels, Vector3 cornerLow, Vector3 cornerHigh, Vector3 objectPosition, byte depth, float size, bool includeEmpty) {
			if ( Value == 0 && !includeEmpty) {
				return;
			}

			Voxel voxel = new Voxel();
			voxel.Depth = depth;
			voxel.Value = Value;
			voxel.Size = size;
			voxel.Position = objectPosition;
				
			voxels.Add(voxel);
		}

		public override object Clone()
		{
			return new VoxOctreeLeaf(Value);
		}
	}
	
}
}
