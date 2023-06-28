using System;
using UnityEditor.Rendering;
using UnityEngine;

namespace Hotspot.Editor
{
    [Serializable, DebugState(typeof(TextureDebugUIField))]
    public class TextureDebugState : DebugState<Texture2D>
    {
    }
}