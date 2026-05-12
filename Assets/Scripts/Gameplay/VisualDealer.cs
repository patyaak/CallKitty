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
        [SerializeField] private GameObject cardBackPrefab;
        public GameObject startingCard;
        public Transform[] BotPositions => botPositions;

        [Header("Playing Phase")]
        [SerializeField] private Transform[] playingPositions = new Transform[4]; // 0: Player, 1: Bot1, 2: Bot2, 3: Bot3

        [Header("Settings")]
        [SerializeField] private float dealSpeed = 0.1f; // Time between each card deal
        [SerializeField] private float cardMoveDuration = 0.5f; // Time it takes for a card to reach its destination
        [SerializeField] private float trickShowDuration = 5f; // Time to show the cards in the trick

        private List<GameObject> activeCards = new List<GameObject>();
        private List<GameObject> activeBotCards = new List<GameObject>();
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

            if (startingCard != null) startingCard.SetActive(false);
            onComplete?.Invoke();

            // Hide bot cards once player cards are revealed
            HideBotCards();
        }

        private void HideBotCards()
        {
            foreach (var card in activeBotCards)
            {
                if (card != null) card.SetActive(false);
            }
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
            activeBotCards.Add(cardObj);

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


        public IEnumerator AnimateAllDiscardsThrow(List<Vector3> startPositions, System.Action onComplete)
        {
            int count = startPositions.Count;
            int finishedCount = 0;

            foreach (var pos in startPositions)
            {
                StartCoroutine(AnimateDiscardThrow(pos, () => {
                    finishedCount++;
                }));
            }

            yield return new WaitUntil(() => finishedCount >= count);
            onComplete?.Invoke();
        }

        public IEnumerator AnimateDiscardThrow(Vector3 startPosition, System.Action onComplete)
        {
            if (cardBackPrefab == null && uiCardPrefab == null) yield break;

            GameObject prefabToUse = cardBackPrefab != null ? cardBackPrefab : uiCardPrefab;
            
            // Use startingPoint.parent to ensure it's in the same UI hierarchy as other cards
            GameObject throwObj = Instantiate(prefabToUse, startPosition, Quaternion.identity, startingPoint.parent);
            
            // Ensure it's visible on top of other elements
            throwObj.transform.SetAsLastSibling();
            throwObj.transform.localScale = Vector3.one;

            // Standardize size for the throw animation
            RectTransform rt = throwObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(212, 298);
            }

            UnityEngine.UI.Image img = throwObj.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.preserveAspect = true;
            }
            
            // If it's a UICard prefab, make sure it's face down
            UICard uiCard = throwObj.GetComponent<UICard>();
            if (uiCard != null)
            {
                uiCard.Initialize(new Card(Suit.Spades, Rank.Ace), false);
                uiCard.IsInteractable = false;
            }

            Vector3 targetPos = startingPoint.position;
            // Create a midpoint for an arc effect
            Vector3 midPoint = (startPosition + targetPos) / 2f + Vector3.up * 200f; // Arc upwards
            
            float duration = 0.6f;
            float elapsed = 0f;
            
            Quaternion startRot = throwObj.transform.rotation;
            Quaternion targetRot = Quaternion.Euler(0, 0, 360f); // Full spin

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Ease out cubic for a fast start and slow finish (throwing feel)
                float tPos = 1f - Mathf.Pow(1f - t, 3f);
                // Ease in-out sine for rotation
                float tRot = -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

                // Quadratic Bezier for the arc
                Vector3 m1 = Vector3.Lerp(startPosition, midPoint, tPos);
                Vector3 m2 = Vector3.Lerp(midPoint, targetPos, tPos);
                throwObj.transform.position = Vector3.Lerp(m1, m2, tPos);

                throwObj.transform.rotation = Quaternion.Slerp(startRot, targetRot, tRot);
                
                // Scale down slightly as it hits the deck
                throwObj.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, tPos);

                yield return null;
            }

            // Snap to final position
            throwObj.transform.position = targetPos;
            throwObj.transform.rotation = targetRot;
            throwObj.transform.localScale = Vector3.one * 0.7f;

            // Wait for 2 seconds as requested
            yield return new WaitForSeconds(2.0f);

            Destroy(throwObj);
            onComplete?.Invoke();
        }

        public IEnumerator ShowTrickAnimation(List<List<Card>> playedHands)
        {
            List<GameObject> currentTrickObjects = new List<GameObject>();

            // Instantiate cards for each player
            for (int i = 0; i < playedHands.Count; i++)
            {
                if (i >= playingPositions.Length || playingPositions[i] == null) continue;

                Transform spawnParent = playingPositions[i];
                List<Card> hand = playedHands[i];
                if (hand == null) continue;

                for (int c = 0; c < hand.Count; c++)
                {
                    GameObject cardObj = Instantiate(uiCardPrefab, spawnParent);
                    currentTrickObjects.Add(cardObj);

                    UICard uiCard = cardObj.GetComponent<UICard>();
                    if (uiCard != null)
                    {
                        uiCard.Initialize(hand[c], faceUp: true);
                        uiCard.IsInteractable = false;
                    }

                    // Spread cards slightly horizontally
                    RectTransform rt = cardObj.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2((c - 1) * 60f, 0); // Spacing of 60
                    }
                }
            }

            // Wait for 5 seconds as requested
            yield return new WaitForSeconds(trickShowDuration);

            // Destroy the cards after showing
            foreach (var obj in currentTrickObjects)
            {
                if (obj != null) Destroy(obj);
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
            activeBotCards.Clear();
            
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
