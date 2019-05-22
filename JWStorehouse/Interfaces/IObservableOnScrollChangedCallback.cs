namespace JWStorehouse
{
    public interface IObservableOnScrollChangedCallback
    {
        void OnScroll(ObservableWebView view, int horizontal, int vertical);
    }
}