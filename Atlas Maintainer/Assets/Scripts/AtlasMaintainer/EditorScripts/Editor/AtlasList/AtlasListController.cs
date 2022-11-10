using UnityEngine.U2D;
using UnityEngine.UIElements;

public class AtlasListController
{
    public ListView AtlasListView { get; private set; }
    public AtlasListEntry SelectedAtlasEntry { get; private set; }

    public AtlasListController(VisualElement rootVisualElement)
    {
        AtlasListView = rootVisualElement
            .Q<VisualElement>("AtlasesArea")
            .Q<ListView>("AtlasesListView");
    }

    internal void FillAtlasesVisualElement()
    {
        bool isSuccess = AtlasMaintainerHelpers
            .TryGetAllAtlases(out SpriteAtlas[] allAtlases);
        if (!isSuccess)
            return;

        // Defines what to create for each item in the given itemsSource
        AtlasListView.makeItem = () =>
        {
            Label newLabel = new();
            AtlasListEntry entryController = new();

            newLabel.SetEnabled(false);
            newLabel.focusable = false;
            newLabel.userData = entryController;
            entryController.SetVisualElement(newLabel);

            return newLabel;
        };

        // Defines the logic of data binding for each item in the given itemsSource
        AtlasListView.bindItem = (item, index) =>
        {
            AtlasListEntry atlasListEntry = item.userData as AtlasListEntry;

            (item as Label).name = allAtlases[index].name + "Label";

            atlasListEntry.SetAtlas(allAtlases[index]);
            atlasListEntry.UpdateText();
        };

        // Sets the itemsSource and triggers makeItem & bindItem
        AtlasListView.itemsSource = allAtlases;
    }

    internal void EnableAtlases(SpriteAtlas[] atlases)
    {
        ToggleAllAtlases(false);

        for (int i = 0; i < atlases.Length; i++)
        {
            Label label = GetLabelReferencingAtlas(atlases[i]);

            label.SetEnabled(true);
            label.focusable = true;
        }
    }

    private void ToggleAllAtlases(bool isEnabled)
    {
        AtlasListView.Query().Children<Label>().ForEach(l => l.SetEnabled(isEnabled));
    }

    internal Label GetLabelReferencingAtlas(SpriteAtlas spriteAtlas)
    {
        return AtlasListView.Q<Label>(spriteAtlas.name + "Label");
    }

    internal bool GetSelectedLabelOrDefault(out Label label)
    {
        label = default;

        if (AtlasListView.selectedItem == null)
            return false;

        label = GetLabelReferencingAtlas(AtlasListView.selectedItem as SpriteAtlas);

        return true;
    }
}
