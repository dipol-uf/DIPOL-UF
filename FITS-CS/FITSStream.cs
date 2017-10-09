using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Image = ImageDisplayLib.Image;

namespace FITS_CS
{
    public class FITSStream : Stream, IDisposable
    {
        private Stream baseStream;

        private int bytesConsumed = 0;

        public bool IsDisposed
        {
            get;
            private set;
        } = false;

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

        public void WriteUnit(FITSUnit unit)
            => Write(unit.Data, 0, FITSUnit.UnitSizeInBytes);

        public FITSStream(Stream str)
            =>  baseStream = str ?? throw new ArgumentNullException($"{nameof(str)} is null");
                    

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
                if (baseStream != null)
                {
                    baseStream.Close();
                    baseStream.Dispose();
                    IsDisposed = true;
                }
                        
        }

        public override void Close() => Dispose();

        public FITSUnit ReadUnit()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Stream is already disposed.");
            if (!CanRead)
                throw new NotSupportedException("Stream does not support reading.");

            byte[] buffer = new byte[FITSUnit.UnitSizeInBytes];
            try
            {
                if (CanSeek && Position + FITSUnit.UnitSizeInBytes > Length)
                    throw new ArgumentException("Stream ended");
                baseStream.Read(buffer, 0, FITSUnit.UnitSizeInBytes);
                bytesConsumed += FITSUnit.UnitSizeInBytes;
                return new FITSUnit(buffer);
            }
            catch (ArgumentException ae)
            {
                throw new EndOfStreamException("Stream end was reached.", ae);
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new IOException("An unexpected error happened while reading the stream.", e);
            }
        }

        public bool TryReadUnit(out FITSUnit unit)
        {
            unit = null;
            try
            {
                unit = ReadUnit();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void WriteImage(Image image, FITSImageType type, string path)
        {

            List<FITSKey> keys = new List<FITSKey>
            {
                FITSKey.CreateNew("SIMPLE", FITSKeywordType.Logical, true),
                FITSKey.CreateNew("BITPIX", FITSKeywordType.Integer, (int)Math.Abs((short)type)),
                FITSKey.CreateNew("NAXIS", FITSKeywordType.Integer, 2),
                FITSKey.CreateNew("NAXIS1", FITSKeywordType.Integer, image.Width),
                FITSKey.CreateNew("NAXIS2", FITSKeywordType.Integer, image.Height),
                FITSKey.CreateNew("NAXIS", FITSKeywordType.Integer, 2),
                FITSKey.CreateNew("END", FITSKeywordType.Blank, null)
            };

            var keyUnits = FITSUnit.GenerateFromKeywords(keys.ToArray());
            var dataUnits = FITSUnit.GenerateFromArray(image.GetBytes(), type);

            using (var str = new FITSStream(new FileStream(path, FileMode.OpenOrCreate)))
            {
                foreach (var unit in keyUnits)
                    str.WriteUnit(unit);
                foreach (var unit in dataUnits)
                    str.WriteUnit(unit);
            }
        }
    }
}
