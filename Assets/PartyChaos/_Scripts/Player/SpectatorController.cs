using Unity.Netcode;
using UnityEngine;

namespace PartyChaos.Player
{
    public class SpectatorController : NetworkBehaviour
    {
        public NetworkVariable<bool> IsSpectator = new(false);

        [ServerRpc]
        public void SetSpectatorServerRpc(bool spectator)
        {
            IsSpectator.Value = spectator;

            // Your movement controller should disable input when spectator
            var move = GetComponent<MovementControllerStub>();
            if (move != null) move.SetEnabled(!spectator);
        }
    }

    // Replace this with your real movement controller. This is just a stub.
    public class MovementControllerStub : MonoBehaviour
    {
        bool _enabled = true;
        public void SetEnabled(bool e) => _enabled = e;
    }
}
