using Anywhen.Composing;
using UnityEngine;

namespace Anywhen
{
    [AddComponentMenu("Anywhen/AnywhenPlayerTrigger")]
    public class AnywhenPlayerTrigger : AnywhenTrigger
    {
        [SerializeField] private AnywhenPlayer anywhenPlayer;


        [SerializeField] private AnysongPlayerBrain.TriggerBehaviour triggerBehaviour;


        [SerializeField] private AnysongPlayerBrain.TransitionMode transitionMode;

        protected override void Trigger()
        {
            if (anywhenPlayer == null)
            {
                AnywhenRuntime.Log("No player assigned in AnywhenPlayerTrigger", AnywhenRuntime.DebugMessageType.Error);
                return;
            }
            AnysongPlayerBrain.TransitionTo(anywhenPlayer, triggerBehaviour, transitionMode);
        }
    }
}