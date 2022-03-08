using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DipolImage;
using FITS_CS;
using NUnit.Framework;

namespace FitsTests
{
    [TestFixture]
    public class KeyTests
    {
        [Test]
        public async Task Test_CorrectBasicKeys()
        {
            var image = new AllocatedImage(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 4, 2);
            
            #if NETCOREAPP2_0_OR_GREATER
            await
            #endif
            using var mem = new MemoryStream(FitsUnit.UnitSizeInBytes * 2);
            
            await FitsStream.WriteImageAsync(image, FitsImageType.Int32, mem);
            mem.Seek(0, SeekOrigin.Begin);
            var _ = FitsStream.ReadImage(mem, out List<FitsKey> keys);
            CollectionAssert.AreEqual(
                new[]
                {
                    new FitsKey("SIMPLE", FitsKeywordType.Logical, true),
                    new FitsKey("BITPIX", FitsKeywordType.Integer, sizeof(int) * 8),
                    new FitsKey("NAXIS", FitsKeywordType.Integer, 2),
                    new FitsKey("NAXIS1", FitsKeywordType.Integer, 4),
                    new FitsKey("NAXIS2", FitsKeywordType.Integer, 2),
                    FitsKey.End
                },
                keys
            );
        }
    }
}