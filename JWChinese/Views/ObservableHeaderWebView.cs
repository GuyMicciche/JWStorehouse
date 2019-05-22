using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Webkit;

namespace JWChinese
{
    public class ObservableHeaderWebView : WebView
    {
        private IOnScrollChangedCallback mOnScrollChangedCallback;
        private int headerHeight;
        private bool touchInHeader;

        public ObservableHeaderWebView(Context context)
            : base(context)
        {
        }

        public ObservableHeaderWebView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
        }

        public ObservableHeaderWebView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
        }


        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            View title = GetChildAt(0);
            headerHeight = title == null ? 0 : title.MeasuredHeight;
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            return true;
        }


        public override bool DispatchTouchEvent(MotionEvent me)
        {

            bool wasInTitle = false;
            switch (me.ActionMasked)
            {
                case MotionEventActions.Down:
                    touchInHeader = (me.YPrecision <= VisibleHeaderHeight);
                    break;

                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    wasInTitle = touchInHeader;
                    touchInHeader = false;
                    break;
            }
            if (touchInHeader || wasInTitle)
            {
                View title = GetChildAt(0);
                if (title != null)
                {
                    me.OffsetLocation(0, ScrollY);
                    return title.DispatchTouchEvent(me);
                }
            }
            me.OffsetLocation(0, -headerHeight);
            return base.DispatchTouchEvent(me);
        }

        private int VisibleHeaderHeight
        {
            get 
            {
                return headerHeight - ScrollY;
            }
        }

        protected override void OnDraw(Canvas c)
        {
            c.Save();
            int tH = VisibleHeaderHeight;
            if (tH > 0)
            {
                int sx = ScrollX, sy = ScrollY;
                c.ClipRect(sx, sy + tH, sx + Width, sy + Height);
            }
            c.Translate(0, headerHeight);
            base.OnDraw(c);
            c.Restore();
        }

        protected override void OnScrollChanged(int l, int t, int oldl, int oldt)
        {
            base.OnScrollChanged(l, t, oldl, oldt);

            View title = GetChildAt(0);
            if (title != null) // undo horizontal scroll, so that title scrolls only vertically
            {
                title.OffsetLeftAndRight(l - title.Left);
            }

            if (mOnScrollChangedCallback != null)
            {
                mOnScrollChangedCallback.OnScroll(l, t);
            }
        }

        public IOnScrollChangedCallback ScrollChangedCallback
        {
            get
            {
                return this.mOnScrollChangedCallback;
            }
            set
            {
                this.mOnScrollChangedCallback = value;
            }
        }
    }
}