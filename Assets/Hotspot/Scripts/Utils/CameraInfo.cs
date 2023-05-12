using UnityEngine;

namespace Hotspot.Editor.Utils
{
    public class CameraInfo : MonoBehaviour
    {
        private float _aspectRatio;
        private float _nearClipPlane;
        private float _farClipPlane;

        private Matrix4x4 _viewMatrix;

        private Matrix4x4 _projectionMatrix;
        
        private float _fov;


        public void SetCameraInfo(Camera cam)
        {
            // transform.transform.position = cam.transform.position;
            // transform.transform.rotation = cam.transform.rotation;
            //
            _aspectRatio = cam.aspect;
            _nearClipPlane = cam.nearClipPlane;
            _farClipPlane = cam.farClipPlane;
            
            _fov = Mathf.Tan((Mathf.Deg2Rad * cam.fieldOfView) / 2f);

            RecalculateViewMatrix();
            CalculateProjectionMatrix();
        }

        //--------------------------------------------------------------------------------------------------------------
        // Transform the camera info
        //--------------------------------------------------------------------------------------------------------------

        public void TranslateCamera(Vector3 translation)
        {
            transform.Translate(translation);
            RecalculateViewMatrix();
        }

        public void RotateCamera(Vector3 rotation)
        {
            transform.Rotate(rotation);
            RecalculateViewMatrix();
        }

        public Matrix4x4 GetViewMatrix()
        {
            return _viewMatrix;
        }

        private void RecalculateViewMatrix()
        {
            var transformCached = transform;
            var right = transformCached.right;
            var up = transformCached.up;
            var back = -transformCached.forward;
            var position = transformCached.position;

            _viewMatrix = new Matrix4x4(
                new Vector4(right.x, -right.y, right.z, 0),
                new Vector4(up.x, up.y, up.z, 0),
                new Vector4(back.x, -back.y, back.z, 0),
                new Vector4(-position.x, -position.y, position.z, 1));
        }

        public Matrix4x4 GetProjectionMatrix()
        {
            return _projectionMatrix;
        }

        private void CalculateProjectionMatrix()
        {
            float clipPlaneDifference = _farClipPlane - _nearClipPlane;
            _projectionMatrix = new Matrix4x4(
                new Vector4(1f / (_aspectRatio * _fov), 0f, 0f, 0f),
                new Vector4(0f, 1f / _fov, 0f, 0f),
                new Vector4(0f, 0f, -(_farClipPlane + _nearClipPlane) / clipPlaneDifference, -1f),
                new Vector4(0f, 0f, -2f * _farClipPlane * _nearClipPlane / clipPlaneDifference, 0f));
        }
    }
}