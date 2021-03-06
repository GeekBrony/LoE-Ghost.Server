﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PNet;

namespace PNetS
{
    public partial class Server
    {
        public NetworkConfiguration Configuration { get; private set; }

        /// <summary>
        /// when subscribing to this, you will receive the message the player sent to verify themselves. To let them connect, you must call Player.Approve
        /// </summary>
        public event Action<Player, NetMessage> VerifyPlayer;

        readonly ConcurrentDictionary<Guid, Room> _rooms = new ConcurrentDictionary<Guid, Room>();
        internal ConcurrentDictionary<Guid, Room> Rooms { get { return _rooms; } }
        /// <summary>
        /// The allowed ip addresses that can connect as rooms
        /// </summary>
        public readonly List<IPAddress> AllowedRoomHosts = new List<IPAddress>();
        /// <summary>
        /// A list of passwords that rooms can pass as well to be allowed to connect
        /// </summary>
        public readonly List<string> AllowedPasswords = new List<string>();

        public readonly SerializationManager Serializer = new SerializationManager();

        private readonly IntDictionary<Player> _players = new IntDictionary<Player>(256);
        internal IntDictionary<Player> Players { get { return _players; } }

        /// <summary>
        /// event fired when a Player object is constructing, should be used to return the object used for Player.NetUserData
        /// </summary>
        public event Func<INetSerializable> ConstructNetData;
        /// <summary>
        /// fired when a room connects.
        /// </summary>
        public event Action<Room> RoomAdded;
        /// <summary>
        /// fired when a room disconnects
        /// </summary>
        public event Action<Room> RoomRemoved;

        /// <summary>
        /// fired when a player connects
        /// </summary>
        public event Action<Player> PlayerAdded;
        /// <summary>
        /// fired when a player disconnects
        /// </summary>
        public event Action<Player> PlayerRemoved;

        internal readonly int Id;
        private static int idCounter;

        public Server()
        {
            Id = Interlocked.Increment(ref idCounter);
            _players.Add(Player.ServerPlayer);
        }

        public void Initialize(NetworkConfiguration configuration)
        {
            Configuration = configuration;
            
            var allhosts = Configuration.RoomHosts.Split(';');
            AllowedRoomHosts.AddRange(allhosts.SelectMany(Dns.GetHostAddresses).ToList());

            InternalInitialize();
        }
        partial void InternalInitialize();

        public void Shutdown(string reason = "Dispatcher shutting down")
        {
            ImplementationShutdown(reason);
        }
        partial void ImplementationShutdown(string reason);

        public bool TryGetRooms(string roomId, out Room[] rooms)
        {
            var orooms = new List<Room>(_rooms.Count);
            foreach(var room in _rooms.ToArray())
            {
                if (room.Value.RoomId == roomId)
                    orooms.Add(room.Value);
            }

            rooms = orooms.ToArray();
            return true;
        }

        public bool TryGetRoom(Guid roomId, out Room room)
        {
            return _rooms.TryGetValue(roomId, out room);
        }

        public Room GetRoom(Guid roomId)
        {
            Room room;
            _rooms.TryGetValue(roomId, out room);
            return room;
        }

        /// <summary>
        /// total number of rooms
        /// </summary>
        public int RoomCount { get { return _rooms.Count; } }
        private void AddRoom(Room room)
        {
            _rooms[room.Guid] = room;
            
            try
            {
                RoomAdded?.Invoke(room);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void UpdateRoomsOfNewRoom(Room room)
        {
            //tell all rooms about this room

            var msg = GetMessage(room.RoomId.Length * 2 + 18);
            msg.Write(RpcUtils.GetHeader(ReliabilityMode.Ordered, BroadcastMode.Server, MsgType.Internal));
            msg.Write(DandRRpcs.RoomAdd);
            msg.Write(room.RoomId);
            msg.Write(room.Guid);
            room.SendMessageToOthers(msg, ReliabilityMode.Ordered);

            //tell this room about all other rooms
            msg = GetMessage(1000); // fuck if I know
            msg.Write(RpcUtils.GetHeader(ReliabilityMode.Ordered, BroadcastMode.Server, MsgType.Internal));
            msg.Write(DandRRpcs.RoomAdd);
            foreach (var r in _rooms.ToArray())
            {
                msg.Write(r.Value.RoomId);
                msg.Write(r.Value.Guid);
            }
            room.SendMessage(msg, ReliabilityMode.Ordered);
        }

        private void RemoveRoom(Room room)
        {
            if (room == null) return;

            Room removed;
            _rooms.TryRemove(room.Guid, out removed);
            if (removed != room)
            {
                Debug.LogError($"Removed {removed}, but we were attempting to remove {room}");
            }

            try
            {
                RoomRemoved?.Invoke(removed);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            var msg = GetMessage(18);
            msg.Write(RpcUtils.GetHeader(ReliabilityMode.Ordered, BroadcastMode.Server, MsgType.Internal));
            msg.Write(DandRRpcs.RoomRemove);
            msg.Write(removed.Guid);
            removed.SendMessageToOthers(msg, ReliabilityMode.Ordered);

            Player[] players;
            lock (_players)
                players = _players.Values;
            foreach (var player in players)
            {
                if (player == null) continue;
                if (player.SwitchingToRoom == removed.Guid)
                    player.OnSwitchingToRoomInvalidated();
            }
        }

        internal void BeginPlayerAdd(Player player)
        {
            if (player.Id != 0)
                return;
            
            lock (_players)
            {
                //add a 'fake' player to the slot, to reserve the id.
                var nid = _players.Add(Player.ServerPlayer);
                if (nid > ushort.MaxValue)
                {
                    throw new IndexOutOfRangeException(
                        "Attempted to add more players than the player id is assignable to (65535)");
                }

                player.Id = (ushort) nid;
            }
        }

        internal void FinalizePlayerAdd(Player player)
        {
            if (player.Id == 0)
                return;

            //update the slot with the correct player.
            lock(_players)
                _players.Add(player.Id, player);

            try
            {
                PlayerAdded?.Invoke(player);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        private void RemovePlayer(Player player)
        {
            if (player.Id == 0)
                return;

            //first need to inform the room, if there is one, of the player's disconnect. 
            //Otherwise a new player might obtain the id and send that to a new room before this message gets there
            player.DisconnectOnRoom();
            
            //this will clean up the player whether or not they actually finished being added.
            lock(_players)
                _players.Remove(player.Id);
            
            Room.MovePlayerCount(this, player.CurrentRoomGuid, Guid.Empty);
            
            //todo: anything else? this is run on disconnect

            try
            {
                PlayerRemoved?.Invoke(player);
            }
            catch (Exception e) { Debug.LogException(e);}
        }

#if DEBUG
        private void RemovePlayerNoNotify(Player player)
        {
            Debug.Log($"removing {player} from room, without notifying the room");

            if (player.Id == 0)
                return;

            //this will clean up the player whether or not they actually finished being added.
            lock (_players)
                _players.Remove(player.Id);

            Room.MovePlayerCount(this, player.CurrentRoomGuid, Guid.Empty);

            //todo: anything else? this is run on disconnect

            try
            {
                PlayerRemoved?.Invoke(player);
            }
            catch (Exception e) { Debug.LogException(e); }
        }
#endif

        /// <summary>
        /// get the player associated with the specified id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Player GetPlayer(ushort id)
        {
            Player player;
            _players.TryGetValue(id, out player);
            return player;
        }

        internal NetMessage GetMessage(int length)
        {
            NetMessage msg = null;
            ImplGetMessage(length, ref msg);
            return msg;
        }

        partial void ImplGetMessage(int length, ref NetMessage message);

        #region password authorization cooldown
        private readonly ConcurrentDictionary<IPEndPoint, DateTime> _passCooldowns = new ConcurrentDictionary<IPEndPoint, DateTime>();
        private bool CheckPassCooldown(IPEndPoint sender)
        {
            DateTime existing;
            if (!_passCooldowns.TryGetValue(sender, out existing))
                return true;
            if (DateTime.Now > existing)
            {
                _passCooldowns.TryRemove(sender, out existing);
                return true;
            }
            //if they're checking while still in a cooldown state, let's refresh it.
            _passCooldowns[sender] = DateTime.Now + TimeSpan.FromSeconds(2);
            return false;
        }

        private void AddPassCooldown(IPEndPoint sender)
        {
            _passCooldowns[sender] = DateTime.Now + TimeSpan.FromSeconds(2);
        }
        #endregion

        private bool ApproveRoomConnection(IPEndPoint sender, NetMessage msg, out string denyReason, out Room room)
        {
            room = null;
            string roomId;
            if (msg == null)
            {
                Debug.LogWarning($"Denied room connection to {sender} - no auth message");
                denyReason = DtoRMsgs.NoRoomId;
                return false;
            }

            if (!msg.ReadString(out roomId))
            {
                Debug.LogWarning($"Denied room connection to {sender} - no room id");
                denyReason = DtoRMsgs.NoRoomId;
                return false;
            }

            int iAuthType;
            if (!msg.ReadInt32(out iAuthType))
            {
                Debug.LogWarning($"Denied room connection to {sender} - didn't send a room auth type. Are they an old version?");
                denyReason = DtoRMsgs.NotAllowed + " - no authtype";
                return false;
            }
            var authType = (RoomAuthType)iAuthType;

            string authData;
            if (!msg.ReadString(out authData))
            {
                Debug.LogWarning($"Denied room connection to {sender} - didn't send auth data. Are they an old version?");
                denyReason = DtoRMsgs.NotAllowed + " - no authdata";
                return false;
            }

            string userDefinedAuthData;
            if (!msg.ReadString(out userDefinedAuthData))
            {
                Debug.LogWarning($"Denied room connection to {sender} - didn't send udef auth data. Are they an old version?");
                denyReason = DtoRMsgs.NotAllowed + " - no udef authdata";
                return false;
            }

            switch (authType)
            {
                case RoomAuthType.AllowedHost:
                    break;
                case RoomAuthType.AllowedToken:
                    //we skip even checking the password if they're in a bad password cooldown state
                    if (CheckPassCooldown(sender))
                    {
                        if (AllowedPasswords.Contains(authData))
                        {
                            goto Approved;
                        }
                        AddPassCooldown(sender);
                    }
                    Debug.LogWarning($"Room {sender} tried to auth with password {authData}, but it wasn't valid");
                    break;
                default:
                    denyReason = DtoRMsgs.NotAllowed + " - unrecognized authtype";
                    Debug.LogWarning($"Denied room connection to {sender} - sent {authType} for room auth type, which isn't recognized");
                    return false;
            }

            //we'll always check with allowed hosts
            if (!AllowedRoomHosts.Contains(sender.Address))
            {
                denyReason = DtoRMsgs.NotAllowed + " - " + sender.Address;
                Debug.LogWarning($"Denied room connection to {sender}. Wasn't on allowed hosts list");
                return false;
            }

        Approved:
            var port = msg.ReadInt32();
            var maxPlayers = msg.ReadInt32();
            string supAddr;
            IPAddress supIp = null;
            if (msg.ReadString(out supAddr) && !string.IsNullOrWhiteSpace(supAddr))
            {
                if (!IPAddress.TryParse(supAddr, out supIp))
                {
                    supIp = null;
                }
            }
            supIp = supIp ?? sender.Address;

            room = new Room(this, roomId, Guid.NewGuid(), new IPEndPoint(supIp, port))
            {
                MaxPlayers = maxPlayers,
                UserDefinedAuthData = userDefinedAuthData
            };

            denyReason = null;
            return true;
        }
    }
}