using System;
using System.IO;

namespace NugetVendor.Tests
{
    class InMemoryCallbackStringStream : Stream
    {
        private Action<byte[]> _callback;
        private MemoryStream _stream;

        public InMemoryCallbackStringStream(Action<byte[]> callback)
        {
            _callback = callback;
            _stream = new MemoryStream();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _callback?.Invoke(_stream?.GetBuffer());
                _stream?.Dispose();

                _callback = null;
                _stream = null;
            }
            
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            _callback?.Invoke(_stream.GetBuffer());
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }
    }
}