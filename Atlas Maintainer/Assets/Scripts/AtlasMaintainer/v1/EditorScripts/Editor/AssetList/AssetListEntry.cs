using UnityEngine;
using UnityEngine.UIElements;

public class AssetListEntry
{
    public Sprite ReferencedSprite;
    public Label ReferencedLabel;

    public void SetSprite(Sprite sprite)
    {
        ReferencedSprite = sprite;
    }

    public void UpdateText()
    {
        if (!ReferencedSprite)
            return;

        ReferencedLabel.text = ReferencedSprite.name;
    }

    public void SetVisualElement(Label newLabel)
    {
        ReferencedLabel = newLabel;
    }
}
