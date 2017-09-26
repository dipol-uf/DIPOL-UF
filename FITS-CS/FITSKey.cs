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

        private string data = "";
        private object rawValue = null;

        public byte[] Data => Encoding.ASCII.GetBytes(data.ToArray());
        public string Extension
        {
            get;
            internal set;
        } = null;
        public string KeyString => data;
        public bool IsEmpty => String.IsNullOrWhiteSpace(data);
        public string Header
        {
            get;
            private set;
        } = null;
        public string Body
        {
            get;
            private set;
        }
        public string Comment
        {
            get;
            private set;
        } = null;
        public string Value
        {
            get;
            private set;
        }
        public FITSKeywordType Type
        {
            get;
            private set;
        }

        public bool IsExtension => !IsEmpty && String.IsNullOrWhiteSpace(Header);
        public FITSKey(byte[] data, int offset = 0)
        {
            if (data == null)
                throw new ArgumentNullException($"{nameof(data)} is null");
            if ((data.Length <= KeySize) || (offset + KeySize > data.Length))
                throw new ArgumentException($"{nameof(data)} has wrong length");

            this.data = new string(Encoding.ASCII.GetChars(data, offset, KeySize));

            Header = this.data.Substring(0, KeyHeaderSize).Trim();
            Body = this.data.Substring(KeyHeaderSize).Trim();
            int indexSlash = -1;
            bool isInQuotes = false;
            for (int i = KeyHeaderSize; i < KeySize; i++)
                if (this.data[i] == '\'')
                    isInQuotes = !isInQuotes;
                else if (this.data[i] == '/' && !isInQuotes)
                {
                    indexSlash = i;
                    break;
                }
            Comment = indexSlash >= KeyHeaderSize ? this.data.Substring(indexSlash + 1) : "";
            if (this.data[KeyHeaderSize] == '=')
            {
                Value = indexSlash >= KeyHeaderSize
                    ? this.data.Substring(KeyHeaderSize + 1, indexSlash - KeyHeaderSize - 1)
                    : this.data.Substring(KeyHeaderSize + 1);
                var trimVal = Value.Trim();

                if (String.IsNullOrWhiteSpace(trimVal))
                {
                    Type = FITSKeywordType.Blank;
                    rawValue = null;
                }
                else if (trimVal == "F" || trimVal == "T")
                {
                    Type = FITSKeywordType.Logical;
                    rawValue = trimVal == "T";
                }
                else if (trimVal.Contains('\''))
                {
                    Type = FITSKeywordType.String;
                    rawValue = trimVal.TrimStart('\'').TrimEnd('\'').Replace("''", "'");
                }
                else if (trimVal.Contains(":"))
                {
                    Type = FITSKeywordType.Complex;
                    double[] split = trimVal
                        .Split(':')
                        .Select(s => double.Parse(s, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo))
                        .ToArray();
                    rawValue = new System.Numerics.Complex(split[0], split[1]);
                }
                else if (int.TryParse(trimVal, out int intVal))
                {
                    Type = FITSKeywordType.Integer;
                    rawValue = intVal;
                }
                else if (Double.TryParse(trimVal, out double dVal))
                {
                    Type = FITSKeywordType.Float;
                    rawValue = dVal;
                }
                else throw new ArgumentException("Keyword value is of unknown format.");
            }
            else
            {
                Comment = this.data.Substring(KeyHeaderSize + 1).Trim();
                Value = "";
                if (String.IsNullOrWhiteSpace(Comment))
                    Type = FITSKeywordType.Blank;
                else
                    Type = FITSKeywordType.Comment;
                rawValue = null;
            }
        }

        public T GetValue<T>()
        {
            dynamic ret = default(T);
            if (typeof(T) == typeof(bool) && Type == FITSKeywordType.Logical)
                ret = (bool)rawValue;
            else if (typeof(T) == typeof(string) && Type == FITSKeywordType.String)
                ret = (string)rawValue;
            else if (typeof(T) == typeof(int) && Type == FITSKeywordType.Integer)
                ret = (int)rawValue;
            else if (typeof(T) == typeof(double) && Type == FITSKeywordType.Float)
                ret = (double)rawValue;
            else if (typeof(T) == typeof(System.Numerics.Complex) && Type == FITSKeywordType.Complex)
                ret = (System.Numerics.Complex)rawValue;
            else throw new TypeAccessException($"Illegal combination of {Type} and {typeof(T)}.");
            return ret;
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

        public static IEnumerable<FITSKey> JoinKeywords(params FITSUnit[] keyUnits)
        {
            foreach (var keyUnit in keyUnits)
                if (keyUnit.TryGetKeys(out List<FITSKey> keys))
                    foreach (var key in keys)
                        yield return key;
        }
    }
}
