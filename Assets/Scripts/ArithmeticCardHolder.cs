using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
public class ArithmeticCardHolder : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private HorizontalCardHolder handHolder; // 引用手牌区

    public List<Card> arithmeticCards = new List<Card>();
    private List<CardData> arithmeticDeck = new List<CardData>();

    void Start()
    {
        InitializeDeck();
        RefillCards();
    }

    void InitializeDeck()
    {
        // 初始化算术牌库：乘2、加3、减2
        // 可以根据需要调整数量权重
        for(int i=0; i<5; i++) arithmeticDeck.Add(new CardData(ArithmeticOp.Multiply, 2));
        for(int i=0; i<5; i++) arithmeticDeck.Add(new CardData(ArithmeticOp.Add, 3));
        for(int i=0; i<5; i++) arithmeticDeck.Add(new CardData(ArithmeticOp.Subtract, 2));
        
        // 简单洗牌
        for (int i = 0; i < arithmeticDeck.Count; i++) {
            var temp = arithmeticDeck[i];
            int r = Random.Range(i, arithmeticDeck.Count);
            arithmeticDeck[i] = arithmeticDeck[r];
            arithmeticDeck[r] = temp;
        }
    }

    public void RefillCards()
    {
        // 假设算术区只维持 1 张牌（或者你可以改成多张）
        if (arithmeticCards.Count < 1 && arithmeticDeck.Count > 0)
        {
            CardData data = arithmeticDeck[0];
            arithmeticDeck.RemoveAt(0);

            GameObject slotObj = Instantiate(slotPrefab, transform);
            Card cardScript = slotObj.GetComponentInChildren<Card>();
            
            // 设置数据（注意 Card.cs 里的 SetData 需要能接受 Arithmetic 类型）
            cardScript.SetData(data);
            cardScript.name = data.GetName();
            
            // 绑定基础事件，确保可以被点击选中
            // 注意：这里不需要拖拽交换位置的逻辑，除非你想让它也能动
            // 简单起见，我们只允许点击
            cardScript.PointerUpEvent.AddListener((c, longPress) => {
                // 这里可以处理单独的选中逻辑，或者复用 Card 自身的选中逻辑
                // 如果 Card.cs 的 OnPointerUp 已经处理了 selected 状态切换，这里就不需要额外做什么
            });

            arithmeticCards.Add(cardScript);
            
            // 更新视觉索引（虽然只有一张，为了保持一致性）
            if (cardScript.cardVisual != null) 
                cardScript.cardVisual.UpdateIndex(transform.childCount);
        }
    }

    // 执行算术操作的方法
    public void ApplyArithmeticEffect()
    {
        // 1. 获取选中的算术牌
        Card selectedArithmeticCard = arithmeticCards.FirstOrDefault(c => c.selected);
        if (selectedArithmeticCard == null) return;

        // 2. 获取手牌区选中的牌
        List<Card> selectedHandCards = handHolder.cards.Where(c => c.selected).ToList();
        if (selectedHandCards.Count == 0) return;

        // 3. 执行计算
        foreach (Card targetCard in selectedHandCards)
        {
            if (targetCard.data.cardType != CardType.Regular) continue;

            int newValue = targetCard.data.rank;
            switch (selectedArithmeticCard.data.operation)
            {
                case ArithmeticOp.Add:
                    newValue += selectedArithmeticCard.data.rank;
                    break;
                case ArithmeticOp.Subtract:
                    newValue -= selectedArithmeticCard.data.rank;
                    break;
                case ArithmeticOp.Multiply:
                    newValue *= selectedArithmeticCard.data.rank;
                    break;
            }

            // 模 13 运算 (注意处理负数和 0)
            // 规则：范围 1-13。如果结果是 14 -> 1, 15 -> 2。
            // 这里的模运算公式：((newValue - 1) % 13 + 13) % 13 + 1
            // 解释：先减1变0-12范围，取模处理负数，再加1变回1-13
            newValue = ((newValue - 1) % 13 + 13) % 13 + 1;

            // 4. 更新卡牌数据
            targetCard.data.rank = newValue;
            targetCard.UpdateCardSprite(); // 刷新卡面显示
            
            // 可选：给被修改的牌加个特效反馈
            targetCard.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
        }

        // 5. 消耗算术牌
        arithmeticCards.Remove(selectedArithmeticCard);
        Destroy(selectedArithmeticCard.transform.parent.gameObject);
        
        // 6. 补货
        StartCoroutine(WaitAndRefill());
    }

    IEnumerator WaitAndRefill()
    {
        yield return new WaitForSeconds(0.5f);
        RefillCards();
    }
}