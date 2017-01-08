using System;
using System.Net;

namespace Ghost.Network
{
    public enum NetConnectionState
    {
        None,
        Disconnected,


        ConnectionInitiation,

        AwaitingApproval
    }

    public class NetConnection
    {
        private NetConnectionState m_state;

        public NetPeer Peer
        {
            get;
            private set;
        }

        public DateTime LastPing
        {
            get; set;
        }

        public EndPoint RemoteEndPoint
        {
            get;
            private set;
        }

        public DateTime ConnectionTime
        {
            get; set;
        }

        public bool AwaitingApproval
        {
            get => m_state == NetConnectionState.AwaitingApproval;
        }

        public NetConnection()
        {
            m_state = NetConnectionState.None;
        }

        public NetConnection Initialize(NetPeer peer, EndPoint remote, NetConnectionState state)
        {
            Peer = peer;
            RemoteEndPoint = remote;
            m_state = state;
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

            }
        }

        private bool ValidateHandshakeData(INetMessage message)
        {
            try
            {
                var remoteAppId = message.ReadString();
                var remoteUID = message.ReadInt64();
                var remoteTimeOffset = message.ReadSingle();
                //InitializeRemoteTimeOffset(msg.ReadSingle());
                if (message.Remaining > 0)
                {

                }

                if (remoteAppId != Peer.Configuration.AppId)
                {
                    //ExecuteDisconnect("Wrong application identifier!", true);
                    return false;
                }

                //m_remoteUniqueIdentifier = remoteUniqueIdentifier;
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
    }
}