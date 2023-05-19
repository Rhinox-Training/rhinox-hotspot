using Rhinox.Lightspeed;
using Rhinox.Magnus;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hotspot
{
    [ServiceLoader]
    public class OcclusionPortalTogglingService : AutoService<OcclusionPortalTogglingService>
    {
        private OcclusionPortalBakeData[] _portals;

        private const float THRESHOLD = 10.0f;

        protected override void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            base.OnSceneLoaded(scene, mode);
            _portals = GameObject.FindObjectsOfType<OcclusionPortalBakeData>();
        }

        protected override void Update()
        {
            base.Update();

            // NOTE: should only work with Magnus player
            if (PlayerManager.Instance == null || PlayerManager.Instance.ActivePlayer == null)
                return;

            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            foreach (var portal in _portals)
            {
                var screenPixels = portal.Bounds.GetScreenPixels(mainCamera);
                portal.Portal.open = screenPixels > THRESHOLD;
            }
        }
    }
}