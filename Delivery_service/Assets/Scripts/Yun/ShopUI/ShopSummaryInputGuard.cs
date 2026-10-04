using System.Collections.Generic;
using UnityEngine;

namespace DeliveryService.Yun.ShopUI
{
    // The new PC view releases its input lock when closed. Keep the existing summary modal usable.
    public sealed class ShopSummaryInputGuard : MonoBehaviour
    {
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private ShopPC pc;
        private bool locked;
        private CursorLockMode previousLock;
        private bool previousVisible;

        public void Configure(ShopPC owner)
        {
            pc = owner;
            if (isActiveAndEnabled) Acquire();
        }

        private void OnEnable() { Acquire(); }

        private void Acquire()
        {
            if (locked || pc == null) return;
            locked = true;
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            Suspend(pc);
            if (pc.playerCamera != null)
            {
                Transform player = pc.playerCamera;
                while (player.parent != null && player.GetComponent<CharacterController>() == null)
                    player = player.parent;
                foreach (MonoBehaviour behaviour in player.GetComponentsInChildren<MonoBehaviour>(true))
                    if (behaviour is PlayerMove || behaviour is PlayerMovement || behaviour is CameraMove
                        || behaviour is PlayerInteraction || behaviour is PlayerInteract || behaviour == pc.cameraLookScript)
                        Suspend(behaviour);
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Suspend(Behaviour target)
        {
            if (target != null && target.enabled)
            {
                suspended.Add(target);
                target.enabled = false;
            }
        }

        private void OnDisable()
        {
            if (!locked) return;
            foreach (Behaviour target in suspended) if (target != null) target.enabled = true;
            suspended.Clear();
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
            locked = false;
        }
    }
}
