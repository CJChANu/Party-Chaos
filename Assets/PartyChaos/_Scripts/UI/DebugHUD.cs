using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using PartyChaos.Game;

namespace PartyChaos.UI
{
    public class DebugHUD : MonoBehaviour
    {
        public TMP_Text text;

        [Header("Optional: Leave empty to auto-find")]
        public RoundManager roundManager;
        public GameSessionManager sessionManager;
        public ScoreManager scoreManager;

        private void Reset()
        {
            text = GetComponent<TMP_Text>();
        }

        private void Awake()
        {
            if (roundManager == null) roundManager = FindAnyObjectByType<RoundManager>();
            if (sessionManager == null) sessionManager = FindAnyObjectByType<GameSessionManager>();
            if (scoreManager == null) scoreManager = FindAnyObjectByType<ScoreManager>();
        }

        private void Update()
        {
            if (text == null) return;

            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                text.text = "No NetworkManager";
                return;
            }

            // Local player (NetworkObject -> GameObject)
            NetworkObject localNetObj = null;
            if (nm.SpawnManager != null)
                localNetObj = nm.SpawnManager.GetLocalPlayerObject();

            var sb = new StringBuilder(512);

            sb.AppendLine($"Net: {(nm.IsHost ? "Host" : nm.IsClient ? "Client" : "Offline")}");
            sb.AppendLine($"Clients: {nm.ConnectedClientsList.Count}");

            if (localNetObj == null)
            {
                sb.AppendLine("Local Player: (not spawned yet)");
                text.text = sb.ToString();
                return;
            }

            var localGO = localNetObj.gameObject;

            sb.AppendLine($"Local Player: {localGO.name}");
            sb.AppendLine($"OwnerClientId: {localNetObj.OwnerClientId}");

            // ---- dump NetworkVariables from your controllers (no guessing names) ----
            AppendNetVarDump(sb, localGO, "PartyChaos.Player.DrunkController");
            AppendNetVarDump(sb, localGO, "PartyChaos.Player.PartyLevelController");
            AppendNetVarDump(sb, localGO, "PartyChaos.Player.SpectatorController");

            // ---- round/session info if available ----
            if (roundManager != null)
            {
                sb.AppendLine("--- Round ---");
                sb.AppendLine($"TimeLeft: {roundManager.TimeLeft.Value:0.0}");
            }

            if (sessionManager != null)
            {
                sb.AppendLine("--- Session ---");
                sb.AppendLine($"State: {sessionManager.State.Value}");
            }

            // ---- score (if your ScoreManager exposes a Scores list like in your codebase) ----
            if (scoreManager != null)
            {
                sb.AppendLine("--- Score ---");
                TryAppendMyScore(sb, scoreManager, localNetObj.OwnerClientId);
            }

            text.text = sb.ToString();
        }

        private static void AppendNetVarDump(StringBuilder sb, GameObject go, string typeName)
        {
            var comp = FindComponentByFullName(go, typeName);
            if (comp == null) return;

            sb.AppendLine($"--- {comp.GetType().Name} ---");

            // Reflect fields of type NetworkVariable<T>
            var fields = comp.GetType().GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            bool any = false;

            foreach (var f in fields)
            {
                if (f.FieldType == null) continue;
                if (!f.FieldType.IsGenericType) continue;
                if (f.FieldType.GetGenericTypeDefinition() != typeof(NetworkVariable<>)) continue;

                var nv = f.GetValue(comp);
                if (nv == null) continue;

                var valueProp = nv.GetType().GetProperty("Value");
                if (valueProp == null) continue;

                object val = valueProp.GetValue(nv);
                sb.AppendLine($"{f.Name}: {val}");
                any = true;
            }

            if (!any)
                sb.AppendLine("(No NetworkVariables found)");
        }

        private static Component FindComponentByFullName(GameObject go, string fullTypeName)
        {
            var comps = go.GetComponents<MonoBehaviour>();
            foreach (var c in comps)
            {
                if (c == null) continue;
                if (c.GetType().FullName == fullTypeName)
                    return c;
            }
            return null;
        }

        private static void TryAppendMyScore(StringBuilder sb, ScoreManager scoreManager, ulong myClientId)
        {
            // Your ScoreManager in this project usually has a public list/array called "Scores"
            // with elements that contain ClientId / RoundPoints / MatchPoints.
            // We’ll reflect it safely so it compiles even if structure changes.

            var t = scoreManager.GetType();
            var scoresField = t.GetField("Scores",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (scoresField == null)
            {
                sb.AppendLine("Scores: (field not found)");
                return;
            }

            var scoresObj = scoresField.GetValue(scoreManager);
            if (scoresObj is System.Collections.IEnumerable enumerable)
            {
                foreach (var entry in enumerable)
                {
                    if (entry == null) continue;

                    var et = entry.GetType();
                    var clientIdProp = et.GetProperty("ClientId");
                    var roundProp = et.GetProperty("RoundPoints");
                    var matchProp = et.GetProperty("MatchPoints");

                    if (clientIdProp == null) continue;

                    var cid = (ulong)clientIdProp.GetValue(entry);
                    if (cid != myClientId) continue;

                    var r = roundProp != null ? roundProp.GetValue(entry) : 0;
                    var m = matchProp != null ? matchProp.GetValue(entry) : 0;

                    sb.AppendLine($"My Round/Match: {r}/{m}");
                    return;
                }

                sb.AppendLine("My Round/Match: (not found yet)");
                return;
            }

            sb.AppendLine("Scores: (not enumerable)");
        }
    }
}
