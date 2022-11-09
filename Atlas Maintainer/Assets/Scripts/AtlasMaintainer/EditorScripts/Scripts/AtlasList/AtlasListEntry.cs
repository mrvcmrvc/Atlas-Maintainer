using UnityEngine.U2D;
using UnityEngine.UIElements;

public class AtlasListEntry
{
    public SpriteAtlas ReferencedAtlas;
    public Label ReferencedLabel;

    public void SetAtlas(SpriteAtlas spriteAtlas)
    {
        ReferencedAtlas = spriteAtlas;
    }

    public void UpdateText()
    {
        if (!ReferencedAtlas)
            return;

        ReferencedLabel.text = ReferencedAtlas.name;
    }

    public void SetVisualElement(Label newLabel)
    {
        ReferencedLabel = newLabel;
    }
}
