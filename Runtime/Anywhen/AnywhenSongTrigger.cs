using System.Threading.Tasks;
using Anywhen;
using UnityEngine;

[RequireComponent(typeof(AnysongPlayer))]
public class AnywhenSongTrigger : MonoBehaviour
{
    private AnysongPlayer _anysongPlayer;
    private MaterialPropertyBlock _materialPropertyBlock;
    private Renderer _renderer;
    private Color _initialColor;

    public enum TriggerTypes
    {
        None,
        ObjectStart,
        ObjectDestroy,
        TriggerEnter,
        TriggerExit,
        TriggerEnter2D,
        TriggerExit2D,
        CollisionEnter,
        CollisionExit,
        CollisionEnter2D,
        CollisionExit2D,
        ObjectEnable,
        ObjectDisable,
        MouseEnter,
        MouseExit,
        MouseDown,
        MouseUp,
    }

    public TriggerTypes triggerType;

    void Start()
    {
        TryGetComponent(out _anysongPlayer);
        TryGetComponent(out _renderer);
        _materialPropertyBlock = new MaterialPropertyBlock();
        _initialColor = _renderer.sharedMaterial.color;
        if (triggerType == TriggerTypes.ObjectStart) Trigger();
    }

    private void OnEnable()
    {
        if (triggerType == TriggerTypes.ObjectEnable) Trigger();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerType == TriggerTypes.TriggerEnter) Trigger();
    }

    private void OnTriggerExit(Collider other)
    {
        if (triggerType == TriggerTypes.TriggerExit) Trigger();
    }

    void Trigger()
    {
        if (!_anysongPlayer) return;
        _anysongPlayer.Play();
        Blink();
    }

    async void Blink()
    {
        float f = 0;
        float duration = 1;
        while (f < duration)
        {
            _materialPropertyBlock.SetColor("_Color", Color.Lerp(Color.white, _initialColor, f));
            _renderer.SetPropertyBlock(_materialPropertyBlock);
            f += Time.deltaTime;
            await Task.Yield();
        }
    }
}