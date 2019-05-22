using Java.Lang;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Text;
using System; 

namespace AndroidDonationsLibrary.Google.Util
{
	/// <summary>
	/// Base64 converter class. This code is not a complete MIME encoder;
	/// it simply converts binary data to base64 data and back.
	/// 
	/// <p>Note <seealso cref="CharBase64"/> is a GWT-compatible implementation of this
	/// class.
	/// </summary>
	public class Base64
	{
		/// <summary>
		/// Specify encoding (value is {@code true}). </summary>
		public const bool ENCODE = true;

		/// <summary>
		/// Specify decoding (value is {@code false}). </summary>
		public const bool DECODE = false;

		/// <summary>
		/// The equals sign (=) as a byte. </summary>
        private static sbyte EQUALS_SIGN = (sbyte)'=';

		/// <summary>
		/// The new line character (\n) as a byte. </summary>
        private static sbyte NEW_LINE = (sbyte)'\n';

		/// <summary>
		/// The 64 valid Base64 values.
		/// </summary>
        private static sbyte[] ALPHABET = new sbyte[] { (sbyte)'A', (sbyte)'B', (sbyte)'C', (sbyte)'D', (sbyte)'E', (sbyte)'F', (sbyte)'G', (sbyte)'H', (sbyte)'I', (sbyte)'J', (sbyte)'K', (sbyte)'L', (sbyte)'M', (sbyte)'N', (sbyte)'O', (sbyte)'P', (sbyte)'Q', (sbyte)'R', (sbyte)'S', (sbyte)'T', (sbyte)'U', (sbyte)'V', (sbyte)'W', (sbyte)'X', (sbyte)'Y', (sbyte)'Z', (sbyte)'a', (sbyte)'b', (sbyte)'c', (sbyte)'d', (sbyte)'e', (sbyte)'f', (sbyte)'g', (sbyte)'h', (sbyte)'i', (sbyte)'j', (sbyte)'k', (sbyte)'l', (sbyte)'m', (sbyte)'n', (sbyte)'o', (sbyte)'p', (sbyte)'q', (sbyte)'r', (sbyte)'s', (sbyte)'t', (sbyte)'u', (sbyte)'v', (sbyte)'w', (sbyte)'x', (sbyte)'y', (sbyte)'z', (sbyte)'0', (sbyte)'1', (sbyte)'2', (sbyte)'3', (sbyte)'4', (sbyte)'5', (sbyte)'6', (sbyte)'7', (sbyte)'8', (sbyte)'9', (sbyte)'+', (sbyte)'/' };

		/// <summary>
		/// The 64 valid web safe Base64 values.
		/// </summary>
        private static sbyte[] WEBSAFE_ALPHABET = new sbyte[] { (sbyte)'A', (sbyte)'B', (sbyte)'C', (sbyte)'D', (sbyte)'E', (sbyte)'F', (sbyte)'G', (sbyte)'H', (sbyte)'I', (sbyte)'J', (sbyte)'K', (sbyte)'L', (sbyte)'M', (sbyte)'N', (sbyte)'O', (sbyte)'P', (sbyte)'Q', (sbyte)'R', (sbyte)'S', (sbyte)'T', (sbyte)'U', (sbyte)'V', (sbyte)'W', (sbyte)'X', (sbyte)'Y', (sbyte)'Z', (sbyte)'a', (sbyte)'b', (sbyte)'c', (sbyte)'d', (sbyte)'e', (sbyte)'f', (sbyte)'g', (sbyte)'h', (sbyte)'i', (sbyte)'j', (sbyte)'k', (sbyte)'l', (sbyte)'m', (sbyte)'n', (sbyte)'o', (sbyte)'p', (sbyte)'q', (sbyte)'r', (sbyte)'s', (sbyte)'t', (sbyte)'u', (sbyte)'v', (sbyte)'w', (sbyte)'x', (sbyte)'y', (sbyte)'z', (sbyte)'0', (sbyte)'1', (sbyte)'2', (sbyte)'3', (sbyte)'4', (sbyte)'5', (sbyte)'6', (sbyte)'7', (sbyte)'8', (sbyte)'9', (sbyte)'-', (sbyte)'_' };

		/// <summary>
		/// Translates a Base64 value to either its 6-bit reconstruction value
		/// or a negative number indicating some other meaning.
		/// 
		/// </summary>
        private static sbyte[] DECODABET = new sbyte[] { (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-5), (sbyte)(-5), (sbyte)(-9), (sbyte)(-9), (sbyte)(-5), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-5), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(62), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(63), (sbyte)(52), (sbyte)(53), (sbyte)(54), (sbyte)(55), (sbyte)(56), (sbyte)(57), (sbyte)(58), (sbyte)(59), (sbyte)(60), (sbyte)(61), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-1), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(0), (sbyte)(1), (sbyte)(2), (sbyte)(3), (sbyte)(4), (sbyte)(5), (sbyte)(6), (sbyte)(7), (sbyte)(8), (sbyte)(9), (sbyte)(10), (sbyte)(11), (sbyte)(12), (sbyte)(13), (sbyte)(14), (sbyte)(15), (sbyte)(16), (sbyte)(17), (sbyte)(18), (sbyte)(19), (sbyte)(20), (sbyte)(21), (sbyte)(22), (sbyte)(23), (sbyte)(24), (sbyte)(25), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(26), (sbyte)(27), (sbyte)(28), (sbyte)(29), (sbyte)(30), (sbyte)(31), (sbyte)(32), (sbyte)(33), (sbyte)(34), (sbyte)(35), (sbyte)(36), (sbyte)(37), (sbyte)(38), (sbyte)(39), (sbyte)(40), (sbyte)(41), (sbyte)(42), (sbyte)(43), (sbyte)(44), (sbyte)(45), (sbyte)(46), (sbyte)(47), (sbyte)(48), (sbyte)(49), (sbyte)(50), (sbyte)(51), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9) }; // Decimal  0 -  8

		/// <summary>
		/// The web safe decodabet </summary>
        private static sbyte[] WEBSAFE_DECODABET = new sbyte[] { (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-5), (sbyte)(-5), (sbyte)(-9), (sbyte)(-9), (sbyte)(-5), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-5), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(62), (sbyte)(-9), (sbyte)(-9), (sbyte)(52), (sbyte)(53), (sbyte)(54), (sbyte)(55), (sbyte)(56), (sbyte)(57), (sbyte)(58), (sbyte)(59), (sbyte)(60), (sbyte)(61), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-1), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(0), (sbyte)(1), (sbyte)(2), (sbyte)(3), (sbyte)(4), (sbyte)(5), (sbyte)(6), (sbyte)(7), (sbyte)(8), (sbyte)(9), (sbyte)(10), (sbyte)(11), (sbyte)(12), (sbyte)(13), (sbyte)(14), (sbyte)(15), (sbyte)(16), (sbyte)(17), (sbyte)(18), (sbyte)(19), (sbyte)(20), (sbyte)(21), (sbyte)(22), (sbyte)(23), (sbyte)(24), (sbyte)(25), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(63), (sbyte)(-9), (sbyte)(26), (sbyte)(27), (sbyte)(28), (sbyte)(29), (sbyte)(30), (sbyte)(31), (sbyte)(32), (sbyte)(33), (sbyte)(34), (sbyte)(35), (sbyte)(36), (sbyte)(37), (sbyte)(38), (sbyte)(39), (sbyte)(40), (sbyte)(41), (sbyte)(42), (sbyte)(43), (sbyte)(44), (sbyte)(45), (sbyte)(46), (sbyte)(47), (sbyte)(48), (sbyte)(49), (sbyte)(50), (sbyte)(51), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9), (sbyte)(-9) }; // Decimal  0 -  8

		// Indicates white space in encoding
        private static sbyte WHITE_SPACE_ENC = (sbyte)(-5);
		// Indicates equals sign in encoding
        private static sbyte EQUALS_SIGN_ENC = (sbyte)(-1);

		/// <summary>
		/// Defeats instantiation. </summary>
		private Base64()
		{

		}

		/* ********  E N C O D I N G   M E T H O D S  ******** */

		/// <summary>
		/// Encodes up to three bytes of the array <var>source</var>
		/// and writes the resulting four Base64 bytes to <var>destination</var>.
		/// The source and destination arrays can be manipulated
		/// anywhere along their length by specifying
		/// <var>srcOffset</var> and <var>destOffset</var>.
		/// This method does not check to make sure your arrays
		/// are large enough to accommodate <var>srcOffset</var> + 3 for
		/// the <var>source</var> array or <var>destOffset</var> + 4 for
		/// the <var>destination</var> array.
		/// The actual number of significant bytes in your array is
		/// given by <var>numSigBytes</var>.
		/// </summary>
		/// <param name="source"> the array to convert </param>
		/// <param name="srcOffset"> the index where conversion begins </param>
		/// <param name="numSigBytes"> the number of significant bytes in your array </param>
		/// <param name="destination"> the array to hold the conversion </param>
		/// <param name="destOffset"> the index where output will be put </param>
		/// <param name="alphabet"> is the encoding alphabet </param>
		/// <returns> the <var>destination</var> array
		/// @since 1.3 </returns>
		private static sbyte[] encode3to4(sbyte[] source, int srcOffset, int numSigBytes, sbyte[] destination, int destOffset, sbyte[] alphabet)
		{
			//           1         2         3
			// 01234567890123456789012345678901 Bit position
			// --------000000001111111122222222 Array position from threeBytes
			// --------|    ||    ||    ||    | Six bit groups to index alphabet
			//          >>18  >>12  >> 6  >> 0  Right shift necessary
			//                0x3f  0x3f  0x3f  Additional AND

			// Create buffer with zero-padding if there are only one or two
			// significant bytes passed in the array.
			// We have to shift left 24 in order to flush out the 1's that appear
			// when Java treats a value as negative that is cast from a byte to an int.
			int inBuff = (numSigBytes > 0 ? (((sbyte)source[srcOffset] << 24) >> 8) : 0) | (numSigBytes > 1 ? (((sbyte)source[srcOffset + 1] << 24) >> 16) : 0) | (numSigBytes > 2 ? (((sbyte)source[srcOffset + 2] << 24) >> 24) : 0);

			switch (numSigBytes)
			{
				case 3:
					destination[destOffset] = alphabet[((int)((uint)inBuff >> 18))];
					destination[destOffset + 1] = alphabet[((int)((uint)inBuff >> 12)) & 0x3f];
					destination[destOffset + 2] = alphabet[((int)((uint)inBuff >> 6)) & 0x3f];
					destination[destOffset + 3] = alphabet[(inBuff) & 0x3f];
					return destination;
				case 2:
					destination[destOffset] = alphabet[((int)((uint)inBuff >> 18))];
					destination[destOffset + 1] = alphabet[((int)((uint)inBuff >> 12)) & 0x3f];
					destination[destOffset + 2] = alphabet[((int)((uint)inBuff >> 6)) & 0x3f];
					destination[destOffset + 3] = EQUALS_SIGN;
					return destination;
				case 1:
					destination[destOffset] = alphabet[((int)((uint)inBuff >> 18))];
					destination[destOffset + 1] = alphabet[((int)((uint)inBuff >> 12)) & 0x3f];
					destination[destOffset + 2] = EQUALS_SIGN;
					destination[destOffset + 3] = EQUALS_SIGN;
					return destination;
				default:
					return destination;
			} // end switch
		} // end encode3to4

		/// <summary>
		/// Encodes a byte array into Base64 notation.
		/// Equivalent to calling
		/// {@code encodeBytes(source, 0, source.length)}
		/// </summary>
		/// <param name="source"> The data to convert
		/// @since 1.4 </param>
        public static string encode(sbyte[] source)
		{
			return encode(source, 0, source.Length, ALPHABET, true);
		}

		/// <summary>
		/// Encodes a byte array into web safe Base64 notation.
		/// </summary>
		/// <param name="source"> The data to convert </param>
		/// <param name="doPadding"> is {@code true} to pad result with '=' chars
		///        if it does not fall on 3 byte boundaries </param>
        public static string encodeWebSafe(sbyte[] source, bool doPadding)
		{
			return encode(source, 0, source.Length, WEBSAFE_ALPHABET, doPadding);
		}

		/// <summary>
		/// Encodes a byte array into Base64 notation.
		/// </summary>
		/// <param name="source"> the data to convert </param>
		/// <param name="off"> offset in array where conversion should begin </param>
		/// <param name="len"> length of data to convert </param>
		/// <param name="alphabet"> the encoding alphabet </param>
		/// <param name="doPadding"> is {@code true} to pad result with '=' chars
		/// if it does not fall on 3 byte boundaries
		/// @since 1.4 </param>
        public static string encode(sbyte[] source, int off, int len, sbyte[] alphabet, bool doPadding)
		{
            sbyte[] outBuff = encode(source, off, len, alphabet, int.MaxValue);
			int outLen = outBuff.Length;

			// If doPadding is false, set length to truncate '='
			// padding characters
			while (doPadding == false && outLen > 0)
			{
				if ((char)outBuff[outLen - 1] != '=')
				{
					break;
				}
				outLen -= 1;
			}

            return new string(Array.ConvertAll(outBuff, q => Convert.ToChar(q)), 0, outLen);
		}

		/// <summary>
		/// Encodes a byte array into Base64 notation.
		/// </summary>
		/// <param name="source"> the data to convert </param>
		/// <param name="off"> offset in array where conversion should begin </param>
		/// <param name="len"> length of data to convert </param>
		/// <param name="alphabet"> is the encoding alphabet </param>
		/// <param name="maxLineLength"> maximum length of one line. </param>
		/// <returns> the BASE64-encoded byte array </returns>
        public static sbyte[] encode(sbyte[] source, int off, int len, sbyte[] alphabet, int maxLineLength)
		{
			int lenDiv3 = (len + 2) / 3; // ceil(len / 3)
			int len43 = lenDiv3 * 4;
			sbyte[] outBuff = new sbyte[len43 + (len43 / maxLineLength)]; // New lines -  Main 4:3

			int d = 0;
			int e = 0;
			int len2 = len - 2;
			int lineLength = 0;
			for (; d < len2; d += 3, e += 4)
			{

				// The following block of code is the same as
				// encode3to4( source, d + off, 3, outBuff, e, alphabet );
				// but inlined for faster encoding (~20% improvement)
				int inBuff = ((int)((uint)(source[d + off] << 24) >> 8)) | ((int)((uint)(source[d + 1 + off] << 24) >> 16)) | ((int)((uint)(source[d + 2 + off] << 24) >> 24));
				outBuff[e] = alphabet[((int)((uint)inBuff >> 18))];
				outBuff[e + 1] = alphabet[((int)((uint)inBuff >> 12)) & 0x3f];
				outBuff[e + 2] = alphabet[((int)((uint)inBuff >> 6)) & 0x3f];
				outBuff[e + 3] = alphabet[(inBuff) & 0x3f];

				lineLength += 4;
				if (lineLength == maxLineLength)
				{
					outBuff[e + 4] = NEW_LINE;
					e++;
					lineLength = 0;
				} // end if: end of line
			} // end for: each piece of array

			if (d < len)
			{
				encode3to4(source, d + off, len - d, outBuff, e, alphabet);

				lineLength += 4;
				if (lineLength == maxLineLength)
				{
					// Add a last newline
					outBuff[e + 4] = NEW_LINE;
					e++;
				}
				e += 4;
			}

            System.Diagnostics.Debug.Assert(e == outBuff.Length);
			return outBuff;
		}


		/* ********  D E C O D I N G   M E T H O D S  ******** */


		/// <summary>
		/// Decodes four bytes from array <var>source</var>
		/// and writes the resulting bytes (up to three of them)
		/// to <var>destination</var>.
		/// The source and destination arrays can be manipulated
		/// anywhere along their length by specifying
		/// <var>srcOffset</var> and <var>destOffset</var>.
		/// This method does not check to make sure your arrays
		/// are large enough to accommodate <var>srcOffset</var> + 4 for
		/// the <var>source</var> array or <var>destOffset</var> + 3 for
		/// the <var>destination</var> array.
		/// This method returns the actual number of bytes that
		/// were converted from the Base64 encoding.
		/// 
		/// </summary>
		/// <param name="source"> the array to convert </param>
		/// <param name="srcOffset"> the index where conversion begins </param>
		/// <param name="destination"> the array to hold the conversion </param>
		/// <param name="destOffset"> the index where output will be put </param>
		/// <param name="decodabet"> the decodabet for decoding Base64 content </param>
		/// <returns> the number of decoded bytes converted
		/// @since 1.3 </returns>
		private static int decode4to3(sbyte[] source, int srcOffset, sbyte[] destination, int destOffset, sbyte[] decodabet)
		{
			// Example: Dk==
			if (source[srcOffset + 2] == EQUALS_SIGN)
			{
				int outBuff = ((int)((uint)(decodabet[source[srcOffset]] << 24) >> 6)) | ((int)((uint)(decodabet[source[srcOffset + 1]] << 24) >> 12));

				destination[destOffset] = (sbyte)((int)((uint)outBuff >> 16));
				return 1;
			}
			else if (source[srcOffset + 3] == EQUALS_SIGN)
			{
				// Example: DkL=
				int outBuff = ((int)((uint)(decodabet[source[srcOffset]] << 24) >> 6)) | ((int)((uint)(decodabet[source[srcOffset + 1]] << 24) >> 12)) | ((int)((uint)(decodabet[source[srcOffset + 2]] << 24) >> 18));

				destination[destOffset] = (sbyte)((int)((uint)outBuff >> 16));
				destination[destOffset + 1] = (sbyte)((int)((uint)outBuff >> 8));
				return 2;
			}
			else
			{
				// Example: DkLE
				int outBuff = ((int)((uint)(decodabet[source[srcOffset]] << 24) >> 6)) | ((int)((uint)(decodabet[source[srcOffset + 1]] << 24) >> 12)) | ((int)((uint)(decodabet[source[srcOffset + 2]] << 24) >> 18)) | ((int)((uint)(decodabet[source[srcOffset + 3]] << 24) >> 24));

				destination[destOffset] = (sbyte)(outBuff >> 16);
				destination[destOffset + 1] = (sbyte)(outBuff >> 8);
				destination[destOffset + 2] = (sbyte)(outBuff);
				return 3;
			}
		} // end decodeToBytes


		/// <summary>
		/// Decodes data from Base64 notation.
		/// </summary>
		/// <param name="s"> the string to decode (decoded in default encoding) </param>
		/// <returns> the decoded data
		/// @since 1.4 </returns>
		public static sbyte[] decode(string s)
		{
            sbyte[] bytes = Array.ConvertAll(Encoding.Unicode.GetBytes(s), q => Convert.ToSByte(q));
			return decode(bytes, 0, bytes.Length);
		}

		/// <summary>
		/// Decodes data from web safe Base64 notation.
		/// Web safe encoding uses '-' instead of '+', '_' instead of '/'
		/// </summary>
		/// <param name="s"> the string to decode (decoded in default encoding) </param>
		/// <returns> the decoded data </returns>
		public static sbyte[] decodeWebSafe(string s)
		{
            sbyte[] bytes = Array.ConvertAll(Encoding.Unicode.GetBytes(s), q => Convert.ToSByte(q));
			return decodeWebSafe(bytes, 0, bytes.Length);
		}

		/// <summary>
		/// Decodes Base64 content in byte array format and returns
		/// the decoded byte array.
		/// </summary>
		/// <param name="source"> The Base64 encoded data </param>
		/// <returns> decoded data
		/// @since 1.3 </returns>
		/// <exception cref="Base64DecoderException"> </exception>
		public static sbyte[] decode(sbyte[] source)
		{
			return decode(source, 0, source.Length);
		}

		/// <summary>
		/// Decodes web safe Base64 content in byte array format and returns
		/// the decoded data.
		/// Web safe encoding uses '-' instead of '+', '_' instead of '/'
		/// </summary>
		/// <param name="source"> the string to decode (decoded in default encoding) </param>
		/// <returns> the decoded data </returns>
		public static sbyte[] decodeWebSafe(sbyte[] source)
		{
			return decodeWebSafe(source, 0, source.Length);
		}

		/// <summary>
		/// Decodes Base64 content in byte array format and returns
		/// the decoded byte array.
		/// </summary>
		/// <param name="source"> the Base64 encoded data </param>
		/// <param name="off">    the offset of where to begin decoding </param>
		/// <param name="len">    the length of characters to decode </param>
		/// <returns> decoded data
		/// @since 1.3 </returns>
		/// <exception cref="Base64DecoderException"> </exception>
		public static sbyte[] decode(sbyte[] source, int off, int len)
		{
			return decode(source, off, len, DECODABET);
		}

		/// <summary>
		/// Decodes web safe Base64 content in byte array format and returns
		/// the decoded byte array.
		/// Web safe encoding uses '-' instead of '+', '_' instead of '/'
		/// </summary>
		/// <param name="source"> the Base64 encoded data </param>
		/// <param name="off">    the offset of where to begin decoding </param>
		/// <param name="len">    the length of characters to decode </param>
		/// <returns> decoded data </returns>
		public static sbyte[] decodeWebSafe(sbyte[] source, int off, int len)
		{
			return decode(source, off, len, WEBSAFE_DECODABET);
		}

		/// <summary>
		/// Decodes Base64 content using the supplied decodabet and returns
		/// the decoded byte array.
		/// </summary>
		/// <param name="source"> the Base64 encoded data </param>
		/// <param name="off"> the offset of where to begin decoding </param>
		/// <param name="len"> the length of characters to decode </param>
		/// <param name="decodabet"> the decodabet for decoding Base64 content </param>
		/// <returns> decoded data </returns>
		public static sbyte[] decode(sbyte[] source, int off, int len, sbyte[] decodabet)
		{
			int len34 = len * 3 / 4;
			sbyte[] outBuff = new sbyte[2 + len34]; // Upper limit on size of output
			int outBuffPosn = 0;

            sbyte[] b4 = new sbyte[4];
			int b4Posn = 0;
			int i = 0;
			sbyte sbiCrop = 0;
			sbyte sbiDecode = 0;
			for (i = 0; i < len; i++)
			{
				sbiCrop = (sbyte)(source[i + off] & 0x7f); // Only the low seven bits
				sbiDecode = decodabet[sbiCrop];

				if (sbiDecode >= WHITE_SPACE_ENC) // White space Equals sign or better
				{
					if (sbiDecode >= EQUALS_SIGN_ENC)
					{
						// An equals sign (for padding) must not occur at position 0 or 1
						// and must be the last byte[s] in the encoded value
						if (sbiCrop == EQUALS_SIGN)
						{
							int bytesLeft = len - i;
							sbyte lastByte = (sbyte)(source[len - 1 + off] & 0x7f);
							if (b4Posn == 0 || b4Posn == 1)
							{
								throw new Base64DecoderException("invalid padding byte '=' at byte offset " + i);
							}
							else if ((b4Posn == 3 && bytesLeft > 2) || (b4Posn == 4 && bytesLeft > 1))
							{
								throw new Base64DecoderException("padding byte '=' falsely signals end of encoded value " + "at offset " + i);
							}
							else if (lastByte != EQUALS_SIGN && lastByte != NEW_LINE)
							{
								throw new Base64DecoderException("encoded value has invalid trailing byte");
							}
							break;
						}

						b4[b4Posn++] = sbiCrop;
						if (b4Posn == 4)
						{
							outBuffPosn += decode4to3(b4, 0, outBuff, outBuffPosn, decodabet);
							b4Posn = 0;
						}
					}
				}
				else
				{
					throw new Base64DecoderException("Bad Base64 input character at " + i + ": " + source[i + off] + "(decimal)");
				}
			}

			// Because web safe encoding allows non padding base64 encodes, we
			// need to pad the rest of the b4 buffer with equal signs when
			// b4Posn != 0.  There can be at most 2 equal signs at the end of
			// four characters, so the b4 buffer must have two or three
			// characters.  This also catches the case where the input is
			// padded with EQUALS_SIGN
			if (b4Posn != 0)
			{
				if (b4Posn == 1)
				{
					throw new Base64DecoderException("single trailing character at offset " + (len - 1));
				}
				b4[b4Posn++] = EQUALS_SIGN;
				outBuffPosn += decode4to3(b4, 0, outBuff, outBuffPosn, decodabet);
			}

			sbyte[] outBytes = new sbyte[outBuffPosn];
            System.Array.Copy(outBuff, 0, outBytes, 0, outBuffPosn);
            return outBytes;
		}
	}
}