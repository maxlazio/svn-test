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
using org.OpenVideoPlayer.Media;

namespace org.OpenVideoPlayer {

	public interface IMastPayloadHandler : IPlugin {
		IMastPayload Handle(IMastSource trigger);
	}

}