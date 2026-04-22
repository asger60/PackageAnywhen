using System;
using Anywhen.Composing;
using UnityEngine;

namespace Anywhen.SettingsObjects
{
    public class AnywhenInstrument : AnywhenSettingsBase
    {
        [SerializeField]  AnysongTrackSettings.AnyTrackTypes instrumentType;
        public AnysongTrackSettings.AnyTrackTypes InstrumentType => instrumentType;

        public struct Unmanaged : IEquatable<Unmanaged>
        {
            public AnysongTrackSettings.AnyTrackTypes instrumentType;

            public bool Equals(Unmanaged other)
            {
                return instrumentType == other.instrumentType;
            }

            public override bool Equals(object obj)
            {
                return obj is Unmanaged other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (int)instrumentType;
            }

            public static bool operator ==(Unmanaged left, Unmanaged right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Unmanaged left, Unmanaged right)
            {
                return !left.Equals(right);
            }
        }

        public Unmanaged ToUnmanaged()
        {
            return new Unmanaged
            {
                instrumentType = instrumentType
            };
        }
    }
}
