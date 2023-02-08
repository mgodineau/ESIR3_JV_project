using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public class VoxTerrainEditor
{

	[MenuItem("Assets/Create/VoxTerrain")]
	public static void DoStuff()
	{
		string dirPath = GetSelectedPathOrFallback();
		string filePath = EditorUtility.SaveFilePanel("Create new voxel terrain...", dirPath, "newVoxTerrain", "voxterrain");
		
		File.WriteAllText(filePath, "");
		Debug.Log(filePath);


	}
	
	/// <summary>
	/// Retrieves selected folder on Project view.
	/// </summary>
	/// <returns></returns>
	public static string GetSelectedPathOrFallback()
	{
		string path = "Assets";
  
		foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
		{
			path = AssetDatabase.GetAssetPath(obj);
			if (!string.IsNullOrEmpty(path) && File.Exists(path))
			{
				path = Path.GetDirectoryName(path);
				break;
			}
		}
		return path;
	}
	
	
}