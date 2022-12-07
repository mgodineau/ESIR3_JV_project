using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;

using VoxReader;


[ScriptedImporter(1, "vox")]
public class VoxImporter : ScriptedImporter
{
	[SerializeField] private float voxelSize = 1;
	[SerializeField] private bool addCollider = true;
	
	
	public override void OnImportAsset(AssetImportContext ctx)
	{
		
		VoxReader.Interfaces.IVoxFile file = VoxReader.VoxReader.Read( ctx.assetPath );
		string filename = Path.GetFileNameWithoutExtension(ctx.assetPath);
		
		GameObject gameObject = new GameObject();
		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		
		
		VoxBehaviour behaviour = gameObject.AddComponent<VoxBehaviour>();
		Texture2D texture = ConvertPaletteToTexture(file.Palette);
		texture.name = filename + "_palette";

		Material material = new Material(Shader.Find("Standard") );
		material.name = filename + "_mat";
		material.mainTexture = texture;
		
		ctx.AddObjectToAsset("root", gameObject);
		ctx.SetMainObject(gameObject);
		
		ctx.AddObjectToAsset("material", material);
		ctx.AddObjectToAsset("palette", texture);
		
		foreach( Model rawModel in file.Models) {
			
			
			UnityEngine.Vector3 size = ConvertVector3(rawModel.Size);
			
			
			VoxModel model = ScriptableObject.CreateInstance<VoxModel>();
			model.SetSize( ConvertVector3(rawModel.Size), voxelSize );
			model.content = "des trucs et des machins";

			foreach( Voxel voxel in rawModel.Voxels ) {
				model.Set(
					model.VoxelToObjectPosition(ConvertVector3(voxel.Position)),
					(byte)voxel.ColorIndex
				);
			}
			
			model.name = filename + "_model_" + rawModel.Id;
			ctx.AddObjectToAsset("model_" + rawModel.Id, model);
			behaviour.model = model;
			
			VoxBuilder_cube builder = ScriptableObject.CreateInstance<VoxBuilder_cube>();
			builder.RefreshEntireModel(model);
			builder.name = filename + "_builder_" + rawModel.Id;
			ctx.AddObjectToAsset("builder_"  +rawModel.Id, builder);
			behaviour.builder = builder;
			
			Mesh mesh = builder.GetMesh();
			mesh.name = filename + "_mesh_" + rawModel.Id;
			ctx.AddObjectToAsset("mesh_" + rawModel.Id, mesh);
			
			meshFilter.mesh = mesh;

			if( addCollider ) {
				MeshCollider collider = gameObject.AddComponent<MeshCollider>();
				collider.sharedMesh = meshFilter.sharedMesh;
            }
		}
		
		meshRenderer.material = material;
		
	}
	
	
	private Texture2D ConvertPaletteToTexture( VoxReader.Interfaces.IPalette palette ) {
		Texture2D tex = new Texture2D(16, 16, TextureFormat.RGB24, false);
		
		UnityEngine.Color[] colors = System.Array.ConvertAll(palette.Colors, ConvertColor );
		tex.SetPixels(0, 0, 16, 16, colors);
		tex.filterMode = FilterMode.Point;

		return tex;
	}
	
	
	private UnityEngine.Vector3Int ConvertVector3( VoxReader.Vector3 vec) {
		return new UnityEngine.Vector3Int( vec.X, vec.Z, vec.Y );
	}
	
	private UnityEngine.Color ConvertColor( VoxReader.Color col) {
		return new UnityEngine.Color( (float)col.R/256, (float)col.G/256, (float)col.B/256, (float)col.A/256 );
	}
	
}
