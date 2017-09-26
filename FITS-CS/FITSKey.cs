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

        string data = null;

        public byte[] Data => Encoding.ASCII.GetBytes(data.ToArray());
        public string Extension
        {
            get;
            internal set;
        } = null;
        public string KeyString => data;
        public bool IsEmpty => String.IsNullOrWhiteSpace(data);
        public string Header => data.Substring(0, KeyHeaderSize).Trim();

        public bool IsExtension => !IsEmpty && String.IsNullOrWhiteSpace(Header);
        public FITSKey(byte[] data, int offset = 0)
        {
            if (data == null)
                throw new ArgumentNullException($"{nameof(data)} is null");
            if ((data.Length <= KeySize) || (offset + KeySize > data.Length))
                throw new ArgumentException($"{nameof(data)} has wrong length");

            this.data = new string(Encoding.ASCII.GetChars(data, offset, KeySize)); 
        }
        

        public override string ToString() => data;        

        public static bool IsFITSKey(byte[] data, int offset = 0)
        {
            if (data == null)
                throw new ArgumentNullException($"{nameof(data)} is null");
            if ((data.Length <= KeySize) || (offset + KeySize > data.Length))
                throw new ArgumentException($"{nameof(data)} has wrong length");

            return Encoding.ASCII.GetChars(data, offset, KeyHeaderSize)
                .Where(c => c != ' ')
                .All(c => Char.IsLetterOrDigit(c) || c == '-');
                
            
        }

        public static void JoinKeywords(params FITSUnit[] keyUnits)
        {
            Dictionary<string, FITSKey> result 
                = new Dictionary<string, FITSKey>(keyUnits.Length * FITSUnit.UnitSizeInBytes/KeySize);

            foreach (var keyUnit in keyUnits)
                if (keyUnit.TryGetKeys(out List<FITSKey> keys))
                    foreach (var key in keys)
                        result.Add("", key);

        }
    }
}
