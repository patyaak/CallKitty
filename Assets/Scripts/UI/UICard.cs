using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CallKitty.Core;

namespace CallKitty.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UICard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Card CardData { get; private set; }
        public bool IsFaceUp { get; private set; } = true;
        public bool IsInteractable { get; set; } = true;
        
        [SerializeField] private Image cardImage; // Set this in inspector if using sprites
        [SerializeField] private Text cardText; // Fallback text representation
        
        private CanvasGroup canvasGroup;
        private Transform originalParent;
        private int originalSiblingIndex;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Initialize(Card card, bool faceUp = true)
        {
            CardData = card;
            IsFaceUp = faceUp;
            
            if (cardText != null)
            {
                cardText.text = card.ToString();
            }
            
            UpdateVisual();
        }

        public void SetFaceUp(bool faceUp)
        {
            IsFaceUp = faceUp;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            // Load from Resources and assign sprite
            CardDatabase db = Resources.Load<CardDatabase>("CardDatabase");
            if (db != null && cardImage != null)
            {
                if (IsFaceUp)
                {
                    Sprite s = db.GetSprite(CardData);
                    if (s != null)
                    {
                        cardImage.sprite = s;
                        if (cardText != null) cardText.gameObject.SetActive(false);
                    }
                }
                else
                {
                    cardImage.sprite = db.cardBack;
                    if (cardText != null) cardText.gameObject.SetActive(false);
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsInteractable || !IsFaceUp) return;

            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();

            // Move to the root canvas so it renders on top
            transform.SetParent(transform.root);
            transform.SetAsLastSibling();

            canvasGroup.blocksRaycasts = false; // Allow raycasts to pass through to find drop zones
            canvasGroup.alpha = 0.6f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsInteractable || !IsFaceUp) return;
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsInteractable || !IsFaceUp) return;

            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            // If it wasn't dropped on a valid slot, return to original parent
            if (transform.parent == transform.root)
            {
                ReturnToOriginalParent();
            }
        }

        public void ReturnToOriginalParent()
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
        }
    }
}
