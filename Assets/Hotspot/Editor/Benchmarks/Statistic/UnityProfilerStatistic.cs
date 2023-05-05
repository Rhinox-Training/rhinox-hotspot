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
        public static ProfilerStat UsedSystemMemory = new ProfilerStat(ProfilerCategory.Memory, "System Used Memory");
        public static ProfilerStat GCReservedMemory = new ProfilerStat(ProfilerCategory.Memory, "GC Reserved Memory");
        public static ProfilerStat BatchesCount = new ProfilerStat(ProfilerCategory.Render, "Batches Count");
        
        public static ProfilerStat[] All
        {
            get
            {
                return new[]
                {
                    UsedSystemMemory,
                    GCReservedMemory,
                    BatchesCount
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

        private FieldInfo _categoryField;

        public ProfilerStat(ProfilerCategory category, string name)
        {
            Category = category;
            StatName = name;
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
            return StatName == other.StatName && _serializedCategory == other._serializedCategory;
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
            return HashCode.Combine(StatName, _serializedCategory);
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

        [ValueDropdown(nameof(GetOptions))]
        public ProfilerStat Stat;

        private bool _disposed;
        private float _sampleCache;

        public override void StartNewRun()
        {
            base.StartNewRun();
            _disposed = false;
            _recorder = ProfilerRecorder.StartNew(Stat.Category, Stat.StatName);
        }

        protected override string GetStatName()
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
            if (_disposed)
                return _sampleCache;

            _sampleCache = (float)_recorder.LastValue;
            return _sampleCache;
        }

        protected ValueDropdownItem[] GetOptions()
        {
            return UnityDebugProfilerStats.All.Select(x => new ValueDropdownItem(x.StatName, x)).ToArray();
        }
    }
}