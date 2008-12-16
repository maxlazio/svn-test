using System;
using System.Windows;
using System.Text;
using System.Net;

namespace org.OpenVideoPlayer.Util {


	public static class StringTools {

		#region Byte[]/Hex Conversion Methods
		private static char[] HEX_DIGITS = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
		public static string ConvertToHexString(sbyte value) {
			return ConvertToHexString(new [] { value });
		}
		public static string ConvertToHexString(sbyte[] value) {
			byte[] conversion = new byte[value.Length];
			for (int i = 0; i < value.Length; i++)
				conversion[i] = (byte)value[i];

			return ConvertToHexString(conversion);
		}
		public static string ConvertToHexString(byte value) {
			return ConvertToHexString(new [] { value });
		}
		public static string ConvertToHexString(byte[] value) {
			char[] retArray = new char[value.Length * 2];
			for (int i = 0; i < value.Length; i++) {
				int b = value[i];
				retArray[i * 2] = HEX_DIGITS[b >> 4];
				retArray[i * 2 + 1] = HEX_DIGITS[b & 0xF];
			}

			if (retArray.Length == 0) return "''";
			return "0x" + new string(retArray);
		}
		public static byte[] ConvertToByteArray(string value) {
			if (value.ToLower().StartsWith("0x"))
				value = value.Substring(2);

			byte[] retArray = new byte[value.Length / 2];
			for (int i = 0; i < value.Length; i += 2)
				retArray[i / 2] = Convert.ToByte(value[i].ToString() + value[i + 1], 16);

			return retArray;
		}
		public static sbyte[] ConvertToSByteArray(string value) {
			if (value.ToLower().StartsWith("0x"))
				value = value.Substring(2);

			sbyte[] retArray = new sbyte[value.Length / 2];
			for (int i = 0; i < value.Length; i += 2)
				retArray[i / 2] = Convert.ToSByte(value[i] + value[i + 1].ToString(), 16);

			return retArray;
		}

		public static int GetByteCount(string hexString) {
			int numHexChars = 0;
			char c;
			// remove all none A-F, 0-9, characters
			for (int i = 0; i < hexString.Length; i++) {
				c = hexString[i];
				if (IsHexDigit(c))
					numHexChars++;
			}
			// if odd number of characters, discard last character
			if (numHexChars % 2 != 0) {
				numHexChars--;
			}
			return numHexChars / 2; // 2 characters per byte
		}
		/// <summary>
		/// Creates a byte array from the hexadecimal string. Each two characters are combined
		/// to create one byte. First two hexadecimal characters become first byte in returned array.
		/// Non-hexadecimal characters are ignored. 
		/// </summary>
		/// <param name="hexString">string to convert to byte array</param>
		/// <param name="discarded">number of characters in string ignored</param>
		/// <returns>byte array, in the same left-to-right order as the hexString</returns>
		public static byte[] GetBytes(string hexString, out int discarded) {
			discarded = 0;
			string newString = "";
			char c;
			// remove all none A-F, 0-9, characters
			for (int i = 0; i < hexString.Length; i++) {
				c = hexString[i];
				if (IsHexDigit(c))
					newString += c;
				else
					discarded++;
			}
			// if odd number of characters, discard last character
			if (newString.Length % 2 != 0) {
				discarded++;
				newString = newString.Substring(0, newString.Length - 1);
			}

			int byteLength = newString.Length / 2;
			byte[] bytes = new byte[byteLength];
			string hex;
			int j = 0;
			for (int i = 0; i < bytes.Length; i++) {
				hex = new String(new [] { newString[j], newString[j + 1] });
				bytes[i] = HexToByte(hex);
				j = j + 2;
			}
			return bytes;
		}
		public static string ToString(byte[] bytes) {
			string hexString = "";
			for (int i = 0; i < bytes.Length; i++) {
				hexString += bytes[i].ToString("X2");
			}
			return hexString;
		}

		public static string ToString(Array a) {
			StringBuilder s = new StringBuilder();
			foreach (object o in a) s.AppendFormat("{0} ", o);
			s.Length -= 1;
			//foreach (MediaFormat mf in encoder.InputFormats) input += mf + " ";
			return s.ToString();
		}

		/// <summary>
		/// Determines if given string is in proper hexadecimal string format
		/// </summary>
		/// <param name="hexString"></param>
		/// <returns></returns>
		public static bool InHexFormat(string hexString) {
			bool hexFormat = true;

			foreach (char digit in hexString) {
				if (!IsHexDigit(digit)) {
					hexFormat = false;
					break;
				}
			}
			return hexFormat;
		}

		/// <summary>
		/// Returns true is c is a hexadecimal digit (A-F, a-f, 0-9)
		/// </summary>
		/// <param name="c">Character to test</param>
		/// <returns>true if hex digit, false if not</returns>
		public static bool IsHexDigit(Char c) {
			int numChar;
			int numA = Convert.ToInt32('A');
			int num1 = Convert.ToInt32('0');
			c = Char.ToUpper(c);
			numChar = Convert.ToInt32(c);
			if (numChar >= numA && numChar < (numA + 6))
				return true;
			if (numChar >= num1 && numChar < (num1 + 10))
				return true;
			return false;
		}
		/// <summary>
		/// Converts 1 or 2 character string into equivalant byte value
		/// </summary>
		/// <param name="hex">1 or 2 character string</param>
		/// <returns>byte</returns>
		private static byte HexToByte(string hex) {
			if (hex.Length > 2 || hex.Length <= 0)
				throw new ArgumentException("hex must be 1 or 2 characters in length");
			byte newByte = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
			return newByte;
		}
		#endregion

		#region String to int, etc..
		/// <summary>
		/// Convert a string to an integer without throwing an exception.
		/// </summary>
		/// <param name="str">The string to be converted</param>
		/// <returns>
		/// 0 if the string is invalid, otherwise the integer value of
		/// the given string.  Note:  All decimals are truncated.
		/// </returns>
		public static int StringToInt(string str) {
			return StringToInt(str, 0);
		}

		/// <summary>
		/// Convert a string to an integer without throwing an exception.
		/// </summary>
		/// <param name="str">The string to be converted</param>
		/// <param name="defaultValue">
		/// The value returned if the string is invalid.
		/// </param>
		/// <returns>
		/// defaultValue if the string is invalid, otherwise the integer
		/// value of the given string.  Note:  All decimals are truncated.
		/// </returns>
		public static int StringToInt(string str, int defaultValue) {
			if (str == null) return defaultValue;
			try {
				str = str.Trim();
				if (str.Length == 0) {
					return defaultValue;
				}
				for (int i = 0; i < str.Length; i++) {
					if (str[i] < '0' || str[i] > '9') {
						if (str[i] == '.') {
							for (int j = i + 1; j < str.Length; j++) {
								if (str[j] < '0' || str[j] > '9') {
									return defaultValue;
								}
							}
							str = (i == 0) ? "0" : str.Substring(0, i);
							break;
						} 
						if (str[i] != '-' || i != 0) {
							return defaultValue;
						}
					}
				}
				return Convert.ToInt32(str);
			} catch {
				return defaultValue;
			}
		}

		public static long StringToLong(string str, long defaultValue) {
			if (str == null) return defaultValue;
			try {
				str = str.Trim();
				if (str.Length == 0) {
					return defaultValue;
				}
				for (int i = 0; i < str.Length; i++) {
					if (str[i] < '0' || str[i] > '9') {
						if (str[i] == '.') {
							for (int j = i + 1; j < str.Length; j++) {
								if (str[j] < '0' || str[j] > '9') {
									return defaultValue;
								}
							}
							str = (i == 0) ? "0" : str.Substring(0, i);
							break;
						} 
						if (str[i] != '-' || i != 0) {
							return defaultValue;
						}
					}
				}
				return Convert.ToInt64(str);
			} catch {
				return defaultValue;
			}
		}

		public static int StringToInt(string str, int defaultValue, bool ignore) {
			try {
				str = str.Trim();
				if (str.Length == 0) {
					return defaultValue;
				}
				for (int i = 0; i < str.Length; i++) {
					if (str[i] < '0' || str[i] > '9') {
						if (str[i] == '-' && i == 0) {
							continue;
						} 
						if (ignore) {
							str = str.Substring(0, i);
							break;
						} 
						if (str[i] == '.') {
							for (int j = i + 1; j < str.Length; j++) {
								if (str[j] < '0' || str[j] > '9') {
									return defaultValue;
								}
							}
							str = (i == 0) ? "0" : str.Substring(0, i);
							break;
						}
					}
				}
				return Convert.ToInt32(str);
			} catch {
				return defaultValue;
			}
		}
		#endregion

		#region Http

		/// <summary>
		/// Encodes string into a format that is safe for use with ASCII or HTTP query strings
		/// </summary>
		public static string StringToHttpSafeBase64(string plainString) {
			byte[] bytes = Encoding.UTF8.GetBytes(plainString);
			string b64 = Convert.ToBase64String(bytes);
			return new StringBuilder(b64).Replace('+', '*').Replace('/', '-').Replace('=', '_').ToString();
		}

		public static string Base64ToHttpSafeBase64(string base64) {
			return new StringBuilder(base64).Replace('+', '*').Replace('/', '-').Replace('=', '_').ToString();
		}

		public static string HttpSafeBase64ToBase64(string safebase64) {
			return new StringBuilder(safebase64).Replace('*', '+').Replace('-', '/').Replace('_', '=').ToString();
		}

		/// <summary>
		/// Gives you back your original string from StringToHttpSafeBase64
		/// </summary>
		public static string StringFromHttpSafeBase64(string specialBase64String) {
			string b64 = new StringBuilder(specialBase64String).Replace('*', '+').Replace('-', '/').Replace('_', '=').ToString();
			byte[] bytes = Convert.FromBase64String(b64);
			return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
		}

		#endregion

		#region Misc

		public static string FriendlyBitsPerSec(int bps) {
			if (bps < 1024) {
				return bps + " bps";
			} 
			if (bps < (1024 * 1024)) {
				return (bps / 1024) + " kbps";
			} 
			if (bps < (1024 * 1024 * 1024)) {
				return Math.Round((float)bps / (1024 * 1024), 1) + " mbps";
			} 
			return Math.Round((float)bps / (1024 * 1024 * 1024), 1) + " gbps";
		}

		public static string FriendlyBytes(long bytes) {
			if (bytes < 1024) {
				return bytes + " B";
			}
			if (bytes < (1024*1024)) {
				return (bytes/1024) + " K";
			}
			if (bytes < (1024*1024*1024)) {
				return Math.Round((float) bytes/(1024*1024), 1) + " M";
			}

			return Math.Round((float) bytes/(1024*1024*1024), 1) + " G";
		}

		/// <summary>
		/// Determine whether a string fits a given mask.
		/// (NOTE: This is case insensitive.)
		/// </summary>
		/// <param name="entry">The string to compare.</param>
		/// <param name="mask">The mask to match against.</param>
		/// <returns>True if the string fits the mask, false if not.</returns>
		public static bool MatchMask(string entry, string mask) {
			bool retVal = true;
			int index = 0;
			int found = 0;
			entry = entry.ToLower();
			mask = mask.ToLower();
			string[] tokens = mask.Split('*');
			if (tokens.Length == 1) {
				if (entry != mask) {
					retVal = false;
				}
			} else {
				for (int i = 0; i < tokens.Length; i++) {
					if (i == 0) {
						if (!entry.StartsWith(tokens[i])) {
							retVal = false;
							break;
						} 
						index += tokens[i].Length;
						
					} else if (i == tokens.Length - 1) {
						if (!entry.EndsWith(tokens[i])) {
							retVal = false;
							break;
						}
					} else {
						found = entry.IndexOf(tokens[i], index);
						if (found == -1) {
							retVal = false;
							break;
						} 
						index += found + tokens[i].Length;
					}
				}
			}
			return retVal;
		}

		/// <summary>
		/// Convert a string in "1.1.1.1:8000" format to an IPEndPoint.
		/// </summary>
		public static IPEndPoint StringToEndPoint(string ip) {
			int port = 0;
			ip = ip.Trim();
			int colonPos = ip.LastIndexOf(":");
			if (colonPos > -1) {
				port = Convert.ToInt32(ip.Substring(colonPos + 1));
			} else {
				colonPos = ip.Length;
			}
			int startPos = ip.LastIndexOf("/");
			string ipString = ip.Substring(startPos + 1, colonPos - (startPos + 1));
			return new IPEndPoint(IPAddress.Parse(ipString), port);
		}

		public static Size SizefromString(string res) {
			Size s;
			try {
				int x = res.IndexOf("x");
				if (x > 0) {
					int w = Convert.ToInt32(res.Substring(0, x));
					int h = Convert.ToInt32(res.Substring(x + 1));
					s = new Size(w, h);
				} else {
					int y = res.IndexOf(",");
					int w = StringToInt(res.Substring(1, y), 0);
					int h = StringToInt(res.Substring(y + 1), 0);
					s = new Size(w, h);
				}
			} catch {
				s = Size.Empty;
			}
			return s;
		}

		public static String SizetoString(Size res) {
			return res.Width + "x" + res.Height;
		}

		public static string ConvertToOctetString(byte[] values) {

			return ConvertToOctetString(values, false, false);

		}

		public static string ConvertToOctetString(byte[] values, bool

		isAddBackslash) {

			return ConvertToOctetString(values, isAddBackslash, false);

		}

		public static string ConvertToOctetString(byte[] values, bool

		isAddBackslash, bool isUpperCase) {
			int iterator;

			StringBuilder builder = new StringBuilder(values.Length * 2);

			string slash;

			if (isAddBackslash) {
				slash = "\\";
			} else {
				slash = string.Empty;
			}

			string formatCode;

			if (isUpperCase) {
				formatCode = "X2";
			} else {
				formatCode = "x2";
			}

			for (iterator = 0; iterator < values.Length; iterator++) {
				builder.Append(slash);

				builder.Append(values[iterator].ToString(formatCode));
			}

			return builder.ToString();
		}

		//public static String LogListToString(IList<OutputEntry> list) {
		//    StringBuilder logs = new StringBuilder(8192);
		//    foreach (OutputEntry oe in list) {
		//        try {
		//            logs.AppendLine(oe.ToString());
		//            //string.Format("{0}, {1}, {2}, {3}, {4}",
		//            //              oe.Timestamp.ToString("yyyy-MM-dd - hh:mm:ss.ff"),
		//            //              oe.Source,
		//            //              oe.OutputType,
		//            //              oe.Description,
		//            //              oe.ExtendedDesc
		//            //    ));
		//        } catch { }
		//    }
		//    return logs.ToString();
		//}

		//public static string CommaDelimitEnumNames(object e) {
		//    try {
		//        if (!e.GetType().IsEnum) return "";
		//        StringBuilder ret = new StringBuilder(64);
		//        foreach (long i in Enum.GetValues(e.GetType())) {
		//            if (((long)e & i) != 0) {
		//                ret.AppendFormat("{0},", Enum.GetName(e.GetType(), i));
		//            }
		//        }
		//        ret.Length -= 1;
		//        return ret.ToString();
		//    } catch {// (Exception ex) {
		//        throw;
		//    }
		//}
		#endregion

		public static bool ToBool(string p) {
			if (p == null) return false;
			string t = p.Trim().ToLower();
			return (t == "1" || t == "true");
		}

		public static float ToSingle(string p, int p_2) {
			try {
				return Convert.ToSingle(p);
			} catch {
				return p_2;
			}
		}
	}

}