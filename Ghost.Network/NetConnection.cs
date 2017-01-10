using System;
using System.Net;
using Ghost.Network.Utilities;

namespace Ghost.Network
{
    public class NetConnection
    {
        private NetConnectionState m_state;
        private INetSerializable m_hail;
        private double m_remoteTimeOffset;
        private double m_averageRoundtripTime;
        private long m_remoteUniqueIdentifier;

        public NetPeer Peer
        {
            get;
            private set;
        }

        public double LastPing
        {
            get; set;
        }

        public EndPoint RemoteEndPoint
        {
            get;
            private set;
        }

        public double ConnectionTime
        {
            get;
            set;
        }

        public NetConnectionState State => m_state;

        public NetConnection()
        {
            m_state = NetConnectionState.None;
        }

        public NetConnection Clean()
        {
            Peer = null;
            RemoteEndPoint = null;
            m_state = NetConnectionState.None;
            m_hail = null;
            return this;
        }

        public NetConnection Initialize(NetPeer peer, EndPoint remote, NetConnectionState state)
        {
            Peer = peer;
            RemoteEndPoint = remote;
            m_state = state;
            m_hail = peer.CreateHail();
            return this;
        }

        public void Ping()
        {

        }

        public void Disconect(string message)
        {

        }

        public void ProcessConnect(double now, INetMessage message)
        {
            if (ValidateHandshakeData(message))
            {
                ConnectionTime = NetTime.Now;
            }
        }

        private bool ValidateHandshakeData(INetMessage message)
        {
            try
            {
                var remoteAppId = message.ReadString();
                m_remoteUniqueIdentifier = message.ReadInt64();
                InitializeRemoteTimeOffset(message.ReadSingle());
                if (message.Remaining > 0)
                    m_hail?.OnDeserialize(message);

                if (remoteAppId != Peer.Configuration.AppId)
                {
                    //ExecuteDisconnect("Wrong application identifier!", true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                // whatever; we failed
                //ExecuteDisconnect("Handshake data validation failed", true);
                //m_peer.LogWarning("ReadRemoteHandshakeData failed: " + ex.Message);
                return false;
            }
            return true;
        }

        // this might happen more than once
        internal void InitializeRemoteTimeOffset(float remoteSendTime)
        {
            m_remoteTimeOffset = (remoteSendTime + (m_averageRoundtripTime / 2.0)) - NetTime.Now;
        }
    }
}