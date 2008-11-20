using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace org.OpenVideoPlayer.Player
{
    /// <summary>
    /// Defines an interface for generating a MediaStreamSource mainly used
    /// for getting a particular version of the adaptive heuristics.
    /// </summary>
    public interface IAdaptiveStreamSourceFactory
    {
        /// <summary>
        /// Returns a MediaStreamSource Object implementation for a particular
        /// MediaElement and URI
        /// </summary>
        /// <param name="mediaElement"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        MediaStreamSource GetMediaStreamSource(MediaElement mediaElement, Uri uri);
    }
}
