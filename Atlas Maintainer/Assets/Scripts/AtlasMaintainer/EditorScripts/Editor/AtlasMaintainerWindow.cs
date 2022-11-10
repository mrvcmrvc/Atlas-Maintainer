using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

public class AtlasMaintainerWindow : EditorWindow
{
    private OperationsController operationsController;
    private AtlasListController atlasListController;
    private AssetListController assetListController;

    private VisualElement assetPreview;

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

        assetPreview = rootVisualElement
            .Q<VisualElement>("OthersArea")
            .Q<VisualElement>("PreviewArea")
            .Q<VisualElement>("AssetPreview");

        operationsController = new OperationsController(rootVisualElement);
        atlasListController = new AtlasListController(rootVisualElement);
        assetListController = new AssetListController(rootVisualElement);

        assetListController.FillAssetsVisualElement();
        atlasListController.FillAtlasesVisualElement();
        operationsController.SetButtons();

        SetupEvents();
    }

    private void GenerateLayoutFromUXML(VisualElement root)
    {
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/Scripts/AtlasMaintainer/EditorWindow/AtlasMaintainerWindow.uxml");

        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);
    }

    private void SetupEvents()
    {
        assetListController.AssetsListView.onSelectionChange += OnAssetSelectionChanged;
        atlasListController.AtlasListView.onSelectionChange += OnAtlasSelectionChanged;

        operationsController.AddToAtlasButton.clicked += OnAddToAtlasClicked;
        operationsController.RemoveFromAtlasButton.clicked += OnRemoveFromAtlasClicked;
        operationsController.MoveToAtlasButton.clicked += OnMoveToAtlasButtonClicked;
    }

    private void OnAtlasSelectionChanged(IEnumerable<object> chosenAtlases)
    {
        UpdateOperationButtons();
    }

    private void OnAssetSelectionChanged(IEnumerable<object> chosenAssets)
    {
        Sprite selectedSprite = assetListController.AssetsListView.selectedItem as Sprite;

        UpdateWindowFor(selectedSprite);
    }

    private void UpdateOperationButtons()
    {
        bool isAnyAssetSelected = assetListController.GetSelectedLabelOrDefault(out Label assetLabel);
        bool isAnyAtlasSelected = atlasListController.GetSelectedLabelOrDefault(out Label atlasLabel);

        operationsController.UpdateButtons(
            isAnyAssetSelected ? assetLabel : null,
            isAnyAtlasSelected ? atlasLabel : null);
    }

    private void SetupPreview(Texture2D selectedTexture)
    {
        assetPreview.style.backgroundImage = new StyleBackground(selectedTexture);
    }

    private void EnableIncludingAtlases(Sprite selectedSprite)
    {
        SpriteAtlas[] atlases = AtlasMaintainerHelpers.SearchSpriteFromAtlases(selectedSprite);

        atlasListController.EnableAtlases(atlases);
    }

    private void OnMoveToAtlasButtonClicked()
    {
        Debug.Log("OnMoveToAtlasButtonClicked");
    }

    private void OnRemoveFromAtlasClicked()
    {
        assetListController.GetSelectedLabelOrDefault(out Label assetLabel);
        Sprite sprite = (assetLabel.userData as AssetListEntry).ReferencedSprite;

        atlasListController.GetSelectedLabelOrDefault(out Label atlasLabel);
        SpriteAtlas spriteAtlas = (atlasLabel.userData as AtlasListEntry).ReferencedAtlas;

        AtlasMaintainerHelpers.RemoveAssetsFromAtlas(spriteAtlas, new Object[] { sprite.texture }, true);

        UpdateWindowFor(sprite);
    }

    private void OnAddToAtlasClicked()
    {
        assetListController.GetSelectedLabelOrDefault(out Label assetLabel);
        Sprite sprite = (assetLabel.userData as AssetListEntry).ReferencedSprite;

        atlasListController.GetSelectedLabelOrDefault(out Label atlasLabel);
        SpriteAtlas spriteAtlas = (atlasLabel.userData as AtlasListEntry).ReferencedAtlas;

        AtlasMaintainerHelpers.AddAssetsToAtlas(spriteAtlas, new Object[] { sprite.texture }, true);

        UpdateWindowFor(sprite);
    }

    private void UpdateWindowFor(Sprite sprite)
    {
        SetupPreview(sprite.texture);

        EnableIncludingAtlases(sprite);

        UpdateOperationButtons();
    }
}
