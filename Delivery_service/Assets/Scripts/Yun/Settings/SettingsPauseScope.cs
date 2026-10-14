using System.Collections.Generic;
using UnityEngine;

namespace DeliveryService.Yun.Settings
{
    // Settings owns this scope, including when opened directly from the hierarchy.
    // Do not deactivate gameplay GameObjects: that would stop their coroutines permanently.
    internal sealed class SettingsPauseScope
    {
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private readonly List<Animator> unscaledAnimators = new List<Animator>();
        private readonly List<ParticleSystem> unscaledParticles = new List<ParticleSystem>();
        private float previousScale;
        private bool acquired;

        public void Acquire()
        {
            if (acquired) return;
            acquired = true;
            previousScale = Time.timeScale;
            Time.timeScale = 0;
            foreach (var script in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // Input readers still receive Update at timeScale zero, including bike mouse look.
                if (script != null && script.enabled && (script is PlayerMove || script is PlayerMovement
                    || script is CameraMove || script is PlayerInteraction || script is PlayerInteract
                    || script is MotorcycleController || script is BikePizzaStorage || script is ShopPC
                    || script is PhoneController || script is NavigationManager || script is WeatherManager
                    || script is DayNightCycle || script is NPCControl || script is NPCManager
                    || script is RobberAI || script is RobberyManager || script is CarMov))
                {
                    suspended.Add(script);
                    script.enabled = false;
                }
            }
            foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (animator.updateMode == AnimatorUpdateMode.UnscaledTime
                    && animator.GetComponentInParent<Canvas>() == null)
                {
                    unscaledAnimators.Add(animator);
                    animator.updateMode = AnimatorUpdateMode.Normal;
                }
            foreach (var particle in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (particle.main.useUnscaledTime && particle.GetComponentInParent<Canvas>() == null)
                {
                    unscaledParticles.Add(particle);
                    var main = particle.main;
                    main.useUnscaledTime = false;
                }
        }

        public void Maintain()
        {
            if (!acquired) return;
            Time.timeScale = 0;
            foreach (var script in suspended)
                if (script != null) script.enabled = false;
        }

        public void Release()
        {
            if (!acquired) return;
            acquired = false;
            Time.timeScale = previousScale;
            foreach (var script in suspended)
                if (script != null) script.enabled = true;
            foreach (var animator in unscaledAnimators)
                if (animator != null) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            foreach (var particle in unscaledParticles)
                if (particle != null)
                {
                    var main = particle.main;
                    main.useUnscaledTime = true;
                }
            suspended.Clear();
            unscaledAnimators.Clear();
            unscaledParticles.Clear();
        }
    }
}
