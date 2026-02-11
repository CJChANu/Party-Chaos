using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PartyChaos.Game
{
    public enum SessionState { Lobby, LoadingRound, Countdown, Playing, Results, MatchOver }

    public class GameSessionManager : NetworkBehaviour
    {
        public static GameSessionManager I { get; private set; }

        [Header("Scenes")]
        public string hubSceneName = "HubLobby";
        public string roundSceneName = "BeachRound";

        [Header("Match Rules")]
        public int roundsPerMatch = 3;
        public float resultsDuration = 8f;

        public NetworkVariable<int> CurrentRoundIndex = new(0);
        public NetworkVariable<SessionState> State = new(SessionState.Lobby);

        // Ready list (server authoritative)
        private readonly HashSet<ulong> _ready = new();

        private void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        void OnClientDisconnected(ulong clientId)
        {
            _ready.Remove(clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetReadyServerRpc(bool ready, ServerRpcParams rpc = default)
        {
            var id = rpc.Receive.SenderClientId;
            if (ready) _ready.Add(id);
            else _ready.Remove(id);

            TryStartIfAllReady();
        }

        void TryStartIfAllReady()
        {
            if (!IsServer) return;
            if (State.Value != SessionState.Lobby) return;

            // Need at least 2 to start; you can enforce 16 if you want.
            if (NetworkManager.Singleton.ConnectedClientsIds.Count < 2) return;

            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
                if (!_ready.Contains(id)) return;

            // all ready
            StartMatch();
        }

        void StartMatch()
        {
            CurrentRoundIndex.Value = 0;
            LoadRound();
        }

        void LoadRound()
        {
            State.Value = SessionState.LoadingRound;
            NetworkManager.Singleton.SceneManager.LoadScene(roundSceneName, LoadSceneMode.Single);
        }

        [ServerRpc(RequireOwnership = false)]
        public void NotifyRoundEndedServerRpc()
        {
            if (!IsServer) return;
            State.Value = SessionState.Results;
            Invoke(nameof(AdvanceOrEndMatch), resultsDuration);
        }

        void AdvanceOrEndMatch()
        {
            if (!IsServer) return;

            CurrentRoundIndex.Value++;

            if (CurrentRoundIndex.Value >= roundsPerMatch)
            {
                State.Value = SessionState.MatchOver;
                // Back to lobby (you can show winner UI first)
                NetworkManager.Singleton.SceneManager.LoadScene(hubSceneName, LoadSceneMode.Single);
                State.Value = SessionState.Lobby;
                _ready.Clear();
                return;
            }

            LoadRound();
        }
    }
}
