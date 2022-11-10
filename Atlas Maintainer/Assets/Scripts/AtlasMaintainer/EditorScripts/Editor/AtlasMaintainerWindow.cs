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
        List<Sprite> selectedSprites = new();

        foreach (object item in chosenAssets)
            selectedSprites.Add(item as Sprite);

        UpdateWindowFor(selectedSprites.ToArray());
    }

    private void OnMoveToAtlasButtonClicked()
    {
        Debug.Log("OnMoveToAtlasButtonClicked");
    }

    private void OnRemoveFromAtlasClicked()
    {
        atlasListController.GetSelectedLabelOrDefault(out Label atlasLabel);
        SpriteAtlas spriteAtlas = (atlasLabel.userData as AtlasListEntry).ReferencedAtlas;

        assetListController.GetSelectedLabelsOrDefault(out Label[] assetLabels);
        (Sprite[], Texture2D[]) spritesAndTextures = assetListController.ConvertLabelsToSpritesAndTextures(assetLabels);

        AtlasMaintainerHelpers.RemoveAssetsFromAtlas(spriteAtlas, spritesAndTextures.Item2, true);

        UpdateWindowFor(spritesAndTextures.Item1);
    }

    private void OnAddToAtlasClicked()
    {
        atlasListController.GetSelectedLabelOrDefault(out Label atlasLabel);
        SpriteAtlas spriteAtlas = (atlasLabel.userData as AtlasListEntry).ReferencedAtlas;

        assetListController.GetSelectedLabelsOrDefault(out Label[] assetLabels);
        (Sprite[], Texture2D[]) spritesAndTextures = assetListController.ConvertLabelsToSpritesAndTextures(assetLabels);

        AtlasMaintainerHelpers.AddAssetsToAtlas(spriteAtlas, spritesAndTextures.Item2, true);

        UpdateWindowFor(spritesAndTextures.Item1);
    }

    private void UpdateWindowFor(Sprite[] sprites)
    {
        SetupPreview(sprites);

        EnableIncludingAtlases(sprites);

        UpdateOperationButtons();
    }

    private void SetupPreview(Sprite[] selectedSprite)
    {
        if (selectedSprite.Length > 0)
            assetPreview.style.backgroundImage = null;
        else
            assetPreview.style.backgroundImage = new StyleBackground(selectedSprite[0].texture);
    }

    private void EnableIncludingAtlases(Sprite[] selectedSprites)
    {
        SpriteAtlas[] candidateAtlases = AtlasMaintainerHelpers.GetSpriteAtlasesOrEmpty(selectedSprites[0]);

        for (int i = 1; i < selectedSprites.Length; i++)
        {
            candidateAtlases = AtlasMaintainerHelpers.GetSpriteAtlasesOrEmpty(selectedSprites[i], candidateAtlases);

            if (candidateAtlases.Length == 0)
                break;
        }

        atlasListController.EnableAtlases(candidateAtlases);
    }

    private void UpdateOperationButtons()
    {
        bool isAnyAssetSelected = assetListController.GetSelectedLabelsOrDefault(out Label[] assetLabels);
        bool isAnyAtlasSelected = atlasListController.GetSelectedLabelOrDefault(out Label atlasLabel);

        operationsController.UpdateButtons(
            isAnyAssetSelected ? assetLabels : null,
            isAnyAtlasSelected ? atlasLabel : null);
    }
}
