using System;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;

public class AnywhenInstrument : AnywhenSettingsBase
{
    [SerializeField]  AnysongTrack.AnyTrackTypes instrumentType;

    public AnysongTrack.AnyTrackTypes InstrumentType => instrumentType;

}
