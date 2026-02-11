using Unity.Netcode;
using UnityEngine;

namespace PartyChaos.Networking
{
    public class NetworkBootstrapper : MonoBehaviour
    {
        [Header("Auto start")]
        public bool autoStartFromArgs = true;

        void Start()
        {
            if (!autoStartFromArgs) return;

            // Example args:
            // -batchmode -nographics -server (dedicated)
            // -client (client)
            var args = System.Environment.GetCommandLineArgs();
            bool isServer = HasArg(args, "-server");
            bool isClient = HasArg(args, "-client");

            if (isServer)
            {
                NetworkManager.Singleton.StartServer();
                Debug.Log("Started Dedicated Server");
            }
            else if (isClient)
            {
                NetworkManager.Singleton.StartClient();
                Debug.Log("Started Client");
            }
            else
            {
                // Editor convenience: host
                NetworkManager.Singleton.StartHost();
                Debug.Log("Started Host (Editor default)");
            }
        }

        bool HasArg(string[] args, string arg)
        {
            for (int i = 0; i < args.Length; i++)
                if (args[i].Equals(arg)) return true;
            return false;
        }
    }
}
