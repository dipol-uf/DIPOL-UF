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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Complex = System.Numerics.Complex;

namespace FITS_CS
{
    [DebuggerDisplay(@"\{Header: {Header}, Value: {Value}, Comment: {Comment}\}")]
    public class FitsKey
    {
        public enum FitsKeyLayout : byte
        {
            Fixed = 1,
            //NonFixed = 2,
        }

        public static readonly int KeySize = 80;
        public static readonly int KeyHeaderSize = 8;
        public static readonly int ReservedSize = 2;
        public static readonly int LastValueColumnFixed = 29;
        public static readonly int StringQuotePos = 20;
        public static readonly int NumericValueMaxLengthFixed = 20;

        public static FitsKey Empty => new FitsKey();

        public object RawValue { get; private set; }

        public byte[] Data => Encoding.ASCII.GetBytes(KeyString.ToArray());
        public string Extension
        {
            get;
            internal set;
        } = null;

        public string KeyString => $"{Header, -8}{Body}";
        public bool IsEmpty => string.IsNullOrWhiteSpace(KeyString) ;
        public string Header
        {
            get;
            private set;
        }

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

        public string Comment { get; private set; } = "";
        public string Value { get; private set; } = "";
        public FitsKeywordType Type
        {
            get;
            private set;
        }

        public bool IsExtension => !IsEmpty && string.IsNullOrWhiteSpace(Header);
        public FitsKey(byte[] data, int offset = 0)
        {
            if (data == null)
                throw new ArgumentNullException($"{nameof(data)} is null");
            if (data.Length < KeySize + offset)
                throw new ArgumentException($"{nameof(data)} has wrong length");

            var keyString = new string(Encoding.ASCII.GetChars(data, offset, KeySize));

            Header = keyString.Substring(0, KeyHeaderSize).Trim();
            //Body = keyString.Substring(KeyHeaderSize);
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
            Comment = indexSlash >= KeyHeaderSize ? keyString.Substring(indexSlash + 1) : "";
            if (keyString[KeyHeaderSize] == '=')
            {
                Value = indexSlash >= KeyHeaderSize
                    ? keyString.Substring(KeyHeaderSize + 1, indexSlash - KeyHeaderSize - 1)
                    : keyString.Substring(KeyHeaderSize + 1);
                var trimVal = Value.Trim();

                if (string.IsNullOrWhiteSpace(trimVal))
                {
                    Type = FitsKeywordType.Blank;
                    RawValue = null;
                }
                else if (trimVal == "F" || trimVal == "T")
                {
                    Type = FitsKeywordType.Logical;
                    RawValue = trimVal == "T";
                }
                else if (trimVal.Contains('\''))
                {
                    Type = FitsKeywordType.String;
                    //RawValue = trimVal.TrimStart('\'').TrimEnd('\'').Replace("''", "'");
                    RawValue = Regex.Match(trimVal, "'(.*)'").Groups[1].Value.Replace("''", "'");
                }
                else if (trimVal.Contains(":"))
                {
                    Type = FitsKeywordType.Complex;
                    var split = trimVal
                        .Split(':')
                        .Select(s => double.Parse(s.Trim(), 
                                    System.Globalization.NumberStyles.Any, 
                                    System.Globalization.NumberFormatInfo.InvariantInfo))
                        .ToList();
                    RawValue = new Complex(split[0], split[1]);
                }
                else if (int.TryParse(trimVal, out var intVal))
                {
                    Type = FitsKeywordType.Integer;
                    RawValue = intVal;
                }
                else if (double.TryParse(trimVal, out var dVal))
                {
                    Type = FitsKeywordType.Float;
                    RawValue = dVal;
                }
                else throw new ArgumentException("Keyword value is of unknown format.");
            }
            else
            {
                Comment = keyString.Substring(KeyHeaderSize + 1).Trim();
                Value = "";
                if (string.IsNullOrWhiteSpace(Comment))
                    Type = FitsKeywordType.Blank;
                else
                    Type = FitsKeywordType.Comment;
                RawValue = null;
            }
        }
        private FitsKey()
        { }

        public T GetValue<T>()
        {
            dynamic ret;
            if (typeof(T) == typeof(bool) && Type == FitsKeywordType.Logical)
                ret = (bool)RawValue;
            else if (typeof(T) == typeof(string) && Type == FitsKeywordType.String)
                ret = (string)RawValue;
            else if (typeof(T) == typeof(int) && Type == FitsKeywordType.Integer)
                ret = (int)RawValue;
            else if (typeof(T) == typeof(double) && Type == FitsKeywordType.Float)
                ret = (double)RawValue;
            else if (typeof(T) == typeof(Complex) && Type == FitsKeywordType.Complex)
                ret = (Complex)RawValue;
            else throw new TypeAccessException($"Illegal combination of {Type} and {typeof(T)}.");
            return ret;
        }
        public override string ToString() => KeyString;
        public override bool Equals(object obj)
        {
            if (obj is FitsKey key)
                return Header.Equals(key.Header) &&
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

            var isKey = strRep.StartsWith("HISTORY ") || 
                        strRep.StartsWith("COMMENT ") ||
                        strRep.StartsWith("END") ||
                        string.IsNullOrWhiteSpace(strRep);

            isKey |= Regex.IsMatch(strRep, $@"^[A-Za-z\ \-0-9]{{{KeyHeaderSize}}}=\ ");

            return isKey;
        }

        public static IEnumerable<FitsKey> JoinKeywords(params FitsUnit[] keyUnits)
        {
            foreach (var keyUnit in keyUnits)
                if (keyUnit.TryGetKeys(out var keys))
                    foreach (var key in keys)
                        yield return key;
        }

        public static FitsKey CreateNew(string header, FitsKeywordType type, object value,
            string comment = "", FitsKeyLayout layout = FitsKeyLayout.Fixed)
        {
            if(header is null)
                throw new ArgumentNullException(nameof(header));

            if (header.Length > KeyHeaderSize)
                throw new ArgumentException($"\"{nameof(header)}\" is too long");

            var key = new FitsKey
            {
                Header = header.Trim(),
                RawValue = value,
                Type = type
            };

            var maxValuePos = NumericValueMaxLengthFixed;
            var dblDecPrecision = maxValuePos - 7;
            const string complexSep = "";
            var strQuotePos = StringQuotePos - 1;

            switch (type)
            {
                case FitsKeywordType.Logical:
                    if (value is bool bVal)
                        key.Value = string.Format($"{{0, {maxValuePos}}}", bVal ? 'T': 'F');
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type {typeof(bool)}.");
                    break;
                case FitsKeywordType.Integer:
                    if (value is int iVal)
                        key.Value = string.Format($"{{0, {maxValuePos}}}", iVal);
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type {typeof(int)}.");
                    break;
                case FitsKeywordType.Float:
                    if (value is float fVal)
                        key.Value = string.Format($"{{0, {maxValuePos}}}", fVal.ToString("E8"));
                    else if (value is double dVal)
                    {
                        if(Math.Abs(dVal) >= 1e100 || Math.Abs(dVal) <= 1e-100)
                            throw new OverflowException($"{typeof(double)} with exponent" +
                                                        " greater than 99 or smaller than -99 " +
                                                        "cannot be represented in fixed mode.");
                        key.Value = string.Format($"{{0, {maxValuePos}}}", FormatDouble(dVal, dblDecPrecision));
                    }
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type" +
                                                     $" {typeof(float)} or {typeof(double)}.");
                    break;
                case FitsKeywordType.Complex:
                    if (value is Complex cVal)
                    {
                        if(Math.Abs(cVal.Real) >= 1e100 ||
                           Math.Abs(cVal.Real) <= 1e-100 ||
                           Math.Abs(cVal.Imaginary) >= 1e100 ||
                           Math.Abs(cVal.Imaginary) <= 1e-100)
                            throw new OverflowException($"{typeof(Complex)} with exponent" +
                                                        " greater than 99 or smaller than -99 " +
                                                        "cannot be represented in fixed mode.");
                        key.Value = string.Format($"{{0, {maxValuePos}}}{complexSep}{{1, {maxValuePos}}}", 
                            FormatDouble(cVal.Real, dblDecPrecision),
                            FormatDouble(cVal.Imaginary, dblDecPrecision));
                    }
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type" +
                                                     $" {typeof(Complex)}.");
                    break;

                case FitsKeywordType.String:
                    if (value is string sVal)
                    {
                        sVal = sVal.Replace("\'", "''").TrimEnd();
                        if(sVal.Length > KeySize - KeyHeaderSize - ReservedSize - 2)
                            throw new ArgumentException($"String \"{nameof(value)}\" is too long.");
                        key.Value = string.Format($"'{{0, {-strQuotePos}}}'", sVal);
                    }
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type" +
                                                     $" {typeof(string)}.");
                    break;
                case FitsKeywordType.Comment:
                    if(header != "COMMENT" && header != "HISTORY")
                        throw new ArgumentException($"\"{nameof(header)}\" should be either \"COMMENT\" or \"HISTORY\".");
                    if (value is string commVal)
                        key.Value = commVal.Substring(0, Math.Min(commVal.Length, KeySize - KeyHeaderSize));
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type {typeof(string)}.");
                    break;
                case FitsKeywordType.Blank:
                    if (value is string blankVal)
                        key.Value = blankVal?.Substring(0, Math.Min(blankVal.Length, KeySize - KeyHeaderSize));
                    else if (value is null)
                        key.Value = "";
                    else throw new ArgumentException($"\"{nameof(value)}\" should be of type {typeof(string)}.");
                    break;
                default:
                    throw new NotSupportedException($"Unsupported \"{nameof(type)}\".");
            }

            if (!string.IsNullOrWhiteSpace(comment))
            {
                var remSpace = KeySize - KeyHeaderSize;
                if (type != FitsKeywordType.Blank && type != FitsKeywordType.Comment)
                    remSpace -= ReservedSize + key.Value.Length;

                if (remSpace > 0)
                    key.Comment = comment.Substring(0, Math.Min(comment.Length, remSpace));
            }

            return key;
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
