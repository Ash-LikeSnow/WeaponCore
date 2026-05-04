using System;

namespace WeaponCore.Data.Scripts.CoreSystems.Support
{
    public class RingBuffer<T>
    {
        public readonly T[] Buffer;
      
        private int _head;
        private int _tail;
        
        public int Count { get; private set; }

        public RingBuffer(int capacity)
        {
            Buffer = new T[capacity];
        }

        public void Enqueue(T item)
        {
            if (Count == Buffer.Length)
            {
                throw new InvalidOperationException("Buffer full");
            }
            
            Buffer[_tail] = item;
            _tail = (_tail + 1) % Buffer.Length;
            Count++;
        }

        public T Dequeue()
        {
            if (Count == 0)
            {
                throw new Exception("Buffer empty");
            }
            
            var item = Buffer[_head];
            Buffer[_head] = default(T);
            _head = (_head + 1) % Buffer.Length;
            Count--;
            
            return item;
        }
        
        public T GetHistory(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new Exception($"Index must be between 0 and {Count - 1}");
            }
            
            return Buffer[(_tail - 1 - index + Buffer.Length) % Buffer.Length];
        }
        
        public void Clear()
        {
            _head = 0;
            _tail = 0;
            Count = 0;
    
            Array.Clear(Buffer, 0, Buffer.Length);
        }

        public T this[int index] => Buffer[(_head + index) % Buffer.Length];
        public int Capacity => Buffer.Length;
    }
}