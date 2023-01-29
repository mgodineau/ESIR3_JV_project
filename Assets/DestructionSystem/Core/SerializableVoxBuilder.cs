using UnityEngine;
using UnityEngine.Serialization;

namespace DestructionSystem {
public class SerializableVoxBuilder : ScriptableObject, IVoxBuilder, ISerializationCallbackReceiver {
	
	private IVoxBuilder _instance;

	[SerializeField] private int _instanceType;
	
	[FormerlySerializedAs("serialVoxBuilderCube")] [SerializeField] private VoxBuilderCube serializedVoxBuilderCube;
	[SerializeField] private VoxBuilderMarchingCube serializedVoxBuilderMarchingCube;
	
	
	public void Init(IVoxBuilder instance) {
		_instance = instance;

		_instanceType = _instance is VoxBuilderCube ? 0 : 1;
	}
	
	
	
	public void OnBeforeSerialize() {
		switch (_instanceType) {
			case 0:
				serializedVoxBuilderCube = _instance as VoxBuilderCube;
				break;
			case 1:
				serializedVoxBuilderMarchingCube = _instance as VoxBuilderMarchingCube;
				break;
		}
	}

	public void OnAfterDeserialize() {
		switch (_instanceType) {
			case 0:
				_instance = serializedVoxBuilderCube;
				break;
			case 1:
				_instance = serializedVoxBuilderMarchingCube;
				break;
		}
	}
	
	
	public Mesh GetMesh() {
		return _instance.GetMesh();
	}

	public void UpdateMesh(Mesh mesh) {
		_instance.UpdateMesh(mesh);
	}

	public void RefreshEntireModel(IVoxModel model) {
		_instance.RefreshEntireModel(model);
	}

	public void RefreshRegion(IVoxModel model, Vector3 cornerLow, Vector3 cornerHigh) {
		_instance.RefreshRegion(model, cornerLow, cornerHigh);
	}

	
}
}