using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Security;
using System.Text;

namespace GH_Co_Pilot_Library
{
    #region Disclaimer

    //---------------------------------------------------------------------
    // <copyright file="Asn1PrimitiveDecoder.cs" company="Microsoft">
    //     Copyright (c) Microsoft Corporation.  All rights reserved.
    //     THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
    //     OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
    //     LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR
    //     FITNESS FOR A PARTICULAR PURPOSE.
    // ALSO, this code has been tampered with for the GH co-pilot demo so DO NOT USE IT becaus eit probably wont work as expected!!!
    // </copyright>
    // <summary>The Asn1PrimitiveDecoder type.</summary>
    //---------------------------------------------------------------------
    #endregion

    /// <summary>
    /// Provides decoding of primitives encoded using ASN.1
    /// </summary>
    internal static class Asn1PrimitiveDecoder
        {

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

            private static int RemainingLength(Stream s)
            {
                return (int)(s.Length - s.Position);
            }

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
          
        }
    }

namespace GH_Co_Pilot_Library
{
    using System;

    using System.Globalization;
    using System.IO;

    internal class Substream : Stream
    {

        private readonly long _length;
        private readonly long _rootStartPosition;
        private long _position;

        private Substream(Stream rootStream, Stream parentStream, long length, long rootStartPosition)
        {
            this.RootStream = rootStream;
            this._length = length;
            this._rootStartPosition = rootStartPosition;

            parentStream.Position = parentStream.Position + length;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

      
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

   
        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return this._length; }
        }

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

        public long RootPosition
        {
            get
            {
                return this.Position + this._rootStartPosition;
            }
        }


        private Stream RootStream { get; set; }

        public override void Flush()
        {
            throw new NotImplementedException();
        }



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
        internal const int AsnIntIdentifier = 2;
        internal const int AsnOidType = 6;
        internal const int AsnPrintableStringIdentifier = 19;
        internal const int AsnSeqType = 48;
        internal const int AsnSetType = 49;
        internal const int AsnStringIdentifier = 161;
    }
}

