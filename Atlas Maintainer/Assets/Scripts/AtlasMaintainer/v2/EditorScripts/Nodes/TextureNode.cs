using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class TextureNodeData : NodeData
{
    public Sprite Sprite { get; }

    public TextureNodeData(Sprite sprite, Vector2 nodePosition) : base(nodePosition)
    {
        Sprite = sprite;
    }
}

public class TextureNode : NodeBase<TextureNodeData>
{
    protected override Sprite previewSprite { get; set; } = null;

    protected override string headerText { get; set; } = "Empty Texture Node";

    public TextureNode(GraphView view) : base(view)
    {
    }

    protected override void InitialiseCustomActions(TextureNodeData nodeData)
    {
        if (nodeData.Sprite != null)
        {
            previewSprite = nodeData.Sprite;
            headerText = nodeData.Sprite.name;
        }

        SetPosition(new Rect(nodeData.NodePosition, Vector2.zero));
    }

    protected override void DrawCustomActions()
    {
        CreateOutputPort();
    }

    public override void ConnectTo(NodeBase targetNode)
    {
        if (!IsPortCompatible(OutputPort, targetNode.InputPort))
            return;

        Edge edge = OutputPort.ConnectTo(targetNode.InputPort);

        OutputPort.Add(edge);
    }
}
