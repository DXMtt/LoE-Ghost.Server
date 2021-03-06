﻿using Ghost.Server.Utilities;
using PNet;
using PNetR;
using System;
using System.Numerics;

namespace Ghost.Server.Objects.Managers
{
    public abstract class MotionState
    {
        public abstract int Priority
        {
            get; set;
        }
    }

    public class MotionManager : NetworkManager<MovableObject>
    {
        private struct SyncEntry : INetSerializable
        {
            public double Time;
            public Vector3 Position;
            public Vector3 Rotation;
            public bool FullRotation;

            public int AllocSize
            {
                get
                {
                    return (FullRotation ? 3 : 1) + 10;
                }
            }

            public override string ToString()
            {
                return $"SyncEntry[{Time:0000.000000}] <{Position.X:0000.000}, {Position.Y:0000.000}, {Position.Z:0000.000}> <{Rotation.Y:00.000}>";
            }

            public void OnSerialize(NetMessage message)
            {
                message.WriteFixedTime(Time, false);
                message.WritePosition(Position);
                message.WriteRotation(Rotation, FullRotation);
            }

            public void OnDeserialize(NetMessage message)
            {
                Time = message.ReadFixedTime(false);
                Position = message.ReadVector3();
                Rotation = message.ReadRotation(ref FullRotation);
            }
        }

        protected const int LockedFlag = 0x00000001;
        protected const int RunningFlag = 0x00000002;
        protected const int StreamingFlag = 0x00000004;
        protected const int ReciveingFlag = 0x00000008;

        private double m_time;
        private SyncEntry m_sync;
        private SyncEntry m_last;

        public bool Locked
        {
            get
            {
                return (m_state & LockedFlag) != 0;
            }
        }

        public bool Running
        {
            get
            {
                return (m_state & RunningFlag) != 0;
            }
        }

        public bool Streaming
        {
            get
            {
                return (m_state & StreamingFlag) != 0;
            }
            set
            {
                m_state = value ? m_state | StreamingFlag : m_state & ~StreamingFlag;
            }
        }

        public bool Reciveing
        {
            get
            {
                return (m_state & ReciveingFlag) != 0;
            }
            set
            {
                m_state = value ? m_state | ReciveingFlag : m_state & ~ReciveingFlag;
            }
        }

        public MotionManager()
            : base()
        {
            m_state |= StreamingFlag | ReciveingFlag;
        }

        public void SendStream()
        {
            m_time = PNet.Utilities.Now * 1.025;
            var stream = m_view.CreateStream(m_sync.AllocSize);
            var sync = default(SyncEntry);
            sync.Time = m_time;
            sync.Position = m_owner.Position;
            sync.Rotation = m_owner.Rotation;
            sync.OnSerialize(stream);
            m_view.SendStream(stream);
        }

        #region Events Handlers
        private void View_ReceivedStream(NetMessage message, Player player)
        {
            if ((m_state & ReciveingFlag) != 0)
            {
                SyncEntry sync = default(SyncEntry), last = m_last;
                sync.OnDeserialize(message);
                if (m_time > sync.Time)
                {
                    sync.Position = m_owner.Position + (sync.Position - last.Position);
                    sync.Rotation = m_owner.Rotation + (sync.Rotation - last.Rotation);
                }
                m_last = sync;
                m_owner.UpdateLocation(sync.Position, sync.Rotation);
            }
        }
        #endregion        
        #region Overridden Methods
        protected override void OnViewCreated()
        {
            base.OnViewCreated();
            m_view.ReceivedStream += View_ReceivedStream;
        }
        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            if ((m_state & (LockedFlag | StreamingFlag)) == StreamingFlag)
                SendStream();
        }
        #endregion
    }
}