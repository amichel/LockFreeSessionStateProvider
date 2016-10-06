using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Web.SessionState;

namespace LockFreeSessionStateProvider
{
    public class LockFreeSessionStateProvider : SessionStateStoreProviderBase
    {
        private static readonly ConcurrentDictionary<string, SessionStateStoreData> Store = new ConcurrentDictionary<string, SessionStateStoreData>();
        private static SessionStateItemExpireCallback _expireCallback;
        private static readonly Lazy<SessionStateSection> Config = new Lazy<SessionStateSection>(LoadConfigSection);
        private static int Timeout => (int)Config.Value.Timeout.TotalMinutes;

        private readonly Timer _expirationTimer = new Timer(RemoveExpiredSessions, null, TimeSpan.FromSeconds(60 - DateTime.Now.Second), TimeSpan.FromMinutes(1));

        private static void RemoveExpiredSessions(object state)
        {
            try
            {

                var expiredSessions = new List<string>();
                foreach (var sessionStateStoreData in Store)
                {
                    var value = (LockFreeSessionStoreData)sessionStateStoreData.Value;
                    value.DecrementTimeout();

                    if (value.Timeout == 0)
                        expiredSessions.Add(sessionStateStoreData.Key);
                }

                foreach (var session in expiredSessions)
                {
                    SessionStateStoreData value;
                    if (Store.TryRemove(session, out value))
                        try
                        {
                            _expireCallback(session, value);
                        }
                        catch { }
                }
            }
            catch { }
        }

        private static SessionStateSection LoadConfigSection()
        {
            var cfg = WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            return (SessionStateSection)cfg.GetSection("system.web/sessionState");
        }

        public override void Dispose()
        {
            _expirationTimer.Dispose();
            Store.Clear();
        }

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            Interlocked.Exchange(ref _expireCallback, expireCallback);
            return false;
        }

        public override void InitializeRequest(HttpContext context)
        {
        }

        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId,
            out SessionStateActions actions)
        {
            locked = false;
            lockAge = TimeSpan.Zero;
            lockId = null;
            actions = SessionStateActions.None;

            return Store.GetOrAdd(id, x => CreateNewStoreData(context, Timeout));
        }

        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked, out TimeSpan lockAge,
            out object lockId, out SessionStateActions actions)
        {
            return GetItem(context, id, out locked, out lockAge, out lockId, out actions);
        }

        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
        }

        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item, object lockId, bool newItem)
        {
            Store.AddOrUpdate(id, k => item, (k, v) => item);
        }

        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            SessionStateStoreData value;
            Store.TryRemove(id, out value);
        }

        public override void ResetItemTimeout(HttpContext context, string id)
        {
            SessionStateStoreData value;
            if (Store.TryGetValue(id, out value))
                value.Timeout = Timeout;
        }

        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            return new LockFreeSessionStoreData(new LockFreeSessionStateItemCollection(),
                SessionStateUtility.GetSessionStaticObjects(context), timeout);
        }

        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            bool locked;
            TimeSpan lockAge;
            object lockid;
            SessionStateActions actions;
            GetItem(context, id, out locked, out lockAge, out lockid, out actions);
        }

        public override void EndRequest(HttpContext context)
        {
        }
    }
}