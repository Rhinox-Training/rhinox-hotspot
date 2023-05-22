using Hotspot;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEngine;

/// <summary>
/// This class provides extension methods for the LODGroup class.
/// </summary>
public static class LODGroupExtensions
{
    /// <summary>
    /// Gets the current LOD index for the LODGroup using the main camera.
    /// </summary>
    /// <param name="lodGroup">The LODGroup instance.</param>
    /// <returns>The current LOD index.</returns>
    public static int GetCurrentLODIndex(this LODGroup lodGroup)
    {
        // Use the main camera to get the current LOD index
        return GetCurrentLODIndex(lodGroup, Camera.main);
    }

    /// <summary>
    /// Gets the current LOD index for the LODGroup using a specified camera.
    /// </summary>
    /// <param name="lodGroup">The LODGroup instance.</param>
    /// <param name="cam">The camera to use for calculation.</param>
    /// <returns>The current LOD index.</returns>
    public static int GetCurrentLODIndex(this LODGroup lodGroup, Camera cam)
    {
        // Calculate the current height of the LODGroup
        float lodPercentage = lodGroup.CalculateCurrentHeight(cam);
        var lods = lodGroup.GetLODs();

        // Loop through the LODs to find the current LOD index
        for (int i = 0; i < lods.Length; i++)
        {
            if (lods[i].screenRelativeTransitionHeight < lodPercentage)
                return i;
        }

        // If no LOD index is found, return 0
        return 0;
    }

    /// <summary>
    /// Calculates the current transition percentage for the LODGroup using a specified camera.
    /// </summary>
    /// <param name="lodGroup">The LODGroup instance.</param>
    /// <param name="cam">The camera to use for calculation.</param>
    /// <returns>The current transition percentage.</returns>
    public static float CalculateCurrentTransitionPercentage(this LODGroup lodGroup, Camera cam)
    {
        // Calculate the current height and convert it to a percentage
        return CalculateCurrentHeight(lodGroup, cam) * 100f;
    }

    /// <summary>
    /// Calculates the current height for the LODGroup using a specified camera.
    /// </summary>
    /// <param name="lodGroup">The LODGroup instance.</param>
    /// <param name="cam">The camera to use for calculation.</param>
    /// <returns>The current height.</returns>
    public static float CalculateCurrentHeight(this LODGroup lodGroup, Camera cam)
    {
        // Get the transform of the LODGroup
        Transform transform = lodGroup.transform;
        // Calculate the reference point
        Vector3 referencePoint = transform.TransformPoint(lodGroup.localReferencePoint);
        // Calculate the distance from the reference point to the camera
        float distance = Vector3.Distance(referencePoint, cam.transform.position);
        
        // Get the scale of the LODGroup
        Vector3 lodGroupScale = transform.lossyScale;
        // Find the maximum scale
        float maxScale = Mathf.Max(lodGroupScale.x, lodGroupScale.y, lodGroupScale.z);
        // Calculate the size of the LODGroup
        float size = lodGroup.size * maxScale;
        
        // Calculate the half angle of the camera's field of view
        float halfAngle = Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView * 0.5F);
        // Calculate the relative height of the LODGroup
        float relativeHeight = size * 0.5F / (distance * halfAngle);

        return relativeHeight;
    }

    /// <summary>
    /// Gets the vertex height density for a specific LOD using the main camera.
    /// </summary>
    /// <param name="lodGroup">The LODGroup instance.</param>
    /// <param name="lodIndex">The LOD index.</param>
    /// <returns>The vertex height density.</returns>
    public static float GetVertexHeightDensityForLOD(this LODGroup lodGroup, int lodIndex)
    {
        // Use the main camera to get the vertex height density for the specified LOD
        return lodGroup.GetVertexHeightDensityForLOD(lodIndex, Camera.main);
    }

    /// <summary>
    /// Gets the vertex height density fora specific LOD using a specified camera.
    /// </summary>
    /// <param name="lodGroup">The LODGroup instance.</param>
    /// <param name="lodIndex">The LOD index.</param>
    /// <param name="cam">The camera to use for calculation.</param>
    /// <returns>The vertex height density. Returns -1 if the LOD index is invalid.</returns>
    public static float GetVertexHeightDensityForLOD(this LODGroup lodGroup, int lodIndex, Camera cam)
    {
        var lods = lodGroup.GetLODs();
        // Check if the LOD index is valid
        if (lodIndex < 0 || lodIndex >= lods.Length)
        {
            // Log an error if the LOD index is invalid
            PLog.Error<HotspotLogger>(
                $"[LODGroupExtensions,GetVertexHeightDensityForLOD] Invalid LOD index {lodIndex} in LODGroup {lodGroup.name}");
            return -1f;
        }

        var targetLod = lods[lodIndex];
        // Calculate the current height of the LODGroup
        var currentHeight = lodGroup.CalculateCurrentHeight(cam);
        currentHeight *= cam.pixelHeight;

        int vertexCount = 0;
        // Loop through the renderers in the target LOD
        foreach (Renderer r in targetLod.renderers)
        {
            // Try to create a MeshInfo object from the renderer
            if (!MeshInfo.TryCreate(r, out var meshInfo))
                continue;
            // Add the vertex count of the mesh to the total vertex count
            vertexCount += meshInfo.Mesh.vertexCount;
        }

        // Return the vertex height density
        return vertexCount / currentHeight;
    }
}