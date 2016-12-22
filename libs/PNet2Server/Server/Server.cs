using System;
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
        /// Number of players connected to the server
        /// </summary>
        public int PlayerCount { get; private set; }

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
        /// fired when a player disconnects.
        /// <remarks>
        /// If the player's id is 0, then the player was not finished being added, meaning they had verification done, but either failed it, or disconnected before finishing.
        /// However, having a value does not necessarily mean PlayerAdded was fired on the player, just that AllowConnect was run on them.
        /// </remarks>
        /// </summary>
        public event Action<Player> PlayerRemoved;

        internal readonly int Id;
        private static int idCounter;

        private readonly ADispatchServer _server;

        public Server(ADispatchServer server)
        {
            Id = Interlocked.Increment(ref idCounter);
            _players.Add(Player.ServerPlayer);

            _server = server;
            _server.Server = this;
        }

        public void Initialize(NetworkConfiguration configuration)
        {
            Configuration = configuration;
            
            var allhosts = Configuration.RoomHosts.Split(';');
            AllowedRoomHosts.AddRange(allhosts.SelectMany(Dns.GetHostAddresses).ToList());

            _server.Initialize();
        }

        /// <summary>
        /// Shut down the server, and return a task that completes once the server finishes shutting down.
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public Task Shutdown(string reason = "Dispatcher shutting down")
        {
            return _server.Shutdown(reason);
        }

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

        [JetBrains.Annotations.CanBeNull]
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

        internal void AddRoom(Room room)
        {
            _rooms[room.Guid] = room;
            
            try
            {
                RoomAdded.Raise(room);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            
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

        internal void RemoveRoom(Room room)
        {
            if (room == null) return;

            Room removed;
            _rooms.TryRemove(room.Guid, out removed);
            if (removed != room)
            {
                Debug.LogError("Removed {0}, but we were attempting to remove {1}", removed, room);
            }

            try
            {
                RoomRemoved.Raise(removed);
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
                PlayerCount++;
                if (nid > ushort.MaxValue)
                {
                    throw new IndexOutOfRangeException(
                        "Attempted to add more players than the player id is assignable to (65535)");
                }

                player.Id = (ushort) nid;
            }

            _server.AllowPlayerToConnect(player);
        }

        internal void PlayerConnecting(object connection, IPEndPoint endpoint, Action<Player> ctor, NetMessage msg)
        {
            var player = new Player(this) { Connection = connection, EndPoint = endpoint };
            ctor(player);

            player.Status = ConnectionStatus.Connecting;
            if (ConstructNetData != null)
                player.NetUserData = ConstructNetData();

            if (VerifyPlayer != null)
            {
                VerifyPlayer(player, msg);
            }
            else
            {
                player.AllowConnect();
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
                PlayerAdded.Raise(player);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        internal void RemovePlayer(Player player)
        {
            if (player.Id != 0)
            {
                //first need to inform the room, if there is one, of the player's disconnect. 
                //Otherwise a new player might obtain the id and send that to a new room before this message gets there
                player.DisconnectOnRoom();

                //this will clean up the player whether or not they actually finished being added.
                lock (_players)
                {
                    _players.Remove(player.Id);
                    PlayerCount--;
                }

                Room.MovePlayerCount(this, player.CurrentRoomGuid, Guid.Empty);
            }

            //it is necessary that we still raise the event, in case users are doing something during verification
            try
            {
                PlayerRemoved.Raise(player);
            }
            catch (Exception e) { Debug.LogException(e);}
        }

#if DEBUG
        internal void RemovePlayerNoNotify(Player player)
        {
            Debug.Log("removing {0} from room, without notifying the room", player);

            if (player.Id == 0)
                return;

            //this will clean up the player whether or not they actually finished being added.
            lock (_players)
                _players.Remove(player.Id);

            Room.MovePlayerCount(this, player.CurrentRoomGuid, Guid.Empty);

            //todo: anything else? this is run on disconnect

            try
            {
                PlayerRemoved.Raise(player);
            }
            catch (Exception e) { Debug.LogException(e); }
        }
#endif

        /// <summary>
        /// get the player associated with the specified id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [JetBrains.Annotations.CanBeNull]
        public Player GetPlayer(ushort id)
        {
            Player player;
            _players.TryGetValue(id, out player);
            return player;
        }

        internal NetMessage GetMessage(int length)
        {
            return _server.GetMessage(length);
        }

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

        internal bool ApproveRoomConnection(IPEndPoint sender, NetMessage msg, out string denyReason, out Room room)
        {
            room = null;
            string roomId;
            if (msg == null)
            {
                Debug.LogWarning("Denied room connection to {0} - no auth message", sender);
                denyReason = DtoRMsgs.NoRoomId;
                return false;
            }

            if (!msg.ReadString(out roomId))
            {
                Debug.LogWarning("Denied room connection to {0} - no room id", sender);
                denyReason = DtoRMsgs.NoRoomId;
                return false;
            }

            int iAuthType;
            if (!msg.ReadInt32(out iAuthType))
            {
                Debug.LogWarning("Denied room connection to {0} - didn't send a room auth type. Are they an old version?", sender);
                denyReason = DtoRMsgs.NotAllowed + " - no authtype";
                return false;
            }
            var authType = (RoomAuthType)iAuthType;

            string authData;
            if (!msg.ReadString(out authData))
            {
                Debug.LogWarning("Denied room connection to {0} - didn't send auth data. Are they an old version?", sender);
                denyReason = DtoRMsgs.NotAllowed + " - no authdata";
                return false;
            }

            string userDefinedAuthData;
            if (!msg.ReadString(out userDefinedAuthData))
            {
                Debug.LogWarning("Denied room connection to {0} - didn't send udef auth data. Are they an old version?", sender);
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
                    Debug.LogWarning("Room {0} tried to auth with password {1}, but it wasn't valid", sender, authData);
                    break;
                default:
                    denyReason = DtoRMsgs.NotAllowed + " - unrecognized authtype";
                    Debug.LogWarning("Denied room connection to {0} - sent {1} for room auth type, which isn't recognized", sender, authType);
                    return false;
            }

            //we'll always check with allowed hosts
            if (!AllowedRoomHosts.Contains(sender.Address))
            {
                denyReason = DtoRMsgs.NotAllowed + " - " + sender.Address;
                Debug.LogWarning("Denied room connection to {0}. Wasn't on allowed hosts list", sender);
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

        internal void SendToRoom(Room room, NetMessage msg, ReliabilityMode mode)
        {
            _server.SendToRoom(room, msg, mode);
        }

        internal void SendToOtherRooms(Room room, NetMessage msg, ReliabilityMode mode)
        {
            _server.SendToOtherRooms(room, msg, mode);
        }

        internal void SendToAllRooms(NetMessage msg, ReliabilityMode mode)
        {
            _server.SendToAllRooms(msg, mode);
        }
    }
}