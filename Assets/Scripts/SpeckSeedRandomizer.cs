using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class SpeckSeedRandomizer : MonoBehaviour
{
    [Tooltip("Random range for _SpeckSeed")]
    public float seedMin = 0f;
    public float seedMax = 10000f;

    Renderer rend;
    MaterialPropertyBlock mpb;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        RandomizeSeed();
    }

    [ContextMenu("Randomize Speck Seed")]
    public void RandomizeSeed()
    {
        float seed = Random.Range(seedMin, seedMax);
        rend.GetPropertyBlock(mpb);
        mpb.SetFloat("_SpeckSeed", seed);
        rend.SetPropertyBlock(mpb);
    }
}