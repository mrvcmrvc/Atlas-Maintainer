using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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

    public AtlasNode(GraphView view) : base(view)
    {
    }

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

        RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.NoTrickleDown);
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.tricklesDown && evt.clickCount == 2)
            EditorGUIUtility.PingObject(((AtlasNodeData)userData).SpriteAtlas);
    }

    protected override void CreateHeader()
    {
        TextField header = new("");
        header.value = headerText;

        titleContainer.Insert(0, header);

        header.RegisterValueChangedCallback(OnNodeNameChanged);
    }

    private void OnNodeNameChanged(ChangeEvent<string> changeEvent)
    {
        headerText = changeEvent.newValue;
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
        {
            spriteAtlas = AtlasMaintainerHelpers.CreateAtlas(headerText, GetConnectedSprites());
            userData = new AtlasNodeData(spriteAtlas, ((AtlasNodeData)userData).NodePosition);
        }
        else
        {
            if (!string.Equals(headerText, spriteAtlas.name))
                AtlasMaintainerHelpers.RenameAtlas(spriteAtlas, headerText);

            AtlasMaintainerHelpers.PackAtlases(new[] { spriteAtlas });
        }
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
        AtlasMaintainerHelpers.TryDeleteAtlases(new[] { ((AtlasNodeData)userData).SpriteAtlas }, out bool[] result);

        if (result[0])
        {
            List<Edge> edges = InputPort.connections.ToList();
            foreach (Edge edge in edges)
            {
                edge.output.Disconnect(edge);
                edge.input.Disconnect(edge);

                edge.parent.Remove(edge);
            }
            
            graphView.RemoveElement(this);
        }
    }
}
