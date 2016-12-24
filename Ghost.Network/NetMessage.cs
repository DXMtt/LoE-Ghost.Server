﻿using System;
using System.Net.Sockets;

namespace Ghost.Network
{
    public enum NetMessageType : byte
    {
        Unconnected = 0,

        UserUnreliable = 1,

        UserSequenced1 = 2,
        UserSequenced2 = 3,
        UserSequenced3 = 4,
        UserSequenced4 = 5,
        UserSequenced5 = 6,
        UserSequenced6 = 7,
        UserSequenced7 = 8,
        UserSequenced8 = 9,
        UserSequenced9 = 10,
        UserSequenced10 = 11,
        UserSequenced11 = 12,
        UserSequenced12 = 13,
        UserSequenced13 = 14,
        UserSequenced14 = 15,
        UserSequenced15 = 16,
        UserSequenced16 = 17,
        UserSequenced17 = 18,
        UserSequenced18 = 19,
        UserSequenced19 = 20,
        UserSequenced20 = 21,
        UserSequenced21 = 22,
        UserSequenced22 = 23,
        UserSequenced23 = 24,
        UserSequenced24 = 25,
        UserSequenced25 = 26,
        UserSequenced26 = 27,
        UserSequenced27 = 28,
        UserSequenced28 = 29,
        UserSequenced29 = 30,
        UserSequenced30 = 31,
        UserSequenced31 = 32,
        UserSequenced32 = 33,

        UserReliableUnordered = 34,

        UserReliableSequenced1 = 35,
        UserReliableSequenced2 = 36,
        UserReliableSequenced3 = 37,
        UserReliableSequenced4 = 38,
        UserReliableSequenced5 = 39,
        UserReliableSequenced6 = 40,
        UserReliableSequenced7 = 41,
        UserReliableSequenced8 = 42,
        UserReliableSequenced9 = 43,
        UserReliableSequenced10 = 44,
        UserReliableSequenced11 = 45,
        UserReliableSequenced12 = 46,
        UserReliableSequenced13 = 47,
        UserReliableSequenced14 = 48,
        UserReliableSequenced15 = 49,
        UserReliableSequenced16 = 50,
        UserReliableSequenced17 = 51,
        UserReliableSequenced18 = 52,
        UserReliableSequenced19 = 53,
        UserReliableSequenced20 = 54,
        UserReliableSequenced21 = 55,
        UserReliableSequenced22 = 56,
        UserReliableSequenced23 = 57,
        UserReliableSequenced24 = 58,
        UserReliableSequenced25 = 59,
        UserReliableSequenced26 = 60,
        UserReliableSequenced27 = 61,
        UserReliableSequenced28 = 62,
        UserReliableSequenced29 = 63,
        UserReliableSequenced30 = 64,
        UserReliableSequenced31 = 65,
        UserReliableSequenced32 = 66,

        UserReliableOrdered1 = 67,
        UserReliableOrdered2 = 68,
        UserReliableOrdered3 = 69,
        UserReliableOrdered4 = 70,
        UserReliableOrdered5 = 71,
        UserReliableOrdered6 = 72,
        UserReliableOrdered7 = 73,
        UserReliableOrdered8 = 74,
        UserReliableOrdered9 = 75,
        UserReliableOrdered10 = 76,
        UserReliableOrdered11 = 77,
        UserReliableOrdered12 = 78,
        UserReliableOrdered13 = 79,
        UserReliableOrdered14 = 80,
        UserReliableOrdered15 = 81,
        UserReliableOrdered16 = 82,
        UserReliableOrdered17 = 83,
        UserReliableOrdered18 = 84,
        UserReliableOrdered19 = 85,
        UserReliableOrdered20 = 86,
        UserReliableOrdered21 = 87,
        UserReliableOrdered22 = 88,
        UserReliableOrdered23 = 89,
        UserReliableOrdered24 = 90,
        UserReliableOrdered25 = 91,
        UserReliableOrdered26 = 92,
        UserReliableOrdered27 = 93,
        UserReliableOrdered28 = 94,
        UserReliableOrdered29 = 95,
        UserReliableOrdered30 = 96,
        UserReliableOrdered31 = 97,
        UserReliableOrdered32 = 98,

        Unused1 = 99,
        Unused2 = 100,
        Unused3 = 101,
        Unused4 = 102,
        Unused5 = 103,
        Unused6 = 104,
        Unused7 = 105,
        Unused8 = 106,
        Unused9 = 107,
        Unused10 = 108,
        Unused11 = 109,
        Unused12 = 110,
        Unused13 = 111,
        Unused14 = 112,
        Unused15 = 113,
        Unused16 = 114,
        Unused17 = 115,
        Unused18 = 116,
        Unused19 = 117,
        Unused20 = 118,
        Unused21 = 119,
        Unused22 = 120,
        Unused23 = 121,
        Unused24 = 122,
        Unused25 = 123,
        Unused26 = 124,
        Unused27 = 125,
        Unused28 = 126,
        Unused29 = 127,

        LibraryError = 128,
        Ping = 129, // used for RTT calculation
        Pong = 130, // used for RTT calculation
        Connect = 131,
        ConnectResponse = 132,
        ConnectionEstablished = 133,
        Acknowledge = 134,
        Disconnect = 135,
        Discovery = 136,
        DiscoveryResponse = 137,
        NatPunchMessage = 138, // send between peers
        NatIntroduction = 139, // send to master server
        ExpandMTURequest = 140,
        ExpandMTUSuccess = 141,

        Special = 255// special case (extra header flags)
    }

    public enum SpecialMessageFlags : byte
    {
        Encrypted = 1,
        Compressed = 2,
    }

    public interface INetBuffer
    {
        long Length
        {
            get;
        }

        long Position
        {
            get;
        }

        long Capacity
        {
            get;
        }

        long Remaining
        {
            get;
        }

        bool ReadBoolean();

        byte ReadByte();

        int ReadInt32();

        long ReadInt64();

        void Write(bool value);

        void Write(byte value);

        void Write(int value);

        void Write(long value);

        void FreeBuffer();

        void SetBuffer(byte[] buffer);

        void SetBuffer(int offset, int length);

        void SetBuffer(ArraySegment<byte> seg);

        void SetBuffer(SocketAsyncEventArgs args);

        void SetBuffer(byte[] buffer, int offset, int length);
    }

    public interface INetMessage : INetBuffer
    {
        NetPeer Peer { get; }

        NetMessageType Type { get; }

        NetConnection Sender { get; }

        void PrepareToSend(SocketAsyncEventArgs args);
    }

    public interface INetSerializable
    {
        int AllocSize { get; }
        void OnSerialize(INetMessage message);
        void OnDeserialize(INetMessage message);
    }

    public interface INetIncomingMessage : INetMessage
    {

    }

    public interface INetOutgoingMessage : INetMessage
    {

    }
}