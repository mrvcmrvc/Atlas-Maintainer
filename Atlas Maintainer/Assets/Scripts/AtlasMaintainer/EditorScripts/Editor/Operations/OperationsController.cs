using UnityEngine.UIElements;

public class OperationsController
{
    public Button AddToAtlasButton { get; private set; }
    public Button RemoveFromAtlasButton { get; private set; }
    public Button MoveToAtlasButton { get; private set; }
    public Button CreateAtlasButton { get; private set; }
    public Button DeleteAtlasButton { get; private set; }

    public OperationsController(VisualElement rootVisualElement)
    {
        VisualElement operationsArea = rootVisualElement
            .Q<VisualElement>("OthersArea")
            .Q<VisualElement>("OperationsArea")
            .Q<VisualElement>("Buttons");

        AddToAtlasButton = operationsArea.Q<Button>("AddToAtlasButton");
        RemoveFromAtlasButton = operationsArea.Q<Button>("RemoveFromAtlasButton");
        MoveToAtlasButton = operationsArea.Q<Button>("MoveToAtlasButton");

        CreateAtlasButton = operationsArea.Q<Button>("CreateAtlasButton");
        DeleteAtlasButton = operationsArea.Q<Button>("DeleteAtlasButton");
    }

    internal void SetButtons()
    {
        ToggleButtons(false,
            AddToAtlasButton, RemoveFromAtlasButton,
            MoveToAtlasButton, DeleteAtlasButton);

        ToggleButtons(true,
            CreateAtlasButton);
    }

    internal void UpdateButtons(Label selectedAssetLabel, Label selectedAtlasLabel)
    {
        ToggleButtons(selectedAtlasLabel != null,
            DeleteAtlasButton);

        if (selectedAssetLabel == null || selectedAtlasLabel == null)
        {
            ToggleButtons(false,
                AddToAtlasButton, RemoveFromAtlasButton,
                MoveToAtlasButton);
            
            return;
        }

        ToggleButtons(true,
            AddToAtlasButton, RemoveFromAtlasButton,
            MoveToAtlasButton);
    }

    private void ToggleButtons(bool isEnabled, params Button[] buttons)
    {
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].SetEnabled(isEnabled);
    }
}
