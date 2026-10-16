using UnityEngine;

namespace Rustbound.UI
{
    public sealed class ModalActionRelay : MonoBehaviour
    {
        [SerializeField] private ModalDialogView owner;
        [SerializeField] private ModalAction action;
        public void Invoke() => owner.Request(action);
#if UNITY_EDITOR
        public void Configure(ModalDialogView view, ModalAction requestedAction) { owner = view; action = requestedAction; }
#endif
    }
}
