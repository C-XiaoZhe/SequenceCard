using UnityEngine;

// 花色枚举
public enum Suit { Spades, Hearts, Clubs, Diamonds }

[System.Serializable]
public class CardData
{
    public Suit suit;
    public int rank; // 1 (A) 到 13 (K)

    public CardData(Suit suit, int rank)
    {
        this.suit = suit;
        this.rank = rank;
    }

    // 获取显示的名称，方便调试
    public string GetName()
    {
        string rankStr = rank.ToString();
        switch (rank)
        {
            case 1: rankStr = "A"; break;
            case 11: rankStr = "J"; break;
            case 12: rankStr = "Q"; break;
            case 13: rankStr = "K"; break;
        }
        return $"{suit} {rankStr}";
    }
}