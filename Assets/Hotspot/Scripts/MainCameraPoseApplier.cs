using Rhinox.Lightspeed;
using UnityEngine;

namespace Hotspot
{
    public class MainCameraPoseApplier : ICameraPoseApplier
    {
        private Camera _camera;
        private Pose? _previousPose;

        public bool ApplyPoseToCamera(Pose pose)
        {
            if (_camera == null)
                _camera = Camera.main;

            if (_camera != null)
            {
                _previousPose = _camera.transform.GetWorldPose();
                _camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
                return true;
            }

            return false;
        }

        public bool Restore()
        {
            if (_camera == null)
                return false;

            if (_previousPose.HasValue)
            {
                var pose = _previousPose.Value;
                _camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
                return true;
            }

            return false;
        }
    }
}