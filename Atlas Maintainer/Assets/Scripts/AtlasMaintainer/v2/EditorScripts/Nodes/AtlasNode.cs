using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

public class AtlasNodeData : NodeData
{
    public SpriteAtlas SpriteAtlas { get; }

    public AtlasNodeData(SpriteAtlas spriteAtlas, Vector2 nodePosition) : base(nodePosition)
    {
        SpriteAtlas = spriteAtlas;
    }
}

public class AtlasNode : NodeBase<AtlasNodeData>
{
    protected override Sprite previewSprite { get; set; } = null;

    protected override string headerText { get; set; } = "Empty Atlas Node";

    protected override void InitialiseCustomActions(AtlasNodeData nodeData)
    {
        if (nodeData.SpriteAtlas != null)
            headerText = nodeData.SpriteAtlas.name;

        SetPosition(new Rect(nodeData.NodePosition, Vector2.zero));
    }

    protected override void DrawCustomActions()
    {
        CreateInputPort();

        //CreatePaginationButtons();
        CreatePackButton();
        CreateDeleteButton();
    }

    protected override void CreateHeader()
    {
        TextField header = new("");
        header.value = headerText;

        titleContainer.Insert(0, header);
    }

    private void CreateDeleteButton()
    {
        Button button = new(DeleteAtlas)
        {
            text = "Delete Atlas"
        };

        extensionContainer.Add(button);
    }

    private void CreatePackButton()
    {
        Button button = new(PackAtlas)
        {
            text = "Pack Atlas"
        };

        extensionContainer.Add(button);
    }

    public override void ConnectTo(NodeBase targetNode)
    {
        if (!IsPortCompatible(InputPort, targetNode.OutputPort))
            return;

        Edge edge = InputPort.ConnectTo(targetNode.OutputPort);

        InputPort.Add(edge);
    }

    public void PackAtlas()
    {
        SpriteAtlas spriteAtlas = ((AtlasNodeData)userData).SpriteAtlas;

        if (spriteAtlas == null)
            AtlasMaintainerHelpers.CreateAtlas(headerText, GetConnectedSprites());
        else
            AtlasMaintainerHelpers.PackAtlases(new[] { spriteAtlas });
    }

    private Sprite[] GetConnectedSprites()
    {
        List<Sprite> connectedSprites = new();

        IEnumerable<Edge> edges = InputPort.connections;
        foreach(Edge edge in edges)
        {
            TextureNode connectedTextureNode = (TextureNode)edge.output.userData;
            TextureNodeData textureNodeData = (TextureNodeData)connectedTextureNode.userData;

            connectedSprites.Add(textureNodeData.Sprite);
        }

        return connectedSprites.ToArray();
    }

    private void DeleteAtlas()
    {
        AtlasMaintainerHelpers.DeleteAtlases(new[] { ((AtlasNodeData)userData).SpriteAtlas });
    }
}
