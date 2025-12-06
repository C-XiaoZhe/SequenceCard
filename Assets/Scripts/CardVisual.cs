using System;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.EventSystems;
using Unity.Collections;
using UnityEngine.UI;
using Unity.VisualScripting;
using TMPro; // 引入 TMPro

public class CardVisual : MonoBehaviour
{
    private bool initalize = false;

    [Header("Card")]
    public Card parentCard;
    private Transform cardTransform;
    private Vector3 rotationDelta;
    private int savedIndex;
    Vector3 movementDelta;
    private Canvas canvas;

    [Header("References")]
    public Transform visualShadow;
    private float shadowOffset = 20;
    private Vector2 shadowDistance;
    private Canvas shadowCanvas;
    [SerializeField] private Transform shakeParent;
    [SerializeField] private Transform tiltParent;
    [SerializeField] private Image cardImage;
    
    // 用于显示算术牌文字
    [SerializeField] private TextMeshProUGUI textOverlay; 

    [Header("Follow Parameters")]
    [SerializeField] private float followSpeed = 30;

    [Header("Rotation Parameters")]
    [SerializeField] private float rotationAmount = 20;
    [SerializeField] private float rotationSpeed = 20;
    [SerializeField] private float autoTiltAmount = 30;
    [SerializeField] private float manualTiltAmount = 20;
    [SerializeField] private float tiltSpeed = 20;

    [Header("Scale Parameters")]
    [SerializeField] private bool scaleAnimations = true;
    [SerializeField] private float scaleOnHover = 1.15f;
    [SerializeField] private float scaleOnSelect = 1.25f;
    [SerializeField] private float scaleTransition = .15f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Header("Select Parameters")]
    [SerializeField] private float selectPunchAmount = 20;

    [Header("Hober Parameters")]
    [SerializeField] private float hoverPunchAngle = 5;
    [SerializeField] private float hoverTransition = .15f;

    [Header("Swap Parameters")]
    [SerializeField] private bool swapAnimations = true;
    [SerializeField] private float swapRotationAngle = 30;
    [SerializeField] private float swapTransition = .15f;
    [SerializeField] private int swapVibrato = 5;

    [Header("Curve")]
    [SerializeField] private CurveParameters curve;

    private float curveYOffset;
    private float curveRotationOffset;
    
    // 标记是否已打出
    private bool isPlayed = false;

    // 设置为已打出状态
    public void SetPlayedState()
    {
        isPlayed = true;
        transform.localRotation = Quaternion.identity;
        tiltParent.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one; 
        
        transform.DOKill();
        shakeParent.DOKill();
    }

    private void Start()
    {
        shadowDistance = visualShadow.localPosition;
    }

    public void Initialize(Card target, int index = 0)
    {
        parentCard = target;
        cardTransform = target.transform;
        canvas = GetComponent<Canvas>();
        shadowCanvas = visualShadow.GetComponent<Canvas>();

        parentCard.PointerEnterEvent.AddListener(PointerEnter);
        parentCard.PointerExitEvent.AddListener(PointerExit);
        parentCard.BeginDragEvent.AddListener(BeginDrag);
        parentCard.EndDragEvent.AddListener(EndDrag);
        parentCard.PointerDownEvent.AddListener(PointerDown);
        parentCard.PointerUpEvent.AddListener(PointerUp);
        parentCard.SelectEvent.AddListener(Select);

        initalize = true;
    }

    public void UpdateIndex(int length)
    {
        transform.SetSiblingIndex(parentCard.transform.parent.GetSiblingIndex());
    }

    void Update()
    {
        if (!initalize || parentCard == null) return;

        if (isPlayed)
        {
            transform.position = Vector3.Lerp(transform.position, parentCard.transform.position, followSpeed * Time.deltaTime);
            return; 
        }

        HandPositioning();
        SmoothFollow();
        FollowRotation();
        CardTilt();
    }

    private void HandPositioning()
    {
        curveYOffset = (curve.positioning.Evaluate(parentCard.NormalizedPosition()) * curve.positioningInfluence) * parentCard.SiblingAmount();
        curveYOffset = parentCard.SiblingAmount() < 5 ? 0 : curveYOffset;
        curveRotationOffset = curve.rotation.Evaluate(parentCard.NormalizedPosition());
    }

    private void SmoothFollow()
    {
        Vector3 verticalOffset = (Vector3.up * (parentCard.isDragging ? 0 : curveYOffset));
        transform.position = Vector3.Lerp(transform.position, cardTransform.position + verticalOffset, followSpeed * Time.deltaTime);
    }

    private void FollowRotation()
    {
        Vector3 movement = (transform.position - cardTransform.position);
        movementDelta = Vector3.Lerp(movementDelta, movement, 25 * Time.deltaTime);
        Vector3 movementRotation = (parentCard.isDragging ? movementDelta : movement) * rotationAmount;
        rotationDelta = Vector3.Lerp(rotationDelta, movementRotation, rotationSpeed * Time.deltaTime);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, Mathf.Clamp(rotationDelta.x, -60, 60));
    }

    private void CardTilt()
    {
        savedIndex = parentCard.isDragging ? savedIndex : parentCard.ParentIndex();
        float sine = Mathf.Sin(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);
        float cosine = Mathf.Cos(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);

        Vector3 offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float tiltX = parentCard.isHovering ? ((offset.y * -1) * manualTiltAmount) : 0;
        float tiltY = parentCard.isHovering ? ((offset.x) * manualTiltAmount) : 0;
        float tiltZ = parentCard.isDragging ? tiltParent.eulerAngles.z : (curveRotationOffset * (curve.rotationInfluence * parentCard.SiblingAmount()));

        float lerpX = Mathf.LerpAngle(tiltParent.eulerAngles.x, tiltX + (sine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpY = Mathf.LerpAngle(tiltParent.eulerAngles.y, tiltY + (cosine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpZ = Mathf.LerpAngle(tiltParent.eulerAngles.z, tiltZ, tiltSpeed / 2 * Time.deltaTime);

        tiltParent.eulerAngles = new Vector3(lerpX, lerpY, lerpZ);
    }

    private void Select(Card card, bool state)
    {
        DOTween.Kill(2, true);
        float dir = state ? 1 : 0;
        shakeParent.DOPunchPosition(shakeParent.up * selectPunchAmount * dir, scaleTransition, 10, 1);
        shakeParent.DOPunchRotation(Vector3.forward * (hoverPunchAngle/2), hoverTransition, 20, 1).SetId(2);

        if(scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);
    }

    public void Swap(float dir = 1)
    {
        if (!swapAnimations) return;
        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation((Vector3.forward * swapRotationAngle) * dir, swapTransition, swapVibrato, 1).SetId(3);
    }

    private void BeginDrag(Card card)
    {
        if(scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);
        canvas.overrideSorting = true;
    }

    private void EndDrag(Card card)
    {
        canvas.overrideSorting = false;
        transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    private void PointerEnter(Card card)
    {
        if(scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);
        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation(Vector3.forward * hoverPunchAngle, hoverTransition, 20, 1).SetId(2);
    }

    private void PointerExit(Card card)
    {
        if (!parentCard.wasDragged)
            transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    private void PointerUp(Card card, bool longPress)
    {
        if(scaleAnimations)
            transform.DOScale(longPress ? scaleOnHover : scaleOnSelect, scaleTransition).SetEase(scaleEase);
        canvas.overrideSorting = false;
        visualShadow.localPosition = shadowDistance;
        shadowCanvas.overrideSorting = true;
    }

    private void PointerDown(Card card)
    {
        if(scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);
        visualShadow.localPosition += (-Vector3.up * shadowOffset);
        shadowCanvas.overrideSorting = false;
    }

    // 设置卡面显示 (兼容旧方法)
    public void SetFace(Sprite s)
    {
        if (cardImage != null && s != null)
        {
            cardImage.sprite = s;
        }
    }

    // 设置卡面显示 (新方法：处理算术牌和普通牌)
    public void SetFace(CardData data)
    {
        if (cardImage == null) return;

        Sprite s = null;
        if (VisualCardsHandler.instance != null)
        {
            s = VisualCardsHandler.instance.GetCardSprite(data);
        }

        if (s != null)
        {
            cardImage.sprite = s;
            cardImage.color = Color.white;
            if (textOverlay != null) textOverlay.gameObject.SetActive(false);
        }
        else
        {
            // 没图时的兜底显示逻辑
            if (data.cardType == CardType.Arithmetic)
            {
                if (textOverlay != null)
                {
                    textOverlay.gameObject.SetActive(true);
                    string op = "";
                    switch(data.operation)
                    {
                        case ArithmeticOp.Add: op = "+"; break;
                        case ArithmeticOp.Subtract: op = "-"; break;
                        case ArithmeticOp.Multiply: op = "x"; break;
                    }
                    textOverlay.text = $"{op}{data.rank}";
                    textOverlay.color = Color.black; 
                }
            }
            else
            {
                if (textOverlay != null)
                {
                    textOverlay.gameObject.SetActive(true);
                    textOverlay.text = $"{data.suit}\n{data.rank}";
                }
            }
        }
    }

    // 更新卡牌特效
    public void UpdateShaderEffect(string edition)
    {
        if (cardImage != null)
        {
            ShaderCode shaderScript = cardImage.GetComponent<ShaderCode>();
            if (shaderScript != null)
            {
                shaderScript.SetEdition(edition);
            }
        }
    }

    // [修改] 播放转换时的抖动动画 (旋转版)
    public void PlayConversionShake()
    {
        shakeParent.DOKill();
        
        // DOPunchRotation: 产生震动旋转
        // Vector3.forward * 20f: 绕 Z 轴(前后轴)旋转 20 度，即左右摇摆
        // 0.5f: 持续时间
        // 20: 震动频率 (数值越大抖动越快)
        // 1: 弹性 (0-1)
        shakeParent.DOPunchRotation(Vector3.forward * 25f, 0.75f, 20, 0.7f);
    }
}