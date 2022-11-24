using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;

using VoxReader;


[ScriptedImporter(1, "vox")]
public class VoxImporter : ScriptedImporter
{
	public override void OnImportAsset(AssetImportContext ctx)
	{
			
		VoxReader.Interfaces.IVoxFile file = VoxReader.VoxReader.Read( ctx.assetPath );
		
		GameObject gameObject = new GameObject();
		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		VoxBehaviour behaviour = gameObject.AddComponent<VoxBehaviour>();
		Material material = new Material( Shader.Find("Diffuse") );
		
		
		ctx.AddObjectToAsset("root", gameObject);
		ctx.SetMainObject(gameObject);
		
		ctx.AddObjectToAsset("material", material);
		
		foreach( Model rawModel in file.Models) {
			
			UnityEngine.Vector3 size = ConvertVector3(rawModel.Size);
			
			VoxModel model = new VoxModel(size);
			foreach( Voxel voxel in rawModel.Voxels ) {
				//model.Set( ConvertVector3(voxel.Position), (byte)voxel.Color.GetHashCode() );
				model.Set(ConvertVector3(voxel.Position), 1);
			}
			
			
			model.name = "model_" + rawModel.Id;
			ctx.AddObjectToAsset("model", model);

			Mesh mesh = behaviour.CreateMeshFromModel(model);
			mesh.name = "mesh_" + rawModel.Id;
			ctx.AddObjectToAsset("mesh", mesh);

			meshFilter.mesh = mesh;
		}
		
		meshRenderer.material = material;
		
	}
	
	
	
	private UnityEngine.Vector3 ConvertVector3( VoxReader.Vector3 vec) {
		return new UnityEngine.Vector3( vec.X, vec.Z, vec.Y );
	}
	
}
