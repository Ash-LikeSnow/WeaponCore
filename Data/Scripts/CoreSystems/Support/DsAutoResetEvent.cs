using System.Threading;
using VRage;

namespace CoreSystems.Support
{
    internal class DsAutoResetEvent
    {
        private readonly FastResourceLock _lock = new FastResourceLock();
        private int _waiters;

        public void WaitOne()
        {
            _lock.AcquireExclusive();
            _waiters = 1;
            _lock.AcquireExclusive();
            _lock.ReleaseExclusive();
        }

        public void Set()
        {
            if (Interlocked.Exchange(ref _waiters, 0) > 0)
                _lock.ReleaseExclusive();
        }
    }

    internal class DsPulseEvent
    {
        private readonly object _signal = new object();
        private bool _signalSet;

        public void WaitOne()
        {
            lock (_signal)
            {
                while (!_signalSet)
                {
                    Monitor.Wait(_signal);
                }
                _signalSet = false;
            }
        }

        public void Set()
        {
            lock (_signal)
            {
                _signalSet = true;
                Monitor.Pulse(_signal);
            }
        }
    }
}
