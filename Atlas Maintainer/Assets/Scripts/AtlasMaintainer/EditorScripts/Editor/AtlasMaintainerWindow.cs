using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

public class AtlasMaintainerWindow : EditorWindow
{
    private ListView atlasListView;

    [MenuItem("Assets/Atlas Maintainer/Open in Atlas Maintainer", true)]
    private static bool ValidateCanOpenInAtlasMaintainer()
    {
        return AtlasMaintainerHelpers.ValidateSprite(Selection.activeObject);
    }

    [MenuItem("Assets/Atlas Maintainer/Open in Atlas Maintainer")]
    private static void OpenInAtlasMaintainer()
    {        
        if (AtlasMaintainerHelpers.ValidateSprite(Selection.activeObject))
        {
            // TODO: Open sprite in atlas maintainer

            GenerateWindow();
        }
        else if (AtlasMaintainerHelpers.ValidatePrefab(Selection.activeObject))
        {
            // TODO: Open prefab in atlas maintainer

            GenerateWindow();
        }
    }

    private static void GenerateWindow()
    {
        AtlasMaintainerWindow window = GetWindow<AtlasMaintainerWindow>();
        window.titleContent = new GUIContent("Atlas Maintainer");
    }

    public void CreateGUI()
    {
        GenerateLayoutFromUXML(rootVisualElement);

        FillAtlasesVisualElement(rootVisualElement);
    }

    private void GenerateLayoutFromUXML(VisualElement root)
    {
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/AtlasMaintainer/EditorWindow/AtlasMaintainerWindow.uxml");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);
    }

    private void FillAtlasesVisualElement(VisualElement root)
    {
        bool isSuccess = AtlasMaintainerHelpers.TryGetAllAtlases(out SpriteAtlas[] allAtlases);
        if (!isSuccess)
            return;
        
        VisualElement atlasesArea = root.Q<VisualElement>("AtlasesArea");
        atlasListView = atlasesArea.Q<ListView>("AtlasesListView");

        // Defines what to create for each item in the given itemsSource
        atlasListView.makeItem = () => new Label();

        // Defines the logic of data binding for each item in the given itemsSource
        atlasListView.bindItem = (item, index) =>
        {
            (item as Label).text = allAtlases[index].name;
        };

        // Sets the itemsSource and triggers makeItem & bindItem
        atlasListView.itemsSource = allAtlases;
    }
}
