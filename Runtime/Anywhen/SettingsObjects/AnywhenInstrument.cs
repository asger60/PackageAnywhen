using System;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;

public class AnywhenInstrument : AnywhenSettingsBase
{
    [SerializeField]  AnysongTrackSettings.AnyTrackTypes instrumentType;

    public AnysongTrackSettings.AnyTrackTypes InstrumentType => instrumentType;

}
