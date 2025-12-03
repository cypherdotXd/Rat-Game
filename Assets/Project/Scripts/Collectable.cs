using System;
using UnityEngine;

public class Collectable : MonoBehaviour
{
    public static event Action<Collectable> OnCollected;

    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        OnCollected?.Invoke(this);
        _collider.enabled = false;
        gameObject.SetActive(false);
    }
}
