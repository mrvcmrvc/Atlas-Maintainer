using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class AtlasMaintainer : Editor
{
#region From Sprite

    [MenuItem("Assets/Atlas Maintainer/Find/Atlases For Sprite", true)]
    private static bool ValidateSpriteToSearchInAtlases()
    {
        return ValidateSprite(Selection.activeObject);
    }
    
    [MenuItem("Assets/Atlas Maintainer/Find/Atlases For Sprite")]
    private static void FindAtlasesForSprite()
    {
        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
        
        Texture2D targetTexture = Selection.activeObject as Texture2D;
        Sprite cloneInAtlas = null;

        if (!TryGetAllAtlases(out SpriteAtlas[] atlases))
        {
            Debug.LogWarning("No atlas found in the project!");
            return;
        }
        
        for (int i = 0; i < atlases.Length; i++)
        {
            cloneInAtlas = atlases[i].GetSprite(targetTexture.name);
            if (!cloneInAtlas)
                continue;
            
            Debug.Log($"{atlases[i].name} includes the {targetTexture.name} sprite");
            break;
        }
        
        if (cloneInAtlas)
            return;
        
        Debug.LogWarning($"Atlas Could not found for sprite {targetTexture.name}");
    }

#endregion

#region From Prefab

    [MenuItem("Assets/Atlas Maintainer/Find/Atlases For Prefab", true)]
    private static bool ValidatePrefab()
    {
        GameObject targetGameObject = Selection.activeObject as GameObject;
        if (!targetGameObject)
            return false;

        PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(targetGameObject);
        if (prefabAssetType != PrefabAssetType.Regular
            && prefabAssetType != PrefabAssetType.Variant)
            return false;

        return TryGetAllDrawersInContainer(targetGameObject, out IContainerFunctions[] _);
    }
    
    [MenuItem("Assets/Atlas Maintainer/Find/Atlases For Prefab")]
    private static void FindAtlasesForPrefab()
    {
        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);

        GameObject targetGameObject = Selection.activeObject as GameObject;
        Sprite cloneInAtlas = null;

        if (!TryGetAllAtlases(out SpriteAtlas[] atlases))
        {
            Debug.LogWarning("No atlas found in the project!");
            return;
        }

        TryGetAllDrawersInContainer(targetGameObject, out IContainerFunctions[] wrappedDrawers);

        for (int i = 0; i < wrappedDrawers.Length; i++)
        {
            IContainerFunctions target = wrappedDrawers[i];
            
            for (int j = 0; j < atlases.Length; j++)
            {
                cloneInAtlas = atlases[j].GetSprite(target.GetSpriteName());
                if (!cloneInAtlas)
                    continue;
            
                Debug.Log($"{atlases[j].name} includes the {target.GetSpriteName()} sprite");
                break;
            }
            
            if (cloneInAtlas)
                continue;
        
            Debug.LogWarning($"Atlas Could not found for sprite {target.GetSpriteName()}");
        }
    }
    
    private interface IContainerFunctions
    {
        string GetSpriteName();
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
        public ImageContainer(Image drawer) : base(drawer)
        {
        }
        
        public string GetSpriteName()
        {
            return Drawer.sprite.name;
        }
    }
    
    private class SpriteRendererContainer : SpriteGenericContainer<SpriteRenderer>, IContainerFunctions
    {
        public SpriteRendererContainer(SpriteRenderer drawer) : base(drawer)
        {
        }
        
        public string GetSpriteName()
        {
            return Drawer.sprite.name;
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

#region Edit

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

#region Add & Remove

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

    private static bool TryGetAllAtlases(out SpriteAtlas[] atlases)
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
    
    private static bool ValidateSprite(Object targetObject)
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

    private static bool TryGetSprite(Object targetObject, out Sprite[] sprites)
    {
        sprites = new Sprite[] { };
        
        if (!ValidateSprite(targetObject))
            return false;

        string assetPath = AssetDatabase.GetAssetPath(targetObject);
        sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();

        return true;
    }
}
