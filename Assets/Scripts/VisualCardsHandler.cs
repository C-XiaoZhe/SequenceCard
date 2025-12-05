using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualCardsHandler : MonoBehaviour
{
    public static VisualCardsHandler instance;

    [Header("Sprite Library")]
    // 这里用来存放切分好的52张扑克牌精灵
    // 顺序非常重要！建议顺序：黑桃(A-K) -> 红桃(A-K) -> 梅花(A-K) -> 方块(A-K)
    public List<Sprite> cardSprites = new List<Sprite>();

    private void Awake()
    {
        instance = this;
    }

    // 根据花色和点数获取图片
    public Sprite GetCardSprite(Suit suit, int rank)
    {
        // 确保 Rank 是 1-13
        int validRank = Mathf.Clamp(rank, 1, 13);
        
        // 计算数组索引
        // 假设枚举顺序是 Spades(0), Hearts(1), Clubs(2), Diamonds(3)
        // 且每组有13张牌
        int index = (int)suit * 13 + (validRank - 1);

        if (index >= 0 && index < cardSprites.Count)
        {
            return cardSprites[index];
        }

        Debug.LogError($"找不到图片: {suit} {rank} (Index: {index})");
        return null;
    }
}