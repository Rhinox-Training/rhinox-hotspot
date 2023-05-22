using Hotspot;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEngine;

public static class LODGroupExtensions
{
    public static int GetCurrentLODIndex(this LODGroup lodGroup)
    {
        return GetCurrentLODIndex(lodGroup, Camera.main);
    }

    public static int GetCurrentLODIndex(this LODGroup lodGroup, Camera cam)
    {
        float lodPercentage = lodGroup.CalculateCurrentHeight(cam);
        var lods = lodGroup.GetLODs();
        for (int i = 0; i < lods.Length; i++)
        {
            if (lods[i].screenRelativeTransitionHeight < lodPercentage)
                return i;
        }

        return 0;
    }

    ///<summary>
    ///Calculates the current transition percentage of the LODGroup relative to the Camera.
    ///</summary>
    ///<param name = "lodGroup" > The LODGroup whose height percentage is being calculated.</param>
    ///<param name = "cam" > The Camera to which the LODGroup's height percentage is being calculated relative to.</param>
    ///<returns>The current transition percentage of the LODGroup relative to the Camera.</returns>
    ///<exception cref = "System.ArgumentNullException">Thrown if the distance between the LODGroup and Camera is zero.</exception>
    ///<example>
    ///<code>float transitionPercentage = lodGroup.CalculateCurrentTransitionPercentage(mainCamera);</code>
    ///</example>
    public static float CalculateCurrentTransitionPercentage(this LODGroup lodGroup, Camera cam)
    {
        return CalculateCurrentHeight(lodGroup, cam) * 100f;
    }

    public static float CalculateCurrentHeight(this LODGroup lodGroup, Camera cam)
    {
        // Calculates the distance between LODGroup and the camera
        float distance = Vector3.Distance(lodGroup.transform.position, cam.transform.position);
        if (distance == 0f)
        {
            PLog.Error<HotspotLogger>(
                "Distance between LODGroup and Camera is zero, can't proceed as it will lead to division by zero.");
            return -1;
        }

        // Computes the height based on the size of the LODGroup and the multiplier
        float height = lodGroup.size;

        // Calculates the relative height based on the reciprocal of the distance times the height
        // Also check the lodBias in the QualitySettings
        return 1 / distance * height * QualitySettings.lodBias;
    }

    public static float GetVertexHeightDensityForLOD(this LODGroup lodGroup, int lodIndex)
    {
        return lodGroup.GetVertexHeightDensityForLOD(lodIndex, Camera.main);
    }
    
    public static float GetVertexHeightDensityForLOD(this LODGroup lodGroup, int lodIndex, Camera cam)
    {
        var lods = lodGroup.GetLODs();
        if (lodIndex < 0 || lodIndex >= lods.Length)
        {
            PLog.Error<HotspotLogger>(
                $"[LODGroupExtensions,GetVertexHeightDensityForLOD] Invalid LOD index {lodIndex} in LODGroup {lodGroup.name}");
            return -1f;
        }
        var targetLod = lods[lodIndex];
        var currentHeight = lodGroup.CalculateCurrentHeight(cam);
        currentHeight *= cam.pixelHeight;
        
        int vertexCount = 0;
        foreach (Renderer r in targetLod.renderers)
        {
            if(!MeshInfo.TryCreate(r, out var meshInfo))
                continue;
            vertexCount += meshInfo.Mesh.vertexCount;
        }
        return vertexCount / currentHeight;
    }
}