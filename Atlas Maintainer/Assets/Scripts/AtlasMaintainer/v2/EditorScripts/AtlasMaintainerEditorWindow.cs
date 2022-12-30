using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class AtlasMaintainerEditorWindow : EditorWindow
{
    [MenuItem("Assets/Atlas Maintainer/Open in Atlas Maintainer", true)]
    private static bool ValidateCanOpenInAtlasMaintainer()
    {
        foreach (Object selection in Selection.objects)
        {
            if (!AtlasMaintainerHelpers.ValidateSprite(selection)
                && !AtlasMaintainerHelpers.ValidatePrefab(selection)
                && !AtlasMaintainerHelpers.ValidateFolder(selection))
                return false;
        }

        return true;
    }

    [MenuItem("Assets/Atlas Maintainer/Open in Atlas Maintainer")]
    private static void OpenInAtlasMaintainer()
    {
        GenerateWindow();
    }

    private static void GenerateWindow()
    {
        AtlasMaintainerEditorWindow window = GetWindow<AtlasMaintainerEditorWindow>();
        window.titleContent = new GUIContent("Atlas Maintainer");
    }

    public void CreateGUI()
    {
        GenerateLayoutFromUXML(rootVisualElement);

        AtlasMaintainerGraphView graphView = AddGraphView();
        AddToolbar(graphView);
    }

    private void AddToolbar(AtlasMaintainerGraphView graphView)
    {
        Toolbar toolbar = new();

        Button packAllAtlasesButton = new(graphView.PackAllAtlases)
        {
            text = "Pack All Atlases",
        };

        StyleSheet toolbarStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Assets/Scripts/AtlasMaintainer/v2/EditorWindow/AtlasMaintainerToolbarStyleSheet.uss");

        toolbar.styleSheets.Add(toolbarStyleSheet);
        toolbar.Add(packAllAtlasesButton);

        rootVisualElement.Add(toolbar);
    }

    private AtlasMaintainerGraphView AddGraphView()
    {
        AtlasMaintainerGraphView graphView = new(this);
        graphView.StretchToParentSize();

        graphView.SetActiveObjects(Selection.objects);

        rootVisualElement.Add(graphView);

        return graphView;
    }

    private void GenerateLayoutFromUXML(VisualElement root)
    {
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/Scripts/AtlasMaintainer/v2/EditorWindow/AtlasMaintainerGraphTool.uxml");

        VisualElement visualTreeInstance = visualTree.Instantiate();
        root.Add(visualTreeInstance);
    }
}