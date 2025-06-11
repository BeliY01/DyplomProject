using UnityEngine;
using UnityEngine.Events;

//placed on Graphics and Audio Panel with the SaveSettings function assigned to the event.
public class OnGameobjectDisabled : MonoBehaviour
{
    public UnityEvent OnDisabledEvent;

    private void OnDisable()
    {
        OnDisabledEvent?.Invoke();
    }
}
