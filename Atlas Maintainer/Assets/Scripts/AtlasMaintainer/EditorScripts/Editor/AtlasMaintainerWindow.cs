using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

public class AtlasMaintainerWindow : EditorWindow
{
    private ListView assetsListView;
    private ListView atlasListView;

    [MenuItem("Assets/Atlas Maintainer/Open in Atlas Maintainer", true)]
    private static bool ValidateCanOpenInAtlasMaintainer()
    {
        return AtlasMaintainerHelpers.ValidateSprite(Selection.activeObject)
            || AtlasMaintainerHelpers.ValidatePrefab(Selection.activeObject);
    }

    [MenuItem("Assets/Atlas Maintainer/Open in Atlas Maintainer")]
    private static void OpenInAtlasMaintainer()
    {
        GenerateWindow();
    }

    private static void GenerateWindow()
    {
        AtlasMaintainerWindow window = GetWindow<AtlasMaintainerWindow>();
        window.titleContent = new GUIContent("Atlas Maintainer");
    }

    public void CreateGUI()
    {
        GenerateLayoutFromUXML(rootVisualElement);

        FillAssetsVisualElement(rootVisualElement);
        FillAtlasesVisualElement(rootVisualElement);
    }

    private void GenerateLayoutFromUXML(VisualElement root)
    {
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/AtlasMaintainer/EditorWindow/AtlasMaintainerWindow.uxml");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);
    }

    private void FillAssetsVisualElement(VisualElement root)
    {
        bool isSuccess = AtlasMaintainerHelpers
            .TryGetAllSprites(Selection.activeObject, out Sprite[] sprites);
        if (!isSuccess)
            return;

        VisualElement assetsArea = root.Q<VisualElement>("AssetsArea");
        assetsListView = assetsArea.Q<ListView>("AssetsListView");

        // Defines what to create for each item in the given itemsSource
        assetsListView.makeItem = () =>
        {
            Label newLabel = new();
            AssetListEntry entryController = new();

            newLabel.userData = entryController;
            entryController.SetVisualElement(newLabel);

            return newLabel;
        };

        // Defines the logic of data binding for each item in the given itemsSource
        assetsListView.bindItem = (item, index) =>
        {
            AssetListEntry assetListEntry = item.userData as AssetListEntry;

            assetListEntry.SetSprite(sprites[index]);
            assetListEntry.UpdateText();
        };

        // Sets the itemsSource and triggers makeItem & bindItem
        assetsListView.itemsSource = sprites;

        assetsListView.onSelectionChange += OnAssetsChosen;
    }

    private void OnAssetsChosen(IEnumerable<object> chosenAssets)
    {
        Sprite selectedSprite = assetsListView.selectedItem as Sprite;

        VisualElement assetPreview = rootVisualElement.Q<VisualElement>("OthersArea")
            .Q<VisualElement>("PreviewArea")
            .Q<VisualElement>("AssetPreview");

        assetPreview.style.backgroundImage = new StyleBackground(selectedSprite);
    }

    private void FillAtlasesVisualElement(VisualElement root)
    {
        bool isSuccess = AtlasMaintainerHelpers
            .TryGetAllAtlases(out SpriteAtlas[] allAtlases);
        if (!isSuccess)
            return;
        
        VisualElement atlasesArea = root.Q<VisualElement>("AtlasesArea");
        atlasListView = atlasesArea.Q<ListView>("AtlasesListView");

        // Defines what to create for each item in the given itemsSource
        atlasListView.makeItem = () =>
        {
            Label newLabel = new();
            AtlasListEntry entryController = new();

            newLabel.userData = entryController;
            entryController.SetVisualElement(newLabel);

            return newLabel;
        };

        // Defines the logic of data binding for each item in the given itemsSource
        atlasListView.bindItem = (item, index) =>
        {
            AtlasListEntry atlasListEntry = item.userData as AtlasListEntry;

            atlasListEntry.SetAtlas(allAtlases[index]);
            atlasListEntry.UpdateText();
        };

        // Sets the itemsSource and triggers makeItem & bindItem
        atlasListView.itemsSource = allAtlases;

        atlasListView.onSelectionChange += OnAtlasesChosen;
    }

    private void OnAtlasesChosen(IEnumerable<object> chosenAtlases)
    {
        SpriteAtlas selectedSpriteAtlas = atlasListView.selectedItem as SpriteAtlas;

        Debug.Log(selectedSpriteAtlas);
        Debug.Log(atlasListView[0]);
        atlasListView[0].SetEnabled(false);
    }
}
