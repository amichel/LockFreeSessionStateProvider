using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Web.SessionState;
using System.Linq;

namespace LockFreeSessionStateProvider
{
    public class LockFreeSessionStateItemCollection : ISessionStateItemCollection
    {
        private readonly ConcurrentDictionary<string, object> _items = new ConcurrentDictionary<string, object>();

        public IEnumerator GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int Count { get; }
        public object SyncRoot => new object();
        public bool IsSynchronized => true;
        public void Remove(string name)
        {
            object value;
            _items.TryRemove(name, out value);
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException("Session access by index is not supported");
        }

        public void Clear()
        {
            _items.Clear();
        }

        object ISessionStateItemCollection.this[string name]
        {
            get { return _items.GetOrAdd(name, x => null); }
            set { _items.AddOrUpdate(name, x => value, (x, v) => value); }
        }

        object ISessionStateItemCollection.this[int index]
        {
            get { throw new NotImplementedException("Session access by index is not supported"); }
            set { throw new NotImplementedException("Session access by index is not supported"); }
        }

        public NameObjectCollectionBase.KeysCollection Keys
        {
            get
            {
                var coll = new NameValueCollection();
                foreach (var item in _items)
                    coll.Add(item.Key, string.Empty);
                return coll.Keys;
            }
        }
        public bool Dirty { get; set; }
    }
}