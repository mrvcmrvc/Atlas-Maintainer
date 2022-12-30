using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;


public static class AtlasMaintainerHelpers
{
    #region Find Functions

    /// <summary>
    /// Obsolete. It is not being used yet and nor required!
    /// Finds all the textures under the prefab and seraches them inside of the atlases in the project.
    /// </summary>
    /// <param name="prefab">Prefab to search</param>
    [Obsolete]
    private static void FindAtlasesForPrefab(GameObject prefab)
    {
        TryGetAllDrawersInContainer(prefab, out IContainerFunctions[] wrappedDrawers);

        for (int i = 0; i < wrappedDrawers.Length; i++)
        {
            IContainerFunctions target = wrappedDrawers[i];
            
            GetSpriteAtlasesOrEmpty(target.Sprite);
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

    /// <summary>
    /// Not implemented yet!
    /// </summary>
    public static SpriteAtlas CreateAtlas(string atlasName, Object[] objects)
    {
        throw new NotImplementedException();

        SpriteAtlas spriteAtlas = new();
        spriteAtlas.Add(objects);

        if (!Directory.Exists("Assets/Textures/Atlases"))
            Directory.CreateDirectory("Assets/Textures/Atlases");
            
        AssetDatabase.CreateAsset(spriteAtlas, "Assets/Textures/Atlases/GeneratedAtlas.spriteatlas");
        AssetDatabase.SaveAssets();

        return spriteAtlas;
    }

    /// <summary>
    /// Not implemented yet!
    /// </summary>
    public static void DeleteAtlases(SpriteAtlas[] atlasesToDelete)
    {
        throw new NotImplementedException();

        List<string> failedPaths = new();
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
    /// <summary>
    /// Adds the given objects to the atlas provided.
    /// </summary>
    /// <param name="spriteAtlas">Target atlas to add assets to.</param>
    /// <param name="objects">Objects to add. This can be any texture2D, sprites, or folders;</param>
    /// <param name="packAtlas">Whether to re-pack the atlas or not. Changes will not be visible until the re-pack.</param>
    public static void AddAssetsToAtlas(SpriteAtlas spriteAtlas, Object[] objects,  bool packAtlas = false)
    {
        Object[] packables = spriteAtlas.GetPackables();
        List<Object> newObjects = new();

        for (int i = 0; i < objects.Length; i++)
        {
            if (packables.Contains(objects[i]))
                continue;

            newObjects.Add(objects[i]);
        }

        spriteAtlas.Add(newObjects.ToArray());

        if (packAtlas)
            PackAtlases(new[] { spriteAtlas });
    }

    /// <summary>
    /// Removes the given objects from the atlas provided.
    /// </summary>
    /// <param name="spriteAtlas">Target atlas to remove assets from.</param>
    /// <param name="objects">Objects to remove. This can be any texture2D, sprites, or folders;</param>
    /// <param name="packAtlas">Whether to re-pack the atlas or not. Changes will not be visible until the re-pack.</param>
    public static void RemoveAssetsFromAtlas(SpriteAtlas spriteAtlas, Object[] objects, bool packAtlas = false)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            if (IsParentFolderAddedToAtlas(spriteAtlas, objects[i]))
            {
                Object[] subAssets = GetAllAssetsFromFolder(objects[i], false);

                Object folder = GetParentFolderOf(objects[i]);
                RemoveAssetsFromAtlas_Internal(spriteAtlas, new[] { folder });

                AddAssetsToAtlas(spriteAtlas, subAssets);
            }
        }

        RemoveAssetsFromAtlas_Internal(spriteAtlas, objects, packAtlas);
    }

    private static void RemoveAssetsFromAtlas_Internal(SpriteAtlas spriteAtlas, Object[] objects, bool packAtlas = false)
    {
        spriteAtlas.Remove(objects);

        if (packAtlas)
            PackAtlases(new[] { spriteAtlas });
    }

    public static void PackAtlases(SpriteAtlas[] spriteAtlases)
    {
        SpriteAtlasUtility.PackAtlases(spriteAtlases, EditorUserBuildSettings.activeBuildTarget);
    }

    /// <summary>
    /// Returns all the assets in the folder that contains the targetObject. These assets can be anything.
    /// </summary>
    /// <param name="targetObject">This can be any object and it finds the parent folder of this object.
    /// If this is already a folder, then it seraches inside of this folder.</param>
    /// <param name="traverseSubFolders">Whether return assets inside of the sub-folders or not</param>
    /// <returns></returns>
    private static Object[] GetAllAssetsFromFolder(Object targetObject, bool traverseSubFolders = true)
    {
        Object folder;
        if (ValidateFolder(targetObject))
            folder = targetObject;
        else
            folder = GetParentFolderOf(targetObject);

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
    
    /// <summary>
    /// Gets parent folder of targetObject and checks if that folder is directly referenced in the spriteAtlas.
    /// </summary>
    /// <param name="spriteAtlas">Sprite atlas to look for folder reference</param>
    /// <param name="targetObject">This can be any object and it finds the parent folder of this object.</param>
    /// <returns></returns>
    private static bool IsParentFolderAddedToAtlas(SpriteAtlas spriteAtlas, Object targetObject)
    {
        Object folder = GetParentFolderOf(targetObject);
        Object[] packables = spriteAtlas.GetPackables();

        return packables.Contains(folder);
    }

    /// <summary>
    /// Returns parent folder of given object.
    /// </summary>
    /// <param name="targetObject"></param>
    /// <returns></returns>
    private static Object GetParentFolderOf(Object targetObject)
    {
        string assetPath = AssetDatabase.GetAssetPath(targetObject);
        string folderName = Directory.GetParent(assetPath).Name;

        string[] folderGuids = AssetDatabase.FindAssets($"{folderName} t:Folder");
        string folderPath = AssetDatabase.GUIDToAssetPath(folderGuids[0]);
        
        return AssetDatabase.LoadAssetAtPath<Object>(folderPath);
    }

#endregion

    #region Validation

    /// <summary>
    /// Checks if the given object is a texture2D and texture type is Sprite.
    /// </summary>
    /// <param name="targetObject"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Checks if the given object a texture2D
    /// </summary>
    /// <param name="targetObject"></param>
    /// <returns></returns>
    private static bool ValidateTexture(Object targetObject)
    {
        return targetObject is Texture2D;
    }
    
    /// <summary>
    /// Checks if the given object a prefab.
    /// </summary>
    /// <param name="targetObject"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Checks if the given object is a folder.
    /// </summary>
    /// <param name="targetObject"></param>
    /// <returns></returns>
    public static bool ValidateFolder(Object targetObject)
    {
        string assetPath = AssetDatabase.GetAssetPath(targetObject);

        return AssetDatabase.IsValidFolder(assetPath);
    }

    #endregion

    /// <summary>
    /// Searches for a given sprite name in the given atlases or all the atlases in the project if no atlas is provided.
    /// </summary>
    /// <param name="spriteName">sprite name to search</param>
    /// <param name="candidateAtlases">Atlases to search for</param>
    public static SpriteAtlas[] GetSpriteAtlasesOrEmpty(Sprite sprite, SpriteAtlas[] candidateAtlases = null)
    {
        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
        List<SpriteAtlas> result = new();

        if (candidateAtlases == null)
            TryGetAllAtlases(out candidateAtlases);

        if (candidateAtlases == null)
        {
            Debug.LogWarning("No atlas found in the project!");
            return result.ToArray();
        }

        for (int i = 0; i < candidateAtlases.Length; i++)
        {
            if (!candidateAtlases[i].CanBindTo(sprite))
                continue;

            result.Add(candidateAtlases[i]);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Tries to get all the sprites in given object
    /// </summary>
    /// <param name="targetObject">This can be a folder, texture2D or a prefab.
    /// In case of folder, it also traverses sub-folders. In texture2D, it checks if it is a sprite type.
    /// In prefab, it finds all the Image and SpriteRenderer components and returns their sprites.</param>
    /// <param name="sprites"></param>
    /// <returns></returns>
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
        //TODO: GetAllAssetsFromFolder already checks if the given object is folder or not
        // if not, it finds the parent folder
        else if (ValidateFolder(targetObject))
        {
            Object[] objects = GetAllAssetsFromFolder(targetObject);

            //TODO: Improve this sprite getting part by using TryGetAllSprites() recursively
            List<Sprite> spriteList = new();
            for (int i = 0; i < objects.Length; i++)
            {
                if (ValidateSprite(objects[i]))
                {
                    string assetPath = AssetDatabase.GetAssetPath(objects[i]);
                    Sprite[] subSprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();

                    for (int x = 0; x < subSprites.Length; x++)
                        spriteList.Add(subSprites[x]);
                }
            }

            sprites = spriteList.ToArray();

            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds all the atlases in the project
    /// </summary>
    /// <param name="atlases"></param>
    /// <returns></returns>
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
