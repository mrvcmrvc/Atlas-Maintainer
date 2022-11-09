using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Image = UnityEngine.UI.Image;

public static class AtlasMaintainerHelpers
{
#region Find Functions
    
    private static void FindAtlasesForSprite()
    {
        Texture2D targetTexture = Selection.activeObject as Texture2D;

        SearchSpriteFromAtlases(targetTexture.name);
    }
    
    private static void FindAtlasesForPrefab()
    {
        GameObject targetGameObject = Selection.activeObject as GameObject;

        TryGetAllDrawersInContainer(targetGameObject, out IContainerFunctions[] wrappedDrawers);

        for (int i = 0; i < wrappedDrawers.Length; i++)
        {
            IContainerFunctions target = wrappedDrawers[i];
            
            SearchSpriteFromAtlases(target.Sprite.name);
        }
    }

#endregion

#region Prefab Helpers
    
    private interface IContainerFunctions
    {
        Sprite Sprite { get; }
    }

    private abstract class SpriteGenericContainer<T>
    {
        public T Drawer { get; }

        protected SpriteGenericContainer(T drawer)
        {
            Drawer = drawer;
        }
    }

    private class ImageContainer : SpriteGenericContainer<Image>, IContainerFunctions
    {
        public Sprite Sprite => Drawer.sprite;

        public ImageContainer(Image drawer) : base(drawer)
        {
        }
    }
    
    private class SpriteRendererContainer : SpriteGenericContainer<SpriteRenderer>, IContainerFunctions
    {
        public Sprite Sprite => Drawer.sprite;

        public SpriteRendererContainer(SpriteRenderer drawer) : base(drawer)
        {
        }
    }

    private static bool TryGetAllDrawersInContainer(GameObject targetGameObject, out IContainerFunctions[] containers)
    {
        SpriteRenderer[] renderers = targetGameObject.GetComponentsInChildren<SpriteRenderer>();
        Image[] images = targetGameObject.GetComponentsInChildren<Image>();

        containers = new IContainerFunctions[renderers.Length + images.Length];

        if (containers.Length == 0)
            return false;
        
        int index = 0;
        for (; index < renderers.Length; index++)
            containers[index] = new SpriteRendererContainer(renderers[index]);
        
        for (int i = 0; index < containers.Length; i++)
        {
            containers[index] = new ImageContainer(images[i]);
            index++;
        }

        return true;
    }

#endregion

#region Edit Functions

    [MenuItem("Assets/Atlas Maintainer/Edit/Create new atlas")]
    private static void CreateAtlas()
    {
        SpriteAtlas spriteAtlas = new SpriteAtlas();
    
        if (!Directory.Exists("Assets/Textures/Atlases"))
            Directory.CreateDirectory("Assets/Textures/Atlases");
            
        AssetDatabase.CreateAsset(spriteAtlas, "Assets/Textures/Atlases/GeneratedAtlas.spriteatlas");
        AssetDatabase.SaveAssets();
    }
    
    [MenuItem("Assets/Atlas Maintainer/Edit/Delete atlas")]
    private static void RemoveAtlas()
    {
        List<string> failedPaths = new List<string>();
        AssetDatabase.DeleteAssets(new[]{
            "Assets/Textures/Atlases/GeneratedAtlas.spriteatlas",
        }, failedPaths);
            
        if (failedPaths.Count == 0)
            return;
    
        for (int i = 0; i < failedPaths.Count; i++)
            Debug.LogWarning($"Failed to remove the asset at {failedPaths[i]}");
    }

#endregion

#region Add & Remove Functions

    [MenuItem("Assets/Atlas Maintainer/Edit/Add Asset to atlas")]
    private static void AddAssetToAtlas()
    {
        SpriteAtlas spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/Textures/Atlases/Atlas01.spriteatlas");

        AddAssetsToAtlas(spriteAtlas, new[] { Selection.activeObject }, true);
    }

    [MenuItem("Assets/Atlas Maintainer/Edit/Remove Asset from atlas")]
    private static void RemoveAssetFromAtlas()
    {
        SpriteAtlas spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/Textures/Atlases/Atlas01.spriteatlas");

        if (IsParentFolderAddedToAtlas(spriteAtlas, Selection.activeObject))
        {
            Object[] subAssets = GetAllAssetsFromFolder(Selection.activeObject, false);

            Object folder = GetParentFolderOf(Selection.activeObject);
            RemoveAssetsFromAtlas(spriteAtlas, new[] { folder });

            AddAssetsToAtlas(spriteAtlas, subAssets);
        }
        
        RemoveAssetsFromAtlas(spriteAtlas, new[] { Selection.activeObject }, true);
    }
    
    private static void AddAssetsToAtlas(SpriteAtlas spriteAtlas, Object[] objects,  bool packAtlas = false)
    {
        spriteAtlas.Add(objects);
        
        if (packAtlas)
            SpriteAtlasUtility.PackAtlases(new[] { spriteAtlas }, EditorUserBuildSettings.activeBuildTarget);
    }
    
    private static void RemoveAssetsFromAtlas(SpriteAtlas spriteAtlas, Object[] objects, bool packAtlas = false)
    {
        spriteAtlas.Remove(objects);
        
        if (packAtlas)
            SpriteAtlasUtility.PackAtlases(new[] { spriteAtlas }, EditorUserBuildSettings.activeBuildTarget);
    }

    private static Object[] GetAllAssetsFromFolder(Object targetObject, bool traverseSubFolders = true)
    {
        Object folder = GetParentFolderOf(targetObject);
        string folderPath = AssetDatabase.GetAssetPath(folder);
        string[] assets = AssetDatabase.FindAssets("", new []{ folderPath });

        List<Object> subAssets = new List<Object>();
        for (int i = 0; i < assets.Length; i++)
        {
            string subAssetPath = AssetDatabase.GUIDToAssetPath(assets[i]);
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(subAssetPath);

            if (!traverseSubFolders)
            {
                Object assetFolder = GetParentFolderOf(asset);
                if (assetFolder != folder)
                    continue;
            }

            subAssets.Add(asset);
        }
        
        return subAssets.ToArray();
    }
    
    private static bool IsParentFolderAddedToAtlas(SpriteAtlas spriteAtlas, Object targetObject)
    {
        Object folder = GetParentFolderOf(targetObject);
        
        Object[] packables = spriteAtlas.GetPackables();
        for (int i = 0; i < packables.Length; i++)
        {
            if (packables[i] == folder)
                return true;
        }

        return false;
    }

    private static Object GetParentFolderOf(Object targetObject)
    {
        string assetPath = AssetDatabase.GetAssetPath(targetObject);
        string folderName = Directory.GetParent(assetPath).Name;

        string[] folderGuids = AssetDatabase.FindAssets($"{folderName} t:Folder");
        string folderPath = AssetDatabase.GUIDToAssetPath(folderGuids[0]);
        
        return AssetDatabase.LoadAssetAtPath<Object>(folderPath);
    }

#endregion

    /// <summary>
    /// Searches for a given sprite name in the given atlases or all the atlases in the project if no atlas is provided.
    /// </summary>
    /// <param name="spriteName">sprite name to search</param>
    /// <param name="candidateAtlases">Atlases to search for</param>
    private static void SearchSpriteFromAtlases(string spriteName, SpriteAtlas[] candidateAtlases = null)
    {
        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
        Sprite cloneInAtlas = null;
    
        if (candidateAtlases == null)
            TryGetAllAtlases(out candidateAtlases);
            
        if (candidateAtlases == null)
        {
            Debug.LogWarning("No atlas found in the project!");
            return;
        }
            
        for (int i = 0; i < candidateAtlases.Length; i++)
        {
            cloneInAtlas = candidateAtlases[i].GetSprite(spriteName);
            if (!cloneInAtlas)
                continue;
                
            Debug.Log($"{candidateAtlases[i].name} includes the {spriteName} sprite");
            break;
        }
            
        if (cloneInAtlas)
            return;
            
        Debug.LogWarning($"Atlas Could not found for sprite {spriteName}");
    }
    
    public static bool ValidateSprite(Object targetObject)
    {
        if (!ValidateTexture(targetObject))
            return false;
        
        string assetPath = AssetDatabase.GetAssetPath(targetObject as Texture2D);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        
        if (!importer)
            return false;
        
        return importer.textureType == TextureImporterType.Sprite;
    }

    private static bool ValidateTexture(Object targetObject)
    {
        return targetObject is Texture2D;
    }
    
    public static bool ValidatePrefab(Object targetObject)
    {
        GameObject targetGameObject = targetObject as GameObject;
        if (!targetGameObject)
            return false;

        PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(targetGameObject);
        if (prefabAssetType != PrefabAssetType.Regular
            && prefabAssetType != PrefabAssetType.Variant)
            return false;

        return TryGetAllDrawersInContainer(targetGameObject, out IContainerFunctions[] _);
    }

    public static bool TryGetAllSprites(Object targetObject, out Sprite[] sprites)
    {
        sprites = new Sprite[] { };

        if (ValidateSprite(targetObject))
        {
            string assetPath = AssetDatabase.GetAssetPath(targetObject);
            sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();

            return true;
        }
        else if (ValidatePrefab(targetObject))
        {
            if (TryGetAllDrawersInContainer(targetObject as GameObject, out IContainerFunctions[] containers))
            {
                sprites = new Sprite[containers.Length];
                for(int i = 0; i < containers.Length ; i++)
                    sprites[i] = containers[i].Sprite;

                return true;
            }
        }

        return false;
    }

    public static bool TryGetAllAtlases(out SpriteAtlas[] atlases)
    {
        string[] atlasPaths = AssetDatabase.FindAssets("t:SpriteAtlas");
        atlases = new SpriteAtlas[atlasPaths.Length];

        if (atlasPaths.Length == 0)
            return false;

        for (int i = 0; i < atlasPaths.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(atlasPaths[i]);
            atlases[i] = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetPath);
        }

        return true;
    }
}
