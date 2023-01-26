using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace DestructionSystem
{
	[Serializable]
	public class MonitoredList<T> : List<T>, ISerializationCallbackReceiver
	{

		[SerializeField] private T[] serializableArray;
		
		private readonly SortedList<int, int> m_UpdatedChunks;
		[SerializeField] private int _chunkSize;
		public int chunkSize
		{
			get { return _chunkSize; }
		}
		
		public MonitoredList(int chunkSize=32)
		{
			serializableArray = null;
			_chunkSize = chunkSize;
			m_UpdatedChunks = new SortedList<int, int>();
		}
		
		
		
		public void OnBeforeSerialize()
		{
			serializableArray = ToArray();
		}

		public void OnAfterDeserialize()
		{
			if (Count == 0 && serializableArray != null)
			{
				AddRange(serializableArray);
			}
			serializableArray = null;
		}
		

		public new T this[int index]
		{
			get => base[index];
			set
			{
				base[index] = value;
				
				RegisterUpdateAt(index);
			}
		}

		public new void Add(T item)
		{
			base.Add(item);
			RegisterUpdateAt(base.Count-1);
		}


		public void ClearUpdatedChunks()
		{
			m_UpdatedChunks.Clear();
		}
		
		public ICollection<KeyValuePair<int, int>> GetUpdatedChunks()
		{
			return m_UpdatedChunks.AsReadOnlyCollection();
		}
		
		
		
		
		private void RegisterUpdateAt(int index)
		{
			int chunkLocation = index / chunkSize;
			if (m_UpdatedChunks.ContainsKey(chunkLocation))
			{
				return;
			}

			m_UpdatedChunks.Add(chunkLocation, 1);
			int chunkIndex = m_UpdatedChunks.IndexOfKey(chunkLocation);
			if (chunkIndex == 0)
			{
				return;
			}

			int previousChunkIndex = chunkIndex - 1;
			int previousChunkLocation = m_UpdatedChunks.Keys[previousChunkIndex] +
				m_UpdatedChunks.Values[previousChunkIndex] - 1;

			if (previousChunkLocation < chunkLocation - 1)
			{
				return;
			}

			m_UpdatedChunks.RemoveAt(chunkIndex);
			if (previousChunkLocation == chunkLocation - 1)
			{
				m_UpdatedChunks[m_UpdatedChunks.Keys[previousChunkIndex]]++;
			}
		}

	}
}