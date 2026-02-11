using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PartyChaos.Game
{
    public class RoundManager : NetworkBehaviour
    {
        [Header("Round")]
        public float roundTime = 120f;
        public int roundWinBonus = 100;

        public NetworkVariable<float> TimeLeft = new(0);

        private readonly HashSet<ulong> _alive = new();

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            TimeLeft.Value = roundTime;

            var score = ScoreManager.I;
            if (score != null) score.ResetRoundPoints();

            _alive.Clear();
            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
                _alive.Add(id);
        }

        private void Update()
        {
            if (!IsServer) return;
            if (GameSessionManager.I == null) return;

            // quick state initialization
            if (GameSessionManager.I.State.Value == SessionState.LoadingRound)
                GameSessionManager.I.State.Value = SessionState.Countdown;

            if (GameSessionManager.I.State.Value == SessionState.Countdown)
            {
                // for fast shipping: skip long countdown
                GameSessionManager.I.State.Value = SessionState.Playing;
            }

            if (GameSessionManager.I.State.Value != SessionState.Playing) return;

            TimeLeft.Value -= Time.deltaTime;

            // Last alive ends instantly
            if (_alive.Count <= 1)
            {
                EndRoundByAlive();
                return;
            }

            // Time-out ends by alive+best points
            if (TimeLeft.Value <= 0f)
            {
                EndRoundByPointsOrAlive();
            }
        }

        /// <summary>
        /// Called by EliminationVolume on the server when a player falls into the sea (ring-out).
        /// </summary>
        public void NotifyEliminated(ulong victimId)
        {
            if (!IsServer) return;

            _alive.Remove(victimId);

            if (_alive.Count <= 1)
                EndRoundByAlive();
        }

        private void EndRoundByAlive()
        {
            if (!IsServer) return;
            if (GameSessionManager.I.State.Value != SessionState.Playing) return;

            ulong winner = 0;
            foreach (var id in _alive) { winner = id; break; }

            var score = ScoreManager.I;
            if (winner != 0 && score != null)
                score.AwardRoundWinBonus(winner, roundWinBonus);

            GameSessionManager.I.NotifyRoundEndedServerRpc();
        }

        private void EndRoundByPointsOrAlive()
        {
            if (!IsServer) return;
            if (GameSessionManager.I.State.Value != SessionState.Playing) return;

            // Choose the alive player with highest ROUND points
            ulong best = 0;
            int bestPoints = int.MinValue;

            var score = ScoreManager.I;
            if (score == null)
            {
                GameSessionManager.I.NotifyRoundEndedServerRpc();
                return;
            }

            foreach (var s in score.Scores)
            {
                if (!_alive.Contains(s.ClientId)) continue;

                if (s.RoundPoints > bestPoints)
                {
                    bestPoints = s.RoundPoints;
                    best = s.ClientId;
                }
            }

            if (best != 0)
                score.AwardRoundWinBonus(best, roundWinBonus);

            GameSessionManager.I.NotifyRoundEndedServerRpc();
        }
    }
}
