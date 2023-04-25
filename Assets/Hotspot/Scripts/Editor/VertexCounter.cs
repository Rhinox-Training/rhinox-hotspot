using UnityEngine;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace Rhinox.Hotspot.Editor
{
    public static class VertexCounter// : EditorToolContext
    {
        //should later be exposed
        private const int MAX_VERTEX_COUNT_IN_CUBE = 50;

        [MenuItem("Tools/Vertex Counter", false, 1)]
        public static void ShowVertexCount()
        {
            //FindObjectsOfType is only loaded and active
            //FindObjectsOfTypeAll also includes non-active
            var renderers = Object.FindObjectsOfType<MeshFilter>();

            Bounds sceneBound = new Bounds(Vector3.zero, Vector3.zero);

            List<Vector3> sceneVertices = new List<Vector3>();
            foreach (MeshFilter meshFilter in renderers)
            {
                sceneBound.Encapsulate(meshFilter.sharedMesh.bounds);

                sceneVertices.AddRange(meshFilter.sharedMesh.vertices);
            }

            //Gizmos.DrawWireCube(sceneBound.center, sceneBound.size);
            DrawBounds(sceneBound, 110f);

            //Vector3[] sceneVertices = new Vector3[Cnt];

            //int idx = 0;
            //foreach (MeshFilter meshFilter in renderers)
            //{
            //meshFilter.

            //idx += meshFilter.mesh.vertexCount;
            //}

            //var sdfs = renderers[0].mesh.vertexCount;

            Debug.Log(renderers.Length);

            Debug.Log("I EXIST \\._./");
        }

        static void DrawBounds(Bounds b, float delay = 0)
        {
            // bottom
            var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
            var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
            var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
            var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

            Debug.DrawLine(p1, p2, Color.blue, delay);
            Debug.DrawLine(p2, p3, Color.red, delay);
            Debug.DrawLine(p3, p4, Color.yellow, delay);
            Debug.DrawLine(p4, p1, Color.magenta, delay);

            // top
            var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
            var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
            var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
            var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

            Debug.DrawLine(p5, p6, Color.blue, delay);
            Debug.DrawLine(p6, p7, Color.red, delay);
            Debug.DrawLine(p7, p8, Color.yellow, delay);
            Debug.DrawLine(p8, p5, Color.magenta, delay);

            // sides
            Debug.DrawLine(p1, p5, Color.white, delay);
            Debug.DrawLine(p2, p6, Color.gray, delay);
            Debug.DrawLine(p3, p7, Color.green, delay);
            Debug.DrawLine(p4, p8, Color.cyan, delay);
        }
    }
}