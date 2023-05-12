using UnityEngine;

namespace Hotspot.Editor.Utils
{
    public class CameraInfo
    {
        private float _aspectRatio;
        private float _nearClipPlane;
        private float _farClipPlane;

        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projectionMatrix;
        private float _fov;
        private Vector3 _position;

        private static readonly Vector3 _up = new Vector3(0, 1, 0);
        private static readonly Vector3 _forward = new Vector3(0, 0, 1);
        private static readonly Vector3 _right = new Vector3(1, 0, 0);

        public void SetCameraInfo(Camera cam)
        {
            _position = cam.transform.position;

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
            _position += translation;
            RecalculateViewMatrix();
        }
        
        public Matrix4x4 GetViewMatrix()
        {
            return _viewMatrix;
        }

        private void RecalculateViewMatrix()
        {
            var right = _right;
            var up = _up;
            var back = -_forward;
            var position = _position;

            _viewMatrix = new Matrix4x4(
                new Vector4(right.x, right.y, right.z, 0),
                new Vector4(up.x, up.y, up.z, 0),
                new Vector4(back.x, -back.y, back.z, 0),
                new Vector4(position.x, position.y, position.z, 1));
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