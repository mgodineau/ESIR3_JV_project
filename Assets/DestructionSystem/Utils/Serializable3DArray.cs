using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace DestructionSystem.Utils
{
	[Serializable]
	public class Serializable3DByteArray
	{

		[SerializeField] private List<byte> data;

		[SerializeField] private Vector3Int dim;
		public Vector3Int Dim => dim;


		public Serializable3DByteArray(int dimX, int dimY, int dimZ) :
			this(new Vector3Int(dimX, dimY, dimZ))
		{
		}


		public Serializable3DByteArray(Vector3Int dim)
		{
			this.dim = dim;
			data = Enumerable.Repeat((byte)0, dim.x * dim.y * dim.z).ToList();
		}

		public byte this[Vector3Int indexes] {
			get { return this[indexes.x, indexes.y, indexes.z]; }
			set { this[indexes.x, indexes.y, indexes.z] = value; }
		}

		public byte this[int x, int y, int z]
		{
			get { return data[x + y * dim.x + z * dim.x * dim.y]; }
			set { data[x + y * dim.x + z * dim.x * dim.y] = value; }
		}
	}

}