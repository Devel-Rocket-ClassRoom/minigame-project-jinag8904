using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public struct PoolEntry
    {
        public GameObject prefab;   // 원본 프리팹
        public int prewarmCount;    // 미리 생성할 개수
    }

    [SerializeField] private PoolEntry[] _entries;

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (var entry in _entries)
            Prewarm(entry.prefab, entry.prewarmCount);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        if (!_pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            _pools[prefab] = queue;
        }

        for (int i = 0; i < count; i++)
        {
            var obj = Instantiate(prefab, transform);
            obj.AddComponent<PooledObject>().SourcePrefab = prefab;
            obj.SetActive(false);
            queue.Enqueue(obj);
        }
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!_pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            _pools[prefab] = queue;
        }

        GameObject obj;
        if (queue.Count > 0)    // 있으면 꺼냄
        {
            obj = queue.Dequeue();
            obj.transform.SetPositionAndRotation(position, rotation);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
            obj.AddComponent<PooledObject>().SourcePrefab = prefab;
        }

        obj.transform.SetParent(transform);
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null) return;

        var marker = obj.GetComponent<PooledObject>();
        if (marker == null || marker.SourcePrefab == null)
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform);

        _pools[marker.SourcePrefab].Enqueue(obj);
    }
}

public class PooledObject : MonoBehaviour
{
    public GameObject SourcePrefab;
}