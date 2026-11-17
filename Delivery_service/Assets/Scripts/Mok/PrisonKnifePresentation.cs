using UnityEngine;

namespace DeliveryService.Mok
{
    // Hide only the duplicate world knife while the screen-space knife is being drawn.
    // Leave the quest's transform updates, activation and all cutting rules untouched.
    public sealed class PrisonKnifePresentation : MonoBehaviour
    {
        PrisonChickenQuest quest;
        Transform knife;
        Renderer[] renderers;
        bool[] saved;
        bool hidden;

        public static void Attach(PrisonChickenQuest owner)
        {
            var view = owner.GetComponent<PrisonKnifePresentation>();
            if (!view) view = owner.gameObject.AddComponent<PrisonKnifePresentation>();
            view.quest = owner;
            view.Refresh();
        }

        void LateUpdate() => Refresh();

        void OnGUI()
        {
            if (!quest || !quest.IsPlayingCustomCooking) return;
            var matrix = GUI.matrix;
            int depth = GUI.depth;
            GUI.depth = -10;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));
            PrisonCuttingUI.DrawImpact();
            GUI.matrix = matrix; GUI.depth = depth;
        }

        void Refresh()
        {
            Transform next = quest && quest.teamCooking ? quest.teamCooking.knife : null;
            if (next != knife)
            {
                Restore();
                knife = next;
                renderers = knife ? knife.GetComponentsInChildren<Renderer>(true) : null;
                saved = renderers != null ? new bool[renderers.Length] : null;
            }
            bool shouldHide = quest && quest.isActiveAndEnabled && quest.IsPlayingCustomCooking &&
                quest.currentStep == PrisonChickenQuest.CustomStep.Step1_Cut;
            if (!shouldHide) { Restore(); return; }
            if (hidden || renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i]) continue;
                saved[i] = renderers[i].forceRenderingOff;
                renderers[i].forceRenderingOff = true;
            }
            hidden = true;
        }

        void Restore()
        {
            if (!hidden || renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i]) renderers[i].forceRenderingOff = saved[i];
            hidden = false;
        }
        void OnDisable() => Restore();
        void OnDestroy() => Restore();
    }
}
