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
            AnysongPlayerBrain.TransitionTo(anywhenPlayer, triggerBehaviour, transitionMode);

           
        }
    }
}