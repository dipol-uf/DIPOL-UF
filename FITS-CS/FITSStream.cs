using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FITS_CS
{
    public class FITSStream : Stream
    {
        private Stream baseStream;

        public override bool CanRead => baseStream.CanRead;

        public override bool CanSeek => baseStream.CanSeek;

        public override bool CanWrite => baseStream.CanWrite;

        public override long Length => baseStream.Length;

        public override long Position
        {
            get => baseStream.Position;
            set => baseStream.Position = value;
        }

        public override void Flush()
        {
            baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => baseStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
            => baseStream.Seek(offset, origin);

        public override void SetLength(long value)
            => baseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
            => baseStream.Write(buffer, offset, count);
    }
}
