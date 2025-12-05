using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;
using UnityEngine.UI; 
using TMPro; 

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

    [Header("Played Area Settings")]
    public Transform playedCardArea; 
    public TMP_Text resultText;      

    // [新增] 用于记录当前的打字机协程，以便在需要时停止它
    private Coroutine typewritingCoroutine;

    void Start()
    {
        // --- 步骤 1: 清理场景中残留的旧卡牌 ---
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
        DealCards(10);    

        // --- 步骤 4: UI 组件初始化 ---
        rect = GetComponent<RectTransform>();

        // 初始化文本为空
        if (resultText != null) resultText.text = "";

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
        cards = new List<Card>(); 
        
        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0) break;

            CardData data = deck[0];
            deck.RemoveAt(0);

            GameObject slotObj = Instantiate(slotPrefab, transform);
            Card cardScript = slotObj.GetComponentInChildren<Card>();
            
            cardScript.SetData(data);
            
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
        List<Card> selectedCards = cards.Where(c => c.selected).ToList();

        if (selectedCards.Count < 3)
        {
            Debug.Log("请至少选择3张牌！");
            return;
        }

        // 1. 排序
        selectedCards.Sort((a, b) => a.ParentIndex().CompareTo(b.ParentIndex()));

        // 2. 提取点数
        List<int> ranks = selectedCards.Select(c => c.data.rank).ToList();
        
        // 3. 判定
        List<SequenceEvaluator.SequenceType> results = SequenceEvaluator.Evaluate(ranks);

        if (results.Count > 0)
        {
            // --- 特效判定逻辑 ---
            bool hasFibonacci = results.Contains(SequenceEvaluator.SequenceType.Fibonacci);
            int otherConditionsCount = 0;
            foreach (var type in results)
            {
                if (type != SequenceEvaluator.SequenceType.Fibonacci) otherConditionsCount++;
            }

            string targetEdition = "REGULAR"; 
            if (hasFibonacci) targetEdition = "NEGATIVE";
            else if (otherConditionsCount >= 3) targetEdition = "POLYCHROME";

            foreach (Card card in selectedCards)
            {
                if (card.cardVisual != null) card.cardVisual.UpdateShaderEffect(targetEdition);
            }

            // --- 文本构建 ---
            // 修改点1：移除"判定成功: "前缀
            string resultStr = "判定结果:  "; 
            foreach (var type in results)
            {
                // 修改点2：加上“数列”二字
                switch(type)
                {
                    case SequenceEvaluator.SequenceType.Geometric: resultStr += "等比数列   "; break;
                    case SequenceEvaluator.SequenceType.Arithmetic: resultStr += "等差数列   "; break;
                    case SequenceEvaluator.SequenceType.Increasing: resultStr += "递增数列   "; break;
                    case SequenceEvaluator.SequenceType.Decreasing: resultStr += "递减数列   "; break;
                    case SequenceEvaluator.SequenceType.Odd: resultStr += "奇数列   "; break;
                    case SequenceEvaluator.SequenceType.Even: resultStr += "偶数列   "; break;
                    case SequenceEvaluator.SequenceType.Fibonacci: resultStr += "斐波那契数列   "; break;
                }
            }
            
            // 修改点3：只显示数列名称，不再显示特效描述
            string fullText = resultStr; 

            // --- 调用打字机效果 ---
            if (resultText != null)
            {
                // 如果上一次的打字动画还没播完，先停止它
                if (typewritingCoroutine != null) StopCoroutine(typewritingCoroutine);
                // 开启新的打字动画
                typewritingCoroutine = StartCoroutine(TypewriterEffect(fullText));
            }

            PerformPlaySuccess(selectedCards);
        }
        else
        {
            Debug.Log("判定失败");
            if (resultText != null)
            {
                if (typewritingCoroutine != null) StopCoroutine(typewritingCoroutine);
                resultText.text = "无效牌型";
                resultText.maxVisibleCharacters = 999; // 确保完全显示
            }
        }
    }

    // [新增] 打字机效果协程
    IEnumerator TypewriterEffect(string fullText)
    {
        resultText.text = fullText;
        resultText.maxVisibleCharacters = 0; // 先隐藏所有文字

        // 强制刷新网格，确保 TMP 能正确计算出字符数量 (textInfo)
        resultText.ForceMeshUpdate();

        int totalChars = resultText.textInfo.characterCount; // 获取实际字符数（不包含富文本标签）
        
        for (int i = 0; i <= totalChars; i++)
        {
            resultText.maxVisibleCharacters = i;
            // 这里的  是打字速度，可以根据需要调整
            yield return new WaitForSeconds(0.1f); 
        }
    }

    void PerformPlaySuccess(List<Card> playedCards)
    {
        if (playedCardArea != null)
        {
            for (int i = playedCardArea.childCount - 1; i >= 0; i--)
            {
                Destroy(playedCardArea.GetChild(i).gameObject);
            }
        }

        foreach (Card c in playedCards)
        {
            cards.Remove(c); 
            GameObject oldSlot = c.transform.parent.gameObject; 

            if (playedCardArea != null)
            {
                c.transform.SetParent(playedCardArea); 
                c.transform.localPosition = Vector3.zero; 
                c.transform.localRotation = Quaternion.identity;
                c.transform.localScale = Vector3.one;
                c.OnPlayed(); 
            }
            else
            {
                Destroy(c.gameObject); 
            }

            Destroy(oldSlot); 
        }

        if (playedCardArea != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(playedCardArea.GetComponent<RectTransform>());

        StartCoroutine(WaitAndRefill());
    }

    IEnumerator WaitAndRefill()
    {
        yield return new WaitForSeconds(0.2f); 

        int cardsNeeded = 10 - cards.Count;

        if (cardsNeeded > 0)
        {
            for (int i = 0; i < cardsNeeded; i++)
            {
                if (deck.Count == 0) break;

                CardData data = deck[0];
                deck.RemoveAt(0);

                GameObject slotObj = Instantiate(slotPrefab, transform);
                Card cardScript = slotObj.GetComponentInChildren<Card>();
                
                cardScript.SetData(data);
                
                cardScript.PointerEnterEvent.AddListener(CardPointerEnter);
                cardScript.PointerExitEvent.AddListener(CardPointerExit);
                cardScript.BeginDragEvent.AddListener(BeginDrag);
                cardScript.EndDragEvent.AddListener(EndDrag);
                
                cardScript.name = $"{data.suit}_{data.rank}";
                cards.Add(cardScript);
            }
        }

        yield return new WaitForEndOfFrame();

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
        if (selectedCard == null) return;

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

        if (selectedCard == null) return;
        if (isCrossing) return;

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

        if (cards[index].cardVisual == null) return;

        bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
        cards[index].cardVisual.Swap(swapIsRight ? -1 : 1);

        foreach (Card card in cards)
        {
            card.cardVisual.UpdateIndex(transform.childCount);
        }
    }
}