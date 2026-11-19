using UnityEditor;
using UnityEngine;

public sealed class CatchLabArtImport : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/Art/")) return;
        var importer=(TextureImporter)assetImporter;
        importer.filterMode=FilterMode.Point;
        importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.alphaIsTransparency=true;
    }
    [MenuItem("CatchLab/Refresh Pixel Art")]
    public static void RefreshArt()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Art"}))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid),ImportAssetOptions.ForceUpdate);
    }
}
