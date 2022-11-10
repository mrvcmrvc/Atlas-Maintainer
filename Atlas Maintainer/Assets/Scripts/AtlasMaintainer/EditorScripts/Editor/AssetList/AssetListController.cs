using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AssetListController
{
    public ListView AssetsListView { get; private set; }
    public AssetListEntry[] SelectedAssetEntries { get; private set; }

    public AssetListController(VisualElement rootVisualElement)
    {
        AssetsListView = rootVisualElement
            .Q<VisualElement>("AssetsArea")
            .Q<ListView>("AssetsListView");
    }

    internal void FillAssetsVisualElement()
    {
        bool isSuccess = AtlasMaintainerHelpers
            .TryGetAllSprites(Selection.activeObject, out Sprite[] sprites);
        if (!isSuccess)
            return;

        // Defines what to create for each item in the given itemsSource
        AssetsListView.makeItem = () =>
        {
            Label newLabel = new();
            AssetListEntry entryController = new();

            newLabel.userData = entryController;
            entryController.SetVisualElement(newLabel);

            return newLabel;
        };

        // Defines the logic of data binding for each item in the given itemsSource
        AssetsListView.bindItem = (item, index) =>
        {
            AssetListEntry assetListEntry = item.userData as AssetListEntry;

            (item as Label).name = sprites[index].name + "Label";

            assetListEntry.SetSprite(sprites[index]);
            assetListEntry.UpdateText();
        };

        // Sets the itemsSource and triggers makeItem & bindItem
        AssetsListView.itemsSource = sprites;
    }

    internal Label GetLabelReferencingAsset(Sprite sprite)
    {
        return AssetsListView.Q<Label>(sprite.name + "Label");
    }

    internal bool GetSelectedLabelOrDefault(out Label label)
    {
        label = default;

        if (AssetsListView.selectedItem == null)
            return false;

        label = GetLabelReferencingAsset(AssetsListView.selectedItem as Sprite);

        return true;
    }

    internal bool GetSelectedLabelsOrDefault(out Label[] label)
    {
        label = default;
    
        IEnumerator<object> selectedAssetsEnumerator = AssetsListView.selectedItems.GetEnumerator();
    
        if (!selectedAssetsEnumerator.MoveNext())
            return false;
    
        List<Label> result = new();
        foreach (object item in AssetsListView.selectedItems)
            result.Add(GetLabelReferencingAsset(item as Sprite));
    
        label = result.ToArray();
    
        return true;
    }

    internal (Sprite[], Texture2D[]) ConvertLabelsToSpritesAndTextures(Label[] labels)
    {
        Sprite[] sprites = new Sprite[labels.Length];
        Texture2D[] textures = new Texture2D[labels.Length];

        for (int i = 0; i < labels.Length; i++)
        {
            sprites[i] = (labels[i].userData as AssetListEntry).ReferencedSprite;
            textures[i] = sprites[i].texture;
        }

        return (sprites, textures);
    }
}
