using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualCardsHandler : MonoBehaviour
{
    public static VisualCardsHandler instance;

    [Header("Sprite Library")]
    public List<Sprite> cardSprites = new List<Sprite>();
    
    [Header("Arithmetic Sprites")]
    // 你可以在 Inspector 里拖入代表 x2, +3, -2 的图片
    // 如果没有，我们暂时用 null 处理，并在 CardVisual 里特殊处理
    public Sprite addSprite;
    public Sprite subSprite;
    public Sprite mulSprite;

    private void Awake()
    {
        instance = this;
    }

    public Sprite GetCardSprite(CardData data)
    {
        if (data.cardType == CardType.Regular)
        {
            int validRank = Mathf.Clamp(data.rank, 1, 13);
            int index = (int)data.suit * 13 + (validRank - 1);
            if (index >= 0 && index < cardSprites.Count) return cardSprites[index];
        }
        else if (data.cardType == CardType.Arithmetic)
        {
            // 返回对应的算术牌图片
            switch (data.operation)
            {
                case ArithmeticOp.Add: return addSprite;
                case ArithmeticOp.Subtract: return subSprite;
                case ArithmeticOp.Multiply: return mulSprite;
            }
        }
        return null;
    }
}