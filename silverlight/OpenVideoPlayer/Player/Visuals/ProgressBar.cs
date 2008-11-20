using System;
using System.Windows;

namespace org.OpenVideoPlayer.Player.Visuals {
	[TemplatePart(Name = ProgressBar.DeterminateRoot, Type = typeof(FrameworkElement))]
	public class ProgressBar : System.Windows.Controls.ProgressBar {
		/// <summary>
		/// String for the visual element of the progress bar.
		/// </summary>
		private const string DeterminateRoot = "DeterminateRoot";

		/// <summary>
		/// visual element representing progress bar indicator
		/// </summary>
		private FrameworkElement m_DeterminateRoot;

		/// <summary>
		/// dependancy property for Download Offset
		/// </summary>
		public static readonly DependencyProperty DownloadOffsetProperty = DependencyProperty.Register("DownloadOffset", typeof(Double), typeof(ProgressBar), new PropertyMetadata(new PropertyChangedCallback(ProgressBar.OnDownloadProgressPropertyChanged)));

		/// <summary>
		/// extends ProgressBar by allowing start offset indicating DownloadProgressOffset
		/// </summary>
		public ProgressBar() {
			DownloadOffset = 0.0;
		}
		/// <summary>
		/// overridden OnApplyTemplate, sets the DeterminateRoot member, which is the visual element for the progress indicator
		/// </summary>
		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			m_DeterminateRoot = GetTemplateChild("DeterminateRoot") as FrameworkElement;
		}
		/// <summary>
		/// represents the amount downloaded, ie. offset to the start of the downloaded progress indicator
		/// </summary>
		public Double DownloadOffset {
			get { return (Double)this.GetValue(DownloadOffsetProperty); }
			set { this.SetValue(DownloadOffsetProperty, value); }
		}
		/// <summary>
		/// update the visual for the offset
		/// </summary>
		/// <param name="offset">amount of offset (gets bounded between Minimum and Maximum)</param>
		public void SetOffsetVisual(double offset) {
			offset = Math.Max(Math.Min(offset, Maximum), Minimum);
			Double left = ((offset - Minimum) / (Maximum - Minimum)) * ActualWidth;
			if (m_DeterminateRoot != null) {
				m_DeterminateRoot.Margin = new Thickness(left, m_DeterminateRoot.Margin.Top,
														 m_DeterminateRoot.Margin.Right, m_DeterminateRoot.Margin.Bottom);
			}
		}
		/// <summary>
		/// called when "DownloadProgressOffset" is set
		/// </summary>
		/// <param name="obj">DownloadProgressBar</param>
		/// <param name="args">new value etc.</param>
		protected static void OnDownloadProgressPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {
			ProgressBar downloadProgressBar = obj as ProgressBar;
			if (downloadProgressBar != null) {
				downloadProgressBar.SetOffsetVisual((Double)args.NewValue);
			}
		}
	}
}
