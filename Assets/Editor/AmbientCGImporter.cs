/***********************************************************************************************************************
Copyright (C) 2022 Burning Mime Software, LLC.

This software is provided 'as-is', without any express or implied
warranty.  In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
***********************************************************************************************************************/

#if UNITY_EDITOR
#nullable enable
// ReSharper disable Unity.PreferAddressByIdToGraphicsParams
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UDebug = UnityEngine.Debug;

namespace burningmime.unity.editor
{
    class AmbientCGImporter : EditorWindow
    {
    #region Editor Window
        [MenuItem("Tools/Ambient CG Importer")]
        private static void showWindow() => GetWindow<AmbientCGImporter>(false, "Ambient CG Importer", true);
        private enum MaterialCreationMode { DEFAULT, SMOOTHNESS_IN_ALBEDO, INCLUDE_HEIGHT_MAP, HEIGHT_MAP_IN_BLUE }
        private MaterialCreationMode _mode;
        private Shader? _shader;
        private string? _lastDirectory;
        
        private void OnGUI()
        {
            
            if(!_shader) {
                RenderPipelineAsset renderPipeline = GraphicsSettings.currentRenderPipeline;
                _shader = renderPipeline ? renderPipeline.defaultShader : Shader.Find("Standard"); }
            _shader = (Shader) EditorGUILayout.ObjectField("Shader", _shader, typeof(Shader), false);
            MaterialCreationMode oldMode = _mode;
            
            _mode = (MaterialCreationMode) EditorGUILayout.EnumPopup("Mode", oldMode);
            MaterialCreationMode mode = _mode;
            switch(mode)
            {
                case MaterialCreationMode.SMOOTHNESS_IN_ALBEDO:
                    EditorGUILayout.HelpBox("Materials created like this will use less memory and be faster. However..." +
                    "\n    1. It won't support transparency.\n    2. It won't have occlusion\n    3. You can't have " +
                    "materials with both metal and non-metal parts.\n    4. Metals will look less detailed.", MessageType.Info);
                    break;
                case MaterialCreationMode.INCLUDE_HEIGHT_MAP:
                    EditorGUILayout.HelpBox("Parallax occlusion mapping can be slow, especially on mobile, so only use" +
                    "it on materials that need it.", MessageType.Info);
                    break;
                case MaterialCreationMode.HEIGHT_MAP_IN_BLUE:
                    EditorGUILayout.HelpBox("This is for my custom shaders. Unless you have a special shader that knows about" +
                    "blue height maps, you should not use this mode.", MessageType.Warning);
                    break;
            }
            
            if(GUILayout.Button("Open AmbientCG"))
                Application.OpenURL("https://ambientcg.com/list");

            if(GUILayout.Button("Import ZIP File"))
            {
                string zipFile = EditorUtility.OpenFilePanel("Select ZIP File", _lastDirectory ?? "", "zip");
                if(zipFile != null && File.Exists(zipFile))
                {
                    
                    _lastDirectory = Path.GetDirectoryName(zipFile);
                    const BindingFlags BINDING_FLAGS = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                    string outDir = (string) typeof(ProjectWindowUtil).GetMethod("GetActiveFolderPath", BINDING_FLAGS)!.Invoke(null, Array.Empty<object>());
                    ExtractedTextures textures = extractTextures(zipFile, ref mode);
                    Material material = generateMaterial(outDir, Path.GetFileNameWithoutExtension(zipFile), _shader, textures, mode);
                    ProjectWindowUtil.ShowCreatedAsset(material);
                }
            }
        }
    #endregion
    
    #region Extracting Textures
        private static ExtractedTextures extractTextures(string zipFile, ref MaterialCreationMode mode)
        {
            TempTextures tt = new();
            try
            {
                using ZipArchive arc = new(File.OpenRead(zipFile), ZipArchiveMode.Read);
                tt.albedo = loadEntry(arc, "_color", true, true)!;
                tt.normal = loadEntry(arc, "_normalgl", false, true)!;
                tt.metalness = loadEntry(arc, "_metalness", false, false);
                tt.roughness = loadEntry(arc, "_roughness", false, true)!;
                tt.occlusion = loadEntry(arc, "_ambientocclusion", false, false);
                tt.height = null;
                if(mode is MaterialCreationMode.INCLUDE_HEIGHT_MAP or MaterialCreationMode.HEIGHT_MAP_IN_BLUE)
                {
                    // sometimes there won't be a height map included, so just revert to default mode
                    tt.height = loadEntry(arc, "_displacement", false, false);
                    if(!tt.height)
                    {
                        UDebug.LogWarning($"Missing height map in zip file {Path.GetFileNameWithoutExtension(zipFile)} -- reverting to non-height-mapped mode");
                        mode = MaterialCreationMode.DEFAULT;
                    }
                }
                
                if(mode == MaterialCreationMode.SMOOTHNESS_IN_ALBEDO)
                {
                    return new ExtractedTextures {
                        albedo = mergeSmoothnessToAlbedoAlpha(ref tt.albedo, ref tt.roughness),
                        normal = claimTexture(ref tt.normal)!,
                        metalness = tt.metalness ? getAverageMetalness(tt.metalness!) : 0 };
                }
                else
                {
                    Texture2D? n = null;
                    Texture2D mos = mode == MaterialCreationMode.HEIGHT_MAP_IN_BLUE
                        ? generateMos(ref tt.metalness, ref tt.occlusion, ref tt.height, ref tt.roughness)
                        : generateMos(ref tt.metalness, ref tt.occlusion, ref n, ref tt.roughness);
                    return new ExtractedTextures {
                        albedo = claimTexture(ref tt.albedo)!,
                        normal = claimTexture(ref tt.normal)!,
                        mos = mos,
                        height = mode == MaterialCreationMode.INCLUDE_HEIGHT_MAP ? claimTexture(ref tt.height) : null,
                        metalness = 1 };
                }
            }
            finally
            {
                tt.Dispose();
            }
        }
        
        private static Texture2D? loadEntry(ZipArchive arc, string suffix, bool sRGB, bool throwIfNotFound)
        {
            suffix += ".png";
            ZipArchiveEntry? e = arc.Entries.FirstOrDefault(x => x.Name.ToLowerInvariant().EndsWith(suffix));
            if(e != null)
            {
                GraphicsFormat format = sRGB ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                Texture2D t = new(1, 1, format, TextureCreationFlags.None);
                using Stream es = e.Open();
                using MemoryStream ms = new();
                es.CopyTo(ms);
                t.LoadImage(ms.ToArray(), false);
                t.name = Path.GetFileNameWithoutExtension(e.Name);
                return t;
            }
            else if(throwIfNotFound)
                throw new Exception($"Could not find entry ending with {suffix} in [{string.Join(", ", arc.Entries.Select(x => x.Name))}]");
            else
                return null;
        }

        private static void matchSizes(ref Texture2D? a, ref Texture2D? b, ref Texture2D? c, ref Texture2D? d)
        {
            int width = 0, height = 0;
            if(a) { width = Math.Max(a!.width, width); height = Math.Max(a.height, height); }
            if(b) { width = Math.Max(b!.width, width); height = Math.Max(b.height, height); }
            if(c) { width = Math.Max(c!.width, width); height = Math.Max(c.height, height); }
            if(d) { width = Math.Max(d!.width, width); height = Math.Max(d.height, height); }
            if(a && (a!.width != width || a.height != height)) { resizeTexture(ref a, width, height, true); }
            if(b && (b!.width != width || b.height != height)) { resizeTexture(ref b, width, height, false); }
            if(c && (c!.width != width || c.height != height)) { resizeTexture(ref c, width, height, false); }
            if(d && (d!.width != width || d.height != height)) { resizeTexture(ref d, width, height, false); }
        }
        
        private static void resizeTexture(ref Texture2D source, int width, int height, bool srgb)
        {
            UDebug.Log($"Resize {source.name} from {source.width}x{source.height} to {width}x{height}");
            RenderTexture oldActive = RenderTexture.active;
            GraphicsFormat format = srgb ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, format);
            try
            {
                source.filterMode = FilterMode.Bilinear;
                rt.filterMode = FilterMode.Point;
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                Texture2D result = new(width, height, format, TextureCreationFlags.None) { name = source.name };
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                result.Apply();
                RenderTexture.ReleaseTemporary(rt);
                DestroyImmediate(source);
                source = result;
            }
            finally
            {
                RenderTexture.active = oldActive;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
        
        private static Texture2D generateMos(ref Texture2D? metalT, ref Texture2D? occlusionT, ref Texture2D? heightT, ref Texture2D roughT)
        {
            matchSizes(ref metalT, ref occlusionT, ref heightT, ref roughT!);
            
            // ReSharper disable Unity.NoNullPropagation
            Color[]? metalC = metalT?.GetPixels();
            Color[]? occlusionC = occlusionT?.GetPixels();
            Color[]? heightC = heightT?.GetPixels();
            Color[] roughC = roughT.GetPixels();
            // ReSharper restore Unity.NoNullPropagation
            
            int length = roughC.Length, width = roughT.width, height = roughT.height;
            Color[] resultC = new Color[length];
            for(int i = 0; i < length; ++i)
            {
                resultC[i] = new Color(
                    metalC == null ? 0 : metalC[i].r,
                    occlusionC == null ? 1 : occlusionC[i].r,
                    heightC == null ? 0 : heightC[i].r,
                    1 - roughC[i].r);
            }
            
            Texture2D result = new(width, height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
            result.SetPixels(resultC);
            return result;
        }
        
        private static Texture2D mergeSmoothnessToAlbedoAlpha(ref Texture2D albedoT, ref Texture2D roughT)
        {
            Texture2D? n0 = null, n1 = null;
            matchSizes(ref albedoT!, ref n0, ref n1, ref roughT!);
            Color[] albedoC = albedoT.GetPixels();
            Color[] roughC = roughT.GetPixels();
            int length = albedoC.Length, width = albedoT.width, height = albedoT.height;
            Color[] resultC = new Color[length];
            for(int i = 0; i < length; ++i) {
                Color c = albedoC[i], r = roughC[i];
                resultC[i] = new Color(c.r, c.g, c.b, 1 - r.r); }
            Texture2D result = new(width, height, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
            result.SetPixels(resultC);
            return result;
        }

        private static float getAverageMetalness(Texture2D metalT)
        {
            // adding up 50,000 floats and dividing by 50,000 isn't very precise. there are better ways to do this,
            // by exploiting the nature of IEEE floating point or using BigInteger, but this is fast and good enough
            Color[] colors = metalT.GetPixels();
            int length = colors.Length;
            double sum = 0;
            for(int i = 0; i < length; ++i)
                sum += colors[i].r;
            double coarseAverage = sum / length;
            sum = 0;
            for(int i = 0; i < length; ++i)
                sum += colors[i].r - coarseAverage;
            double diffAverage = sum / length;
            return (float) (coarseAverage + diffAverage);
        }
        
        private struct ExtractedTextures 
        {
            public Texture2D albedo;
            public Texture2D normal;
            public Texture2D? mos;
            public Texture2D? height; 
            public float metalness;
        }
        
        private static Texture2D? claimTexture(ref Texture2D? t) { Texture2D? r = t; t = null; return r; }
        private struct TempTextures : IDisposable
        {
            public Texture2D? albedo;
            public Texture2D? normal;
            public Texture2D? metalness;
            public Texture2D? roughness;
            public Texture2D? occlusion;
            public Texture2D? height;
            
            public void Dispose()
            {
                if(albedo) DestroyImmediate(albedo);
                if(normal) DestroyImmediate(normal);
                if(metalness) DestroyImmediate(metalness);
                if(roughness) DestroyImmediate(roughness);
                if(occlusion) DestroyImmediate(occlusion);
                if(height) DestroyImmediate(height);
            }
        }
    #endregion
    
    #region Generating material
        private enum TextureKind { SRGB, LINEAR, NORMAL_MAP }
        private static void writeTexture(ref Texture2D? source, string dir, string name, string suffix, TextureKind kind)
        {
            string path = Path.Combine(dir, $"{name}_{suffix}.png");
            if(source)
            {
                byte[] bytes = source.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                if(kind != TextureKind.SRGB)
                {
                    TextureImporter importer = (TextureImporter) AssetImporter.GetAtPath(path);
                    importer.sRGBTexture = false;
                    importer.textureType = kind switch {
                        TextureKind.NORMAL_MAP => TextureImporterType.NormalMap,
                        _ => TextureImporterType.Default };
                    importer.SaveAndReimport();
                }
                DestroyImmediate(source);
                source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        private static Material generateMaterial(string dir, string name, Shader shader, ExtractedTextures textures, MaterialCreationMode mode)
        {
            // First write all the textures to disk and re-import them
            writeTexture(ref textures.albedo!, dir, name, "albedo", TextureKind.SRGB);
            writeTexture(ref textures.normal!, dir, name, "normal", TextureKind.NORMAL_MAP);
            writeTexture(ref textures.mos, dir, name, "mos", TextureKind.LINEAR);
            writeTexture(ref textures.height, dir, name, "height", TextureKind.LINEAR);
            Material m = new(shader) { name = name };
            setupMaterialProperties(m, textures, mode);
            AssetDatabase.CreateAsset(m, Path.Combine(dir, name + ".mat"));
            return m;
        }
        
        private static void setFloat(Material material, float value, params string[] names) => setProperty(material, value, names, (m, n, v) => m.SetFloat(n, v));
        private static void setTexture(Material material, Texture? value, params string[] names) => setProperty(material, value, names, (m, n, v) => m.SetTexture(n, v));
        private static void setProperty<T>(Material material, T value, string[] names, Action<Material, string, T> doSet)
        {
            bool found = false;
            foreach(string name in names)
            {
                if(material.HasProperty(name))
                {
                    doSet(material, name, value);
                    found = true;
                }
            }
            
            if(!found)
            {
                StringBuilder sb = new();
                sb.Append("Could not find any property named [");
                for(int i = 0; i < names.Length; ++i)
                    sb.Append(i != 0 ? ", ": "").Append(names[i]);
                sb.Append("] in shader ").Append(material.shader.name).Append(". You'll need to set up this property manually.");
                UDebug.LogWarning(sb.ToString());
            }
        }
        
        private static void setupMaterialProperties(Material m, ExtractedTextures textures, MaterialCreationMode mode)
        {
            setTexture(m, textures.albedo, "_BaseMap", "_BaseColorMap", "_MainTex");
            setTexture(m, textures.normal, "_NormalMap", "_BumpMap");
            setFloat(m, textures.metalness, "_Metallic");
            setFloat(m, 1, "_Smoothness", "_Glossiness");
            if(mode == MaterialCreationMode.SMOOTHNESS_IN_ALBEDO)
                setFloat(m, 1, "_SmoothnessTextureChannel");
            if(textures.mos)
                setTexture(m, textures.mos!, "_MaskMap", "_MetallicGlossMap", "_OcclusionMap");
            if(textures.height)
                setTexture(m, textures.height, "_HeightMap", "_ParallaxMap");
            
            // TODO make sure all keywords are set correctly for each pipeline
        }
    #endregion
    }
}
#endif