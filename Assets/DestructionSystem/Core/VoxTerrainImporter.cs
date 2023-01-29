using System.Collections;
using System.Collections.Generic;
using System.IO;
using DestructionSystem;
using Unity.Collections;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace DestructionSystem
{

	[ScriptedImporter(1, "voxterrain")]
	public class VoxTerrainImporter : ScriptedImporter
	{
		[SerializeField] private Texture2D heightmap;
		[SerializeField] private Material material;

		[SerializeField] private float terrainWidth = 100;
		[SerializeField] private float terrainHeight = 10;
		[SerializeField] private float heightOffset = 5;

		[SerializeField] private float voxelSize = 1;
		[SerializeField] private int metaVoxelSize = 4;
		
		// should the prefab has a collider ?
		[SerializeField] private bool addCollider = true;


		public override void OnImportAsset(AssetImportContext ctx)
		{
			if (heightmap == null)
			{
				ctx.LogImportError("A heightmap must be provided");
				return;
			}

			string filename = Path.GetFileNameWithoutExtension(ctx.assetPath);


			GameObject gameObject = new GameObject();
			ctx.AddObjectToAsset("root", gameObject);
			ctx.SetMainObject(gameObject);

			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
			VoxBehaviour behaviour = gameObject.AddComponent<VoxBehaviour>();

			VoxModelOctree model = ScriptableObject.CreateInstance<VoxModelOctree>();
			FillModel(model, filename);

			model.name = filename + "_model";
			ctx.AddObjectToAsset("model", model);
			behaviour.model = model;


			// VoxBuilderCube rawBuilder = new VoxBuilderCube {
			// 	uvMapping = VoxBuilderCube.UVMappingStrategy.ProjectionXZ
			// };	

			SerializableVoxBuilder builder = ScriptableObject.CreateInstance<SerializableVoxBuilder>();
			builder.Init( new VoxBuilderMarchingCube(metaVoxelSize) );
			
			builder.RefreshEntireModel(model);
			builder.name = filename + "_builder";
			ctx.AddObjectToAsset("builder", builder);
			behaviour.builder = builder;

			Mesh mesh = builder.GetMesh();
			mesh.name = filename + "_mesh";
			ctx.AddObjectToAsset("mesh", mesh);

			meshFilter.mesh = mesh;

			Material currentMaterial = material;
			if (currentMaterial == null)
			{
				currentMaterial = new Material(Shader.Find("Standard"));
				currentMaterial.name = filename + "_mat";
				ctx.AddObjectToAsset(currentMaterial.name, currentMaterial);
				ctx.LogImportWarning("material is not set");
			}

			meshRenderer.material = currentMaterial;

			if (addCollider)
			{
				MeshCollider collider = gameObject.AddComponent<MeshCollider>();
				collider.sharedMesh = meshFilter.sharedMesh;
			}


		}


		private void FillModel(IVoxModel model, string filename)
		{

			Vector3Int boundingBox = new Vector3Int(
				(int)(terrainWidth / voxelSize),
				(int)((heightOffset + terrainHeight) / voxelSize),
				-1);

			boundingBox.z = boundingBox.x * heightmap.height / heightmap.width;

			model.SetSize(boundingBox, voxelSize);


			string progressBarTitle = "Creating voxel terrain \"" + filename + "\"...";
			EditorUtility.DisplayProgressBar(progressBarTitle, "", 0);
			for (int x = 0; x < boundingBox.x; x++)
			{
				for (int z = 0; z < boundingBox.z; z++)
				{
					Vector2 normalizedCoords = new Vector2(x, z) * voxelSize / terrainWidth;
					Color currentHeightmapColor = heightmap.GetPixelBilinear(normalizedCoords.x, normalizedCoords.y);
					int currentHeight =
						(int)(currentHeightmapColor.grayscale * terrainHeight + heightOffset / voxelSize);

					for (int y = 0; y < currentHeight; y++)
					{
						Vector3Int currentPosition = new Vector3Int(x, y, z);
						model.Set(model.VoxelToObjectPosition(currentPosition), 1);
					}

					EditorUtility.DisplayProgressBar(progressBarTitle, "",
						(float)(x * boundingBox.z + z) / (boundingBox.x * boundingBox.z));
				}
			}

			EditorUtility.ClearProgressBar();
		}


	}

}