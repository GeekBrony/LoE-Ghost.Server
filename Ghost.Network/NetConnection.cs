using System;
using System.Net;

namespace Ghost.Network
{
    public class NetConnection
    {
        public NetPeer Peer
        {
            get; set;
        }

        public DateTime LastPing
        {
            get; set;
        }

        public EndPoint RemoteEndPoint
        {
            get; set;
        }

        public DateTime ConnectionTime
        {
            get; set;
        }

        public bool WaitingForApproval
        {
            get; set;
        }

        public void Ping()
        {

        }

        public void Disconect(string message)
        {

        }
    }
}