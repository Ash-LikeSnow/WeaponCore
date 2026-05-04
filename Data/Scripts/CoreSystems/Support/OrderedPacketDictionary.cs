using System.Collections.Generic;
using CoreSystems;

namespace WeaponCore.Data.Scripts.CoreSystems.Support
{
    /// <summary>
    ///     Doubly-linked dictionary. Characteristics:
    ///     <list type="bullet">
    ///         <item><description>Always preserves insertion order. This means packets that are emitted with the globally increasing counter will be ordered correctly on copy.</description></item>
    ///         <item><description>Moves item to the end of the collection on update. When we replace the value with a newer one, the entry gets moved to the end of the list.</description></item>
    ///     </list>
    /// </summary>
    internal class OrderedPacketDictionary
    {
        private class Node
        {
            public Session.PacketInfo Value;
            public Node Next;
            public Node Previous;

            public void Clean()
            {
                Value = default(Session.PacketInfo);
                Next = null;
                Previous = null;
            }
        }

        private readonly Dictionary<object, Node> _dictionary = new Dictionary<object, Node>(128);
        private readonly Stack<Node> _nodePool = new Stack<Node>(128);
        private Node _head;
        private Node _tail;

        public int Count => _dictionary.Count;
        
        /// <summary>
        ///     Inserts or updates.
        ///     Insertion will append to the end of the collection, and updating will remove the previous value and also append to the end of the collection.
        /// </summary>
        /// <param name="key"></param>
        public Session.PacketInfo this[object key]
        {
            get
            {
                return _dictionary[key].Value;
            }
            set
            {
                Node node;
                if (_dictionary.TryGetValue(key, out node))
                {
                    Unlink(node);
                    node.Value = value;
                }
                else
                {
                    node = _nodePool.Count > 0 ? _nodePool.Pop() : new Node();
                    node.Value = value;
                    _dictionary[key] = node;
                }

                if (_tail == null)
                {
                    _head = node;
                }
                else
                {
                    _tail.Next = node;
                    node.Previous = _tail;
                }

                _tail = node;
            }
        }
        
        /// <summary>
        ///     Transfers the packets to the packet list in order, and clears the collection.
        /// </summary>
        /// <param name="target"></param>
        public void Transfer(List<Session.PacketInfo> target)
        {
            var node = _head;
            while (node != null)
            {
                target.Add(node.Value);
                var next = node.Next;
                node.Clean();
                _nodePool.Push(node);
                node = next;
            }

            _head = null;
            _tail = null;
            _dictionary.Clear();
        }
        
        public bool ContainsKey(object key) => _dictionary.ContainsKey(key);

        public bool TryGetValue(object key, out Session.PacketInfo value)
        {
            Node node;
            if (_dictionary.TryGetValue(key, out node))
            {
                value = node.Value;
                return true;
            }
            
            value = default(Session.PacketInfo);
            return false;
        }

        public void Remove(object key)
        {
            Node node;
            if (_dictionary.TryGetValue(key, out node))
            {
                _dictionary.Remove(key);
                Unlink(node);
                node.Clean();
                _nodePool.Push(node);
            }
        }
        
        private void Unlink(Node node)
        {
            var previous = node.Previous;
            var next = node.Next;

            if (previous != null)
            {
                previous.Next = next;
            }
            else
            {
                _head = next;
            }

            if (next != null)
            {
                next.Previous = previous;
            }
            else
            {
                _tail = previous;
            }

            node.Previous = null;
            node.Next = null;
        }
    }
}