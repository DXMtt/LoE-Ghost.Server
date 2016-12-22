﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PNet;

namespace PNetS
{
    public partial class Room
    {
        internal void ConsumeData(NetMessage msg)
        {
            byte header;
            if (!msg.ReadByte(out header))
                return;
            MsgType msgType;
            BroadcastMode broadcast;
            ReliabilityMode reliability;
            SubMsgType sub;
            RpcUtils.ReadHeader(header, out reliability, out broadcast, out msgType, out sub);

            switch (msgType)
            {
                case MsgType.Internal:
                    ProcessInternal(broadcast, reliability, msg);
                    break;
                case MsgType.Static:
                    ProcessStatic(broadcast, reliability, msg);
                    break;
                default:
                    Debug.LogWarning("Unsupported {0} byte message of type {1} from {2}", msg.LengthBytes - 1, msgType, this);
                    break;
            }
        }

        private void ProcessStatic(BroadcastMode broadcast, ReliabilityMode reliability, NetMessage msg)
        {
            byte rpc;
            if (!msg.ReadByte(out rpc))
                return;
            CallRpc(rpc, msg);

            if (broadcast == BroadcastMode.All || broadcast == BroadcastMode.Others)
            {
                var nmsg = _server.GetMessage(msg.LengthBytes);
                nmsg.Write(msg.Data, 0, msg.LengthBytes);
                NetMessage.RecycleMessage(msg);
                if (broadcast == BroadcastMode.All)
                    SendToAll(nmsg, reliability);
                else
                    SendMessageToOthers(nmsg, reliability);
            }
        }

        private void ProcessInternal(BroadcastMode broadcast, ReliabilityMode reliability, NetMessage msg)
        {
            byte rpc;
            if (!msg.ReadByte(out rpc))
            {
                Debug.LogError("Malformed internal rpc");
                return;
            }

            try
            {
                ushort pid;
                Player player;
                switch (rpc)
                {
                    case DandRRpcs.RoomSwitch:
                        pid = msg.ReadUInt16();
                        var room = msg.ReadString();
                        player = _server.GetPlayer(pid);
                        if (player != null)
                            player.ChangeRoom(room);
                        else
                            Debug.LogError("Attempted to switch a null player");
                        break;
                    case DandRRpcs.SyncNetUser:
                        pid = msg.ReadUInt16();
                        player = _server.GetPlayer(pid);
                        if (player != null && player.NetUserData != null)
                        {
                            player.NetUserData.OnDeserialize(msg);
                            player.OnNetUserDataChanged();
                        }
                        break;
                    case DandRRpcs.PlayerConnected:
                        pid = msg.ReadUInt16();
                        player = _server.GetPlayer(pid);
                        if (player != null)
                        {
                            player.FinishedRoomSwitch();
                        }
                        break;
                    case DandRRpcs.DisconnectMessage:
                        string reason;
                        if (!msg.ReadString(out reason))
                            reason = "Shutting down";
                        Debug.Log("Room disconnecting: {0}", reason);
                        Running = false;
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
