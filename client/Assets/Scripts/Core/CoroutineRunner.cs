// ============================================
// CoroutineRunner — run coroutines from non-MonoBehaviour classes
// ============================================
using System.Collections;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;

        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("CoroutineRunner");
                    _instance = go.AddComponent<CoroutineRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static Coroutine Run(IEnumerator routine) =>
            Instance.StartCoroutine(routine);

        public static void Stop(Coroutine routine)
        {
            if (routine != null) Instance.StopCoroutine(routine);
        }
    }
}
