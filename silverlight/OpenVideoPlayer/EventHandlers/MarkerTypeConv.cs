using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace org.OpenVideoPlayer.EventHandlers
{
    public class MarkerTypeConv
    {
        private const String CHAPTER_MARKER = "NAME";
        private const String CAPTION_MARKER = "CAPTION";
        private const String INTERRUPT_MARKER = "INTERRUPT";
        public static MarkerTypes StringToMarkerType(string name)
        {
            switch (name.ToUpper())
            {
                case "NAME":    //SUPPORT MULTIPLE NAMES FOR A CHAPTER MARKER
                case "CHAPTER":
                    return MarkerTypes.Chapter;
                case "CAPTION":
                    return MarkerTypes.Caption;
                case "INTERRUPT":
                    return MarkerTypes.Interrupt;
                default:
                    return MarkerTypes.Unknown;
            }
        }
    }
}
