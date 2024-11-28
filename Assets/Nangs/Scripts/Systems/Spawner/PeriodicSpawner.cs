using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class PeriodicSpawner : MonoBehaviour, IEventListener
{
    #region Fields
    
    public EventReference EventPath;

    [FormerlySerializedAs("cubeInteractable")]
    [FormerlySerializedAs("cube")] 
    [SerializeField] private GameObject sphereInteractable;
    [SerializeField] private GameObject cubeInteractable;
    [SerializeField] private GameObject capsuleInteractable;
    [SerializeField] private Renderer arenaRenderer = null;
    [SerializeField] private Vector3 transformBounds;
    [SerializeField] private TextMeshProUGUI beatIndicator;

    private int _tempBeat = 0;
    private int _currentBeat;
    private float timer = 0;

    private Vector3 minScale = Vector3.one * 0.5f;
    private Vector3 maxScale = Vector3.one * 2f;

    [RequireInterface(typeof(IEventListener))]
    public Object listeners;
    
    public float scaleSpeed = 0.05f;

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
        _musicManager.AddListener(this);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        CheckForButton();
    }

    private void OnDestroy()
    {
        _musicManager.ReleaseInstance(_musicManager.eventInstance);
    }

    #endregion

    #region Private Methods

    private void AnimateSphere(MusicManager musicManager)
    {
        if (sphereInteractable.GetComponent<MeshRenderer>().material.color == Color.red)
        {
            sphereInteractable.GetComponent<MeshRenderer>().material.color = Color.cyan;
        }
        else
        {
            sphereInteractable.GetComponent<MeshRenderer>().material.color = Color.red;
        }
    }

    private void AnimateCube(MusicManager musicManager)
    {
        float randomX = Random.Range(minScale.x, maxScale.x);
        float randomY = Random.Range(minScale.y, maxScale.y);
        float randomZ = Random.Range(minScale.z, maxScale.z);

        cubeInteractable.transform.localScale = new Vector3(randomX, randomY, randomZ);
    }

    private void AnimateCapsule(MusicManager musicManager)
    {
        var rotation = capsuleInteractable.transform.eulerAngles;
        int[] rotationList = new [] { 90, 70, -70, 120 };
        if (Mathf.Approximately(rotation.z, 0f))
        {
            
            rotation.z = 70f;
            capsuleInteractable.transform.rotation =
                Quaternion.Euler(rotation.x, rotation.y, rotationList[Random.Range(0, 4)]);
        }
        else
        {
            rotation.z = 0f;
            capsuleInteractable.transform.rotation = Quaternion.Euler(rotation.x, rotation.y, 0f);
        }
    }
    
    #endregion

    #region Public Methods

    public void CheckForButton()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!_musicManager.IsInstancePlaying())
            {
                _musicManager.StartInstance();
            }
            else
            {
                _musicManager.StopInstance(_musicManager.eventInstance, STOP_MODE.ALLOWFADEOUT);
            }
        }
    }

    #endregion

    #region Reusable Methods

    public void OnBeat(MusicManager currentMusicEvent)
    {
        beatIndicator.text = "Current beat: " + currentMusicEvent.timelineInfo.currentBeat.ToString();
        if (currentMusicEvent.timelineInfo.currentBeat == 2)
        {
            AnimateCube(currentMusicEvent);
            _tempBeat = _currentBeat;
        }
        
        if (currentMusicEvent.timelineInfo.currentBeat == 2 || currentMusicEvent.timelineInfo.currentBeat == 4)
        {
            AnimateSphere(currentMusicEvent);
            _tempBeat = _currentBeat;
        }
        
        if (currentMusicEvent.timelineInfo.currentBeat == 4)
        {
            AnimateCapsule(currentMusicEvent);
            _tempBeat = _currentBeat;
        }
    }

    #endregion
}