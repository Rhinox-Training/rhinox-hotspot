using UnityEngine;

namespace Hotspot
{
    public interface ICameraPoseApplier
    {
        bool ApplyPoseToCamera(Pose pose);
        bool Restore();
    }
}