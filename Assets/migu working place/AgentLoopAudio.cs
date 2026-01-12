using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(TriangleAgent))]
public class AgentLoopAudio : MonoBehaviour
{
    [Header("FMOD")]
    public EventReference chordEvent;      // event:/Chord/Note
    public string noteParam = "Note";      // "Note" (index) or "Semitone"
    public string modeParam = "Mode";      // optional

    [Header("Chord Degrees (semitones from root)")]
    public int[] chordDegrees = { 0, 3, 7, 10 }; // minor7 example

    [Header("Timing")]
    public float updateInterval = 0.05f;

    private TriangleAgent agent;
    private EventInstance inst;
    private bool playing;
    private float timer;

    void Start()
    {
        agent = GetComponent<TriangleAgent>();
    }

    void Update()
    {
        if (agent == null || agent.manager == null)
            return;

        TriangleManager mgr = agent.manager;

        // Only play in Spiral or Circle (adjust if you want Triangle too)
        bool active = (mgr.currentMode == TriangleManager.GlobalMode.Spiral ||
                       mgr.currentMode == TriangleManager.GlobalMode.Circle);

        if (active && !playing) StartVoice();
        if (!active && playing) StopVoice();

        if (!playing)
            return;

        timer += Time.deltaTime;
        if (timer < updateInterval)
            return;
        timer = 0f;

        // Your manager uses ONE shared center
        Vector3 center = mgr.center;

        Vector3 v = transform.position - center;
        v.y = 0f;

        if (v.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(v.z, v.x);               // -PI..PI
        float t = (angle + Mathf.PI) / (2f * Mathf.PI);    // 0..1

        int idx = Mathf.Clamp(Mathf.FloorToInt(t * chordDegrees.Length),
                              0, chordDegrees.Length - 1);

        // If FMOD param is an index 0..(len-1):
        inst.setParameterByName(noteParam, idx);

        // If FMOD param expects semitones, use this instead:
        // inst.setParameterByName(noteParam, chordDegrees[idx]);

        // Optional mode param: 0=Triangle, 1=Spiral, 2=Circle
        if (!string.IsNullOrEmpty(modeParam))
        {
            float mode = (float)mgr.currentMode;
            inst.setParameterByName(modeParam, mode);
        }
    }

    void StartVoice()
    {
        inst = RuntimeManager.CreateInstance(chordEvent);
        RuntimeManager.AttachInstanceToGameObject(inst, transform);
        inst.start();
        playing = true;
    }

    void StopVoice()
    {
        if (inst.isValid())
        {
       inst.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            inst.release();
        }
        playing = false;
    }

    void OnDestroy()
    {
        if (playing)
            StopVoice();
    }
}
