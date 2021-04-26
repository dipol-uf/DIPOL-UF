using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Complex = System.Numerics.Complex;

using static System.BitConverter;
using static FITS_CS.ExtendedBitConverter;

namespace FITS_CS
{
    [DebuggerDisplay(@"\{Header: {Header}, Value: {Value}, Comment: {Comment}\}")]
    [DataContract]
    public class FitsKey
    {
        public enum FitsKeyLayout : byte
        {
            Fixed = 1,
            //NonFixed = 2,
        }

        public const int KeySize = 80;
        public const int KeyHeaderSize = 8;
        public const int ReservedSize = 2;
        public const int StringQuotePos = 20;
        public const int NumericValueMaxLengthFixed = 20;

        public static FitsKey End => new FitsKey("END", FitsKeywordType.Comment, "");
        public static FitsKey Empty => new FitsKey();

        [field: DataMember]
        private byte[]? RawValue { get;}

        [field: DataMember]
        public string? Header
        {
            get;
        }
        [field: DataMember]
        public string Comment { get;  } = "";
        [field: DataMember]
        public string Value { get; } = "";
        [field: DataMember]
        public FitsKeywordType Type
        {
            get;
        }


        public byte[] Data => Encoding.ASCII.GetBytes(KeyString.ToArray());
        public string KeyString => $"{Header, -8}{Body}";
        public bool IsEmpty => string.IsNullOrWhiteSpace(Header) &&
                               string.IsNullOrWhiteSpace(Value) &&
                               string.IsNullOrWhiteSpace(Comment);
        public string Body
        {
            get
            {
                var isUsual = Type != FitsKeywordType.Blank && Type != FitsKeywordType.Comment;
                var body = string.Empty;
                if (!string.IsNullOrWhiteSpace(Value))
                {
                    if (isUsual)
                        body += "= ";
                    body += Value;
                }
                if (!string.IsNullOrWhiteSpace(Comment))
                    body += " / " + Comment;


                body = string.Format($"{{0, {(isUsual ? -1 : 1) * (KeySize - KeyHeaderSize)}}}", body)
                                 .Substring(0, KeySize - KeyHeaderSize);
                return body;
            }
        }
        
        public FitsKey(byte[] data, int offset = 0)
        {
            if (data == null)
                throw new ArgumentNullException($"{nameof(data)} is null");
            if (data.Length < KeySize + offset)
                throw new ArgumentException($"{nameof(data)} has wrong length");

            var keyString = new string(Encoding.ASCII.GetChars(data, offset, KeySize));

            int maxPos = NumericValueMaxLengthFixed;

            Header = keyString.Substring(0, KeyHeaderSize).Trim();
            var indexSlash = -1;
            var isInQuotes = false;
            for (var i = KeyHeaderSize; i < KeySize; i++)
                if (keyString[i] == '\'')
                    isInQuotes = !isInQuotes;
                else if (keyString[i] == '/' && !isInQuotes)
                {
                    indexSlash = i;
                    break;
                }
            Comment = indexSlash >= KeyHeaderSize ? keyString.Substring(indexSlash + 2).Trim() : "";
            if (keyString[KeyHeaderSize] == '=')
            {
                Value = indexSlash >= KeyHeaderSize
                    ? keyString.Substring(KeyHeaderSize + 2, indexSlash - KeyHeaderSize - 3)
                    : keyString.Substring(KeyHeaderSize + 2).TrimEnd();
                var trimVal = Value.Trim();

                if (string.IsNullOrWhiteSpace(trimVal))
                {
                    Type = FitsKeywordType.Blank;
                    RawValue = new byte[0];
                }
                else if (trimVal is "F" or "T")
                {
                    Type = FitsKeywordType.Logical;
                    RawValue = GetBytes(trimVal == "T");
                }
                else if (trimVal.Contains('\''))
                {
                    Type = FitsKeywordType.String;
                    var match = Regex.Match(trimVal, "'(.*)'").Groups[1].Value.Replace("''", "'");
                    RawValue = new byte[match.Length * sizeof(char)];
                    match.AsSpan().GetBytes(RawValue);
                }
                else if (Value.Length > maxPos + 1 && 
                         double.TryParse(Value.Substring(0, maxPos), 
                             NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var real) &&
                         double.TryParse(Value.Substring(maxPos), 
                             NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var img))
                {
                    Type = FitsKeywordType.Complex;
                    RawValue = new byte[sizeof(double) * 2];
                    new Complex(real, img).GetBytes(RawValue);
                }
                else if (int.TryParse(trimVal, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var intVal))
                {
                    Type = FitsKeywordType.Integer;
                    RawValue = GetBytes(intVal);
                }
                else if (double.TryParse(trimVal, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var dVal))
                {
                    Type = FitsKeywordType.Float;
                    RawValue = GetBytes((float)dVal);
                }
                else throw new ArgumentException("Keyword value is of unknown format.");
            }
            else
            {
                Value = keyString.AsSpan(KeyHeaderSize).Trim().ToString();
                RawValue = new byte[Value.Length * sizeof(char)];
                Value.AsSpan().GetBytes(RawValue);
                Type = FitsKeywordType.Comment;
            }

        }

        public FitsKey(string header, FitsKeywordType type, object value,
            string comment = "", 
            // ReSharper disable once UnusedParameter.Local
            FitsKeyLayout layout = FitsKeyLayout.Fixed)
        {
            if (header is null)
                throw new ArgumentNullException(nameof(header));

            if (header.Length > KeyHeaderSize)
                throw new ArgumentException($"\"{nameof(header)}\" is too long");


            Header = header.Trim();
            Type = type;

            var maxValuePos = NumericValueMaxLengthFixed;
            var dblDecPrecision = maxValuePos - 7;
            const string complexSep = "";
            var strQuotePos = StringQuotePos - 2 - ReservedSize - KeyHeaderSize;

            switch (type)
            {
                case FitsKeywordType.Logical:
                    if (value is bool bVal)
                    {
                        Value = string.Format($"{{0, {maxValuePos}}}", bVal ? 'T' : 'F');
                        RawValue = GetBytes(bVal);
                    }
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type {typeof(bool)}.");
                    break;
                case FitsKeywordType.Integer:
                    if (value is int iVal)
                    {
                        Value = string.Format($"{{0, {maxValuePos}}}", iVal);
                        RawValue = GetBytes(iVal);
                    }
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type {typeof(int)}.");
                    break;
                case FitsKeywordType.Float:
                    if (value is float fVal)
                    {
                        Value = string.Format($"{{0, {maxValuePos}}}", fVal.ToString("E8"));
                        RawValue = GetBytes(fVal);
                    }
                    else if (value is double dVal)
                    {
                        if (Math.Abs(dVal) >= 1e100 || Math.Abs(dVal) <= 1e-100)
                            throw new OverflowException($"{typeof(double)} with exponent" +
                                                        " greater than 99 or smaller than -99 " +
                                                        "cannot be represented in fixed mode.");
                        Value = string.Format($"{{0, {maxValuePos}}}", FormatDouble(dVal, dblDecPrecision));
                        RawValue = GetBytes((float)dVal);
                    }
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type" +
                                                     $" {typeof(float)} or {typeof(double)}.");
                    break;
                case FitsKeywordType.Complex:
                    if (value is Complex cVal)
                    {
                        if (Math.Abs(cVal.Real) >= 1e100 ||
                           Math.Abs(cVal.Real) <= 1e-100 ||
                           Math.Abs(cVal.Imaginary) >= 1e100 ||
                           Math.Abs(cVal.Imaginary) <= 1e-100)
                            throw new OverflowException($"{typeof(Complex)} with exponent" +
                                                        " greater than 99 or smaller than -99 " +
                                                        "cannot be represented in fixed mode.");
                        Value = string.Format($"{{0, {maxValuePos}}}{complexSep}{{1, {maxValuePos}}}",
                            FormatDouble(cVal.Real, dblDecPrecision),
                            FormatDouble(cVal.Imaginary, dblDecPrecision));
                        RawValue = cVal.GetBytes();

                    }
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type" +
                                                     $" {typeof(Complex)}.");
                    break;

                case FitsKeywordType.String:
                    if (value is string sVal)
                    {
                        var srcVal = sVal;
                        sVal = sVal.Replace("\'", "''").TrimEnd();
                        if (sVal.Length > KeySize - KeyHeaderSize - ReservedSize - 2)
                            throw new ArgumentException($"String \"{nameof(value)}\" is too long.");
                        Value = string.Format($"'{{0, {-strQuotePos}}}'", sVal);
                        if (Value.Length < maxValuePos)
                            Value += new string(' ', maxValuePos - Value.Length);

                        RawValue = srcVal.GetBytes();

                    }
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type" +
                                                     $" {typeof(string)}.");
                    break;
                case FitsKeywordType.Comment:
                    if (header != "COMMENT" && header != "HISTORY" && header != "END")
                        throw new ArgumentException($"\"{nameof(header)}\" should be either \"COMMENT\" or \"HISTORY\" or \"END\".");
                    if (value is string commVal)
                        Value = commVal.Substring(0, Math.Min(commVal.Length, KeySize - KeyHeaderSize));
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type {typeof(string)}.");
                    RawValue = Value.GetBytes();
                    break;
                case FitsKeywordType.Blank:
                    if (value is string blankVal)
                        Value = blankVal.Substring(0, Math.Min(blankVal.Length, KeySize - KeyHeaderSize));
                    else if (value is null)
                        Value = "";
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type {typeof(string)}.");
                    RawValue = Value.GetBytes();
                    break;
                default:
                    throw new NotSupportedException($"Unsupported \"{nameof(type)}\".");
            }

            if (!string.IsNullOrWhiteSpace(comment))
            {
                var remSpace = KeySize - KeyHeaderSize;
                if (type != FitsKeywordType.Blank && type != FitsKeywordType.Comment)
                    remSpace -= ReservedSize + Value.Length;

                if (remSpace > 0)
                    Comment = comment.Substring(0, Math.Min(comment.Length, remSpace));
            }

        }

        private FitsKey()
        { }

        /// <summary>
        /// Inefficient way to access strongly typed raw value.
        /// Relies on <code>dynamic</code>.
        /// </summary>
        /// <typeparam name="T">Type of the contained value</typeparam>
        /// <returns>Raw value cast to the type.</returns>
        [return:MaybeNull]
        public T GetValue<T>()
        {
            if (RawValue is null)
            {
                return default;
            }

            if (typeof(T) == typeof(bool)
                && Type == FitsKeywordType.Logical)
            {
                return Unsafe.As<byte, T>(ref RawValue[0]);
            }

            if (typeof(T) == typeof(int)
                && Type == FitsKeywordType.Integer)
            {
                return Unsafe.As<byte, T>(ref RawValue[0]);
            }

            if (typeof(T) == typeof(float)
                && Type == FitsKeywordType.Float)
            {
                return Unsafe.As<byte, T>(ref RawValue[0]);
            }
            
            if (typeof(T) == typeof(double)
                && Type == FitsKeywordType.Float)
            {
                var floatVal = Unsafe.As<byte, float>(ref RawValue[0]);
                var dblVal = (double) floatVal;
                return Unsafe.As<double, T>(ref dblVal);
            }
            
            if (typeof(T) == typeof(Complex)
                && Type == FitsKeywordType.Complex)
            {
                var cmplVal = ToComplex(RawValue);
                return Unsafe.As<Complex, T>(ref cmplVal);
            }

            if (typeof(T) == typeof(string)
                && Type is FitsKeywordType.Comment or FitsKeywordType.String)
            {
                var strVal = ExtendedBitConverter.ToString(RawValue);
                return Unsafe.As<string, T>(ref strVal);
            }


            throw new ArgumentException(
                "Generic type does not match the keyword type and conversion is impossible."
            );
        }

        public override string ToString() => KeyString;
        public override bool Equals(object? obj)
        {
            if (obj is FitsKey key)
                return Type.Equals(key.Type) &&
                       string.Equals(Header, key.Header) &&
                       Value.Equals(key.Value) &&
                       Comment.Equals(key.Comment);
            return false;
        }
        public override int GetHashCode()
            => KeyString.GetHashCode();

        /// <summary>
        /// Checks if data chunk has a valid FITS header
        /// </summary>
        /// <param name="data">Input array. Should be at least the size of one keyword.</param>
        /// <param name="offset">Optional offset. Allows to check arbitrary chunk from the array.</param>
        /// <returns>true if header is valid, false otherwise.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static bool IsFitsKey(byte[] data, int offset = 0)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length + offset < KeySize)
                throw new ArgumentException($"{nameof(data)} has wrong length");

            var strRep = new string(Encoding.ASCII.GetChars(data, offset, KeySize));

            var isKey = string.IsNullOrWhiteSpace(strRep) ||
                        strRep.StartsWith("HISTORY ") || 
                        strRep.StartsWith("COMMENT ") ||
                        strRep.StartsWith("END");

            isKey |= Regex.IsMatch(strRep, $@"^[A-Za-z\ \-_0-9]{{{KeyHeaderSize}}}=\ ");

            return isKey;
        }
        public static bool IsEmptyKey(byte[] data, int offset = 0)
        {
            return Enumerable.Range(offset, KeySize).All(i => data[i] == 0) ||
                Enumerable.Range(offset, KeySize).All(i => data[i] == (byte)' ');
        }

        public static FitsKey CreateComment(string text)
            => new FitsKey("COMMENT", FitsKeywordType.Comment, text);

        public static FitsKey CreateHistory(string text)
            => new FitsKey("HISTORY", FitsKeywordType.Comment, text);

        public static FitsKey CreateDate(string header, DateTime date, 
            string comment = "", string format = "yyyy/MM/dd HH:mm:ss.fff")
            => new FitsKey(header, FitsKeywordType.String, string.Format($"{{0:{format}}}", date), comment);

        [Obsolete("Use constructor instead")]
        public static FitsKey CreateNew(string header, FitsKeywordType type, object value,
            string comment = "", FitsKeyLayout layout = FitsKeyLayout.Fixed)
        {
            throw new NotSupportedException();
        }

        public static bool operator ==(FitsKey key1, FitsKey key2)
            => key1?.Equals(key2) ?? false;

        public static bool operator !=(FitsKey key1, FitsKey key2)
            => !(key1 == key2);

        private static string FormatDouble(double input, int decPlaces)
        {
            var format = $"E{decPlaces}";
            var str = input.ToString(format);
            return Regex.Replace(str, "([+-])[0-9]([0-9]{2})$", "$1$2");
        }

       
    }
}
