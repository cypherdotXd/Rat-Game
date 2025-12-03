using System;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    public Action<GameObject> OnInteract;
    
    private void OnTriggerEnter(Collider other)
    {
        OnInteract?.Invoke(other.gameObject);
    }
}
