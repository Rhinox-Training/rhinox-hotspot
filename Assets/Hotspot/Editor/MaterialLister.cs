using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Rhinox.Hotspot.Editor
{
    public class MaterialLister : MonoBehaviour
    {
        [ContextMenu("Get Mat List")]
        public void GetListOfMaterials()
        {
            var renderers = Object.FindObjectsOfType<Renderer>();

            Dictionary<Material, int> occurenceList = new Dictionary<Material, int>();

            foreach (var renderer in renderers)
            {
                //renderer.
                //var mats = renderers[0].sharedMaterials;
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (!occurenceList.TryAdd(mat, 1))
                    {
                        ++occurenceList[mat];
                    }
                }
            }

            Debug.Log($"Unique Materials: {occurenceList.Count}");

            var myList = occurenceList.ToList();
            myList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            foreach (var item in myList)
            {
                Debug.Log(item.ToString());
            }


            //var sortedDict = from pair in occurenceList orderby pair.Value ascending select pair.Value;

        }
    }
}