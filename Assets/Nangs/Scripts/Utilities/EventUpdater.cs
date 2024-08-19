using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventUpdater : Singleton<EventUpdater>
{
    public delegate void UpdateEvent();
    public static event UpdateEvent OnEventUpdate;

    private void Update()
    {
        OnEventUpdate?.Invoke();
    }
    
    public static void AddUpdateListener(UpdateEvent updateEvent)
    {
        OnEventUpdate += updateEvent;
    }

    public static void RemoveUpdateListener(UpdateEvent updateEvent)
    {
        OnEventUpdate -= updateEvent;
    }
}
