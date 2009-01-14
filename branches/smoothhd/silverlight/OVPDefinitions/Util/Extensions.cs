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

		public static void Combine<T,U> (this IDictionary<T,U> d, T key, U val) {
			if (d.ContainsKey(key)) {
				d[key] = val;
			} else {
				d.Add(key, val);
			}
		}

		public static Size FullSize(this FrameworkElement c) {
			return new Size(c.ActualWidth + c.Margin.Left + c.Margin.Right, c.ActualHeight + c.Margin.Top + c.Margin.Bottom);
		}
	}
}
