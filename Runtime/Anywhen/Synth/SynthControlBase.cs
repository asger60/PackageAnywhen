using UnityEngine;

namespace Anywhen.Synth
{
    public class SynthControlBase
    {
    

        
        public virtual void DoUpdate()
        {
      
        }
        
        public virtual void NoteOn()
        {
            
        }
        
        public virtual void NoteOff()
        {
            
        }

        public virtual float Process(bool unipolar = false)
        {
            return 1;
        }
    }
}
