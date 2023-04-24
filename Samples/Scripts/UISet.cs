using System.Collections;
using Samples.Scripts;
using UnityEngine;

public class UISet : MonoBehaviour
{
    public FillSelector fillSelector;
    public MixView mixView;


    public void Init(IMixableObject mixableObject)
    {
        fillSelector.Init(mixableObject);
    }

    private void Start()
    {
        gameObject.SetActive(true);
    }

    public void SetIsActive(bool state)
    {
        mixView.SetIsActive(state);
        fillSelector.SetIsActive(state);
        if (state)
        {
            StopAllCoroutines();
            StartCoroutine(WaitAndSetFill());
        }
        else
        {
            fillSelector.SetFillIndex(-1);
        }
    }

    IEnumerator WaitAndSetFill()
    {
        yield return new WaitForSeconds(0.5f);
        fillSelector.SetFillIndex(0);
    }
}
