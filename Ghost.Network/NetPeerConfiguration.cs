using System;
using System.Net;

namespace Ghost.Network
{
    public class NetPeerConfiguration
    {
        public string AppId
        {
            get; private set;
        }

        public bool CanAccept
        {
            get; private set;
        }

        public EndPoint BindPoint
        {
            get; set;
        }

        public double PingTimeout
        {
            get; set;
        }

        public TimeSpan PingInterval
        {
            get; set;
        }

        public double ConnectionTimeout
        {
            get; set;
        }

        public TimeSpan HeartbeatInterval
        {
            get; set;
        }

        public NetPeerConfiguration(string appId, bool canAccept)
        {
            AppId = appId;
            CanAccept = canAccept;
        }
    }
}