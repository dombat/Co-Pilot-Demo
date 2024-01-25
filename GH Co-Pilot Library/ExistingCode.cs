using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Security;
using System.Text;

namespace GH_Co_Pilot_Library
{
    //---------------------------------------------------------------------
    // <copyright file="Asn1PrimitiveDecoder.cs" company="Microsoft">
    //     Copyright (c) Microsoft Corporation.  All rights reserved.
    //     THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
    //     OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
    //     LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR
    //     FITNESS FOR A PARTICULAR PURPOSE.
    // </copyright>
    // <summary>The Asn1PrimitiveDecoder type.</summary>
    //---------------------------------------------------------------------

  


        /// <summary>
        /// Provides decoding of primitives encoded using ASN.1
        /// </summary>
        internal static class Asn1PrimitiveDecoder
        {
            /// <summary>
            /// Decodes an integer in an ASN.1 stream.
            /// </summary>
            /// <param name="s">The stream of data to be decoded.</param>
            /// <returns>The decoded integer.</returns>
            internal static int DecodeInt(Substream s)
            {
                ExpectAsnIdentifier(s, Asn1Constants.AsnIntIdentifier);
                var len = ExpectAsnLength(s);

                var result = DecodeUintRaw(s, len, 8);

                return (int)result;
            }

            /// <summary>
            /// Decodes a string in an ASN.1 stream.
            /// </summary>
            /// <param name="s">The stream of data to be decoded.</param>
            /// <returns>The decoded string.</returns>
            internal static string DecodeString(Substream s)
            {
                ExpectAsnIdentifier(s, Asn1Constants.AsnStringIdentifier);
                var len = ExpectAsnLength(s);
                var innerStream = Substream.Create(s, len);
                ExpectAsnIdentifier(innerStream, Asn1Constants.AsnPrintableStringIdentifier);
                var innerLen = ExpectAsnLength(innerStream);

                var buffer = new byte[innerLen];
                innerStream.Read(buffer, 0, innerLen);
                var result = Encoding.ASCII.GetString(buffer);

                return result;
            }

            /// <summary>
            /// Decodes an object identifier in an ASN.1 stream.
            /// </summary>
            /// <param name="s">The stream of data to be decoded.</param>
            /// <param name="typeCode">The actual type identifier for this OID.</param>
            /// <returns>The decoded object identifier.</returns>
            internal static int[] DecodeOid(Substream s, byte typeCode)
            {
                var oid = new List<int>(10); // heuristic that there won't be more than 10 subidentifiers.

                ExpectAsnIdentifier(s, typeCode);
                var oidLen = ExpectAsnLength(s);

                var lastPos = s.Position + oidLen;
                var firstPair = DecodeOidSubidentifier(s, lastPos);
                oid.Add(firstPair / 40);
                oid.Add(firstPair % 40);

                while (s.Position < lastPos)
                {
                    oid.Add(DecodeOidSubidentifier(s, lastPos));
                }

                return oid.ToArray();
            }

            /// <summary>
            /// Decodes a sequence in an ASN.1 stream.
            /// </summary>
            /// <param name="s">The stream of data to be decoded.</param>
            /// <returns>A sub-stream containing the entire sequence.</returns>
            internal static Substream DecodeSeq(Substream s)
            {
                ExpectAsnIdentifier(s, Asn1Constants.AsnSeqType);
                var len = ExpectAsnLength(s);
                var result = Substream.Create(s, len);
                return result;
            }

            /// <summary>
            /// Decodes a set in an ASN.1 stream.
            /// </summary>
            /// <param name="s">The stream of data to be decoded.</param>
            /// <returns>A sub-stream containing the entire set.</returns>
            internal static Substream DecodeSet(Substream s)
            {
                ExpectAsnIdentifier(s, Asn1Constants.AsnSetType);
                var len = ExpectAsnLength(s);
                var result = Substream.Create(s, len);
                return result;
            }

            /// <summary>
            /// Checks the stream to see that the next data represents the expected identifier.
            /// </summary>
            /// <param name="s">The stream being decoded.</param>
            /// <param name="expectedAsnIdentifier">The ASN.1 identifier that is expected at this position in the data.</param>
            private static void ExpectAsnIdentifier(Substream s, byte expectedAsnIdentifier)
            {
                var errorPosition = (int)s.RootPosition;
                var actualIdentifier = s.ReadByte();
                if (actualIdentifier == -1)
                {
                    throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, "Data exhausted before Identifier ({0}) could be read at offset {1} in the data", expectedAsnIdentifier, errorPosition));
                }
                else if (actualIdentifier != expectedAsnIdentifier)
                {
                    throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, "Found Identifier for type {0} when {1} was expected at offset {2} in the data", actualIdentifier, expectedAsnIdentifier, errorPosition));
                }
            }

            /// <summary>
            /// Reads the length and verifies it fits in the stream and in the enclosing structure.
            /// </summary>
            /// <param name="s">The stream being decoded.</param>
            /// <returns>The length decoded from the stream.</returns>
            private static int ExpectAsnLength(Substream s)
            {
                var errorPosition = (int)s.RootPosition;
                var actualLength = s.ReadByte();
                if (actualLength == -1)
                {
                    throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, "Data exhausted before length could be read at offset {0} in the data", errorPosition));
                }
                else if ((actualLength & 0x80) == 0x80)
                {
                    // long form
                    var n = actualLength & 0x7F;
                    if (n > RemainingLength(s))
                    {
                        throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, "Data exhausted before length could be read at offset {0} in the data", errorPosition));
                    }

                    actualLength = (int)DecodeUintRaw(s, n, 8);
                }

                if (RemainingLength(s) < (long)actualLength)
                {
                    throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, "Length {0} greater than remaining length {1} at offset {2} in the data", actualLength, RemainingLength(s), errorPosition));
                }

                if (s.Position + actualLength - 1 > s.Length)
                {
                    throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, "Length {0} greater than enclosing length at offset {1} in the data", actualLength, errorPosition));
                }

                return actualLength;
            }

            /// <summary>
            /// Computes the remaining length of the stream.
            /// </summary>
            /// <param name="s">The stream to get the remaining length for.</param>
            /// <returns>The number of bytes left to be read from the stream.</returns>
            private static int RemainingLength(Stream s)
            {
                return (int)(s.Length - s.Position);
            }

            /// <summary>
            /// Decodes up to 4 bytes into a <see cref="uint"/>.
            /// </summary>
            /// <param name="bytes">The bytes being decoded.</param>
            /// <param name="count">The number of bytes to actually process in <paramref name="bytes"/>.</param>
            /// <param name="bitsPerOctet">The number of bits from the unsigned value that are to be encoded per octet.</param>
            /// <returns>The bytes needed to represent the unsigned integer.</returns>
            private static uint DecodeUintRaw(byte[] bytes, int count, int bitsPerOctet)
            {
                if (count > 4)
                {
                    throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Number too big, length is {0} octets, maximum allowed is 4", count));
                }
                else if (count > bytes.Length)
                {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    throw new ArgumentException("count is bigger than actual length", "count");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                }

                var mask = (byte)((1 << bitsPerOctet) - 1);
                uint result = 0;
                for (var i = 0; i < count; i++)
                {
                    result = (result << bitsPerOctet) | (uint)(bytes[i] & mask);
                }

                return result;
            }

            /// <summary>
            /// Decodes up to 4 bytes into a <see cref="uint"/>.
            /// </summary>
            /// <param name="s">The stream being decoded.</param>
            /// <param name="n">The number of octets that make up the <see cref="uint"/>, cannot be more than 4.</param>
            /// <param name="bitsPerOctet">The number of bits from the unsigned value that are to be encoded per octet.</param>
            /// <returns>The bytes needed to represent the unsigned integer.</returns>
            private static uint DecodeUintRaw(Substream s, int n, int bitsPerOctet)
            {
                if (n > 4)
                {
                    throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Number too big, length is {0} octets, maximum allowed is 4 at offset {1} in the data", n, s.RootPosition - 1));
                }

                var bytes = new byte[4];
                var bytesRead = s.Read(bytes, 0, n);
                var result = DecodeUintRaw(bytes, bytesRead, bitsPerOctet);

                return result;
            }

            /// <summary>
            /// Decodes a sub-identifier from an object identifier.
            /// </summary>
            /// <param name="s">The stream being decoded.</param>
            /// <param name="lastPosition">The last available position in <paramref name="s"/> for the object identifier.</param>
            /// <returns>The decoded sub-identifier.</returns>
            private static int DecodeOidSubidentifier(Substream s, long lastPosition)
            {
                var result = 0;
                var n = 0;
                byte octet;
                var octets = new byte[4];

                do
                {
                    if (s.Position >= lastPosition)
                    {
                        throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, "Data exhausted before sub-identifier could be read at offset {0} in the data", s.RootPosition - 1));
                    }

                    if (n >= octets.Length)
                    {
                        throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, "Sub-identifier too big, maximum length allowed is {0} octets at offset {1} in the data", octets.Length, s.RootPosition));
                    }

                    octet = (byte)s.ReadByte();
                    octets[n++] = octet;
                }
                while ((octet & 0x80) != 0);

                result = (int)DecodeUintRaw(octets, n, 7);

                return result;
            }
        }
    }

//---------------------------------------------------------------------
// <copyright file="Substream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
//     THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
//     OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
//     LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR
//     FITNESS FOR A PARTICULAR PURPOSE.
// </copyright>
// <summary>The Substream type.</summary>
//---------------------------------------------------------------------

namespace GH_Co_Pilot_Library
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Creates a read-only stream which is a subset of an existing stream.
    /// </summary>
    /// <remarks>
    /// The sub-stream can be read without affecting the position of the parent stream. Supports a hierarchy of sub-streams, and provides a <see cref="Substream.RootPosition"/>
    /// property which is the position of the stream at the root of the hierarchy, to ease reporting of error positions.
    /// </remarks>
    internal class Substream : Stream
    {
        /// <summary>
        /// The length of the sub-stream
        /// </summary>
        private readonly long _length;

        /// <summary>
        /// Records the position in the root stream where this sub-stream starts.
        /// </summary>
        private readonly long _rootStartPosition;

        /// <summary>
        /// The position inside the sub-stream.
        /// </summary>
        private long _position;

        /// <summary>
        /// Initializes a new instance of the <see cref="Substream"/> class.
        /// </summary>
        /// <remarks>
        /// Creates a sub-stream from a parent stream. The sub-stream starts at the current position in the parent stream. The parent stream position is moved to the first byte after the end of the sub-stream.
        /// </remarks>
        /// <param name="rootStream">The root stream for the hierarchy. The root stream must support seeking.</param>
        /// <param name="parentStream">The parent stream from which the sub-stream takes its data. The parent stream must support seeking.</param>
        /// <param name="length">The length of the sub-stream.</param>
        /// <param name="rootStartPosition">The position in the root stream where this sub-stream starts.</param>
        private Substream(Stream rootStream, Stream parentStream, long length, long rootStartPosition)
        {
            this.RootStream = rootStream;
            this._length = length;
            this._rootStartPosition = rootStartPosition;

            parentStream.Position = parentStream.Position + length;
        }

        /// <summary>
        /// Gets a value indicating whether the stream supports reading.
        /// </summary>
        /// <remarks>
        /// The <see cref="Substream"/> class supports reading, so this will always be <c>true</c>.
        /// </remarks>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the stream supports seeking.
        /// </summary>
        /// <remarks>
        /// The <see cref="Substream"/> class supports seeking, so this will always be <c>true</c>.
        /// </remarks>
        public override bool CanSeek
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the stream supports writing.
        /// </summary>
        /// <remarks>
        /// The <see cref="Substream"/> class does not support writing, so this will always be <c>false</c>.
        /// </remarks>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the length of the sub-stream.
        /// </summary>
        public override long Length
        {
            get { return this._length; }
        }

        /// <summary>
        /// Gets or sets the position in the stream.
        /// </summary>
        public override long Position
        {
            get
            {
                return this._position;
            }

            set
            {
                this._position = value;
            }
        }

        /// <summary>
        /// Gets the position in the root stream.
        /// </summary>
        public long RootPosition
        {
            get
            {
                return this.Position + this._rootStartPosition;
            }
        }

        /// <summary>
        /// Gets or sets the stream which is the root of this hierarchy.
        /// </summary>
        private Stream RootStream { get; set; }

        /// <summary>
        /// Flushes written content.
        /// </summary>
        /// <remarks>
        /// As writing is not supported, this method is not supported.
        /// </remarks>
        public override void Flush()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// Preserves the position of the parent streams.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the <paramref name="buffer"/> contains the specified byte array
        /// with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes
        /// read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // delegate buffer parameter validation to the RootStream
            var bytesLeft = (int)(this.Length - this.Position);
            var bytesToRead = count > bytesLeft ? bytesLeft : count;
            var savePosition = this.RootStream.Position;
            this.RootStream.Position = this._rootStartPosition + this.Position;
            int bytesRead;
            try
            {
                bytesRead = this.RootStream.Read(buffer, offset, bytesToRead);
                this.Position = this.Position + bytesRead;
            }
            finally
            {
                this.RootStream.Position = savePosition;
            }

            return bytesRead;
        }

        /// <summary>
        /// Sets the position within the current stream
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream. </returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/>  at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a sub-stream from a parent stream.
        /// </summary>
        /// <remarks>
        /// The sub-stream starts at the current position in the parent stream. The parent stream position is moved to the first byte after the end of the sub-stream.
        /// </remarks>
        /// <param name="parentStream">The parent stream from which the sub-stream takes its data. The parent stream must support seeking.</param>
        /// <param name="length">The length of the sub-stream.</param>
        /// <returns>The new instance of the <see cref="Substream"/> class.</returns>
        internal static Substream Create(Stream parentStream, long length)
        {
            var rootStream = parentStream;
            var rootStartPosition = parentStream.Position;
            var parentSubstream = parentStream as Substream;
            if (parentSubstream != null)
            {
                rootStream = parentSubstream.RootStream;
                rootStartPosition += parentSubstream._rootStartPosition;
            }

            if (length > parentStream.Length - parentStream.Position)
            {
                throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, "The stream is too short for a sub-stream of {0} bytes at root offset {1}", length, rootStartPosition));
            }

            return new Substream(rootStream, parentStream, length, rootStartPosition);
        }
    }

    internal class Asn1Constants
    {
        /// <summary>
        /// The Identifier for the integer type.
        /// </summary>
        internal const int AsnIntIdentifier = 2;

        /// <summary>
        /// The Identifier for the object identifier type.
        /// </summary>
        internal const int AsnOidType = 6;

        /// <summary>
        /// The Identifier for the printable string type.
        /// </summary>
        internal const int AsnPrintableStringIdentifier = 19;

        /// <summary>
        /// The Identifier for the sequence type.
        /// </summary>
        internal const int AsnSeqType = 48;

        /// <summary>
        /// The Identifier for the set type.
        /// </summary>
        internal const int AsnSetType = 49;

        /// <summary>
        /// The Identifier for the string type.
        /// </summary>
        /// <remarks>
        /// This value does not appear to match the identifier rules, following the rules this value would appear to be a context-specific, constructed, boolean. Instances
        /// of this type contain a printable string inside.
        /// </remarks>
        internal const int AsnStringIdentifier = 161;
    }
}

