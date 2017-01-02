﻿using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ghost.Network.Buffers
{
    internal unsafe class NetBufferLE : INetBuffer
    {
        protected byte* m_end;
        protected byte* m_start;
        protected byte* m_offset;
        protected byte* m_length;
        protected int m_ref_count;
        protected GCHandle m_handle;
        protected byte m_bits_offset;
        protected BufferSegment m_segment;
        protected NetMemoryManager m_manager;
        protected ArraySegment<byte> m_buffer;

        public long Length
        {
            get
            {
                return m_length - m_start;
            }
        }

        public long Position
        {
            get
            {
                return m_offset - m_start;
            }
        }

        public long Capacity
        {
            get
            {
                return m_end - m_start;
            }
        }

        public long Remaining
        {
            get
            {
                return m_length - m_offset;
            }
        }

        public long LengthBits
        {
            get
            {
                return (m_length - m_start) << 3;
            }
        }

        public long PositionBits
        {
            get
            {
                return ((m_offset - m_start) << 3) + m_bits_offset;
            }
        }

        public long CapacityBits
        {
            get
            {
                return (m_end - m_start) << 3;
            }
        }

        public NetBufferLE(NetMemoryManager manager)
        {
            m_manager = manager;
        }

        public void Free()
        {
            if (m_handle.IsAllocated)
            {
                if (m_segment.IsAllocated)
                {
                    if (Interlocked.Decrement(ref m_ref_count) == 0)
                        m_segment.Free();
                    else return;
                }
                FreeBuffer();
                m_manager.Free(this);
            }
        }

        public void SetBuffer(byte[] buffer)
        {
            SetBuffer(buffer, 0, buffer.Length);
        }

        public void SetBuffer(BufferSegment segment)
        {
            if (!segment.IsAllocated)
                throw new ArgumentNullException(nameof(segment));
            SetBuffer(segment.Buffer, segment.Offset, segment.Length);
            m_ref_count = 1;
            m_segment = segment;
        }

        public void SetBuffer(SocketAsyncEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (args.Buffer != m_segment.Buffer)
                SetBuffer(args.Buffer, args.Offset, args.Count);
            if (args.BytesTransferred > 0)
                m_length = m_start + args.BytesTransferred;
        }

        public void SetBuffer(byte[] buffer, int offset, int length)
        {
            CheckBuffer(buffer, offset, length);
            if (m_segment.IsAllocated)
                m_segment.Free();
            if (m_handle.IsAllocated)
                FreeBuffer();
            m_handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            m_buffer = new ArraySegment<byte>(buffer, offset, length);
            m_start = (byte*)m_handle.AddrOfPinnedObject().ToPointer() + offset;
            m_end = m_start + length;
            m_length = m_start;
            m_offset = m_start;
            m_bits_offset = 0;
        }

        public bool ReadBoolean()
        {
            CheckRead(0);
            var value = ((*m_offset >> m_bits_offset) & 1) == 1;
            m_bits_offset = (byte)((m_bits_offset + 1) % (sizeof(byte) << 3));
            if (m_bits_offset == 0) m_offset++;
            return value;
        }

        public byte ReadByte()
        {
            CheckRead(sizeof(byte));
            byte value;
            if (m_bits_offset == 0)
                value = *m_offset;
            else
                value = (byte)(*(ushort*)m_offset >> m_bits_offset);
            m_offset += sizeof(byte);
            return value;
        }

        public short ReadInt16()
        {
            CheckRead(sizeof(short));
            short value;
            if (m_bits_offset == 0)
                value = *(short*)m_offset;
            else
                value = (short)((*(ushort*)m_offset | (ulong)m_offset[sizeof(ushort)] << (sizeof(ushort) << 3)) >> m_bits_offset);
            m_offset += sizeof(short);
            return value;
        }

        public ushort ReadUInt16()
        {
            CheckRead(sizeof(ushort));
            ushort value;
            if (m_bits_offset == 0)
                value = *(ushort*)m_offset;
            else
                value = (ushort)((*(ushort*)m_offset | (ulong)m_offset[sizeof(ushort)] << (sizeof(ushort) << 3)) >> m_bits_offset);
            m_offset += sizeof(ushort);
            return value;
        }

        public int ReadInt32()
        {
            CheckRead(sizeof(int));
            int value;
            if (m_bits_offset == 0)
                value = *(int*)m_offset;
            else
                value = (int)((*(uint*)m_offset | (ulong)m_offset[sizeof(uint)] << (sizeof(uint) << 3)) >> m_bits_offset);
            m_offset += sizeof(int);
            return value;
        }

        public long ReadInt64()
        {
            CheckRead(sizeof(long));
            long value;
            if (m_bits_offset == 0)
                value = *(long*)m_offset;
            else
                value = (long)((*(ulong*)m_offset >> m_bits_offset) | ((ulong)m_offset[sizeof(ulong)] << ((sizeof(ulong) << 3) - m_bits_offset)));
            m_offset += sizeof(long);
            return value;
        }

        public void Read(INetBuffer buffer, int bytes)
        {
            CheckRead(sizeof(byte) * bytes);
            unsafe
            {
                var myBuffer = (byte[])m_handle.Target;
                var myOffset = (int)(m_start - (byte*)m_handle.AddrOfPinnedObject().ToPointer());
                if (m_bits_offset == 0)
                    buffer.Write(myBuffer, myOffset, bytes);
                else throw new NotImplementedException();
            }
            m_offset += bytes;
        }

        public void Write(bool value)
        {
            CheckWrite(m_bits_offset != 0 ? 0 : 1);
            if (value)
                *m_offset |= (byte)(1 << m_bits_offset);
            else
                *m_offset &= (byte)~(1 << m_bits_offset);
            if (m_bits_offset == 0) m_length = m_offset + 1;
            m_bits_offset = (byte)((m_bits_offset + 1) % (sizeof(byte) << 3));
            if (m_bits_offset == 0)
            {
                m_offset++;
                if (m_offset > m_length) m_length = m_offset;
            }
        }

        public void Write(byte value)
        {
            CheckWrite(sizeof(byte));
            if (m_bits_offset == 0)
            {
                *m_offset = value;
                m_offset += sizeof(byte);
            }
            else
            {
                *(ushort*)m_offset = (ushort)((*(ushort*)m_offset & ~(byte.MaxValue << m_bits_offset)) | (value << m_bits_offset));
                m_offset += sizeof(byte);
            }
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
        }

        public void Write(short value)
        {
            CheckWrite(sizeof(short));
            if (m_bits_offset == 0)
            {
                *(short*)m_offset = value;
                m_offset += sizeof(short);
            }
            else
            {
                *(short*)m_offset = (short)((*m_offset & ~(byte.MaxValue << m_bits_offset)) | (value << m_bits_offset));
                m_offset += sizeof(short);
                *m_offset = (byte)((byte)(*m_offset & (byte.MaxValue << m_bits_offset)) | ((uint)value >> (sizeof(short) << 3) - m_bits_offset));
            }
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
        }

        public void Write(int value)
        {
            CheckWrite(sizeof(int));
            if (m_bits_offset == 0)
            {
                *(int*)m_offset = value;
                m_offset += sizeof(int);
            }
            else
            {
                *(int*)m_offset = (*m_offset & ~(byte.MaxValue << m_bits_offset)) | (value << m_bits_offset);
                m_offset += sizeof(int);
                *m_offset = (byte)((byte)(*m_offset & (byte.MaxValue << m_bits_offset)) | ((uint)value >> (sizeof(int) << 3) - m_bits_offset));
            }
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
        }

        public void Write(long value)
        {
            CheckWrite(sizeof(long));
            if (m_bits_offset == 0)
            {
                *(long*)m_offset = value;
                m_offset += sizeof(long);
            }
            else
            {
                *(long*)m_offset = (byte)(*m_offset & ~(byte.MaxValue << m_bits_offset)) | (value << m_bits_offset);
                m_offset += sizeof(long);
                *m_offset = (byte)((byte)(*m_offset & (byte.MaxValue << m_bits_offset)) | ((ulong)value >> (sizeof(long) << 3) - m_bits_offset));
            }
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
        }

        public void Write(byte[] buffer, int offset, int length)
        {
            CheckBuffer(buffer, offset, length);
            CheckWrite(sizeof(byte) * length);
            fixed (byte* src = &buffer[offset])
            {
                if (m_bits_offset == 0)
                    Buffer.MemoryCopy(src, m_offset, m_end - m_offset, length);
                else throw new NotImplementedException();
            }
            m_offset += length;
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CheckRead(int length)
        {
            if ((m_offset + length) > m_length)
                throw new InvalidOperationException("Read past buffer length");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CheckWrite(int length)
        {
            if ((m_offset + length) > m_end)
                DoExpand((int)((m_offset - m_start) + length));
        }

        private void DoExpand(int minSize)
        {
            var start = m_start;
            var segment = m_manager.Allocate(minSize);
            var handle = GCHandle.Alloc(segment.Buffer, GCHandleType.Pinned);
            Buffer.MemoryCopy(m_start, handle.AddrOfPinnedObject().ToPointer(), segment.Length, Length);         
            if (m_handle.IsAllocated) m_handle.Free();
            if (m_segment.IsAllocated) m_segment.Free();
            m_start = (byte*)handle.AddrOfPinnedObject().ToPointer() + segment.Offset;
            m_end = m_start + segment.Length;
            m_length = m_start + (m_length - start);
            m_offset = m_start + (m_offset - start);
            m_handle = handle;
            m_segment = segment;
        }

        private void FreeBuffer()
        {
            m_end = (byte*)0;
            m_start = (byte*)0;
            m_offset = (byte*)0;
            m_length = (byte*)0;
            m_bits_offset = 0;
            m_handle.Free();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void CheckBuffer(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0 || (offset + length) > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(length));
        }
    }
}