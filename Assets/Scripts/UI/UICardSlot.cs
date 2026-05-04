using UnityEngine;
using UnityEngine.EventSystems;

namespace CallKitty.UI
{
    public class UICardSlot : MonoBehaviour, IDropHandler
    {
        public enum SlotType
        {
            UnassignedPool,
            HandZone,
            DiscardZone
        }

        public SlotType Type;
        public int Capacity = 13;

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null)
            {
                UICard card = eventData.pointerDrag.GetComponent<UICard>();
                if (card != null)
                {
                    if (transform.childCount < Capacity)
                    {
                        // Accept the card and determine correct insertion index (horizontal)
                        int newIndex = transform.childCount;
                        for (int i = 0; i < transform.childCount; i++)
                        {
                            if (eventData.position.x < transform.GetChild(i).position.x)
                            {
                                newIndex = i;
                                break;
                            }
                        }

                        card.transform.SetParent(transform);
                        card.transform.SetSiblingIndex(newIndex);
                        
                        UIArrangementManager.Instance?.OnCardMoved();
                    }
                    else if (Capacity == 1 && transform.childCount > 0)
                    {
                        // Slot is full but has capacity 1 (like discard zone), swap with current card
                        UICard existingCard = transform.GetChild(0).GetComponent<UICard>();
                        if (existingCard != null)
                        {
                            Transform sourceParent = card.originalParent;
                            int sourceIndex = card.originalSiblingIndex;

                            // Move existing to source
                            existingCard.transform.SetParent(sourceParent);
                            existingCard.transform.SetSiblingIndex(sourceIndex);

                            // Move incoming to here
                            card.transform.SetParent(transform);
                            card.transform.SetSiblingIndex(0);

                            UIArrangementManager.Instance?.OnCardMoved();
                        }
                    }
                    else
                    {
                        // Slot is full, reject
                        card.ReturnToOriginalParent();
                    }
                }
            }
        }
    }
}
