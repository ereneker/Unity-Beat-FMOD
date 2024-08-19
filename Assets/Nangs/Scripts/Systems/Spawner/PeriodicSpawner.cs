using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class PeriodicSpawner : MonoBehaviour, IEventListener
{
    #region Fields
    
    public EventReference EventPath;

    [SerializeField] private GameObject objectToSpawn = null;
    [SerializeField] private Renderer arenaRenderer = null;
    [SerializeField] private Vector3 transformBounds;

    private int _tempBeat = 0;
    private int _currentBeat;

    [RequireInterface(typeof(IEventListener))]
    public Object listeners;

    private MusicManager _musicManager;
    #endregion

    #region Monobehaviours

    private void Awake()
    {
        transformBounds = arenaRenderer.bounds.extents * 0.9f;
    }

    private void Start()
    {
        _musicManager = new MusicManager(EventPath, listeners);
    }

    private void OnDestroy()
    {
        _musicManager.ReleaseInstance(_musicManager.eventInstance);
    }

    #endregion

    #region Private Methods

    private void SpawnObject()
    {
        float xRand = Random.Range(-transformBounds.x, transformBounds.x);
        float zRand = Random.Range(-transformBounds.z, transformBounds.z);
        var obj = Instantiate(objectToSpawn, new Vector3(xRand, 0, zRand), Quaternion.identity);
    }
    

    #endregion

    #region Public Methods

    public void CheckForButton()
    {
        if (!_musicManager.IsInstancePlaying())
        {
            _musicManager.StartInstance();
            _musicManager.AddListener(this);
        }
        else
        {
            _musicManager.StopInstance(_musicManager.eventInstance, STOP_MODE.ALLOWFADEOUT);
        }
    }

    #endregion

    #region Reusable Methods

    public void OnBeat(MusicManager currentMusicEvent)
    {
        if (currentMusicEvent.timelineInfo.currentBeat == 2 || currentMusicEvent.timelineInfo.currentBeat == 4)
        {
            SpawnObject();
            _tempBeat = _currentBeat;
        }
    }

    #endregion
}