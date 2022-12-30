using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class NodeData
{
    public Vector2 NodePosition { get; }

    public NodeData(Vector2 nodePosition)
    {
        NodePosition = nodePosition;
    }
}

public abstract class NodeBase : Node
{
    public Port InputPort { get; private set; } = null;
    public Port OutputPort { get; private set; } = null;

    protected abstract Sprite previewSprite { get; set; }
    protected abstract string headerText { get; set; }

    public virtual void Draw()
    {
        CreateHeader();

        CreateAssetPreview();

        DrawCustomActions();

        RefreshExpandedState();
    }

    protected virtual void CreateHeader()
    {
        Label header = new(headerText);
        titleContainer.AddToClassList(".unity-toggle__text");

        titleContainer.Insert(0, header);
    }

    protected virtual void CreateAssetPreview()
    {
        VisualElement assetPreview = new();
        assetPreview.AddToClassList(".atlasMaintainer-node__preview-container");
        assetPreview.style.width = 100f;
        assetPreview.style.height = 100f;

        if (previewSprite != null)
            assetPreview.style.backgroundImage = new StyleBackground(previewSprite);

        VisualElement assetPreviewBackground = new();
        assetPreviewBackground.AddToClassList(".atlasMaintainer-node__preview-container-parent");

        assetPreviewBackground.Add(assetPreview);

        extensionContainer.Add(assetPreviewBackground);
    }

    protected virtual void CreateInputPort()
    {
        InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(Sprite));
        InputPort.userData = this;
        InputPort.portName = "Input";
        outputContainer.Add(InputPort);
    }

    protected virtual void CreateOutputPort()
    {
        OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(Sprite));
        OutputPort.userData = this;
        OutputPort.portName = "Output";
        outputContainer.Add(OutputPort);
    }

    protected virtual void DrawCustomActions()
    {
    }

    public abstract void ConnectTo(NodeBase targetNode);
    public abstract Type GetNodeType();

    public static bool IsPortCompatible(Port fromPort, Port toPort)
    {
        if (fromPort.direction == toPort.direction
            || ((NodeBase)fromPort.userData).GetNodeType() == ((NodeBase)toPort.userData).GetNodeType())
            return false;

        return true;
    }
}

public abstract class NodeBase<T> : NodeBase where
    T : NodeData
{
    public void Initialise(T nodeData)
    {
        userData = nodeData;

        titleContainer.AddToClassList(".atlasMaintainer-node__title-container");
        outputContainer.AddToClassList(".atlasMaintainer-node__output-container");

        InitialiseCustomActions(nodeData);
    }

    public override Type GetNodeType()
    {
        return typeof(T);
    }

    protected abstract void InitialiseCustomActions(T nodeData);
}