using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Complex = System.Numerics.Complex;

namespace FITS_CS
{
    public class FITSKey
    {
        public enum FITSKeyLayout : byte
        {
            Fixed = 1,
            NonFixed = 2,
        }

        public static readonly int KeySize = 80;
        public static readonly int KeyHeaderSize = 8;
        public static readonly int LastValueColumnFixed = 29;
        public static readonly int NumericValueMaxLengthFixed = 20;
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
            Body = this.data.Substring(KeyHeaderSize);
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
        private FITSKey()
        { }

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

        /// <summary>
        /// Creates a new custom FITS keyword.
        /// </summary>
        /// <param name="header">Header, char string of max 8 symbols.</param>
        /// <param name="type">Keyword data type.</param>
        /// <param name="value">Actual keyword value. Should agree with type. String values CAN BE truncated.</param>
        /// <param name="comment">An optional comment. Comments can be truncated (or removed) to fit in FITS keyword size limit of 80 chars.</param>
        /// <param name="layout">Indicates which layout to use.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <returns>A new isntance of FITS keyword</returns>
        public static FITSKey CreateNew(
            string header, FITSKeywordType type, object value, 
            string comment = "", FITSKeyLayout layout = FITSKeyLayout.Fixed)
        {
            // Throw if no header
            if (string.IsNullOrWhiteSpace(header))
                throw new ArgumentNullException($"{nameof(header)} cannot be null/empty string (provided '{header}'),");
            // Throws of header is too large
            if (header.Length > KeyHeaderSize)
                throw new ArgumentException($"Header's length ({header.Length}) is too large (max {KeyHeaderSize}).");

            // Instance of constructed keyword.
            FITSKey key = new FITSKey();
            // String representation of keyword
            StringBuilder result = new StringBuilder(KeySize);
            // Initialize with blanks
            result.Append(' ', KeySize);
            // Right-justified header
            result.Insert(0, String.Format($"{{0, -{KeyHeaderSize}}}", header));
            key.Header = header;
            key.Type = type;
            key.rawValue = value;

            // If keyword is of value type
            if (type != FITSKeywordType.Comment | type != FITSKeywordType.Blank)
            {
                // Header/content separator
                result.Insert(KeyHeaderSize, "= ");
                // Throw is no value provided
                if (value == null)
                    throw new ArgumentNullException($"Key requires a value ({type}), but non provided.");
            }
            // If layout is fixed (old)
            if (layout == FITSKeyLayout.Fixed)
            {
                // Last index of data synbol. Used to find position of comment section.
                int lastIndex = 0;

                // Switch keyword type
                switch (type)
                {
                    case FITSKeywordType.Logical:
                        if (value is bool logicalValue)
                        {
                            key.Value = logicalValue ? "T" : "F";
                            // Puts T or F in 29 char (30th column)
                            result.Insert(LastValueColumnFixed, key.Value);
                            // Index of last symbol of value string
                            lastIndex = LastValueColumnFixed;
                        }
                        else
                            throw new ArgumentException($"{type} key requires {typeof(bool)} value, but caller provided {value.GetType()}.");
                        break;

                    case FITSKeywordType.Integer:
                        if (value is int integerValue)
                        {
                            key.Value = integerValue.ToString();
                            // Inserts right-justivied int up to 3-th column
                            result.Insert(KeyHeaderSize + 2, String.Format($"{{0, {NumericValueMaxLengthFixed}}}", integerValue));
                            // Index of last symbol of value string
                            lastIndex = LastValueColumnFixed;
                        }
                        else
                            throw new ArgumentException($"{type} key requires {typeof(int)} value, but caller provided {value.GetType()}.");
                        break;

                    case FITSKeywordType.Float:
                        // If value is double
                        if (value is double doubleValue)
                            key.Value = String.Format($"{{0, {NumericValueMaxLengthFixed}: 0.{new string('0', NumericValueMaxLengthFixed - 7)}E+000}}", doubleValue);
                        // If value is float
                        else if (value is float floatValue)
                            key.Value = String.Format($"{{0, {NumericValueMaxLengthFixed}: 0.{new string('0', NumericValueMaxLengthFixed - 6)}E+00}}", floatValue);
                        else
                            throw new ArgumentException($"{type} key requires {typeof(double)} or {typeof(float)} value, but caller provided {value.GetType()}.");

                        result.Insert(KeyHeaderSize + 1, key.Value);
                        key.Value = key.Value.Trim();
                        // Index of last symbol of value string
                        lastIndex = LastValueColumnFixed;
                        break;

                    case FITSKeywordType.String:
                        if (value is string stringValue)
                        {
                            // Replaces single quotes ' with double '', adds preceding '
                            stringValue = ('\'' + stringValue.Replace("'", "''"));
                            // Truncates string
                            if (stringValue.Length > KeySize - KeyHeaderSize - 3)
                                stringValue = stringValue.Substring(0, KeySize - KeyHeaderSize - 3);
                            // Insert string value with trailing '
                            result.Insert(KeyHeaderSize + 2,
                                stringValue + '\'');

                            key.Value = stringValue;
                            // Last index depends on the actual string value length
                            lastIndex = Math.Max(LastValueColumnFixed, stringValue.Length + KeyHeaderSize + 2);
                        }
                        else
                            throw new ArgumentException($"{type} key requires {typeof(string)} value, but caller provided {value.GetType()}.");

                        break;

                    case FITSKeywordType.Complex:
                        // Complex is represented as two subsequent floats
                        if (value is Complex complexValue)
                            key.Value = String.Format($"{{0, {NumericValueMaxLengthFixed}: 0.{new string('0', NumericValueMaxLengthFixed - 8)}E+000}}" +
                                $"{{1, {NumericValueMaxLengthFixed}: 0.{new string('0', NumericValueMaxLengthFixed - 8)}E+000}}", complexValue.Real, complexValue.Imaginary);
                        else
                            throw new ArgumentException($"{type} key requires {typeof(Complex)} value, but caller provided {value.GetType()}.");

                        result.Insert(KeyHeaderSize + 2, key.Value);
                        key.Value = key.Value.Trim();
                        // Last index depends on 2 times size of numerical value (20 symbols)
                        lastIndex = KeyHeaderSize + 1 + 2 * NumericValueMaxLengthFixed;
                        break;
                }

                // If comment is not null
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    var commLength = Math.Max(KeySize - lastIndex - 4, 0);
                    // Comment delimeter
                    result.Insert(lastIndex + 2, "\\ ");
                    // Truncated comment
                    key.Comment = comment.Substring(0, commLength);
                    // Inserts comment after comment delimeter
                    result.Insert(lastIndex + 4, key.Comment);
                }
            }
            else
                throw new NotImplementedException();

            // Ensures keyword representation can be fitted into 80 symbols string 
            if (result.Length > KeySize)
                result = result.Remove(KeySize, result.Length - KeySize);

            // Assigns constructed string 
            key.data = result.ToString();
            // And body
            key.Body = key.data.Substring(KeyHeaderSize);

            return key;
        }
    }
}
