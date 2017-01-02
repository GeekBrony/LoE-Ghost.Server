using System;
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
            set
            {
                throw new NotImplementedException();
            }
        }

        public NetMessageType Type
        {
            get;

            set;
        }

        public NetConnection Sender
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
                if (m_segment.IsAllocated)
                    args.SetBuffer(m_segment.Buffer, m_segment.Offset, m_segment.Length);
                else
                {
                    unsafe
                    {
                        var length = (int)(m_end - m_start);
                        var buffer = (byte[])m_handle.Target;
                        var offset = (int)(m_start - (byte*)m_handle.AddrOfPinnedObject().ToPointer());
                        args.SetBuffer(buffer, offset, length);
                    }
                }
            }
            else throw new InvalidOperationException("Buffer not set");
        }
    }
}