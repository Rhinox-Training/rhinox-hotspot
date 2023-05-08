using System;
using System.Linq;
using System.Reflection;
using Rhinox.Lightspeed.Reflection;
using Sirenix.OdinInspector;
using Unity.Profiling;
using UnityEngine;

namespace Hotspot.Editor
{
    public static class UnityDebugProfilerStats
    {
        public static ProfilerStat MainThreadTime = new ProfilerStat(ProfilerCategory.Internal, "Main Thread", 15);
        public static ProfilerStat RenderingUsage = new ProfilerStat(ProfilerCategory.Render, "Camera.Render");
        public static ProfilerStat ScriptsUsage = new ProfilerStat(ProfilerCategory.Scripts, "");
        public static ProfilerStat AnimationUsage = new ProfilerStat(ProfilerCategory.Internal, "Animation");
        public static ProfilerStat UsedSystemMemory = new ProfilerStat(ProfilerCategory.Memory, "System Used Memory");
        public static ProfilerStat GCReservedMemory = new ProfilerStat(ProfilerCategory.Memory, "GC Reserved Memory");
        public static ProfilerStat MaterialCount = new ProfilerStat(ProfilerCategory.Memory, "Material Count");
        public static ProfilerStat MeshMemory = new ProfilerStat(ProfilerCategory.Memory, "Mesh Memory");
        public static ProfilerStat TextureMemory = new ProfilerStat(ProfilerCategory.Memory, "Texture Memory");
        public static ProfilerStat ObjectCount = new ProfilerStat(ProfilerCategory.Memory, "Object Count");
        public static ProfilerStat BatchesCount = new ProfilerStat(ProfilerCategory.Render, "Batches Count");
        public static ProfilerStat TrianglesCount = new ProfilerStat(ProfilerCategory.Render, "Triangles Count");
        public static ProfilerStat VerticesCount = new ProfilerStat(ProfilerCategory.Render, "Vertices Count");
        
        public static ProfilerStat[] All
        {
            get
            {
                return new[]
                {
                    MainThreadTime,
                    RenderingUsage,
                    ScriptsUsage,
                    AnimationUsage,
                    UsedSystemMemory,
                    GCReservedMemory,
                    MaterialCount,
                    MeshMemory,
                    TextureMemory,
                    ObjectCount,
                    BatchesCount,
                    TrianglesCount,
                    VerticesCount
                };
            }
        }
    }

    [Serializable]
    public class ProfilerStat : ISerializationCallbackReceiver, IEquatable<ProfilerStat>
    {
        [NonSerialized]
        public ProfilerCategory Category;
        public string StatName;

        [SerializeField, HideInInspector]
        private ushort _serializedCategory;

        public int SampleCapacity = 1;

        private FieldInfo _categoryField;

        public ProfilerStat(ProfilerCategory category, string name, int sampleCapacity = 1)
        {
            Category = category;
            StatName = name;
            SampleCapacity = 1;
            OnBeforeSerialize();
        }

        public void OnBeforeSerialize()
        {
            if (_categoryField == null)
                _categoryField = typeof(ProfilerCategory).GetField("m_CategoryId",BindingFlags.Instance | BindingFlags.NonPublic);

            _serializedCategory = (ushort)_categoryField.GetValue(Category);
        }

        public void OnAfterDeserialize()
        {
            if (_categoryField == null)
                _categoryField = typeof(ProfilerCategory).GetField("m_CategoryId",BindingFlags.Instance | BindingFlags.NonPublic);
            var properties = typeof(ProfilerCategory).GetProperties(BindingFlags.Public | BindingFlags.Static);
            Category = properties.Where(x =>
                    x.GetReturnType() == typeof(ProfilerCategory) &&
                    _serializedCategory == (ushort) _categoryField.GetValue(x.GetValue(null)))
                .Select(x => (ProfilerCategory)x.GetValue(null))
                .FirstOrDefault();
        }
        
        public bool Equals(ProfilerStat other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return StatName == other.StatName && _serializedCategory == other._serializedCategory && SampleCapacity == other.SampleCapacity;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProfilerStat) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StatName, _serializedCategory, SampleCapacity);
        }

        public static bool operator ==(ProfilerStat left, ProfilerStat right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ProfilerStat left, ProfilerStat right)
        {
            return !Equals(left, right);
        }
    }
    
    
    public class UnityProfilerStatistic : BaseMeasurableBenchmarkStatistic
    {
        private ProfilerRecorder _recorder;

        public override UnitConverter Converter
        {
            get
            {
                if (Stat != null)
                    return GetConverter(Stat);
                return base.Converter;
            }
        }

        [ValueDropdown(nameof(GetOptions))]
        public ProfilerStat Stat;

        private bool _disposed;
        private float _sampleCache;

        public override bool StartNewRun()
        {
            if (!base.StartNewRun())
                return false;
            
            _disposed = false;

            if (Stat == null || !UnityDebugProfilerStats.All.Contains(Stat))
                return false;
            
            _recorder = ProfilerRecorder.StartNew(Stat.Category, Stat.StatName, Stat.SampleCapacity);
            return true;
        }

        protected override string GetStatNameInner()
        {
            return Stat.StatName;
        }

        public override void CleanUp()
        {
            _recorder.Dispose();
            _disposed = true;
            base.CleanUp();
        }

        protected override float SampleStatistic()
        {
            if (_disposed || !_recorder.Valid)
                return _sampleCache;

            _sampleCache = (float)_recorder.LastValue;
            return _sampleCache;
        }

        protected ValueDropdownItem[] GetOptions()
        {
            return UnityDebugProfilerStats.All.Select(x => new ValueDropdownItem(x.StatName, x)).ToArray();
        }

        private UnitConverter GetConverter(ProfilerStat stat)
        {
            if (Stat == UnityDebugProfilerStats.MainThreadTime)
                return UnitConverter.ThreadMillisecondConverter;
            else if (Stat.Category == ProfilerCategory.Memory)
                return UnitConverter.MegabyteConverter;
            return UnitConverter.DefaultConverter;
        }
    }
}