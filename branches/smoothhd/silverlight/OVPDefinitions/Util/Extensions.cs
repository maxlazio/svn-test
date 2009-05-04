using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace org.OpenVideoPlayer.Util {
	public static class Extensions {
		/// <summary>
		/// Calculates the sum of a size's width and height.  Goos for quick comparisons.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static double Sum(this Size s) {
			return s.Height * s.Width;
		}

		public static int Search(this string source, string[] strings, bool caseSensitive) {
			if (!caseSensitive) source = source.ToLower();
			foreach (string s in strings) {
				int i = source.IndexOf(s, caseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
				if (i >= 0) return i;
			}
			return -1;
		}

		public static bool Contains(this string[] strings, string item, bool caseSensitive) {
			foreach (string s in strings) {
				if (string.Compare(s, item, caseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase) == 0) {
					return true;
				}
			}
			return false;
		}

		public static string Section(this string source, string start, string end) {
			int s = source.IndexOf(start);
			if (s >= 0) {
				s += start.Length;
				int e = source.IndexOf(end, s);
				if (e > s) {
					return source.Substring(s, e - s);
				}
			}
			return "";
		}

		public static string SectionReplace(this string source, string start, string end, string replace) {
			StringBuilder sb = new StringBuilder(512);
			int s = source.IndexOf(start);
			if (s >= 0) {
				s += start.Length;
				int e = source.IndexOf(end, s);
				if (e > s) {
					sb.Append(source.Substring(0, s));
					sb.Append(replace);
					sb.Append(source.Substring(e));
					return sb.ToString();
				}
			}
			return source;
		}

		/// <summary>
		/// Scales a size based on another provided size
		/// </summary>
		/// <param name="s"></param>
		/// <param name="scale">A size used to scale this size</param>
		/// <returns>a new instance of Size</returns>
		public static Size Scale(this Size s, Size scale) {
			return new Size(s.Width + scale.Width, s.Height + scale.Height);
		}

		public static void Combine(this IDictionary d, object key, object val) {
			if (d.Contains(key)) {
				d[key] = val;
			} else {
				d.Add(key, val);
			}
		}

		public static void Combine(this IList l, object val) {
			if (!l.Contains(val)) {
				l.Add(val);
			}
		}

		public static void Combine<T,U> (this IDictionary<T,U> d, T key, U val) {
			if (d.ContainsKey(key)) {
				d[key] = val;
			} else {
				d.Add(key, val);
			}
		}

		public static void Randomize<T>(this IList<T> l) {
			Randomize<T>(l, 0);
		}

		public static void Randomize<T>(this IList<T> l, int startIndex) {
			lock(l) {
				List<T> temp = new List<T>(); //temp shallow copy
				for (int t = startIndex; t < l.Count; t++) temp.Add(l[t]);

				Random random = new Random();
				List<int> rList = new List<int>(); // a list of randomized indicies
				while (rList.Count < temp.Count) {
					int r = (int)Math.Round(random.NextDouble() * (temp.Count-1));
					if (!rList.Contains(r)) rList.Add(r);
				}

				while (l.Count > startIndex) l.RemoveAt(startIndex);
				foreach (int i in rList) l.Add(temp[i]);
			}
		}

		public static Size FullSize(this FrameworkElement c) {
			return new Size(c.ActualWidth + c.Margin.Left + c.Margin.Right, c.ActualHeight + c.Margin.Top + c.Margin.Bottom);
		}
	}


	public static class Test {

		public static bool NullOrEmpty(System.Collections.ICollection col) {
			return col == null || col.Count < 1;
		}
		public static bool NullOrEmpty(string s) {
			return s == null || s.Length < 1;
		}
	}


}
