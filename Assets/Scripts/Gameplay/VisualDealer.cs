using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CallKitty.Gameplay;

namespace CallKitty.Gameplay
{
    public class VisualDealer : MonoBehaviour
    {
        public static VisualDealer Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject backCardPrefab;

        [Header("Transforms")]
        [SerializeField] private Transform startingPoint;
        [SerializeField] private Transform[] botPositions = new Transform[3];
        [SerializeField] private Transform[] handZones = new Transform[4];
        [SerializeField] private Transform discardZone;

        [Header("Settings")]
        [SerializeField] private float dealSpeed = 0.1f; // Time between each card deal
        [SerializeField] private float cardMoveDuration = 0.5f; // Time it takes for a card to reach its destination

        private List<GameObject> activeCards = new List<GameObject>();

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
            // For testing: automatically start dealing when the scene loads
            StartDealAnimation(null);
        }

        public void StartDealAnimation(System.Action onComplete)
        {
            StartCoroutine(DealAnimationRoutine(onComplete));
        }

        private IEnumerator DealAnimationRoutine(System.Action onComplete)
        {
            ClearCards();

            int totalRounds = 13;

            for (int round = 0; round < totalRounds; round++)
            {
                // Deal to each bot
                for (int botIndex = 0; botIndex < botPositions.Length; botIndex++)
                {
                    if (botPositions[botIndex] != null)
                    {
                        SpawnAndAnimateToBot(botPositions[botIndex]);
                        yield return new WaitForSeconds(dealSpeed);
                    }
                }

                // Deal to player (distribute among handzones)
                Transform targetZone = GetPlayerTargetZoneForRound(round);
                if (targetZone != null)
                {
                    SpawnAndAnimateToZone(targetZone);
                    yield return new WaitForSeconds(dealSpeed);
                }
            }

            yield return new WaitForSeconds(cardMoveDuration);
            onComplete?.Invoke();
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

        private void SpawnAndAnimateToBot(Transform target)
        {
            if (backCardPrefab == null || startingPoint == null) return;

            GameObject card = Instantiate(backCardPrefab, startingPoint.position, startingPoint.rotation, startingPoint.parent);
            activeCards.Add(card);

            Image cardImage = card.GetComponent<Image>();
            if (cardImage != null) cardImage.preserveAspect = true;

            StartCoroutine(MoveAndDestroyRoutine(card, target.position));
        }

        private void SpawnAndAnimateToZone(Transform targetZone)
        {
            if (backCardPrefab == null || startingPoint == null) return;

            GameObject card = Instantiate(backCardPrefab, startingPoint.position, startingPoint.rotation, startingPoint.parent);
            activeCards.Add(card);

            Image cardImage = card.GetComponent<Image>();
            if (cardImage != null) cardImage.preserveAspect = true;

            StartCoroutine(MoveAndParentRoutine(card, targetZone));
        }

        private IEnumerator MoveAndDestroyRoutine(GameObject card, Vector3 targetPosition)
        {
            yield return StartCoroutine(MoveCardSmoothRoutine(card.transform, targetPosition));
            
            if (card != null)
            {
                activeCards.Remove(card);
                Destroy(card);
            }
        }

        private IEnumerator MoveAndParentRoutine(GameObject card, Transform targetZone)
        {
            yield return StartCoroutine(MoveCardSmoothRoutine(card.transform, targetZone.position));
            
            if (card != null && targetZone != null)
            {
                card.transform.SetParent(targetZone);
                card.transform.localPosition = Vector3.zero;
                card.transform.localScale = Vector3.one;
                card.transform.localRotation = Quaternion.identity;
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
