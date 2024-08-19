using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class MusicManager
{
    #region Fields

    private EVENT_CALLBACK _callback;

    private GCHandle _timelineHandle;

    public EventInstance eventInstance;
    public EventReference EventReference;

    //-----------------

    private int lastBeat = 0;
    private float lastMarkerTemp = 0;
    private string lastMarkerName = "";


    private List<IEventListener> _listeners = new List<IEventListener>();

    public TimelineInfo timelineInfo = null;

    public bool IsListeningForMarkers { get; set; }

    #endregion

    public MusicManager(EventReference _eventReference)
    {
        EventReference = _eventReference;
        SetupEvent();

        timelineInfo = new TimelineInfo();
    }
    
    public MusicManager(EventReference _eventReference, Object _eventListener, bool _isListeningForMarkers = true)
    {
        timelineInfo = new TimelineInfo();
        foreach (var eventListener in _listeners)
        {
            AddListener(eventListener);
        }

        IsListeningForMarkers = _isListeningForMarkers;
        EventReference = _eventReference;
        SetupEvent();
    }

    public MusicManager(EventReference _eventReference, Object[] _eventListener, bool _isListeningForMarkers = true)
    {
        foreach (Object listener in _eventListener)
        {
            if (listener is IEventListener)
            {
                AddListener((IEventListener)listener);
            }
        }

        IsListeningForMarkers = _isListeningForMarkers;
        EventReference = _eventReference;
        SetupEvent();

        timelineInfo = new TimelineInfo();
    }

    private void Update()
    {
        if (IsInstancePlaying())
        {
            CheckOnBeat();
            CheckNewMarker();
        }
    }

    private void OnDestroy()
    {
        SetUserData(eventInstance, IntPtr.Zero);
        StopInstance(eventInstance, STOP_MODE.ALLOWFADEOUT);
        ReleaseInstance(eventInstance);
        _timelineHandle.Free();
    }

    #region Public Methods

    public void AddListener(IEventListener listener)
    {
        _listeners.Add(listener);
    }

    public void RemoveListener(IEventListener listener)
    {
        _listeners.Remove(listener);
    }

    public void RemoveAllListeners()
    {
        _listeners.Clear();
    }
    
    public void StartInstance()
    {
        eventInstance.start();
    }

    public void StopInstance(EventInstance eventInstance, STOP_MODE _stopMode)
    {
        eventInstance.stop(_stopMode);
    }

    public void SetPauseInstance(EventInstance eventInstance, bool _isPaused)
    {
        eventInstance.setPaused(_isPaused);
    }

    public void GetPauseInstance(EventInstance eventInstance, bool _isPaused)
    {
        eventInstance.getPaused(out _isPaused);
    }

    public bool IsInstancePlaying()
    {
        PLAYBACK_STATE _playbackState;
        eventInstance.getPlaybackState(out _playbackState);

        Debug.Log("Instance: " + eventInstance.getPlaybackState(out _playbackState));

        return (_playbackState == PLAYBACK_STATE.PLAYING);
    }

    public void ReleaseInstance(EventInstance eventInstance)
    {
        eventInstance.release();
    }

    public void SetUserData(EventInstance eventInstance, IntPtr _intPtr)
    {
        eventInstance.setUserData(_intPtr);
    }

    #endregion


    #region Private Methods

    private void SetupEvent()
    {
        if (Singleton<EventUpdater>.Instance == null)
        {
            var updater = new GameObject("MusicManagerSingleton");
            updater.AddComponent<Singleton<EventUpdater>>();
        }

        timelineInfo.lastMarker = new StringWrapper();
        _callback = new EVENT_CALLBACK(BeatEventCallback);

        eventInstance = RuntimeManager.CreateInstance(EventReference);


        eventInstance.setCallback(_callback,
            EVENT_CALLBACK_TYPE.TIMELINE_BEAT | EVENT_CALLBACK_TYPE.TIMELINE_MARKER);

        var tempString = EventReference.ToString().Split('/');
        timelineInfo.eventName = tempString[tempString.Length - 1];

        EventUpdater.AddUpdateListener(Update);
    }

    private void CheckNewMarker()
    {
        if (timelineInfo.lastMarkerPos != lastMarkerTemp)
        {
            lastMarkerTemp = timelineInfo.lastMarkerPos;
            string markerName = timelineInfo.lastMarker;
            lastMarkerName = markerName;

            CheckInvokeFromMarker(markerName);
        }
    }

    private void CheckInvokeFromMarker(string marker)
    {
        if (marker.Contains(" "))
        {
            var strings = marker.Split(' ');
            marker = null;

            foreach (string stringPart in strings)
            {
                marker += stringPart;
            }
        }

        foreach (MonoBehaviour listener in _listeners)
        {
            Type T = listener.GetType();
            foreach (MethodInfo m in T.GetMethods())
            {
                if (m.Name == marker)
                {
                    listener.SendMessage(marker, this);
                    break;
                }
            }
        }
    }

    private void CheckOnBeat()
    {
        if (lastBeat != timelineInfo.currentBeat)
        {
            lastBeat = timelineInfo.currentBeat;

            if (_listeners == null || _listeners.Count <= 0)
            {
                return;
            }

            foreach (IEventListener listener in _listeners)
            {
                listener.OnBeat(this);
            }
        }
    }
    
    #if UNITY_EDITOR
    private void OnGUI()
    {
        GUILayout.Box($"Current Beat= {timelineInfo.currentBeat}, {(string)timelineInfo.lastMarker}");
    }
#endif

    [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
    private FMOD.RESULT BeatEventCallback(FMOD.Studio.EVENT_CALLBACK_TYPE type, IntPtr instancePtr,
        IntPtr parameterPtr)
    {
        FMOD.Studio.EventInstance instance = new FMOD.Studio.EventInstance(instancePtr);
        IntPtr timelineInfoPtr;
        FMOD.RESULT result = instance.getUserData(out timelineInfoPtr);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError("Timeline Callback error: " + result);
        }

#if UNITY_EDITOR
        else
        {
            Debug.Log("BeatEventCallback timelineInfoPtr is IntPtr.Zero");
        }
#endif

        switch (type)
        {
            case FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_BEAT:
            {
                var parameter = (FMOD.Studio.TIMELINE_BEAT_PROPERTIES)Marshal.PtrToStructure(parameterPtr,
                    typeof(FMOD.Studio.TIMELINE_BEAT_PROPERTIES));
                timelineInfo.currentBeat = parameter.beat;
                timelineInfo.currentBar = parameter.bar;
                timelineInfo.currentTempo = parameter.tempo;
                timelineInfo.currentTimelinePosition = parameter.position;
                timelineInfo.timeSignatureUpper = parameter.timesignatureupper;
                timelineInfo.timeSignatureLower = parameter.timesignaturelower;
            }
                break;
            case FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER:
            {
                var parameter = (FMOD.Studio.TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(parameterPtr,
                    typeof(FMOD.Studio.TIMELINE_MARKER_PROPERTIES));
                timelineInfo.lastMarker = parameter.name;
                timelineInfo.lastMarkerPos = parameter.position;
            }
                break;
            case FMOD.Studio.EVENT_CALLBACK_TYPE.DESTROYED:
                if (timelineInfoPtr != IntPtr.Zero)
                {
                    GCHandle timelineHandle = GCHandle.FromIntPtr(timelineInfoPtr);
                    timelineHandle.Free();
                }

                break;
            case FMOD.Studio.EVENT_CALLBACK_TYPE.NESTED_TIMELINE_BEAT:
                //TODO: Need to do some research on this one
                break;
        }

        return FMOD.RESULT.OK;
    }

    #endregion



    ~MusicManager()
    {
        ReleaseInstance(eventInstance);
        EventUpdater.RemoveUpdateListener(Update);
        RemoveAllListeners();
    }
}