using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionManager : MonoBehavior
{
    [Header("Prefabs to spawn")]
    public List<GameObject> _prefabs = new List<GameObject>();

    [Header("Slide timing / positions")]
    [Tooltip("Seconds to move between offscreen and center")]
    public float _slideDuration = 0.8f;

    [Tooltip("Viewport X where new items spawn (off-screen left)")]
    public float _leftViewportX = -0.2f;

    [Tooltip("Viewport X used as center (usually 0.5)")]
    public float _centerViewportX = 0.5f;

    [Tooltip("Viewport Y used for spawn & center (usually 0.5)")]
    public float _centerViewportY = 0.5f;

    [Tooltip("Viewport X where outgoing items end (off-screen right)")]
    public float _rightViewportX = 1.2f;

    [Tooltip("World Z coordinate for spawned objects (common plane)")]
    public float _spawnZ = 0f;

    [Tooltip("Optional parent for spawned objects")]
    public Transform _parent;

    // currently displayed center object (may be mid-animation)
    GameObject _currentCenter;

    // Start is called once before the first execution of Update after the MonoBehavior is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            SlideRandom();
        }
    }

    public void SlideRandom()
    {
        if (_prefabs == null || _prefabs.Count == 0)
        {
            Debug.LogWarning("SlideManager: no prefabs assigned.");
            return;
        }

        var prefab = _prefabs[Random.Range(0, _prefabs.Count)];
        SlidePrefab(prefab);
    }

    public void SlideByName(string prefabName)
    {
        if (_prefabs == null || _prefabs.Count == 0)
        {
            Debug.LogWarning("SlideManager: no prefabs assigned.");
            return;
        }

        var prefab = _prefabs.Find(p => p != null && p.name == prefabName);
        if (prefab == null)
        {
            Debug.LogWarning($"SlideManager: prefab named '{prefabName}' not found in list.");
            return;
        }

        SlidePrefab(prefab);
    }

    // Internal: instantiate and start animation
    void SlidePrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("SlideManager: null prefab passed to SlidePrefab.");
            return;
        }

        // compute positions in world space
        var leftPos = ViewportToWorld(_leftViewportX, _centerViewportY, _spawnZ);
        var centerPos = ViewportToWorld(_centerViewportX, _centerViewportY, _spawnZ);
        var rightPos = ViewportToWorld(_rightViewportX, _centerViewportY, _spawnZ);

        // capture the object that is considered center at the moment of the call
        var outgoing = _currentCenter;

        // instantiate new object at left
        var instance = Instantiate(prefab, leftPos, Quaternion.identity, _parent);
        // keep local z of prefab if found; otherwise we force spawnZ
        var instPos = instance.transform.position;
        instPos.z = centerPos.z;
        instance.transform.position = instPos;

        // mark the newly created instance as the current center immediately so
        // subsequent calls treat it as the center (prevents "skipping" when spamming)
        _currentCenter = instance;

        // start coroutine to animate outgoing (if any) and incoming simultaneously
        StartCoroutine(SlideRoutine(instance, outgoing, leftPos, centerPos, rightPos));
    }

    IEnumerator SlideRoutine(GameObject incoming, GameObject outgoing, Vector3 startIncoming, Vector3 centerPos, Vector3 endOutgoing)
    {
        float elapsed = 0f;

        // If there is no outgoing, just move incoming from left to center
        // If there is outgoing, move outgoing from wherever it is (may be mid-animation) to right
        Vector3 outgoingStart = outgoing != null ? outgoing.transform.position : Vector3.zero;

        // Set incoming initial position in case something moved it
        incoming.transform.position = startIncoming;

        // animate
        while (elapsed < _slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _slideDuration);
            // smooth step for nicer motion
            float s = Mathf.SmoothStep(0f, 1f, t);

            // incoming: left -> center
            if (incoming != null)
                incoming.transform.position = Vector3.Lerp(startIncoming, centerPos, s);

            // outgoing: wherever it was -> right
            if (outgoing != null)
                outgoing.transform.position = Vector3.Lerp(outgoingStart, endOutgoing, s);

            yield return null;
        }

        // finalize positions
        if (incoming != null) incoming.transform.position = centerPos;
        if (outgoing != null) outgoing.transform.position = endOutgoing;

        // destroy the outgoing object to keep scene clean
        if (outgoing != null)
            Destroy(outgoing);

        // DO NOT overwrite currentCenter here — it was already set when the incoming was created.
        // This prevents older/slower coroutines from reverting currentCenter when multiple slides overlap.
    }

    // helper: convert viewport coordinates + desired world Z plane to a world point
    Vector3 ViewportToWorld(float viewportX, float viewportY, float worldZ)
    {
        if (Camera.main == null)
        {
            Debug.LogError("SlideManager: Camera.main is null. Cannot compute world positions.");
            return new Vector3(viewportX, viewportY, worldZ);
        }

        // distance from camera to the desired world z plane
        float distance = worldZ - Camera.main.transform.position.z;
        Vector3 vp = new Vector3(viewportX, viewportY, distance);
        return Camera.main.ViewportToWorldPoint(vp);
    }
}
