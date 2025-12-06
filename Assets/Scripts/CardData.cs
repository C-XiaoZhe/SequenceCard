using UnityEngine;

public enum Suit { Spades, Hearts, Clubs, Diamonds, None } // 增加 None 用于算术牌
public enum CardType { Regular, Arithmetic } // 卡牌类型
public enum ArithmeticOp { None, Add, Subtract, Multiply } // 算术操作类型

[System.Serializable]
public class CardData
{
    public CardType cardType;
    public Suit suit;
    public int rank; // 对于普通牌是1-13，对于算术牌是操作数（如2, 3）
    public ArithmeticOp operation; // 算术操作类型

    // 普通牌构造函数
    public CardData(Suit suit, int rank)
    {
        this.cardType = CardType.Regular;
        this.suit = suit;
        this.rank = rank;
        this.operation = ArithmeticOp.None;
    }

    // 算术牌构造函数
    public CardData(ArithmeticOp op, int value)
    {
        this.cardType = CardType.Arithmetic;
        this.suit = Suit.None;
        this.rank = value; // 这里 rank 存储操作数
        this.operation = op;
    }

    public string GetName()
    {
        if (cardType == CardType.Regular)
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
        else
        {
            string opStr = "";
            switch (operation)
            {
                case ArithmeticOp.Add: opStr = "+"; break;
                case ArithmeticOp.Subtract: opStr = "-"; break;
                case ArithmeticOp.Multiply: opStr = "x"; break;
            }
            return $"Op: {opStr}{rank}";
        }
    }
}