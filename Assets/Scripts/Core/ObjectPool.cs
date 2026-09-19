using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShikiShiro
{
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly Stack<T> _inactive = new Stack<T>();
        private readonly List<T> _all = new List<T>();
        private readonly Func<T> _factory;
        private readonly Transform _parent;

        public ObjectPool(Func<T> factory, Transform parent, int prewarm)
        {
            _factory = factory;
            _parent = parent;
            for (int i = 0; i < prewarm; i++)
            {
                T item = Create();
                item.gameObject.SetActive(false);
                _inactive.Push(item);
            }
        }

        public T Get()
        {
            T item = _inactive.Count > 0 ? _inactive.Pop() : Create();
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            item.gameObject.SetActive(false);
            item.transform.SetParent(_parent, false);
            _inactive.Push(item);
        }

        public IReadOnlyList<T> All => _all;

        private T Create()
        {
            T item = _factory();
            item.transform.SetParent(_parent, false);
            _all.Add(item);
            return item;
        }
    }
}
