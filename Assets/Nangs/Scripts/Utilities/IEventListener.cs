using System.Runtime.InteropServices;

public interface IEventListener
{
    public void OnBeat(MusicManager currentMusicEvent);
}

[StructLayout(LayoutKind.Sequential)]
public class TimelineInfo
{
    public int currentBeat;
    public int currentBar;
    public float currentTempo;
    public string lastMarker;
    public float lastMarkerPos;
    public float currentTimelinePosition;
    public int timeSignatureUpper;
    public int timeSignatureLower;
    public string eventName;
}