//
// © Copyright Henrik Ravn 2004
//
// Use, modification and distribution are subject to the Boost Software License, Version 1.0.
// (See accompanying file LICENSE_1_0.txt or copy at http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.Runtime.InteropServices;

namespace reWZ
{
    internal sealed class Inflater : IDisposable
    {
        [DllImport("ZLIB1.dll", CallingConvention=CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
        private static extern int inflateInit_(ref ZStream sz, string vs, int size);

        [DllImport("ZLIB1.dll", CallingConvention=CallingConvention.Cdecl)]
        private static extern int inflate(ref ZStream sz, int flush);

        [DllImport("ZLIB1.dll", CallingConvention=CallingConvention.Cdecl)]
        private static extern int inflateReset(ref ZStream sz);

        [DllImport("ZLIB1.dll", CallingConvention=CallingConvention.Cdecl)]
        private static extern int inflateEnd(ref ZStream sz);

        private ZStream _ztream;

        private bool _isDisposed;

        private const int KBufferSize = 16384;

        private readonly byte[] _outBuffer = new byte[KBufferSize];
        private readonly byte[] _inBuffer = new byte[KBufferSize];

        private GCHandle _hInput;
        private GCHandle _hOutput;

        public Inflater()
        {
            try
            {
                _hInput = GCHandle.Alloc(_inBuffer, GCHandleType.Pinned);
                _hOutput = GCHandle.Alloc(_outBuffer, GCHandleType.Pinned);
            }
            catch (Exception)
            {
                CleanUp(false);
                throw;
            }

            int retval = inflateInit_(ref _ztream, ZlibVersion(), Marshal.SizeOf(_ztream));
            if (retval != 0)
                throw new ZLibException(retval, "Could not initialize inflater");

            ResetOutput();
        }

        internal event DataAvailableHandler DataAvailable;

        private void OnDataAvailable()
        {
            if (_ztream.total_out <= 0) return;
            if (DataAvailable != null)
                DataAvailable( _outBuffer, 0, (int)_ztream.total_out);
            ResetOutput();
        }

        internal void Add(byte[] data)
        {
            Add(data,0,data.Length);
        }

        ~Inflater()
        {
            CleanUp(false);
        }

        public void Dispose()
        {
            CleanUp(true);
        }

        private void CleanUp(bool isDisposing)
        {
            if (_isDisposed) return;
            inflateEnd(ref _ztream);
            if (_hInput.IsAllocated)
                _hInput.Free();
            if (_hOutput.IsAllocated)
                _hOutput.Free();

            _isDisposed = true;
        }

        private void ResetOutput()
        {
            _ztream.total_out = 0;
            _ztream.avail_out = KBufferSize;
            _ztream.next_out = _hOutput.AddrOfPinnedObject();
        }

        internal void Add(byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException();
            if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException();
            if ((offset+count) > data.Length) throw new ArgumentException();

            int total = count;
            int inputIndex = offset;
            int err = 0;

            while (err >= 0 && inputIndex < total)
            {
                int count1 = Math.Min(total - inputIndex, KBufferSize);
                Array.Copy(data, inputIndex, _inBuffer,0, count1);
                _ztream.next_in = _hInput.AddrOfPinnedObject();
                _ztream.total_in = 0;
                _ztream.avail_in = (uint)count1;
                err = inflate(ref _ztream, (int)FlushTypes.None);
                if (err == 0)
                    while (_ztream.avail_out == 0)
                    {
                        OnDataAvailable();
                        err = inflate(ref _ztream, (int)FlushTypes.None);
                    }

                inputIndex += (int)_ztream.total_in;
            }
        }

        internal void Finish()
        {
            int err;
            do
            {
                err = inflate(ref _ztream, (int)FlushTypes.Finish);
                OnDataAvailable();
            }
            while (err == 0);
            inflateReset(ref _ztream);
            ResetOutput();
        }

        [DllImport("ZLIB1.dll", CallingConvention=CallingConvention.Cdecl)]
        private static extern string ZlibVersion();
    }

    internal enum FlushTypes
    {
        None = 0,  Finish = 4
    }

    [StructLayout(LayoutKind.Sequential, Pack=4, Size=0, CharSet=CharSet.Ansi)]
    internal struct ZStream
    {
        public IntPtr next_in;
        public uint avail_in;
        public uint total_in;

        public IntPtr next_out;
        public uint avail_out;
        public uint total_out;

        [MarshalAs(UnmanagedType.LPStr)]
        string msg;
        uint state;

        uint zalloc;
        uint zfree;
        uint opaque;

        int data_type;
        private uint adler;
        uint reserved;
    }

    internal sealed class ZLibException : ApplicationException
    {
        public ZLibException(int errorCode, string msg) : base(String.Format("ZLib error {0} {1}", errorCode, msg))
        {
        }
    }

    internal delegate void DataAvailableHandler(byte[] data, int startIndex, int count);
}
