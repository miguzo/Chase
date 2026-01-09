using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(TriangleAgent))]
public class AgentLoopAudio : MonoBehaviour
{
    public EventReference loopEvent;   // ton event FMOD
    public float updateInterval = 0.05f;

    private TriangleAgent agent;
    private EventInstance inst;
    private float timer;

    void Start()
    {
        agent = GetComponent<TriangleAgent>();

        inst = RuntimeManager.CreateInstance(loopEvent);
        RuntimeManager.AttachInstanceToGameObject(inst, transform);

        inst.start();
    }

    void Update()
    {
        if (agent == null || agent.manager == null || agent.agentA == null || agent.agentB == null)
            return;

        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;

        float distA = Vector3.Distance(transform.position, agent.agentA.position);
        float distB = Vector3.Distance(transform.position, agent.agentB.position);

        // 👇 LA valeur musicale
        float dist = Mathf.Min(distA, distB);

        inst.setParameterByName("Distance", dist);
    }

    void OnDestroy()
    {
        inst.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        inst.release();
    }
}
