using System.Threading;
using System.Web;
using System.Web.SessionState;

namespace LockFreeSessionStateProvider
{
    public class LockFreeSessionStoreData : SessionStateStoreData
    {
        public LockFreeSessionStoreData(ISessionStateItemCollection sessionItems, HttpStaticObjectsCollection staticObjects, int timeout) : base(sessionItems, staticObjects, timeout)
        {
        }

        private int _timeout;
        public override int Timeout
        {
            get { return _timeout; }
            set { Interlocked.Exchange(ref _timeout, value); }
        }

        public void DecrementTimeout()
        {
            Interlocked.Decrement(ref _timeout);
        }
    }
}