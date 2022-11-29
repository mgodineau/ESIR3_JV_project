using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;

using VoxReader;


[ScriptedImporter(1, "vox")]
public class VoxImporter : ScriptedImporter
{
	[SerializeField]
	private float voxelSize = 1;
	
	
	
	public override void OnImportAsset(AssetImportContext ctx)
	{
		
		VoxReader.Interfaces.IVoxFile file = VoxReader.VoxReader.Read( ctx.assetPath );
		
		GameObject gameObject = new GameObject();
		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		MeshCollider collider = gameObject.AddComponent<MeshCollider>();
		
		VoxBehaviour behaviour = gameObject.AddComponent<VoxBehaviour>();
		Material material = new Material( Shader.Find("Diffuse") );
		Texture2D texture = ConvertPaletteToTexture(file.Palette);
		texture.name = "texture";
		
		ctx.AddObjectToAsset("root", gameObject);
		ctx.SetMainObject(gameObject);
		
		ctx.AddObjectToAsset("material", material);
		ctx.AddObjectToAsset("texture", texture);
		
		foreach( Model rawModel in file.Models) {
			
			UnityEngine.Vector3 size = ConvertVector3(rawModel.Size);
			
			
			VoxModel model = new VoxModel(size, voxelSize);
			foreach( Voxel voxel in rawModel.Voxels ) {
				//model.Set( ConvertVector3(voxel.Position), (byte)voxel.Color.GetHashCode() );
				model.Set(model.VoxelToObjectPosition(ConvertVector3(voxel.Position)), 1);
			}
			
			
			model.name = "model_" + rawModel.Id;
			ctx.AddObjectToAsset("model", model);

			Mesh mesh = behaviour.CreateMeshFromModel(model);
			mesh.name = "mesh_" + rawModel.Id;
			ctx.AddObjectToAsset("mesh", mesh);

			meshFilter.mesh = mesh;
			collider.sharedMesh = meshFilter.sharedMesh;
		}
		
		meshRenderer.material = material;
		
	}
	
	
	private Texture2D ConvertPaletteToTexture( VoxReader.Interfaces.IPalette palette ) {
		Texture2D tex = new Texture2D(16, 16);
		
		UnityEngine.Color[] colors = System.Array.ConvertAll(palette.Colors, ConvertColor );
		tex.SetPixels(0, 0, 16, 16, colors);
		tex.filterMode = FilterMode.Point;
		
		return tex;
	}
	
	
	private UnityEngine.Vector3 ConvertVector3( VoxReader.Vector3 vec) {
		return new UnityEngine.Vector3( vec.X, vec.Z, vec.Y );
	}
	
	private UnityEngine.Color ConvertColor( VoxReader.Color col) {
		return new UnityEngine.Color( (float)col.R/256, (float)col.G/256, (float)col.B/256, (float)col.A/256 );
	}
	
}
