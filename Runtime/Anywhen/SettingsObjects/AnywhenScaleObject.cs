using Unity.Collections;
using UnityEngine;

namespace Anywhen.SettingsObjects
{
    [CreateAssetMenu(fileName = "New scale object", menuName = "Anywhen/Conductor/ScalesObject")]
    public class AnywhenScaleObject : ScriptableObject
    {
        [NonReorderable] public int[] notes;

        public struct Unmanaged
        {
            public NativeArray<int> notes;

            public bool IsNull()
            {
                return !notes.IsCreated || notes.Length == 0;
            }
        }

        public Unmanaged ToUnmanaged()
        {
            return new Unmanaged
            {
                notes = new NativeArray<int>(notes, Allocator.Persistent)
            };
        }
    }
}