using System;
using UnityEngine;
using UnityEngine.Events;

public class GirlAnimationsEvents : MonoBehaviour
{
    public event Action OnThrow;
    public UnityEvent OnThrowEvent;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Throw()
    {
        OnThrow?.Invoke();
        OnThrowEvent?.Invoke();
        print("Throw");
    }
}
