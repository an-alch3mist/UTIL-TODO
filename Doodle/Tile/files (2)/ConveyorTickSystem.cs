using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
/// <summary>
/// Centralised tick system for all conveyors (and anything else that wants a
/// fixed game-logic tick separate from Unity's Update).
///
/// Avoids having each ConveyorBelt run its own Coroutine / InvokeRepeating.
/// One MonoBehaviour, one Coroutine, deterministic order.
/// </summary>
public class ConveyorTickSystem : MonoBehaviour
{
    public static ConveyorTickSystem I { get; private set; }

    [Header("Timing")]
    public float tickInterval = 0.5f;

    private static readonly List<ConveyorBelt> _conveyors = new List<ConveyorBelt>();

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    private void Start() => StartCoroutine(TickLoop());

    private IEnumerator TickLoop()
    {
        var wait = new WaitForSeconds(tickInterval);
        while (true)
        {
            // Tick upstream-to-downstream: iterate in insertion order.
            // For a proper factory game you'd want topological sort here.
            for (int i = 0; i < _conveyors.Count; i++)
                _conveyors[i].Tick();

            yield return wait;
        }
    }

    public static void Register(ConveyorBelt c)
    {
        if (!_conveyors.Contains(c)) _conveyors.Add(c);
    }

    public static void Unregister(ConveyorBelt c) => _conveyors.Remove(c);
}
*/