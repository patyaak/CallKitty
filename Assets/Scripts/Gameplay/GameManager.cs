using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CallKitty.Core;
using CallKitty.UI;

namespace CallKitty.Gameplay
{
    public enum GameState
    {
        Init,
        Dealing,
        Bidding,
        Arranging,
        PlayingRound, // Contains 4 turns
        RoundScoring,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Init;

        [SerializeField] private DeckManager deckManager;
        
        // 0 is Human, 1-3 are AI
        public List<Player> Players { get; private set; } = new List<Player>();
        
        public int TargetScore { get; set; } = 5;
        public int CurrentTurnIndex { get; private set; } = 0; // 0 to 3

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnTurnStarted;
        public event Action<int, List<HandEvaluatedResult>, Player> OnTurnPlayed; // TurnIndex, Hands, Winner
        private int stateChangeVersion = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Start the game loop automatically for testing
            // We wait one frame to ensure all other scripts (like UIManager) have initialized and subscribed to events
            StartCoroutine(DelayedInit());
        }

        private IEnumerator DelayedInit()
        {
            yield return null; 
            InitializeGame();
        }

        public void InitializeGame()
        {
            // Setup players (1 Human, 3 AI)
            Players.Clear();
            
            // Add Human
            var humanObj = new GameObject("Player_Human");
            var human = humanObj.AddComponent<Player>();
            human.PlayerID = 0;
            human.PlayerName = "You";
            human.IsAI = false;
            Players.Add(human);

            // Add 3 AI
            for (int i = 1; i <= 3; i++)
            {
                var aiObj = new GameObject($"Player_AI_{i}");
                var ai = aiObj.AddComponent<PlayerAI>();
                ai.PlayerID = i;
                ai.PlayerName = $"Bot {i}";
                ai.IsAI = true;
                Players.Add(ai);
            }

            ChangeState(GameState.Dealing);
        }

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            int changeVersion = ++stateChangeVersion;
            OnStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.Dealing:
                    StartCoroutine(DealRoutine());
                    break;
                case GameState.Bidding:
                    StartCoroutine(BiddingRoutine(changeVersion));
                    break;
                case GameState.Arranging:
                    StartCoroutine(ArrangingRoutine());
                    break;
                case GameState.PlayingRound:
                    StartCoroutine(PlayingRoundRoutine());
                    break;
                case GameState.RoundScoring:
                    StartCoroutine(ScoreRoundRoutine());
                    break;
                case GameState.GameOver:
                    Debug.Log("Game Over!");
                    break;
            }
        }

        private IEnumerator DealRoutine()
        {
            Debug.Log("Dealing cards...");
            foreach (var player in Players)
            {
                player.ResetForNewRound();
            }

            deckManager.InitializeDeck();
            deckManager.ShuffleDeck();

            foreach (var player in Players)
            {
                var hand = deckManager.DealHand(13);
                player.ReceiveCards(hand);
            }

            if (VisualDealer.Instance != null)
            {
                bool dealingComplete = false;
                VisualDealer.Instance.StartDealAnimation(Players[0].DealtCards, () => {
                    dealingComplete = true;
                    UIArrangementManager.Instance?.OnCardMoved();
                });
                yield return new WaitUntil(() => dealingComplete);
                // Additional short delay after visual deal
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield return new WaitForSeconds(1f); // Artificial delay for UI
            }
            ChangeState(GameState.Arranging);
        }

        public void CompleteBidding()
        {
            foreach (var player in Players)
            {
                player.IsReady = true;
            }
        }

        public void ReturnToArrangingFromBidding()
        {
            if (CurrentState != GameState.Bidding) return;

            ResetReadyStates();
            ChangeState(GameState.Arranging);
        }

        private IEnumerator BiddingRoutine(int changeVersion)
        {
            Debug.Log("Bidding Phase...");
            // Wait for the bidding UI to collect the human call, bot calls, and discard animation.
            yield return new WaitUntil(() => changeVersion != stateChangeVersion || AllPlayersReady());
            if (changeVersion != stateChangeVersion || CurrentState != GameState.Bidding) yield break;

            ResetReadyStates();
            CurrentTurnIndex = 0;
            ChangeState(GameState.PlayingRound);
        }

        private IEnumerator ArrangingRoutine()
        {
            Debug.Log("Arrangement Phase...");
            // AI performs arrangement automatically
            foreach (var player in Players)
            {
                if (player.IsAI)
                {
                    ((PlayerAI)player).PerformArrangement();
                }
            }

            // Wait for human player to arrange cards
            yield return new WaitUntil(() => AllPlayersReady());

            ResetReadyStates();
            ChangeState(GameState.Bidding);
        }

        private IEnumerator PlayingRoundRoutine()
        {
            Debug.Log($"Playing Phase... Turn {CurrentTurnIndex + 1}/4");
            
            while (CurrentTurnIndex < 4)
            {
                OnTurnStarted?.Invoke(CurrentTurnIndex);

                List<HandEvaluatedResult> evaluatedHands = new List<HandEvaluatedResult>();
                List<List<Core.Card>> rawHands = new List<List<Core.Card>>();
                List<Player> activePlayers = new List<Player>();

                // Collect hands for this turn
                for (int i = 0; i < Players.Count; i++)
                {
                    var hand = Players[i].GetHandForTurn(CurrentTurnIndex);
                    rawHands.Add(hand); // Can be null, VisualDealer handles it
                    
                    if (hand != null)
                    {
                        var eval = HandEvaluator.Evaluate3CardHand(hand);
                        evaluatedHands.Add(eval);
                        activePlayers.Add(Players[i]);
                    }
                }

                // Determine winner
                Player turnWinner = null;
                int winnerPlayerID = 0; // Fallback to Player ID 0
                if (activePlayers.Count > 0)
                {
                    int winnerIndex = 0;
                    for (int i = 1; i < evaluatedHands.Count; i++)
                    {
                        if (evaluatedHands[i].CompareTo(evaluatedHands[winnerIndex]) > 0)
                        {
                            winnerIndex = i;
                        }
                    }

                    turnWinner = activePlayers[winnerIndex];
                    turnWinner.HandsWonThisRound++;
                    winnerPlayerID = turnWinner.PlayerID;
                    
                    Debug.Log($"Turn {CurrentTurnIndex + 1} Winner: {turnWinner.PlayerName} with {evaluatedHands[winnerIndex].Rank}");
                }

                // Show visual animation of the trick
                if (VisualDealer.Instance != null)
                {
                    yield return StartCoroutine(VisualDealer.Instance.ShowTrickAnimation(rawHands, winnerPlayerID));
                }

                // Invoke event for UI
                if (turnWinner != null)
                {
                    OnTurnPlayed?.Invoke(CurrentTurnIndex, evaluatedHands, turnWinner);
                }

                CurrentTurnIndex++;
            }

            ChangeState(GameState.RoundScoring);
        }

        private IEnumerator ScoreRoundRoutine()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ProcessRoundEnd();
            }
            else
            {
                CalculateScoresFallback();
            }

            yield return new WaitForSeconds(5f);

            // Check Game Over
            bool gameOver = false;
            foreach (var player in Players)
            {
                if (player.TotalScore >= TargetScore)
                {
                    gameOver = true;
                    break;
                }
            }

            if (gameOver)
            {
                ChangeState(GameState.GameOver);
            }
            else
            {
                ChangeState(GameState.Dealing);
            }
        }

        private void CalculateScoresFallback()
        {
            Debug.Log("Scoring Phase (Fallback)...");
            foreach (var player in Players)
            {
                int call = player.CurrentCall;
                int won = player.HandsWonThisRound;
                float roundScore = 0f;

                if (call == 0)
                {
                    roundScore = 0f; // Bid 0 = opt out, no scoring regardless of wins
                }
                else
                {
                    if (won >= call)
                    {
                        roundScore = call + ((won - call) * 0.1f);
                    }
                    else
                    {
                        roundScore = -call;
                    }
                }

                player.TotalScore += roundScore;
            }
        }

        private bool AllPlayersReady()
        {
            foreach (var player in Players)
            {
                if (!player.IsReady) return false;
            }
            return true;
        }

        private void ResetReadyStates()
        {
            foreach (var player in Players)
            {
                player.IsReady = false;
            }
        }
    }
}
