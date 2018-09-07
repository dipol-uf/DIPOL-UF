﻿//    This file is part of Dipol-3 Camera Manager.

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
using System.Linq;
using System.Text;
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

        public static FITSKey Empty => new FITSKey();

        public object RawValue { get; private set; }

        public byte[] Data => Encoding.ASCII.GetBytes(KeyString.ToArray());
        public string Extension
        {
            get;
            internal set;
        } = null;

        public string KeyString { get; private set; } = new string(' ', KeySize);
        public bool IsEmpty => string.IsNullOrWhiteSpace(KeyString);
        public string Header
        {
            get;
            private set;
        }
        public string Body
        {
            get;
            private set;
        }
        public string Comment
        {
            get;
            private set;
        }
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

        public bool IsExtension => !IsEmpty && string.IsNullOrWhiteSpace(Header);
        public FITSKey(byte[] data, int offset = 0)
        {
            if (data == null)
                throw new ArgumentNullException($"{nameof(data)} is null");
            if ((data.Length <= KeySize) || (offset + KeySize > data.Length))
                throw new ArgumentException($"{nameof(data)} has wrong length");

            KeyString = new string(Encoding.ASCII.GetChars(data, offset, KeySize));

            Header = KeyString.Substring(0, KeyHeaderSize).Trim();
            Body = KeyString.Substring(KeyHeaderSize);
            var indexSlash = -1;
            var isInQuotes = false;
            for (var i = KeyHeaderSize; i < KeySize; i++)
                if (KeyString[i] == '\'')
                    isInQuotes = !isInQuotes;
                else if (KeyString[i] == '/' && !isInQuotes)
                {
                    indexSlash = i;
                    break;
                }
            Comment = indexSlash >= KeyHeaderSize ? KeyString.Substring(indexSlash + 1) : "";
            if (KeyString[KeyHeaderSize] == '=')
            {
                Value = indexSlash >= KeyHeaderSize
                    ? KeyString.Substring(KeyHeaderSize + 1, indexSlash - KeyHeaderSize - 1)
                    : KeyString.Substring(KeyHeaderSize + 1);
                var trimVal = Value.Trim();

                if (string.IsNullOrWhiteSpace(trimVal))
                {
                    Type = FITSKeywordType.Blank;
                    RawValue = null;
                }
                else if (trimVal == "F" || trimVal == "T")
                {
                    Type = FITSKeywordType.Logical;
                    RawValue = trimVal == "T";
                }
                else if (trimVal.Contains('\''))
                {
                    Type = FITSKeywordType.String;
                    RawValue = trimVal.TrimStart('\'').TrimEnd('\'').Replace("''", "'");
                }
                else if (trimVal.Contains(":"))
                {
                    Type = FITSKeywordType.Complex;
                    var split = trimVal
                        .Split(':')
                        .Select(s => double.Parse(s, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo))
                        .ToArray();
                    RawValue = new Complex(split[0], split[1]);
                }
                else if (int.TryParse(trimVal, out var intVal))
                {
                    Type = FITSKeywordType.Integer;
                    RawValue = intVal;
                }
                else if (double.TryParse(trimVal, out var dVal))
                {
                    Type = FITSKeywordType.Float;
                    RawValue = dVal;
                }
                else throw new ArgumentException("Keyword value is of unknown format.");
            }
            else
            {
                Comment = KeyString.Substring(KeyHeaderSize + 1).Trim();
                Value = "";
                if (string.IsNullOrWhiteSpace(Comment))
                    Type = FITSKeywordType.Blank;
                else
                    Type = FITSKeywordType.Comment;
                RawValue = null;
            }
        }
        private FITSKey()
        { }

        public T GetValue<T>()
        {
            dynamic ret;
            if (typeof(T) == typeof(bool) && Type == FITSKeywordType.Logical)
                ret = (bool)RawValue;
            else if (typeof(T) == typeof(string) && Type == FITSKeywordType.String)
                ret = (string)RawValue;
            else if (typeof(T) == typeof(int) && Type == FITSKeywordType.Integer)
                ret = (int)RawValue;
            else if (typeof(T) == typeof(double) && Type == FITSKeywordType.Float)
                ret = (double)RawValue;
            else if (typeof(T) == typeof(Complex) && Type == FITSKeywordType.Complex)
                ret = (Complex)RawValue;
            else throw new TypeAccessException($"Illegal combination of {Type} and {typeof(T)}.");
            return ret;
        }
        public override string ToString() => KeyString;
       
        public static bool IsFitsKey(byte[] data, int offset = 0)
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
                if (keyUnit.TryGetKeys(out var keys))
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
        /// <returns>A new instance of FITS keyword</returns>
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
            var key = new FITSKey();
            // String representation of keyword
            var result = new StringBuilder(KeySize);
            // Initialize with blanks
            result.Append(' ', KeySize);
            // Right-justified header
            result.Insert(0, string.Format($"{{0, -{KeyHeaderSize}}}", header));
            key.Header = header;
            key.Type = type;
            key.RawValue = value;

            // If keyword is of value type
            if (type != FITSKeywordType.Comment & type != FITSKeywordType.Blank)
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
                var lastIndex = 0;

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
                    // Comment delimiter
                    result.Insert(lastIndex + 2, "\\ ");
                    // Truncated comment
                    key.Comment = comment.Substring(0, commLength);
                    // Inserts comment after comment delimiter
                    result.Insert(lastIndex + 4, key.Comment);
                }
            }
            else
                throw new NotImplementedException();

            // Ensures keyword representation can be fitted into 80 symbols string 
            if (result.Length > KeySize)
                result = result.Remove(KeySize, result.Length - KeySize);

            // Assigns constructed string 
            key.KeyString = result.ToString();
            // And body
            key.Body = key.KeyString.Substring(KeyHeaderSize);

            return key;
        }
    }
}
