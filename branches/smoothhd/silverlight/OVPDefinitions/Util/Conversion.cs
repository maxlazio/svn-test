using System;
using System.Globalization;
using System.Windows.Media;

namespace org.OpenVideoPlayer.Util {
	/// <summary>
	/// The conversions class provides a number of conversions for various types
	/// </summary>
	public static class Conversion {

		public static object Change(object o, Type t) {
			if(o==null)return null;
			string s = o.ToString();

			if(t.IsEnum) {
				if (t == typeof(Stretch)) {
					return ParseStretchMode(s);
				}
				return Enum.Parse(t, s, true);
			}

			if (t == typeof(bool)) {
				return ParseBool(s);
			}

			return Convert.ChangeType(o, t, null);
		}

		public static bool ParseBool(string s) {
			return (s == "1" || s.ToUpper() == "TRUE");
		}

		public static Stretch ParseStretchMode(string s) {
			s = s.ToLower();  // force it to-lower for matching
			if (s == "1" || s == "uniform" || s == "fit") {
				return Stretch.Uniform;
			}

			if (s == "2" || s == "uniformtofill" || s == "fill") {
				return Stretch.UniformToFill;
			}

			if (s == "3" || s == "stretch") {
				return Stretch.Fill;
			}

			//nativs
			return Stretch.None;
		}

		/// <summary>
		/// Creates a URI from both a URI and a filename.  This is used to get an absolute path from a remote
		/// source when only the relative path is given and also to give the absolute path directly when the
		/// filename given is an absolute URI
		/// </summary>
		/// <param name="uri">The uri from which the filename is loaded</param>
		/// <param name="fileName">The filename to load into a uri</param>
		/// <returns>Resulting merged uri object</returns>
		public static Uri GetPathFromUri(Uri uri, String fileName) {
			//detect if the fileName is a well-formed uri
			Uri u = new Uri(fileName);

			
		//	if (u.Host == "localhost" && u.Port == 80) {
			if (!Uri.IsWellFormedUriString(fileName, UriKind.Absolute)) {
				string f = Uri.EscapeUriString(fileName);
				if (Uri.IsWellFormedUriString(f, UriKind.Absolute)) {
					return new Uri(f);
				}
				return new Uri(uri, fileName);
				//if the host is localhost we can assume innocuously that the url passed in was relative
				//so add the source uri to get an absolute one
				//UriBuilder urib = new UriBuilder(uri);
				//urib.Path =
				//    System.Uri.UnescapeDataString(
				//        uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf("/", StringComparison.Ordinal)) +
				//        "/" + fileName);
				//return urib.Uri;
			} else {
				return new Uri(fileName);
			}
		}

		/// <summary>
		/// Converts a string of an ARGB color to a System.color type
		/// </summary>
		/// <param name="color">The ARGB color profile to parse</param>
		/// <returns>The resulting System.color type</returns>
		public static Color ColorFromString(String color) {
			UInt32 uiValue = UInt32.Parse(color.Substring(1), System.Globalization.NumberStyles.AllowHexSpecifier | System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			byte a = (byte)((uiValue >> 0x18) & 0xFF);
			byte r = (byte)((uiValue >> 0x10) & 0xFF);
			byte g = (byte)((uiValue >> 0x08) & 0xFF);
			byte b = (byte)((uiValue) & 0xFF);
			return Color.FromArgb(a, r, g, b);
		}
	}
}
