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
using org.OpenVideoPlayer.Advertising.VPAID;

namespace org.OpenVideoPlayer.Plugins {

	public interface IAdPlayer : IVPAID, IPlugin {
		string[] SupportedMimeTypes { get; }

		bool CheckSupport(string content);

		bool SetTarget(IMastTarget target, out Size size);

		bool Remove(IMastTarget target);
	}
}
