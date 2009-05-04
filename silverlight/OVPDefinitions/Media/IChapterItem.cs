namespace org.OpenVideoPlayer.Media
{
    public interface IChapterItem
    {
        string Title { get; set; }
        double Position { get; set; }
        string PositionText { get; }
        string ThumbSource { get; set; }
        bool HasInterstitial { get; set; }
    }
}