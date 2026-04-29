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
                        // Accept the card
                        card.transform.SetParent(transform);
                        
                        // We could inform an arrangement manager here
                        UIArrangementManager.Instance?.OnCardMoved();
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
