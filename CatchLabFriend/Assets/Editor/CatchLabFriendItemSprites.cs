using System;
using UnityEditor;
using UnityEngine;

public static class CatchLabFriendItemSprites
{
    [MenuItem("CatchLab Friend/Repair Falling Item Art")]
    public static void Rebuild()
    {
        Sprite gold = ImportCoin("Assets/FriendCoins/S_ItemNoOutline_CoinGold_00.png");
        Sprite red = ImportCoin("Assets/FriendCoins/S_ItemNoOutline_CoinRed_00.png");
        Sprite fuel = null;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Player_Sprite/Fuel_icon.png"))
            if (asset is Sprite sprite) { fuel = sprite; break; }
        if (fuel == null) throw new InvalidOperationException("Missing fuel sprite");
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/GeneratedItems/ItemUnlit.mat");
        if (material == null) throw new InvalidOperationException("Missing item material");
        Repair("Assets/Square (1).prefab", gold, material, "Gold coin (+4)", .5f, 4);
        Repair("Assets/Square (2).prefab", red, material, "Red coin (+10)", .5f, 10);
        Repair("Assets/Square.prefab", fuel, material, "Fuel (+20 energy)", 0.31f, 0);
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        foreach (var root in scene.GetRootGameObjects())
        foreach (var ui in root.GetComponentsInChildren<UIManager>(true))
        {
            if (ui.comboText == null) continue;
            var rect = ui.comboText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .8f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            EditorUtility.SetDirty(rect);
        }
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Friend items updated: gold 4, red 10, fuel 0 points / 20 energy.");
    }
    static Sprite ImportCoin(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        importer.spritePixelsPerUnit = Mathf.Max(width, height);
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void Repair(string path, Sprite sprite, Material material, string name, float scale, int points)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var renderer = root.GetComponentInChildren<SpriteRenderer>(true);
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 5;
            renderer.transform.localPosition = -sprite.bounds.center * scale;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            renderer.gameObject.name = name;
            var item = root.GetComponent<FallingObject>();
            if (item.itemType == FallingObject.ItemType.Fuel) { item.fuelScore = points; item.fuelAmount = 20; }
            else item.scoreValue = points;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
