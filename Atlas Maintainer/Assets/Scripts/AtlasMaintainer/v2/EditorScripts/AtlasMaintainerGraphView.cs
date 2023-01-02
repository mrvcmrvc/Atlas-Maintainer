using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.U2D;

public class AtlasMaintainerGraphView : GraphView
{
    AtlasMaintainerSearchWindowProvider searchWindowProvider;
    AtlasMaintainerEditorWindow editorWindow;

    #region Initialisation

    public AtlasMaintainerGraphView(AtlasMaintainerEditorWindow editorWindow)
    {
        this.editorWindow = editorWindow;

        AddManipulators();
        AddGrids();
        AddSearchWindowProvider();

        AddStyleSheet();
    }

    private void AddManipulators()
    {
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());

        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        deleteSelection = DeleteNode;
    }

    private void AddGrids()
    {
        GridBackground gridBackground = new GridBackground();
        gridBackground.StretchToParentSize();

        Insert(0, gridBackground);
    }

    private void AddSearchWindowProvider()
    {
        if (searchWindowProvider == null)
            searchWindowProvider = ScriptableObject.CreateInstance<AtlasMaintainerSearchWindowProvider>();

        searchWindowProvider.Initialise(editorWindow, this);

        nodeCreationRequest = context =>
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindowProvider);
    }

    private void AddStyleSheet()
    {
        StyleSheet graphViewStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Assets/Scripts/AtlasMaintainer/v2/EditorWindow/AtlasMaintainerGraphViewStyleSheet.uss");

        StyleSheet nodeStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Assets/Scripts/AtlasMaintainer/v2/EditorWindow/AtlasMaintainerNodeStyleSheet.uss");

        styleSheets.Add(graphViewStyleSheet);
        styleSheets.Add(nodeStyleSheet);
    }

    #endregion

    #region Add / Delete Nodes

    private List<TextureNode> CreateTextureNodes(Sprite[] sprites)
    {
        List<TextureNode> textureNodes = new();

        Vector2 position = Vector2.zero;
        for (int i = 0; i < sprites.Length; i++)
        {
            TextureNode textureNode = AddTextureNode(new TextureNodeData(sprites[i], position));
            textureNodes.Add(textureNode);

            position.y += 220f;
        }

        return textureNodes;
    }

    private TextureNode AddTextureNode(TextureNodeData textureNodeData)
    {
        TextureNode newNode = new(this);
        newNode.Initialise(textureNodeData);
        newNode.Draw();
        AddElement(newNode);

        return newNode;
    }

    private List<AtlasNode> CreateAtlasNodes(SpriteAtlas[] spriteAtlases)
    {
        AtlasNode[] atlasNodesInGraph = GetAllNodesInGraph<AtlasNode>();

        List<AtlasNode> atlasNodes = new();

        Vector2 position = Vector2.zero;
        for (int i = 0; i < spriteAtlases.Length; i++)
        {
            bool nodeFound = false;
            for (int z = 0; z < atlasNodesInGraph.Length; z++)
            {
                if (((AtlasNodeData)atlasNodesInGraph[z].userData).SpriteAtlas == spriteAtlases[i])
                {
                    nodeFound = true;
                    atlasNodes.Add(atlasNodesInGraph[z]);
                    break;
                }
            }

            if (!nodeFound)
            {
                AtlasNode atlasNode = AddAtlasNode(new AtlasNodeData(spriteAtlases[i], position));
                atlasNodes.Add(atlasNode);

                position.y += 220f;
            }
        }

        return atlasNodes;
    }

    public AtlasNode AddAtlasNode(AtlasNodeData atlasNodeData)
    {
        AtlasNode newNode = new(this);
        newNode.Initialise(atlasNodeData);
        newNode.Draw();
        AddElement(newNode);

        return newNode;
    }

    public Group AddGroup(string title, IEnumerable<GraphElement> content, Vector2 localMousePosition)
    {
        Group newGroup = new()
        {
            title = title
        };

        newGroup.SetPosition(new Rect(localMousePosition, Vector2.zero));

        AddElement(newGroup);

        newGroup.AddElements(content);
        newGroup.UpdateGeometryFromContent();

        return newGroup;
    }

    private void DeleteNode(string operationName, AskUser askUser)
    {
        List<ISelectable> filteredSelection = new();
        for (int i = selection.Count - 1; i >= 0; i--)
        {
            ISelectable targetSelection = selection[i];

            if (targetSelection is not TextureNode
                && targetSelection is not Group)
                filteredSelection.Add(targetSelection);
        }

        ClearSelection();

        selection = filteredSelection;

        DeleteSelection();
    }

    #endregion

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        List<Port> compatiblePorts = new();

        Type nodeType = ((NodeBase)startPort.userData).GetNodeType();

        ports.ForEach(port =>
        {
            if (!NodeBase.IsPortCompatible(startPort, port))
                return;

            compatiblePorts.Add(port);
        });

        return compatiblePorts;
    }

    public void SetActiveObjects(UnityEngine.Object[] activeObjects)
    {
        for (int y = 0; y < activeObjects.Length; y++)
        {
            bool isSuccess = AtlasMaintainerHelpers
                .TryGetAllSprites(activeObjects[y], out Sprite[] sprites);
            if (!isSuccess)
                return;

            List<TextureNode> textureNodes = CreateTextureNodes(sprites);

            for (int i = 0; i < textureNodes.Count; i++)
            {
                SpriteAtlas[] atlases = AtlasMaintainerHelpers.GetSpriteAtlasesOrEmpty(((TextureNodeData)textureNodes[i].userData).Sprite);
                List<AtlasNode> atlasNodes = CreateAtlasNodes(atlases);

                for (int x = 0; x < atlasNodes.Count; x++)
                    textureNodes[i].ConnectTo(atlasNodes[x]);
            }

            if (textureNodes.Count > 1)
                AddGroup(activeObjects[y].name, textureNodes, Vector2.zero);
        }
    }

    public void PackAllAtlases()
    {
        AtlasNode[] atlasNodes = GetAllNodesInGraph<AtlasNode>();
        for (int i = 0; i < atlasNodes.Length; i++)
            atlasNodes[i].PackAtlas();
    }

    private T[] GetAllNodesInGraph<T>() where
    T : Node
    {
        List<T> nodes = new();

        this.nodes.ForEach(node =>
        {
            if (node is not T)
                return;

            nodes.Add((T)node);
        });

        return nodes.ToArray();
    }
}
