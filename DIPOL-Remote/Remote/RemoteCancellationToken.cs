//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
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
using System.Runtime.Serialization;
using System.Threading;

namespace DIPOL_Remote.Remote
{
    [DataContract]
    public struct RemoteCancellationToken
    {
        public static RemoteCancellationToken None { get; } = default;

        [DataMember(Name = "Id")]
        private byte[] GuidRep
        {
            get => Id.ToByteArray();
            set => Id = new Guid(value);
        }

        public Guid Id { get; private set; }

        public override int GetHashCode()
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            // [Id] is modified only in deserialization; otherwise, it's immutable.
            => Id.GetHashCode();

        public static RemoteCancellationToken CreateFromToken(CancellationToken token)
            => token == CancellationToken.None
                ? None
                : new RemoteCancellationToken {Id = Guid.NewGuid()};

        public static RemoteCancellationToken CreateFromToken(RemoteCancellationToken token)
            => new RemoteCancellationToken {Id = token.Id};

    }
}
