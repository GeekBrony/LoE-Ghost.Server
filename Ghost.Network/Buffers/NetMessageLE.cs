using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;

namespace Ghost.Network.Buffers
{
    internal class NetMessageLE : NetBufferLE, INetMessage
    {
        public NetPeer Peer
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public NetConnection Sender
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public NetMessageType Type
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public NetMessageLE(NetMemoryManager manager)
            : base(manager)
        {

        }

        public void BindTo(SocketAsyncEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (m_handle.IsAllocated)
            {
                Interlocked.Increment(ref m_ref_count);
                args.UserToken = this;
            }
            else throw new InvalidOperationException("Buffer not set");
        }
    }
}