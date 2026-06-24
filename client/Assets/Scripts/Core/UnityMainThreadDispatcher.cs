// ============================================
// UnityMainThreadDispatcher
// — queues callbacks from background threads onto Unity main thread
// ============================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        public static UnityMainThreadDispatcher Instance { get; private set; }

        private readonly Queue<Action> _queue = new Queue<Action>();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Enqueue(Action action)
        {
            lock (_queue) { _queue.Enqueue(action); }
        }

        void Update()
        {
            lock (_queue)
            {
                while (_queue.Count > 0)
                {
                    try { _queue.Dequeue()?.Invoke(); }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Dispatcher] {ex.Message}");
                    }
                }
            }
        }
    }
}
