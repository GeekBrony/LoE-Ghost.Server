using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Ghost.Network.Buffers
{
    internal class NetMessageLE : NetBufferLE, INetMessage
    {
        private static readonly EventHandler<SocketAsyncEventArgs> s_handler = new EventHandler<SocketAsyncEventArgs>(FreeBuffer);

        private static readonly ConcurrentDictionary<int, ValueTuple<NetMemoryManager, ArraySegment<byte>>> s_segments = new ConcurrentDictionary<int, ValueTuple<NetMemoryManager, ArraySegment<byte>>>();

        private static void FreeBuffer(object sender, SocketAsyncEventArgs args)
        {
            if (args.LastOperation == SocketAsyncOperation.SendTo)
            {
                ValueTuple<NetMemoryManager, ArraySegment<byte>> tuple;
                if (s_segments.TryRemove(args.GetHashCode(), out tuple))
                    tuple.Item1.Free(tuple.Item2);
            }
        }

        public NetPeer Peer => throw new NotImplementedException();

        public NetConnection Sender => throw new NotImplementedException();

        public NetMessageType Type => throw new NotImplementedException();

        public NetMessageLE(NetMemoryManager manager)
            : base(manager)
        {

        }

        public void PrepareToSend(SocketAsyncEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (m_handle.IsAllocated)
            {  
                if (s_segments.TryAdd(args.GetHashCode(), new ValueTuple<NetMemoryManager, ArraySegment<byte>>(m_manager, m_segment)))
                {
                    args.Completed += s_handler;
                    args.SetBuffer(m_segment.Array, m_segment.Offset, (int)Length);
                    FreeBuffer();
                    m_manager.Free(this);
                }
                else throw new InvalidOperationException("Send operation on same socket args");
            }
            else throw new InvalidOperationException("Buffer not set");
        }
    }
}