using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using VoxReader;

namespace DestructionSystem {


/// <summary>
/// Custom Importer for .vox files
/// </summary>
[ScriptedImporter(1, "vox")]
public class VoxImporter : ScriptedImporter
{
	// size of each voxel in object space
	[SerializeField] private float voxelSize = 1;
	
	// should the prefab has a collider ?
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

		Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") );
		material.name = filename + "_mat";
		material.mainTexture = texture;
		
		ctx.AddObjectToAsset("root", gameObject);
		ctx.SetMainObject(gameObject);
		
		ctx.AddObjectToAsset("material", material);
		ctx.AddObjectToAsset("palette", texture);
		
		foreach( VoxReader.Interfaces.IModel rawModel in file.Models) {
			
			
			VoxModelOctree modelOctree = ScriptableObject.CreateInstance<VoxModelOctree>();
			modelOctree.SetSize( ConvertVector3(rawModel.Size), voxelSize );
			
			foreach( Voxel voxel in rawModel.Voxels ) {
				modelOctree.Set(ConvertVector3(voxel.Position),
					(byte)voxel.ColorIndex
				);
			}
			
			modelOctree.name = filename + "_model_" + rawModel.Id;
			ctx.AddObjectToAsset("model_" + rawModel.Id, modelOctree);
			behaviour.model = modelOctree;
			
			SerializableVoxBuilder builder = ScriptableObject.CreateInstance<SerializableVoxBuilder>();
			builder.Init( new VoxBuilderCube() );
			
			builder.RefreshEntireModel(modelOctree);
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
	
	
	/// <summary>
	/// Create a 16x16 RGB texture from a color palette  
	/// </summary>
	/// <param name="palette">The original color palette</param>
	/// <returns>The texture created from the palette</returns>
	private Texture2D ConvertPaletteToTexture( VoxReader.Interfaces.IPalette palette ) {
		Texture2D tex = new Texture2D(16, 16, TextureFormat.RGB24, false);
		
		UnityEngine.Color[] colors = System.Array.ConvertAll(palette.Colors, ConvertColor );
		tex.SetPixels(0, 0, 16, 16, colors);
		tex.filterMode = FilterMode.Point;

		return tex;
	}
	
	
	/// <summary>
	/// Convert a VoxReader.Vector3 to a UnityEngine.Vector3Int
	/// </summary>
	/// <param name="vec">a position in voxel coordinates</param>
	/// <returns>A new Vector3Int with the same values as vec</returns>
	private Vector3Int ConvertVector3( VoxReader.Vector3 vec) {
		return new Vector3Int( vec.X, vec.Z, vec.Y );
	}
	
	/// <summary>
	/// Convert VoxReader.Color to UnityEngine.Color 
	/// </summary>
	/// <param name="col">an instance of VoxReader.Color</param>
	/// <returns>The same RGBA color as col, as a UnityEngine.Color</returns>
	private UnityEngine.Color ConvertColor( VoxReader.Color col) {
		return new UnityEngine.Color( (float)col.R/256, (float)col.G/256, (float)col.B/256, (float)col.A/256 );
	}
	
}
}
