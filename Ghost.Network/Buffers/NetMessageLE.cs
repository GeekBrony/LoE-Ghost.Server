using System;
using System.Net.Sockets;
using System.Threading;

namespace Ghost.Network.Buffers
{
    internal class NetMessageLE : NetBufferLE, INetMessage
    {
        public NetPeer Peer
        {
            get;
            set;
        }

        public NetMessageType Type
        {
            get;
            set;
        }

        public NetConnection Sender
        {
            get;
            set;
        }

        public ushort Sequence
        {
            get;
            set;
        }

        public bool IsFragmented
        {
            get;
            set;
        }

        public NetMessageLE(NetMemoryManager manager)
            : base(manager)
        {

        }

        public void BindTo(SocketAsyncEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (m_segment.IsAllocated)
            {
                Interlocked.Increment(ref m_ref_count);
                args.UserToken = this;
                args.SetBuffer(m_segment.Buffer, m_segment.Offset, m_segment.Length);
            }
            else throw new InvalidOperationException("Buffer not set");
        }
    }
}