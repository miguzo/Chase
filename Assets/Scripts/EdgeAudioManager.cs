using UnityEngine;
using System.Collections.Generic;
using FMODUnity;
using FMOD.Studio;

public class TriangleEdgeAudio : MonoBehaviour
{
    [Header("FMOD Events")]
    public EventReference edgeHumEvent;     // event:/Edge/Hum
    public EventReference edgeSparkEvent;   // event:/Edge/Spark

    [Header("Update Rate")]
    public float updateInterval = 0.05f; // 20 Hz

    [Header("Spark")]
    public float sparkTensionThreshold = 0.9f;
    public float sparkCooldown = 0.25f;

    TriangleManager mgr;
    float timer;

    class Edge
    {
        public TriangleAgent owner;
        public Transform target;
        public Transform emitter;
        public EventInstance hum;
        public float seed;
        public float sparkCd;
        public float prevTension;
    }

    readonly List<Edge> edges = new();

    void Awake()
    {
        mgr = GetComponent<TriangleManager>();
    }

    void Start()
    {
        if (mgr == null || mgr.agents == null) return;

        foreach (var agent in mgr.agents)
        {
            if (!agent || !agent.agentA || !agent.agentB) continue;

            edges.Add(CreateEdge(agent, agent.agentA, "Edge_A"));
            edges.Add(CreateEdge(agent, agent.agentB, "Edge_B"));
        }
    }

    Edge CreateEdge(TriangleAgent owner, Transform target, string name)
    {
        var go = new GameObject($"{owner.name}_{name}");
        go.transform.parent = transform;

        var e = new Edge
        {
            owner = owner,
            target = target,
            emitter = go.transform,
            seed = Random.value * 1000f,
            sparkCd = 0f,
            prevTension = 0f
        };

        e.hum = RuntimeManager.CreateInstance(edgeHumEvent);
        RuntimeManager.AttachInstanceToGameObject(e.hum, e.emitter);
        e.hum.start();

        return e;
    }

    void Update()
    {
        if (mgr == null) return;

        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        float dt = timer;
        timer = 0f;

        float spiral = mgr.spiralActive ? 1f : 0f;

        foreach (var e in edges)
        {
            if (!e.owner || !e.target) continue;

            // Position du son au milieu du lien (comme une corde)
            Vector3 a = e.owner.transform.position;
            Vector3 b = e.target.position;
            e.emitter.position = (a + b) * 0.5f;

            float dist = Vector3.Distance(a, b);

            float tension = 1f - Mathf.InverseLerp(mgr.minDistance, mgr.maxDistance, dist);
            tension = Mathf.Clamp01(tension);

            // vitesse relative (approx) : owner.velocity n'est pas public, donc on approx par delta position
            // -> meilleure version: expose une propriété Velocity dans TriangleAgent (voir note plus bas)
            float relSpeed01 = 0.5f; // fallback neutre

            float wobble = Mathf.PerlinNoise(Time.time * 0.25f, e.seed); // lent

            e.hum.setParameterByName("Tension", tension);
            e.hum.setParameterByName("RelSpeed", relSpeed01);
            e.hum.setParameterByName("Wobble", wobble);
            e.hum.setParameterByName("Spiral", spiral);

            // Spark sur franchissement de seuil (hystérésis simple)
            e.sparkCd -= dt;
            bool crossedUp = (e.prevTension < sparkTensionThreshold && tension >= sparkTensionThreshold);

            if (crossedUp && e.sparkCd <= 0f)
            {
                RuntimeManager.PlayOneShot(edgeSparkEvent, e.emitter.position);
                e.sparkCd = sparkCooldown;
            }

            e.prevTension = tension;
        }
    }

    void OnDestroy()
    {
        foreach (var e in edges)
        {
         e.hum.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            e.hum.release();
        }
        edges.Clear();
    }
}
