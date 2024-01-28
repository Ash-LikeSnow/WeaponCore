using System;
using System.Collections.Generic;

namespace CoreSystems.Support
{
    internal class FutureEvents
    {
        internal struct FutureAction
        {
            internal Action<object> Callback;
            internal object Arg1;

            internal FutureAction(Action<object> callBack, object arg1)
            {
                Callback = callBack;
                Arg1 = arg1;
            }

            internal void Purge()
            {
                Callback = null;
                Arg1 = null;
            }
        }

        internal FutureEvents()
        {
            for (int i = 0; i <= _maxDelay; i++) _callbacks[i] = new List<FutureAction>();
        }

        private volatile bool Active = true;
        private const int _maxDelay = 14400;
        private List<FutureAction>[] _callbacks = new List<FutureAction>[_maxDelay + 1]; // and fill with list instances
        private uint _offset;
        private uint _lastTick;
        internal void Schedule(Action<object> callback, object arg1, uint delay)
        {
            lock (_callbacks)
            {
                delay = delay <= 0 ? 1 : delay;
                _callbacks[(_offset + delay) % _maxDelay].Add(new FutureAction(callback, arg1));
            }
        }

        internal void Schedule(Action<object> callback, object arg1, uint delay, out uint tickIndex, out int listIndex)
        {
            lock (_callbacks)
            {
                delay = delay <= 0 ? 1 : delay;
                tickIndex = (_offset + delay) % _maxDelay;
                var list = _callbacks[tickIndex];
                listIndex = list.Count;
                list.Add(new FutureAction(callback, arg1));
            }
        }

        internal void DeSchedule(uint tickIndex, int listIndex)
        {
            lock (_callbacks)
            {
                var list = _callbacks[tickIndex];
                list.RemoveAt(listIndex);
            }
        }

        internal void Tick(uint tick, bool purge = false)
        {
            if (_callbacks.Length > 0 && Active)
            {
                lock (_callbacks)
                {
                    if (_lastTick == tick - 1 || purge)
                    {
                        var index = tick % _maxDelay;
                        for (int i = 0; i < _callbacks[index].Count; i++) 
                            _callbacks[index][i].Callback(_callbacks[index][i].Arg1);

                        _callbacks[index].Clear();
                        _offset = tick;
                    }
                    else 
                    {
                        var replayLen = tick - _lastTick;
                        var idx = replayLen;
                        for (int i = 0; i < tick - _lastTick; i++)
                        {
                            var pastIdx = (tick - --idx) % _maxDelay;
                            for (int j = 0; j < _callbacks[pastIdx].Count; j++) 
                                _callbacks[pastIdx][j].Callback(_callbacks[pastIdx][j].Arg1);

                            _callbacks[pastIdx].Clear();
                            _offset = tick;
                        }
                    }

                    _lastTick = tick;
                }
            }
        }

        internal void Purge(int tick)
        {
            try
            {
                for (int i = tick; i < tick + _maxDelay; i++)
                    Tick((uint)i, true);

                lock (_callbacks)
                {
                    Active = false;
                    foreach (var list in _callbacks)
                    {
                        foreach (var call in list)
                            call.Purge();
                        list.Clear();
                    }

                    _callbacks = null;
                }
            }
            catch (Exception e)
            {
                Log.Line($"Exception in FutureEvent purge, Callback likely null {e}", null, true);
            }
        }
    }
}
