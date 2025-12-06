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

    // 牌堆
    private List<CardData> deck = new List<CardData>();

    [Header("Played Area Settings")]
    public Transform playedCardArea; 
    public TMP_Text resultText;      

    [Header("References")]
    // [重要] 请在 Inspector 中将场景里的 ArithmeticArea 物体拖入此处
    public ArithmeticCardHolder arithmeticHolder; 

    // 打字机协程引用
    private Coroutine typewritingCoroutine;

    void Start()
    {
        // 1. 清理
        if (transform.childCount > 0)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        // 2. 初始化
        InitializeDeck(); 
        ShuffleDeck();

        // 3. 发牌
        DealCards(10);    

        // 4. UI 初始化
        rect = GetComponent<RectTransform>();

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
            if (s == Suit.None) continue; // 跳过算术牌类型

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
        // --- [修改] 算术牌逻辑整合 ---
        // 检查是否有选中的算术牌
        if (arithmeticHolder != null && arithmeticHolder.arithmeticCards.Any(c => c.selected))
        {
            // 如果选中了算术牌，执行带动画的算术流程
            StartCoroutine(PerformArithmeticSequence());
            return; 
        }
        // ------------------------------------------

        List<Card> selectedCards = cards.Where(c => c.selected).ToList();

        if (selectedCards.Count < 3)
        {
            Debug.Log("请至少选择3张牌！");
            return;
        }

        // 排序
        selectedCards.Sort((a, b) => a.ParentIndex().CompareTo(b.ParentIndex()));

        // 提取点数
        List<int> ranks = selectedCards.Select(c => c.data.rank).ToList();
        
        // 判定
        List<SequenceEvaluator.SequenceType> results = SequenceEvaluator.Evaluate(ranks);

        if (results.Count > 0)
        {
            // 特效判定
            bool hasFibonacci = results.Contains(SequenceEvaluator.SequenceType.Fibonacci);
            int otherConditionsCount = 0;
            foreach (var type in results)
            {
                if (type != SequenceEvaluator.SequenceType.Fibonacci) otherConditionsCount++;
            }

            string targetEdition = "REGULAR"; 
            if (hasFibonacci) targetEdition = "NEGATIVE";
            else if (otherConditionsCount >= 3) targetEdition = "POLYCHROME";

            // 判断是否触发特殊效果抖动
            bool triggerShake = (targetEdition != "REGULAR");

            foreach (Card card in selectedCards)
            {
                if (card.cardVisual != null) 
                {
                    card.cardVisual.UpdateShaderEffect(targetEdition);
                    if (triggerShake) card.cardVisual.PlayConversionShake();
                }
            }

            // 文本构建
            string resultStr = ""; 
            foreach (var type in results)
            {
                switch(type)
                {
                    case SequenceEvaluator.SequenceType.Geometric: resultStr += "等比数列 "; break;
                    case SequenceEvaluator.SequenceType.Arithmetic: resultStr += "等差数列 "; break;
                    case SequenceEvaluator.SequenceType.Increasing: resultStr += "递增数列 "; break;
                    case SequenceEvaluator.SequenceType.Decreasing: resultStr += "递减数列 "; break;
                    case SequenceEvaluator.SequenceType.Odd: resultStr += "奇数列 "; break;
                    case SequenceEvaluator.SequenceType.Even: resultStr += "偶数列 "; break;
                    case SequenceEvaluator.SequenceType.Fibonacci: resultStr += "斐波那契数列 "; break;
                }
            }
            
            string fullText = resultStr; 

            if (resultText != null)
            {
                if (typewritingCoroutine != null) StopCoroutine(typewritingCoroutine);
                typewritingCoroutine = StartCoroutine(TypewriterEffect(fullText));
            }

            // 如果有特效抖动，稍等一下再飞出
            if (triggerShake)
            {
                StartCoroutine(DelayedPerformPlaySuccess(selectedCards, 0.5f));
            }
            else
            {
                PerformPlaySuccess(selectedCards);
            }
        }
        else
        {
            Debug.Log("判定失败");
            if (resultText != null)
            {
                if (typewritingCoroutine != null) StopCoroutine(typewritingCoroutine);
                resultText.text = "无效牌型";
                resultText.maxVisibleCharacters = 999;
            }
        }
    }

    // [新增] 算术牌动画流程协程
    IEnumerator PerformArithmeticSequence()
    {
        // 1. 获取手牌区选中的卡牌
        List<Card> selectedHandCards = cards.Where(c => c.selected).ToList();

        if (selectedHandCards.Count > 0)
        {
            // 2. 对选中的手牌播放抖动动画
            foreach (Card c in selectedHandCards)
            {
                if (c.cardVisual != null)
                {
                    c.cardVisual.PlayConversionShake();
                }
            }

            // 3. 等待动画播放（让玩家看到牌在抖动，预示着变化）
            yield return new WaitForSeconds(0.5f);
        }

        // 4. 执行实际的算术逻辑（数值转换）
        // 这里调用 ArithmeticCardHolder 中的方法来修改数值和消耗算术牌
        if (arithmeticHolder != null)
        {
            arithmeticHolder.ApplyArithmeticEffect();
        }
        
        // 可选：转换完成后，可以再次刷新一下文本提示，或者让牌闪烁一下
    }

    IEnumerator TypewriterEffect(string fullText)
    {
        resultText.text = fullText;
        resultText.maxVisibleCharacters = 0; 
        resultText.ForceMeshUpdate();
        int totalChars = resultText.textInfo.characterCount; 
        
        for (int i = 0; i <= totalChars; i++)
        {
            resultText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(0.05f); 
        }
    }

    IEnumerator DelayedPerformPlaySuccess(List<Card> playedCards, float delay)
    {
        yield return new WaitForSeconds(delay);
        PerformPlaySuccess(playedCards);
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