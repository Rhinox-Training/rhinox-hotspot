using Rhinox.Lightspeed;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hotspot
{
    public interface IRendererFilter
    {
        public bool IsValid(Renderer r);
    }

    [Serializable]
    public class MaterialInstanceFilter : IRendererFilter
    {

        public List<Material> _allowedMaterials = new List<Material>();

        public bool IsValid(Renderer renderer)
        {
            return renderer.sharedMaterials.ContainsAny(_allowedMaterials);
        }
    }

    [Serializable]
    public class MaterialKeyWordFilter : IRendererFilter
    {
        public List<string> _allowedMaterialKeyWords = new List<string>();

        public bool IsValid(Renderer renderer)
        {
            //loop over all materials
            foreach (var mat in renderer.sharedMaterials)
            {
                //check if evaluated materials name contains of the allowed keywords
                if (mat.name.ContainsOneOf(_allowedMaterialKeyWords.ToArray()))
                    return true;
            }

            return false;
        }
    }

    [Serializable]
    public class MeshKeyWordFilter : IRendererFilter
    {
        public List<string> _allowedMeshNameKeyWords = new List<string>();

        public bool IsValid(Renderer renderer)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter != null)
                return filter.sharedMesh.name.ContainsOneOf(_allowedMeshNameKeyWords.ToArray());

            return false;
        }
    }

    [Serializable]
    public class BigObjectSizeFilter : IRendererFilter
    {
        public Vector3 _sizeThreshold = new Vector3(.5f, .5f, .01f);

        public bool IsValid(Renderer renderer)
        {
            return renderer.bounds.IsSizeBiggerThan(_sizeThreshold);
        }
    }
}