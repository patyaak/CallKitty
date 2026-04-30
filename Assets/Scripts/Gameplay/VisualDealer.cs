using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CallKitty.Gameplay;
using CallKitty.Core;
using CallKitty.UI;

namespace CallKitty.Gameplay
{
    public class VisualDealer : MonoBehaviour
    {
        public static VisualDealer Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject uiCardPrefab;

        [Header("Transforms")]
        [SerializeField] private Transform startingPoint;
        [SerializeField] private Transform[] botPositions = new Transform[3];
        [SerializeField] private Transform[] handZones = new Transform[4];
        [SerializeField] private Transform discardZone;
        public GameObject startingCard;

        [Header("Settings")]
        [SerializeField] private float dealSpeed = 0.1f; // Time between each card deal
        [SerializeField] private float cardMoveDuration = 0.5f; // Time it takes for a card to reach its destination

        private List<GameObject> activeCards = new List<GameObject>();
        private List<UICard> playerDealtCards = new List<UICard>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Removed automatic start for testing since it requires actual card data now
        }

        public void StartDealAnimation(List<Card> playerCards, System.Action onComplete)
        {
            Debug.Log($"[VisualDealer] StartDealAnimation called with {playerCards?.Count ?? 0} player cards.");
            
            if (uiCardPrefab == null) Debug.LogError("[VisualDealer] UI Card Prefab is NULL! Assign it in the Inspector.");
            if (startingPoint == null) Debug.LogError("[VisualDealer] Starting Point is NULL! Assign it in the Inspector.");
            if (botPositions == null || botPositions.Length == 0) Debug.LogWarning("[VisualDealer] Bot Positions array is empty.");
            if (handZones == null || handZones.Length == 0) Debug.LogWarning("[VisualDealer] Hand Zones array is empty.");

            StartCoroutine(DealAnimationRoutine(playerCards, onComplete));
        }

        private IEnumerator DealAnimationRoutine(List<Card> playerCards, System.Action onComplete)
        {
            ClearCards();
            playerDealtCards.Clear();

            int totalRounds = 13;

            for (int round = 0; round < totalRounds; round++)
            {
                Debug.Log($"[VisualDealer] Dealing Round {round + 1}/13");
                // Deal to each bot
                for (int botIndex = 0; botIndex < botPositions.Length; botIndex++)
                {
                    if (botPositions[botIndex] != null)
                    {
                        SpawnAndAnimateToBot(botPositions[botIndex], round);
                        yield return new WaitForSeconds(dealSpeed);
                    }
                }

                // Deal to player (distribute among handzones and discard)
                Transform targetZone = GetPlayerTargetZoneForRound(round);
                if (targetZone != null && round < playerCards.Count)
                {
                    SpawnAndAnimateToPlayer(targetZone, playerCards[round]);
                    yield return new WaitForSeconds(dealSpeed);
                }
                else
                {
                    Debug.LogWarning($"[VisualDealer] Player TargetZone is null or round {round} index out of player card bounds.");
                }
            }

            yield return new WaitForSeconds(cardMoveDuration);
            
            // Wait 1 second before flipping the cards
            yield return new WaitForSeconds(1.0f);
            
            foreach (var uiCard in playerDealtCards)
            {
                if (uiCard != null)
                {
                    uiCard.SetFaceUp(true);
                    uiCard.IsInteractable = true;
                }
            }

            onComplete?.Invoke();
            if (startingCard != null) startingCard.SetActive(false);
        }

        private Transform GetPlayerTargetZoneForRound(int round)
        {
            // round is 0-indexed (0 to 12)
            if (round < 3 && handZones.Length > 0) return handZones[0];
            if (round < 6 && handZones.Length > 1) return handZones[1];
            if (round < 9 && handZones.Length > 2) return handZones[2];
            if (round < 12 && handZones.Length > 3) return handZones[3];
            return discardZone;
        }

        private void SpawnAndAnimateToBot(Transform target, int cardIndex)
        {
            if (uiCardPrefab == null || startingPoint == null) return;

            GameObject cardObj = Instantiate(uiCardPrefab, startingPoint.position, startingPoint.rotation, startingPoint.parent);
            activeCards.Add(cardObj);

            UICard uiCard = cardObj.GetComponent<UICard>();
            if (uiCard != null)
            {
                // Initialize as face-down with dummy card data, not interactable
                uiCard.Initialize(new Card(Suit.Spades, Rank.Two), faceUp: false);
                uiCard.IsInteractable = false;
            }

            Image cardImage = cardObj.GetComponent<Image>();
            if (cardImage != null) cardImage.preserveAspect = true;

            StartCoroutine(MoveAndStackRoutine(cardObj, target, cardIndex));
        }

        private void SpawnAndAnimateToPlayer(Transform targetZone, Card cardData)
        {
            if (uiCardPrefab == null || startingPoint == null) return;

            GameObject cardObj = Instantiate(uiCardPrefab, startingPoint.position, startingPoint.rotation, startingPoint.parent);
            activeCards.Add(cardObj);

            UICard uiCard = cardObj.GetComponent<UICard>();
            if (uiCard != null)
            {
                // Initialize as face-down and non-interactable while moving
                uiCard.Initialize(cardData, faceUp: false);
                uiCard.IsInteractable = false;
                playerDealtCards.Add(uiCard);
            }

            Image cardImage = cardObj.GetComponent<Image>();
            if (cardImage != null) cardImage.preserveAspect = true;
            
            if (targetZone == discardZone)
            {
                RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(400, 200);
                }
            }

            StartCoroutine(MoveAndParentPlayerRoutine(cardObj, targetZone));
        }

        private IEnumerator MoveAndStackRoutine(GameObject card, Transform targetZone, int cardIndex)
        {
            // Add a slight offset so they look like a stacked hand
            Vector3 offset = new Vector3(cardIndex * 2f, cardIndex * -2f, 0);
            yield return StartCoroutine(MoveCardSmoothRoutine(card.transform, targetZone.position + offset));
            
            if (card != null && targetZone != null)
            {
                card.transform.SetParent(targetZone);
            }
        }

        private IEnumerator MoveAndParentPlayerRoutine(GameObject card, Transform targetZone)
        {
            yield return StartCoroutine(MoveCardSmoothRoutine(card.transform, targetZone.position));
            
            if (card != null && targetZone != null)
            {
                card.transform.SetParent(targetZone);
                card.transform.localScale = Vector3.one;
                card.transform.localRotation = Quaternion.identity;
                
                if (targetZone == discardZone)
                {
                    RectTransform rectTransform = card.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.sizeDelta = new Vector2(400, 200);
                    }
                }
            }
        }


        private IEnumerator MoveCardSmoothRoutine(Transform cardTransform, Vector3 targetPosition)
        {
            Vector3 startPos = cardTransform.position;
            float elapsedTime = 0f;

            while (elapsedTime < cardMoveDuration)
            {
                if (cardTransform == null) yield break;

                // Smooth step for a nicer tween effect without external libraries
                float t = elapsedTime / cardMoveDuration;
                t = t * t * (3f - 2f * t);

                cardTransform.position = Vector3.Lerp(startPos, targetPosition, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (cardTransform != null)
            {
                cardTransform.position = targetPosition;
            }
        }

        public void ClearCards()
        {
            foreach (var card in activeCards)
            {
                if (card != null)
                {
                    Destroy(card);
                }
            }
            activeCards.Clear();
            
            // Clear cards that were parented to bot zones
            if (botPositions != null)
            {
                foreach (var botZone in botPositions)
                {
                    if (botZone != null)
                    {
                        foreach (Transform child in botZone)
                        {
                            Destroy(child.gameObject);
                        }
                    }
                }
            }
            
            // Also clear cards that were parented to hand zones
            if (handZones != null)
            {
                foreach (var zone in handZones)
                {
                    if (zone != null)
                    {
                        foreach (Transform child in zone)
                        {
                            Destroy(child.gameObject);
                        }
                    }
                }
            }
            if (discardZone != null)
            {
                foreach (Transform child in discardZone)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
}
