namespace org.OpenVideoPlayer.Player
{
    /// <summary>
    /// Defines the methods that an IMediaControl object must export.  This
    /// is one of the main interfaces implemented by the main player control
    /// some or all of these may be implemented as Scriptable Methods for
    /// easy access via JavaScript.
    /// </summary>
    public interface IMediaControl
    {
        /// <summary>
        /// Play current item
        /// </summary>
        void Play();

        /// <summary>
        /// Pause current item
        /// </summary>
        void Pause();

        /// <summary>
        /// Stop current play
        /// </summary>
        void Stop();

        /// <summary>
        /// Seeks to the next Chapter
        /// </summary>
        void SeekToNextChapter();

        /// <summary>
        /// Seek to the previous chapter
        /// </summary>
        void SeekToPreviousChapter();

        /// <summary>
        /// Seek to the next item in the playlist
        /// </summary>
        void SeekToNextItem();

        /// <summary>
        /// Seek to the previous item in the playlist
        /// </summary>
        void SeekToPreviousItem();

        /// <summary>
        /// Increments the volume by the given positive or negative number
        /// </summary>
        /// <param name="incrementValue">amount to increment by</param>
        void VolumeIncrement(double incrementValue);

        ///<summary>
        /// Shows or hides the debug panel
        /// </summary>
        void ToggleStatPanel();
    }
}
