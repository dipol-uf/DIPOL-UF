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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
            => _baseStream.Flush();
        

        public override int Read(byte[] buffer, int offset, int count)
            => _baseStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
            => _baseStream.Seek(offset, origin);

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => _baseStream.Write(buffer, offset, count);

        public void WriteUnit(FitsUnit unit)
        {
            Write(unit._data, 0, FitsUnit.UnitSizeInBytes);
            Flush();
        }

        public async Task WriteUnitAsync(FitsUnit unit, CancellationToken token = default)
        {
            await WriteAsync(unit._data, 0, unit._data.Length, token);
            await FlushAsync(token);
        }

        public FitsStream(Stream str)
            =>  _baseStream = str ?? throw new ArgumentNullException($"{nameof(str)} is null");
                    
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;
            if (!disposing) return;

            _baseStream.Dispose();
            IsDisposed = true;
        }

        public override void Close() => Dispose();

        public FitsUnit ReadUnit()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Stream is already disposed.");
            if (!CanRead)
                throw new NotSupportedException("Stream does not support reading.");

            var buffer = new byte[FitsUnit.UnitSizeInBytes];
            
                if (CanSeek && Position + FitsUnit.UnitSizeInBytes > Length)
                    throw new ArgumentException("Stream ended");
                _baseStream.Read(buffer, 0, FitsUnit.UnitSizeInBytes);
                return new FitsUnit(buffer);
            
        }

        public bool TryReadUnit(
            [MaybeNullWhen(false)]out FitsUnit unit
            )
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

        public static void WriteImage(Image image, FitsImageType type, Stream stream, IEnumerable<FitsKey>? extraKeys = null)
        {
            var keys = new List<FitsKey>
            {
                new("SIMPLE", FitsKeywordType.Logical, true),
                new("BITPIX", FitsKeywordType.Integer, (int)(short)type),
                new("NAXIS", FitsKeywordType.Integer, 2),
                new("NAXIS1", FitsKeywordType.Integer, image.Width),
                new("NAXIS2", FitsKeywordType.Integer, image.Height)
            };

            if (extraKeys != null)
            {
                keys.AddRange(extraKeys);
            }

            keys.Add(FitsKey.End);

            var keyUnits = FitsUnit.GenerateFromKeywords(keys.ToArray());
            var dataUnits = FitsUnit.GenerateFromDataArray(image.ByteView(), type);

            var str = new FitsStream(stream);
            foreach (var unit in keyUnits)
            {
                str.WriteUnit(unit);
            }

            foreach (var unit in dataUnits)
            {
                str.WriteUnit(unit);
            }
        }

        public static void WriteImage(Image image, FitsImageType type, string path, IEnumerable<FitsKey>? extraKeys = null)
        {
            using var str = new FileStream(path, FileMode.Create, FileAccess.Write);
            WriteImage(image, type, str, extraKeys);
        }

        public static async Task WriteImageAsync(
            Image image, FitsImageType type, Stream stream,
            IEnumerable<FitsKey>? extraKeys = null, CancellationToken token = default
        )
        {
            // await Task.Run(() =>
            // {
            var keys = new List<FitsKey>
            {
                new("SIMPLE", FitsKeywordType.Logical, true),
                new("BITPIX", FitsKeywordType.Integer, (int) (short) type),
                new("NAXIS", FitsKeywordType.Integer, 2),
                new("NAXIS1", FitsKeywordType.Integer, image.Width),
                new("NAXIS2", FitsKeywordType.Integer, image.Height)
            };

            if (extraKeys != null)
                keys.AddRange(extraKeys);

            keys.Add(FitsKey.End);

            List<FitsUnit> keyUnits = FitsUnit.GenerateFromKeywords(keys.ToArray());
            List<FitsUnit> dataUnits = FitsUnit.GenerateFromDataArray(image.ByteView(), type);
            // }, token);


            var str = new FitsStream(stream);
            foreach (var unit in keyUnits)
            {
                await str.WriteUnitAsync(unit, token);
            }

            foreach (var unit in dataUnits)
            {
                await str.WriteUnitAsync(unit, token);
            }
        }

        public static async Task WriteImageAsync(Image image, FitsImageType type, string path,
                                                 IEnumerable<FitsKey>? extraKeys = null, CancellationToken token = default)
        {
            using var str = new FileStream(path, FileMode.Create, FileAccess.Write);
            await WriteImageAsync(image, type, str, extraKeys, token);
        }


        public static Image ReadImage(Stream stream, out List<FitsKey> keywords)
        {
            var units = new List<FitsUnit>(2);

            var str = new FitsStream(stream);
            while (str.TryReadUnit(out var unit))
                units.Add(unit);

            keywords = new List<FitsKey>(6);

            foreach (var keywordUnit in units.TakeWhile(u => u.IsKeywords))
                if (keywordUnit.TryGetKeys(out var keys))
                    keywords.AddRange(keys);

            keywords = keywords.Where(k => !k.IsEmpty).ToList();

            var type = (FitsImageType)(keywords.FirstOrDefault(k => k.Header == "BITPIX")?.GetValue<int>()
                                        ?? throw new FormatException(
                                                "Fits data has no required keyword \"BITPIX\"."));
            var width = keywords.FirstOrDefault(k => k.Header == "NAXIS1")?.GetValue<int>()
                        ?? throw new FormatException(
                            "Fits data has no required keyword \"NAXIS1\".");
            var height = keywords.FirstOrDefault(k => k.Header == "NAXIS2")?.GetValue<int>()
                         ?? throw new FormatException(
                             "Fits data has no required keyword \"NAXIS2\".");

            Array GetData<T>() where T : unmanaged
            {

                var data = new T[width * height];
                var pos = 0;
                var size = Unsafe.SizeOf<T>();
                var n = FitsUnit.UnitSizeInBytes / size;
                T[] buffer = new T[n];
                Span<T> bufferView = buffer.AsSpan();
                foreach (var dataUnit in units.SkipWhile(u => u.IsKeywords))
                {
                    dataUnit.GetData(bufferView);
                    var len = Math.Min(n, data.Length - pos);
                    bufferView.Slice(0, len).CopyTo(data.AsSpan(pos, len));
                    // Array.Copy(buffer, 0, data, pos, Math.Min(n, data.Length - pos));
                    pos += len;
                }

                return data;
            }

            return type switch
            {
                FitsImageType.UInt8 => new AllocatedImage(GetData<byte>(), width, height),
                FitsImageType.Int16 => new AllocatedImage(GetData<short>(), width, height),
                FitsImageType.Int32 => new AllocatedImage(GetData<int>(), width, height),
                FitsImageType.Single => new AllocatedImage(GetData<float>(), width, height),
                FitsImageType.Double => new AllocatedImage(GetData<double>(), width, height),
                _ => throw new NotSupportedException($"Fits image of type {type} is not supported.")
            };
        }

        public static Image ReadImage(string path, out List<FitsKey> keywords)
        {
            // This needed to allow multiple simultaneous reads
            using var str = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return ReadImage(str, out keywords);
        }
        
    }
}
