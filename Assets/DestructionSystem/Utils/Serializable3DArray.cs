using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace DestructionSystem.Utils
{
	[Serializable]
	public abstract class Serializable3DArray<T>
	{
		protected abstract List<T> Data {
			get; 
			set;
		}

		[SerializeField] private Vector3Int dim;
		public Vector3Int Dim => dim;
		
		
		
		
		public Serializable3DArray(Vector3Int dim, T defaultValue)
		{
			this.dim = dim;
			Data = Enumerable.Repeat(defaultValue, dim.x * dim.y * dim.z).ToList();
		}


		public bool AreIndexesValid( Vector3Int indexes ) {
			for ( int i=0; i<3; i++ ) {
				if (indexes[i] < 0 || indexes[i] >= Dim[i]) {
					return false;
				}
			}

			return true;
		}
		
		
		public T this[Vector3Int indexes] {
			get => this[indexes.x, indexes.y, indexes.z];
			set => this[indexes.x, indexes.y, indexes.z] = value;
		}

		public T this[int x, int y, int z]
		{
			get => Data[x + y * dim.x + z * dim.x * dim.y];
			set => Data[x + y * dim.x + z * dim.x * dim.y] = value;
		}
	}


[Serializable]
public class Serializable3DByteArray : Serializable3DArray<byte> {
	
	[SerializeField] private List<byte> data; 
	protected override List<byte> Data {
		get => data;
		set => data = value;
	}
	
	public Serializable3DByteArray(Vector3Int dim, byte defaultValue=0) : base(dim, defaultValue) {}
}

[Serializable]
public class Serializable3DIntArray : Serializable3DArray<int> {
	
	[SerializeField] private List<int> data; 
	protected override List<int> Data {
		get => data;
		set => data = value;
	}
	public Serializable3DIntArray(Vector3Int dim, int defaultValue=0) : base(dim, defaultValue) {}
}


}