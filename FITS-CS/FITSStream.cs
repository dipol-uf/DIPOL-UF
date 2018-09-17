//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using DipolImage;


namespace FITS_CS
{
    public class FitsStream : Stream, IDisposable
    {
        private readonly Stream _baseStream;

        public bool IsDisposed
        {
            get;
            private set;
        }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => _baseStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
            => _baseStream.Seek(offset, origin);

        public override void SetLength(long value)
            => _baseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
            => _baseStream.Write(buffer, offset, count);

        public void WriteUnit(FitsUnit unit)
            => Write(unit.Data, 0, FitsUnit.UnitSizeInBytes);

        public FitsStream(Stream str)
            =>  _baseStream = str ?? throw new ArgumentNullException($"{nameof(str)} is null");
                    

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
                if (_baseStream != null)
                {
                    _baseStream.Close();
                    _baseStream.Dispose();
                    IsDisposed = true;
                }
                        
        }

        public override void Close() => Dispose();

        public FitsUnit ReadUnit()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Stream is already disposed.");
            if (!CanRead)
                throw new NotSupportedException("Stream does not support reading.");

            var buffer = new byte[FitsUnit.UnitSizeInBytes];
            try
            {
                if (CanSeek && Position + FitsUnit.UnitSizeInBytes > Length)
                    throw new ArgumentException("Stream ended");
                _baseStream.Read(buffer, 0, FitsUnit.UnitSizeInBytes);
                return new FitsUnit(buffer);
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

        public bool TryReadUnit(out FitsUnit unit)
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

        public static void WriteImage(Image image, FITSImageType type, string path, List<FITSKey> extraKeys = null)
        {

            var keys = new List<FITSKey>
            {
                FITSKey.CreateNew("SIMPLE", FitsKeywordType.Logical, true),
                FITSKey.CreateNew("BITPIX", FitsKeywordType.Integer, (int)(short)type),
                FITSKey.CreateNew("NAXIS", FitsKeywordType.Integer, 2),
                FITSKey.CreateNew("NAXIS1", FitsKeywordType.Integer, image.Width),
                FITSKey.CreateNew("NAXIS2", FitsKeywordType.Integer, image.Height),
                FITSKey.CreateNew("NAXIS", FitsKeywordType.Integer, 2)
            };

            if(extraKeys != null)
                keys.AddRange(extraKeys);

            keys.Add(FITSKey.CreateNew("END", FitsKeywordType.Blank, null));

            var keyUnits = FitsUnit.GenerateFromKeywords(keys.ToArray());
            var dataUnits = FitsUnit.GenerateFromArray(image.GetBytes(), type);

            using (var str = new FitsStream(new FileStream(path, FileMode.Create)))
            {
                foreach (var unit in keyUnits)
                    str.WriteUnit(unit);
                foreach (var unit in dataUnits)
                    str.WriteUnit(unit);
            }
        }

        public static void ReadImage(string path)
        {

        }
    }
}
