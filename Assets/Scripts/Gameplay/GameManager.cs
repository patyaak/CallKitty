using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CallKitty.Core;

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

        private bool isReadyToBid = false;

        public event Action<GameState> OnStateChanged;
        public event Action<int, List<HandEvaluatedResult>, Player> OnTurnPlayed; // TurnIndex, Hands, Winner

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
            OnStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.Dealing:
                    StartCoroutine(DealRoutine());
                    break;
                case GameState.Bidding:
                    StartCoroutine(BiddingRoutine());
                    break;
                case GameState.Arranging:
                    StartCoroutine(ArrangingRoutine());
                    break;
                case GameState.PlayingRound:
                    StartCoroutine(PlayingRoundRoutine());
                    break;
                case GameState.RoundScoring:
                    ScoreRound();
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
                VisualDealer.Instance.StartDealAnimation(Players[0].DealtCards, () => dealingComplete = true);
                yield return new WaitUntil(() => dealingComplete);
                // Additional short delay after visual deal
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield return new WaitForSeconds(1f); // Artificial delay for UI
            }
            
            // Wait for player to be ready to bid (viewed their cards)
            isReadyToBid = false;
            yield return new WaitUntil(() => isReadyToBid);
            
            ChangeState(GameState.Bidding);
        }

        public void StartBidding()
        {
            isReadyToBid = true;
        }

        private IEnumerator BiddingRoutine()
        {
            Debug.Log("Bidding Phase...");
            // AI performs bidding automatically
            foreach (var player in Players)
            {
                if (player.IsAI)
                {
                    ((PlayerAI)player).PerformBidding();
                }
            }

            // Wait for human player to bid (IsReady = true)
            yield return new WaitUntil(() => AllPlayersReady());

            ResetReadyStates();
            ChangeState(GameState.Arranging);
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
            CurrentTurnIndex = 0;
            ChangeState(GameState.PlayingRound);
        }

        private IEnumerator PlayingRoundRoutine()
        {
            Debug.Log($"Playing Phase... Turn {CurrentTurnIndex + 1}/4");
            
            while (CurrentTurnIndex < 4)
            {
                List<HandEvaluatedResult> playedHands = new List<HandEvaluatedResult>();
                List<Player> activePlayers = new List<Player>();

                // Collect hands for this turn
                foreach (var player in Players)
                {
                    var hand = player.GetHandForTurn(CurrentTurnIndex);
                    if (hand != null)
                    {
                        var eval = HandEvaluator.Evaluate3CardHand(hand);
                        playedHands.Add(eval);
                        activePlayers.Add(player);
                    }
                }

                // Determine winner
                int winnerIndex = 0;
                for (int i = 1; i < playedHands.Count; i++)
                {
                    if (playedHands[i].CompareTo(playedHands[winnerIndex]) > 0)
                    {
                        winnerIndex = i;
                    }
                }

                Player turnWinner = activePlayers[winnerIndex];
                turnWinner.HandsWonThisRound++;
                
                Debug.Log($"Turn {CurrentTurnIndex + 1} Winner: {turnWinner.PlayerName} with {playedHands[winnerIndex].Rank}");

                // Invoke event for UI
                OnTurnPlayed?.Invoke(CurrentTurnIndex, playedHands, turnWinner);

                // Wait a bit so UI can show the trick
                yield return new WaitForSeconds(3f); // Increased delay for better UX

                CurrentTurnIndex++;
            }

            ChangeState(GameState.RoundScoring);
        }

        private void ScoreRound()
        {
            Debug.Log("Scoring Phase...");
            // Move this to ScoreManager soon, but for now, simple implementation
            foreach (var player in Players)
            {
                int call = player.CurrentCall;
                int won = player.HandsWonThisRound;
                float roundScore = 0f;

                if (call == 0)
                {
                    if (won == 0) roundScore = 1.0f; // Bonus for successful Nil
                    else roundScore = -1.0f; // Penalty for failed Nil
                }
                else
                {
                    if (won >= call)
                    {
                        // Exceeded or met call
                        roundScore = call + ((won - call) * 0.1f);
                    }
                    else
                    {
                        // Failed call
                        roundScore = -call;
                    }
                }

                player.TotalScore += roundScore;
                Debug.Log($"{player.PlayerName}: Call {call}, Won {won}, Round Score {roundScore}, Total {player.TotalScore}");
            }

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
                // Next round
                ChangeState(GameState.Dealing);
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
