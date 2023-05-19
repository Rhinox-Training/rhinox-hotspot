using System;
using Rhinox.Lightspeed;
using Rhinox.GUIUtils;
#if UNITY_EDITOR
using Rhinox.GUIUtils.Editor;
#endif
using UnityEditor;
using UnityEngine;

namespace Hotspot
{
    public class OcclusionPortalBakeData : MonoBehaviour, ISerializationCallbackReceiver
    {
        [NonSerialized]
        public Bounds Bounds;

        [SerializeField]
        private Vector3 _center;
        [SerializeField]
        private Vector3 _size;

        private OcclusionPortal _occlusionPortal;
        public OcclusionPortal Portal => _occlusionPortal;

        protected virtual void Awake()
        {
            _occlusionPortal = GetComponent<OcclusionPortal>();
        }
        
#if UNITY_EDITOR
        private const string k_CenterPath = "m_Center";
        private const string k_SizePath = "m_Size";
        
        public static OcclusionPortalBakeData CreateOrUpdate(OcclusionPortal portal)
        {
            var bakeData = portal.GetOrAddComponent<OcclusionPortalBakeData>();

            var so = new SerializedObject(portal);
            if (so != null)
            {
                var prop = so.FindProperty(k_CenterPath);
                var center = (Vector3) prop.GetValue();

                var prop2 = so.FindProperty(k_SizePath);
                var size = (Vector3) prop2.GetValue();

                bakeData.Bounds = new Bounds(center, size);
            }
            else
                bakeData.Bounds = new Bounds();
            
            return bakeData;
        }
#endif
        public void OnBeforeSerialize()
        {
            _center = Bounds.center;
            _size = Bounds.size;
        }

        public void OnAfterDeserialize()
        {
            Bounds = new Bounds(_center, _size);
        }
    }
}