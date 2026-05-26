using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [SerializeField] private ParticleSystem gwishinFXPrefab;
    [SerializeField] private ParticleSystem dokkaebiFXPrefab;
    [SerializeField] private ParticleSystem mulgwishinFXPrefab;

    private void Awake() => Instance = this;

    public void PlayGwishin(Vector3 pos)   => PlayAt(gwishinFXPrefab,   pos);
    public void PlayDokkaebi(Vector3 pos)  => PlayAt(dokkaebiFXPrefab,  pos);
    public void PlayMulgwishin(Vector3 pos) => PlayAt(mulgwishinFXPrefab, pos);

    private static void PlayAt(ParticleSystem prefab, Vector3 pos)
    {
        if (prefab == null) return;
        var ps = Instantiate(prefab, pos, Quaternion.identity);
        ps.Play();
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }
}
