using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class AtlasMaintainerSearchWindowProvider : ScriptableObject, ISearchWindowProvider
{
    private AtlasMaintainerGraphView graphView;
    private AtlasMaintainerEditorWindow editorWindow;
    private Texture2D entryIcon;

    public void Initialise(AtlasMaintainerEditorWindow editorWindow, AtlasMaintainerGraphView graphView)
    {
        this.graphView = graphView;
        this.editorWindow = editorWindow;

        entryIcon = new Texture2D(1, 1);
        entryIcon.SetPixel(0, 0, Color.clear);
        entryIcon.Apply();
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        List<SearchTreeEntry> searchTreeEntry = new()
        {
            new SearchTreeGroupEntry(new GUIContent("Add Element")),
            new SearchTreeEntry(new GUIContent("Add Atlas Node", entryIcon))
            {
                level = 1,
                userData = typeof(AtlasNode),
            },
        };

        return searchTreeEntry;
    }

    public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
    {
        if ((Type)searchTreeEntry.userData != typeof(AtlasNode))
            return true;

        Vector2 windowMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(
            editorWindow.rootVisualElement.parent, context.screenMousePosition - editorWindow.position.position);

        Vector2 graphMousePosition = graphView.contentViewContainer.WorldToLocal(windowMousePosition);

        graphView.AddAtlasNode(new AtlasNodeData(null, graphMousePosition));

        return true;
    }
}
