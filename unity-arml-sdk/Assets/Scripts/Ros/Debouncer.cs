using System.Collections;
using UnityEngine;

public class Debouncer : SingletonBehavior<Debouncer>
{
    private Coroutine debounceCoroutine;

    public void Debounce(float delay, System.Action action)
    {
        if (debounceCoroutine != null)
        {
            StopCoroutine(debounceCoroutine);
        }

        debounceCoroutine = StartCoroutine(InvokeDebounced(delay, action));
    }

    private IEnumerator InvokeDebounced(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);

        action.Invoke();
        debounceCoroutine = null;
    }
}