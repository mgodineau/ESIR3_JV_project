using System;
using UnityEngine;

namespace DestructionSystem {
public interface IVoxBuilder {
	
	
	public Mesh GetMesh();
	
	
	public void UpdateMesh(Mesh mesh);
	
	
	public void RefreshEntireModel(IVoxModel model);
	
	
	public void RefreshRegion(IVoxModel model, Vector3 cornerLow, Vector3 cornerHigh);
	
	
	
	
}
}