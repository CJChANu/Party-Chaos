using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PartyChaos.Game
{
    public struct PlayerScore : INetworkSerializable, IEquatable<PlayerScore>
    {
        public ulong ClientId;
        public int MatchPoints;
        public int RoundPoints;

        public bool Equals(PlayerScore other) => ClientId == other.ClientId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref MatchPoints);
            serializer.SerializeValue(ref RoundPoints);
        }
    }

    public class ScoreManager : NetworkBehaviour
    {
        public static ScoreManager I { get; private set; }

        public NetworkList<PlayerScore> Scores;

        private void Awake()
        {
            I = this;
            Scores = new NetworkList<PlayerScore>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            Scores.Clear();

            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
            {
                Scores.Add(new PlayerScore
                {
                    ClientId = id,
                    MatchPoints = 0,
                    RoundPoints = 0
                });
            }

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        private void OnClientConnected(ulong id)
        {
            if (!IsServer) return;

            // ensure entry exists
            for (int i = 0; i < Scores.Count; i++)
                if (Scores[i].ClientId == id) return;

            Scores.Add(new PlayerScore
            {
                ClientId = id,
                MatchPoints = 0,
                RoundPoints = 0
            });
        }

        public void ResetRoundPoints()
        {
            if (!IsServer) return;

            for (int i = 0; i < Scores.Count; i++)
            {
                var s = Scores[i];
                s.RoundPoints = 0;
                Scores[i] = s;
            }
        }

        public void AddPoints(ulong id, int amount)
        {
            if (!IsServer) return;

            for (int i = 0; i < Scores.Count; i++)
            {
                if (Scores[i].ClientId != id) continue;

                var s = Scores[i];
                s.RoundPoints += amount;
                s.MatchPoints += amount;
                Scores[i] = s;
                return;
            }

            // If player wasn't in list yet (edge case), add them:
            Scores.Add(new PlayerScore
            {
                ClientId = id,
                MatchPoints = amount,
                RoundPoints = amount
            });
        }

        public void AwardRoundWinBonus(ulong winnerId, int bonus)
        {
            if (!IsServer) return;
            AddPoints(winnerId, bonus);
        }
    }
}
