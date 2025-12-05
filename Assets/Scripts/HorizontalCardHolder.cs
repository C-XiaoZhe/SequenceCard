using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;
using System.Linq;

public class HorizontalCardHolder : MonoBehaviour
{

    [SerializeField] private Card selectedCard;
    [SerializeReference] private Card hoveredCard;

    [SerializeField] private GameObject slotPrefab;
    private RectTransform rect;

    [Header("Spawn Settings")]
    [SerializeField] private int cardsToSpawn = 7;
    public List<Card> cards;

    bool isCrossing = false;
    [SerializeField] private bool tweenCardReturn = true;

    // [新增] 牌堆
    private List<CardData> deck = new List<CardData>();

    void Start()
    {
        // --- 步骤 1: 清理场景中残留的旧卡牌 ---
        // 防止场景里预先放了卡牌，或者旧逻辑生成的卡牌残留
        // 注意：一定要检查 transform.childCount，否则会报错
        if (transform.childCount > 0)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        // --- 步骤 2: 初始化并洗牌 ---
        InitializeDeck(); 
        ShuffleDeck();

        // --- 步骤 3: 只发 10 张牌 ---
        // (不要再保留旧的 for(int i=0; i<cardsToSpawn...) 循环了！)
        DealCards(10);    

        // --- 步骤 4: UI 组件初始化 ---
        rect = GetComponent<RectTransform>();

        // 【关键错误点】：不要再写 cards = GetComponentsInChildren<Card>().ToList();
        // 因为 DealCards 方法里已经把生成的牌加入到 cards 列表了。
        // 如果这里再获取一次，可能会把即将被销毁的尸体也抓进来。
        
        // 为每张牌绑定事件（DealCards里其实已经做了一部分，但为了保险起见，这里不需要再重复绑定，除非有遗漏）
        // 原有的逻辑是将事件绑定放在 GetComponentsInChildren 之后，
        // 现在这些逻辑都应该移到 DealCards 内部（如我上一步提供的代码所示）。

        StartCoroutine(Frame());

        IEnumerator Frame()
        {
            yield return new WaitForSecondsRealtime(.1f);
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].cardVisual != null)
                    cards[i].cardVisual.UpdateIndex(transform.childCount);
            }
        }
    }

    void InitializeDeck()
    {
        deck.Clear();
        foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
        {
            for (int i = 1; i <= 13; i++)
            {
                deck.Add(new CardData(s, i));
            }
        }
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            CardData temp = deck[i];
            int randomIndex = UnityEngine.Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    void DealCards(int count)
    {
        cards = new List<Card>(); // 清空列表
        
        // 清理现有的子物体（如果需要重置的话）
        // foreach (Transform child in transform) Destroy(child.gameObject);

        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0) break;

            CardData data = deck[0];
            deck.RemoveAt(0);

            // 生成卡槽和卡牌
            GameObject slotObj = Instantiate(slotPrefab, transform);
            Card cardScript = slotObj.GetComponentInChildren<Card>();
            
            // 重要：将数据传递给卡牌
            cardScript.SetData(data);
            
            // 绑定原有事件
            cardScript.PointerEnterEvent.AddListener(CardPointerEnter);
            cardScript.PointerExitEvent.AddListener(CardPointerExit);
            cardScript.BeginDragEvent.AddListener(BeginDrag);
            cardScript.EndDragEvent.AddListener(EndDrag);
            cardScript.name = $"{data.suit}_{data.rank}";

            cards.Add(cardScript);
        }
    }

    
    public void PlaySelectedCards()
    {
        // 1. 找出所有被选中的牌 (selected 为 true)
        List<Card> selectedCards = cards.Where(c => c.selected).ToList();

        // 2. 验证数量
        if (selectedCards.Count < 3)
        {
            Debug.Log("请至少选择3张牌！");
            return;
        }

        // 3. 核心步骤：按视觉顺序（从左到右）排序
        // 因为玩家可能先选了右边的牌，再选左边的，或者拖拽改变了位置
        selectedCards.Sort((a, b) => a.ParentIndex().CompareTo(b.ParentIndex()));

        // 4. 提取点数用于判定
        List<int> ranks = selectedCards.Select(c => c.data.rank).ToList();
        
        // 打印调试：看看玩家打出了什么
        string playStr = "玩家打出: ";
        foreach (var r in ranks) playStr += r + " ";
        Debug.Log(playStr);

        // 5. 进行数列判定
        List<SequenceEvaluator.SequenceType> results = SequenceEvaluator.Evaluate(ranks);

        if (results.Count > 0)
        {
            string resultStr = "判定成功! 符合条件: ";
            foreach (var type in results) resultStr += type.ToString() + " ";
            Debug.Log(resultStr);

            // TODO: 在这里执行出牌动画，销毁卡牌，计算分数等
            PerformPlaySuccess(selectedCards);
        }
        else
        {
            Debug.Log("判定失败：不符合任何数列条件。");
            // 这里可以播放一个失败的晃动动画
        }
    }

    void PerformPlaySuccess(List<Card> playedCards)
    {
        // 简单的出牌效果：销毁这些牌的父物体（Slot）并从列表中移除
        foreach (Card c in playedCards)
        {
            cards.Remove(c); // 从逻辑列表移除
            Destroy(c.transform.parent.gameObject); // 销毁场景物体(Slot)
        }
        
        // 重新整理剩余卡牌的索引
        StartCoroutine(WaitAndRefill());
    }

    IEnumerator WaitAndRefill()
    {
        yield return new WaitForEndOfFrame();
        // 更新剩余卡牌的视觉索引
        foreach (Card card in cards)
        {
            if (card.cardVisual != null) card.cardVisual.UpdateIndex(transform.childCount);
        }
    }



    private void BeginDrag(Card card)
    {
        selectedCard = card;
    }


    void EndDrag(Card card)
    {
        if (selectedCard == null)
            return;

        selectedCard.transform.DOLocalMove(selectedCard.selected ? new Vector3(0,selectedCard.selectionOffset,0) : Vector3.zero, tweenCardReturn ? .15f : 0).SetEase(Ease.OutBack);

        rect.sizeDelta += Vector2.right;
        rect.sizeDelta -= Vector2.right;

        selectedCard = null;

    }

    void CardPointerEnter(Card card)
    {
        hoveredCard = card;
    }

    void CardPointerExit(Card card)
    {
        hoveredCard = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (hoveredCard != null)
            {
                Destroy(hoveredCard.transform.parent.gameObject);
                cards.Remove(hoveredCard);

            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            foreach (Card card in cards)
            {
                card.Deselect();
            }
        }

        if (selectedCard == null)
            return;

        if (isCrossing)
            return;

        for (int i = 0; i < cards.Count; i++)
        {

            if (selectedCard.transform.position.x > cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() < cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }

            if (selectedCard.transform.position.x < cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() > cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }
        }
    }

    void Swap(int index)
    {
        isCrossing = true;

        Transform focusedParent = selectedCard.transform.parent;
        Transform crossedParent = cards[index].transform.parent;

        cards[index].transform.SetParent(focusedParent);
        cards[index].transform.localPosition = cards[index].selected ? new Vector3(0, cards[index].selectionOffset, 0) : Vector3.zero;
        selectedCard.transform.SetParent(crossedParent);

        isCrossing = false;

        if (cards[index].cardVisual == null)
            return;

        bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
        cards[index].cardVisual.Swap(swapIsRight ? -1 : 1);

        //Updated Visual Indexes
        foreach (Card card in cards)
        {
            card.cardVisual.UpdateIndex(transform.childCount);
        }
    }

}
