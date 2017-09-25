using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITS_CS
{
    public class FITSKey
    {
        public static readonly int KeySize = 80;
        public static readonly int KeyHeaderSize = 8;

        byte[] array = new byte[FITSKey.KeySize];

        byte[] Data => array;

        public FITSKey(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException($"{nameof(data)} is null");
            if (data.Length != KeySize)
                throw new ArgumentException($"{nameof(data)} has wrong length");

            Array.Copy(data, array, data.Length);
        }

        public static char[] IsFITSKey(byte[] data, int offset = 0)
        {
            if (data == null)
                throw new ArgumentNullException($"{nameof(data)} is null");
            if (data.Length <= KeySize)
                throw new ArgumentException($"{nameof(data)} has wrong length");
            if(offset + KeySize > data.Length)
                throw new ArgumentException($"{nameof(data)} has wrong length");


            return data
                .Skip(offset)
                .Take(KeyHeaderSize)
                .Select(b => (char)b)
                .Where(c => c!= ' ')
                .ToArray();               
            
        }
    }
}
