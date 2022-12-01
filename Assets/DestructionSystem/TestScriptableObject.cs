using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScriptableObject : ScriptableObject
{
    private byte _depth;
	public byte Depth {
		get {return _depth;}
	}
	
	[SerializeField]
	private float _voxelSize;
	public float VoxelSize {
		get {return _voxelSize;}
	}
	
	[SerializeField]
	private Vector3 objectCenterOffset;
	[SerializeField]
	private int sizeInVoxels;
    
}
