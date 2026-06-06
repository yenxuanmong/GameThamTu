// ============================================
// ObjectPool — generic Unity GameObject pool
// Reduces GC allocations for frequently spawned UI items
// ============================================
using System.Collections.Generic;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int        _initialSize = 10;
        [SerializeField] private Transform  _container;

        private readonly Queue<GameObject> _pool = new();

        void Awake()
        {
            for (int i = 0; i < _initialSize; i++)
                _pool.Enqueue(CreateNew());
        }

        // ---- Get from pool ----

        public GameObject Get(Transform parent = null)
        {
            var obj = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
            obj.transform.SetParent(parent ?? _container ?? transform);
            obj.SetActive(true);
            return obj;
        }

        // ---- Return to pool ----

        public void Return(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(_container ?? transform);
            _pool.Enqueue(obj);
        }

        // ---- Create new instance ----

        private GameObject CreateNew()
        {
            var obj = Instantiate(_prefab, _container ?? transform);
            obj.SetActive(false);
            return obj;
        }

        // ---- Static convenience pools dictionary ----

        private static readonly Dictionary<string, ObjectPool> _pools = new();

        public static void Register(string key, ObjectPool pool) => _pools[key] = pool;

        public static ObjectPool Get(string key) =>
            _pools.TryGetValue(key, out var pool) ? pool : null;
    }
}
