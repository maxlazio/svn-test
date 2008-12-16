using System;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using org.OpenVideoPlayer.Media;
using org.OpenVideoPlayer.Controls;

namespace org.OpenVideoPlayer.Util {
	public class OutputEntry {
		#region Vars
		public static DateTime Started = DateTime.Now;

		public DateTime Timestamp { get; set; }

		public static string TimeTemplate = "hh:mm:ss";
		public string Time {get {return Timestamp.ToString(TimeTemplate);}}

		public string Elapsed {
			get { 
				TimeSpan ts = (Timestamp - Started);
				return string.Format("{0}:{1}.{2}", ((int)ts.TotalMinutes).ToString("00"), ts.Seconds.ToString("00"), (ts.Milliseconds / 100));
			}
		}

		[DefaultValue(null)]
		public string Source { get; set; }

		public OutputType OutputType { get; set; }

		[DefaultValue(null)]
		public string Description { get; set; }

		[DefaultValue(null)]
		public string ExtendedDesc{get;set;}

		public Visibility Extended {
			get { return (OwnerCollection!=null) ? OwnerCollection.Extended : Visibility.Collapsed; }
		} // Visibility.Collapsed;

		[XmlIgnore]
		public Exception ExceptionObject;

		public Color[] Brushes = new [] {
			Colors.Orange,Colors.Gray, Colors.Red, Colors.White
																	//new SolidColorBrush(Colors.Orange),
																	//new SolidColorBrush(Colors.Gray),
																	//new SolidColorBrush(Colors.Red),
																	//new SolidColorBrush(Colors.White),
		                                                         };

		//TODO - make this more extensible..
		public Color Foreground {
			get {
				switch(OutputType) {
					case OutputType.Error:
						return Brushes[0];
					case OutputType.Debug:
						return Brushes[1];
					case OutputType.Critical:
						return Brushes[2];
					default:
						return Brushes[3];
				}
			}
		}

		/// <summary>
		/// Required to enable declaritve collections where playlistitems are instantiated in XAML with default constructor.
		/// </summary>
		public LogCollection OwnerCollection { get;  set; }

		public int MyIndex {
			get {
				if (OwnerCollection != null)return OwnerCollection.IndexOf(this) + 1;
				return -1;
			}
		}
		#endregion

		public OutputEntry() { }

		#region Methods
		public OutputEntry(DateTime timestamp, string source, OutputType outputType, string desc, string extendedDesc) {
			Timestamp = timestamp;
			Source = source;
			OutputType = outputType;
			Description = desc;
			ExtendedDesc = extendedDesc;
		}

		public OutputEntry(DateTime timestamp, string source, OutputType outputType, string desc, string extendedDesc, Exception ex) {
			Timestamp = timestamp;
			Source = source;
			OutputType = outputType;
			Description = desc;
			ExtendedDesc = extendedDesc;
			ExceptionObject = ex;
		}

		#endregion

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public override string ToString() {
			return string.Format("{0}, {1}, {2}, {3}  {4}",
				Timestamp.ToString("yy-MM-dd  hh:mm:ss"),
				//Timestamp.ToString("yy-MM-dd  hh:mm:ss"),
				OutputType,
				Source,
				Description,
				ExtendedDesc);
		}
	}
}
