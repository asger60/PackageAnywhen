using Anywhen.Composing;
using UnityEngine;

namespace Anywhen
{
    [AddComponentMenu("Anywhen/AnywhenPlayerTrigger")]
    public class AnywhenPlayerTrigger : AnywhenTrigger
    {


        protected override void Trigger()
        {
            Debug.LogWarning("AnywhenPlayerTrigger.Trigger() not implemented");
            //AnysongPlayerBrain.TransitionTo(anywhenPlayer, triggerBehaviour, transitionMode);
        }
    }
}