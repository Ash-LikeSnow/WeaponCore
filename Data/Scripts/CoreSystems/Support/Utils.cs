using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;
using VRageMath;

namespace CoreSystems.Support
{
    internal static class ConcurrentQueueExtensions
    {
        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            T item;
            while (queue.TryDequeue(out item)) { }
        }
    }

    public class JerkRunningAverage
    {
        public JerkRunningAverage(int bufferSize)
        {
            _bufferSize = bufferSize;
        }

        internal Vector3D PreviousAcceleration;
        private readonly int _bufferSize;
        private readonly Queue<Vector3D> _jerkQueue = new Queue<Vector3D>();
        private Vector3D _runningSum;

        public void AddToRunningAverage(Vector3D value)
        {
            if (_jerkQueue.Count == _bufferSize)
            {
                _runningSum -= _jerkQueue.Dequeue();
            }
            _jerkQueue.Enqueue(value);
            _runningSum += value;
        }

        public Vector3D GetAverage()
        {
            return _runningSum / _jerkQueue.Count;
        }

        public void Clean()
        {
            _jerkQueue.Clear();
            _runningSum = Vector3D.Zero;
            PreviousAcceleration = Vector3D.Zero;
        }
    }

    public struct XorShiftRandomStruct
    {

        // Constants
        private const double DoubleUnit = 1.0 / (int.MaxValue + 1.0);

        // State Fields
        private ulong _x;
        private ulong _y;

        // Buffer for optimized bit generation.
        private ulong _buffer;
        private ulong _bufferMask;

        /// <summary>
        ///   Constructs a new  generator
        ///   with the supplied seed.
        /// </summary>
        /// <param name="seed">
        ///   The seed value.
        /// </param>
        public XorShiftRandomStruct(ulong seed)
        {
            _x = seed << 3; _y = seed >> 3;
            _buffer = 0;
            _bufferMask = 0;

            var temp1 = _y; _x ^= _x << 23; var temp2 = _x ^ _y ^ (_x >> 17) ^ (_y >> 26); _x = temp1; _y = temp2;
            var tempX = _y; _x ^= _x << 23; var tempY = _x ^ _y ^ (_x >> 17) ^ (_y >> 26); var newSeed = tempY + _y; _x = tempX; _y = tempY;
            _x = newSeed << 3; _y = newSeed >> 3;
        }

        /// <summary>
        ///   Reinits existing Random class
        ///   with the supplied seed.
        /// </summary>
        /// <param name="seed">
        ///   The seed value.
        /// </param>
        public void Reinit(ulong seed)
        {
            _x = seed << 3; _y = seed >> 3;
            _buffer = 0;
            _bufferMask = 0;

            // 
            // random isn't very random unless we do the below.... likely because hashes produce incrementing numbers for Int3 conversions.
            //

            var temp1 = _y; _x ^= _x << 23; var temp2 = _x ^ _y ^ (_x >> 17) ^ (_y >> 26); _x = temp1; _y = temp2;

            var tempX = _y; _x ^= _x << 23; var tempY = _x ^ _y ^ (_x >> 17) ^ (_y >> 26); var newSeed = tempY + _y; _x = tempX; _y = tempY;

            _x = newSeed << 3; _y = newSeed >> 3;
        }

        public MyTuple<ulong, ulong> GetSeedVaues()
        {
            return new MyTuple<ulong, ulong>(_x, _y);
        }

        public void SyncSeed(ulong x, ulong y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        ///   Generates a pseudorandom boolean.
        /// </summary>
        /// <returns>
        ///   A pseudorandom boolean.
        /// </returns>
        public bool NextBoolean()
        {
            if (_bufferMask > 0)
            {
                var _ = (_buffer & _bufferMask) == 0;
                _bufferMask >>= 1;
                return _;
            }

            var tempX = _y;
            _x ^= _x << 23; var tempY = _x ^ _y ^ (_x >> 17) ^ (_y >> 26);

            _buffer = tempY + _y;
            _x = tempX;
            _y = tempY;

            _bufferMask = 0x8000000000000000;
            return (_buffer & 0xF000000000000000) == 0;
        }

        /// <summary>
        ///   Generates a pseudorandom 16-bit unsigned integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 16-bit unsigned integer.
        /// </returns>
        public ushort NextUInt16()
        {
            var tempX = _y;
            _x ^= _x << 23; var tempY = _x ^ _y ^ (_x >> 17) ^ (_y >> 26);

            var _ = (ushort)(tempY + _y);

            _x = tempX;
            _y = tempY;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom 64-bit unsigned integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 64-bit unsigned integer.
        /// </returns>
        public ulong NextUInt64()
        {
            var tempX = _y;
            _x ^= _x << 23; var tempY = _x ^ _y ^ (_x >> 17) ^ (_y >> 26);

            var _ = tempY + _y;

            _x = tempX;
            _y = tempY;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom double between
        ///   0 and 1 non-inclusive.
        /// </summary>
        /// <returns>
        ///   A pseudorandom double.
        /// </returns>
        public double NextDouble()
        {
            var tempX = _y;
            _x ^= _x << 23; var tempY = _x ^ _y ^ (_x >> 17) ^ (_y >> 26);

            var tempZ = tempY + _y;
            var _ = DoubleUnit * (0x7FFFFFFF & tempZ);

            _x = tempX;
            _y = tempY;

            return _;
        }

        public int Range(int aMin, int aMax)
        {
            var rndInt = (int)NextUInt64();
            var value = aMin + rndInt % (aMax - aMin);

            if (value < aMin || value > aMax)
                value *= -1;

            return value;
        }

        public double Range(double aMin, double aMax)
        {
            var value = aMin + NextDouble() * (aMax - aMin);
            if (value < aMin || value > aMax)
                value *= -1;

            return value;
        }

        public float Range(float aMin, float aMax)
        {
            var value = aMin + NextDouble() * (aMax - aMin);
            if (value < aMin || value > aMax)
                value *= -1;

            return (float)value;
        }

        // corrects bit alignment which might shift the probability slightly to the
        // lower numbers based on the choosen range.
        public ulong FairRange(ulong aRange)
        {
            ulong dif = ulong.MaxValue % aRange;
            // if aligned or range too big, just pick a number
            if (dif == 0 || ulong.MaxValue / (aRange / 4UL) < 2UL)
                return NextUInt64() % aRange;
            ulong v = NextUInt64();
            // avoid the last incomplete set
            while (ulong.MaxValue - v < dif)
                v = NextUInt64();
            return v % aRange;
        }
    }
    public class XorShiftRandom
    {

        // Constants
        public const double DoubleUnit = 1.0 / (int.MaxValue + 1.0);

        // State Fields
        internal ulong X;
        internal ulong Y;

        // Buffer for optimized bit generation.
        internal ulong Buffer;
        internal ulong BufferMask;

        /// <summary>
        ///   Constructs a new  generator
        ///   with the supplied seed.
        /// </summary>
        /// <param name="seed">
        ///   The seed value.
        /// </param>
        public XorShiftRandom(ulong seed)
        {
            X = seed << 3; Y = seed >> 3;
            Buffer = 0;
            BufferMask = 0;

            var temp1 = Y; X ^= X << 23; var temp2 = X ^ Y ^ (X >> 17) ^ (Y >> 26); X = temp1; Y = temp2;
            var tempX = Y; X ^= X << 23; var tempY = X ^ Y ^ (X >> 17) ^ (Y >> 26); var newSeed = tempY + Y; X = tempX; Y = tempY;
            X = newSeed << 3; Y = newSeed >> 3;
        }
    }

    internal class RunningAverage
    {
        private readonly int _size;
        private readonly int[] _values;
        private int _valuesIndex;
        private int _valueCount;
        private int _sum;

        internal RunningAverage(int size)
        {
            _size = Math.Max(size, 1);
            _values = new int[_size];
        }

        internal int Add(int newValue)
        {
            // calculate new value to add to sum by subtracting the 
            // value that is replaced from the new value; 
            var temp = newValue - _values[_valuesIndex];
            _values[_valuesIndex] = newValue;
            _sum += temp;

            _valuesIndex++;
            _valuesIndex %= _size;

            if (_valueCount < _size)
                _valueCount++;

            return _sum / _valueCount;
        }
    }

    public class NetworkReporter
    {
        public Dictionary<PacketType, List<Report>> ReportData = new Dictionary<PacketType, List<Report>>();
        public readonly MyConcurrentPool<Report> ReportPool = new MyConcurrentPool<Report>(3600, reprot => reprot.Clean());

        public NetworkReporter()
        {
            foreach (var suit in (PacketType[])Enum.GetValues(typeof(PacketType)))
                ReportData.Add(suit, new List<Report>());
        }

        public class Report
        {
            public enum Received
            {
                None,
                Server,
                Client
            }

            public Received Receiver;
            public bool PacketValid;
            public int PacketSize;

            public void Clean()
            {
                Receiver = Received.None;
                PacketValid = false;
                PacketSize = 0;
            }
        }
    }

    internal class StallReporter
    {
        private readonly Stopwatch _watch = new Stopwatch();
        internal string Name;
        internal double MaxMs;

        public void Start(string name, double maxMs)
        {
            Name = name;
            MaxMs = maxMs;
            _watch.Restart();
        }

        public void End()
        {
            _watch.Stop();
            var ticks = _watch.ElapsedTicks;
            var ns = 1000000000.0 * ticks / Stopwatch.Frequency;
            var ms = ns / 1000000.0;
            if (ms > MaxMs)
            {
                var message = $"[Warning] {ms} milisecond delay detected in {Name}: ";
                Log.LineShortDate(message, "perf");
            }
        }
    }

    internal class DSUtils
    {
        internal struct Results
        {
            public double Min;
            public double Max;
            public double Median;
            public uint MaxTick;
        }

        internal class Timings
        {
            public double Max;
            public double Min;
            public double Total;
            public double Average;
            public int Events;
            public uint MaxTick;
            public readonly List<int> Values = new List<int>();
            public int[] TmpArray = new int[1];

            internal void Clean()
            {
                Max = 0;
                Min = 0;
                Total = 0;
                Average = 0;
                Events = 0;
            }
        }

        internal Session Session;
        private double _last;
        private bool _time;
        private bool _showTick;

        private Stopwatch Sw { get; } = new Stopwatch();
        private readonly Dictionary<string, Timings> _timings = new Dictionary<string, Timings>();
        public void Start(string name, bool time = true)
        {
            _time = time;
            Sw.Restart();
        }

        public void Purge()
        {
            Clean();
            Clear();
            Session = null;
        }

        public void Clear()
        {
            _timings.Clear();
        }

        public void Clean()
        {
            foreach (var timing in _timings.Values)
                timing.Clean();
        }

        public Results GetValue(string name)
        {
            Timings times;
            if (_timings.TryGetValue(name, out times) && times.Values.Count > 0)
            {
                var itemCnt = times.Values.Count;
                var tmpCnt = times.TmpArray.Length;
                if (itemCnt != tmpCnt)
                    Array.Resize(ref times.TmpArray, itemCnt);
                for (int i = 0; i < itemCnt; i++)
                    times.TmpArray[i] = times.Values[i];

                times.Values.Clear();
                var median = MathFuncs.GetMedian(times.TmpArray);

                return new Results { Median = median / 1000000.0, Min = times.Min, Max = times.Max, MaxTick = times.MaxTick};
            }

            return new Results();
        }

        public void Complete(string name, bool store, bool display = false)
        {
            Sw.Stop();
            var ticks = Sw.ElapsedTicks;
            var ns = 1000000000.0 * ticks / Stopwatch.Frequency;
            var ms = ns / 1000000.0;
            if (store)
            {
                Timings timings;
                if (_timings.TryGetValue(name, out timings))
                {
                    timings.Total += ms;
                    timings.Values.Add((int)ns);
                    timings.Events++;
                    timings.Average = (timings.Total / timings.Events);
                    if (ms > timings.Max)
                    {
                        timings.Max = ms;
                        timings.MaxTick = Session.I.Tick;
                    }
                    if (ms < timings.Min || timings.Min <= 0) timings.Min = ms;
                }
                else
                {
                    timings = new Timings();
                    timings.Total += ms;
                    timings.Values.Add((int)ns);
                    timings.Events++;
                    timings.Average = ms;
                    timings.Max = ms;
                    timings.MaxTick = Session.I.Tick;
                    timings.Min = ms;
                    _timings[name] = timings;
                }
            }
            Sw.Reset();
            if (display)
            {
                var message = $"[{name}] ms:{(float)ms} last-ms:{(float)_last}";
                _last = ms;
                if (_time) Log.LineShortDate(message);
                else Log.CleanLine(message);
            }
        }
    }
}
