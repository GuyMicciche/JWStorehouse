using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Android.Widget;

using System;
using System.Threading;

namespace JWChinese
{
    public class ObservableWebView : WebView
    {
        private IObservableOnScrollChangedCallback scrollChangedCallback;

        private ArticleFragment parentFragment;
        private bool isDeflated = false;

        public ActionMode.ICallback actionModeCallback;
        public ActionMode.ICallback selectActionModeCallback;
        public GestureDetector gestureDetector;
        public ActionMode actionMode;

        public ObservableWebView(Context context)
            : base(context)
        {
            Init();
        }

        public ObservableWebView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Init();
        }

        public ObservableWebView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Init();
        }

        public ObservableWebView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Init();
        }

        private void Init()
        {
            WebViewGestureListener listener = new WebViewGestureListener(this);
            gestureDetector = new GestureDetector(listener);
        }

        public IObservableOnScrollChangedCallback ScrollChangedCallback
        {
            get
            {
                return this.scrollChangedCallback;
            }
            set
            {
                this.scrollChangedCallback = value;
            }
        }

        protected override void OnScrollChanged(int horizontal, int vertical, int oldHorizontal, int oldVertical)
        {
            base.OnScrollChanged(horizontal, vertical, oldHorizontal, oldVertical);
            if (scrollChangedCallback != null)
            {
                scrollChangedCallback.OnScroll(this, horizontal, vertical);
            }
        }

        //public override ActionMode StartActionMode(ActionMode.ICallback callback)
        //{
        //    string name = ((Java.Lang.Object)callback).Class.ToString();
        //    Console.WriteLine("THIS IS THE CLASS NAME -> " + name);
        //    if (name.Contains("SelectActionModeCallback"))
        //    {
        //        selectActionModeCallback = callback;
        //    }

        //    actionModeCallback = new WebViewActionBarCallback(this);

        //    return base.StartActionModeForChild(this, actionModeCallback);
        //}

        public override bool OnTouchEvent(MotionEvent e)
        {
            switch (e.Action & MotionEventActions.Mask)
            {
                // Two-finger up
                case MotionEventActions.Pointer1Up:
                    WebviewExpand();
                    break;
            }

            gestureDetector.OnTouchEvent(e);

            return base.OnTouchEvent(e);
        }

        public void WebviewExpand()
        {
            try
            {
                LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent);

                if (((LinearLayout.LayoutParams)((LinearLayout)this.Parent).LayoutParameters).Weight == 1)
                {
                    lp.Weight = 0;
                    IsDeflated = true;
                }
                else
                {
                    lp.Weight = 1;
                    IsDeflated = false;
                }

                ((LinearLayout)this.Parent).LayoutParameters = lp;

                if ((string)this.Tag == "primary")
                {
                    App.STATE.WebviewWeights[0] = lp.Weight;
                }
                else
                {
                    App.STATE.WebviewWeights[1] = lp.Weight;
                }

                ThreadPool.QueueUserWorkItem((o) => App.STATE.SaveUserPreferences());
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public ArticleFragment ParentFragment
        {
            get
            {
                return this.parentFragment;
            }
            set
            {
                this.parentFragment = value;
            }
        }

        public bool IsDeflated
        {
            get
            {
                return this.isDeflated;
            }
            set
            {
                this.isDeflated = value;
            }
        }
    }

    public class WebViewActionBarCallback : Java.Lang.Object, ActionMode.ICallback
    {
        private const int HIGHLIGHT_MENU = 0;
        private const int CLEAR_HIGHLIGHT_MENU = 1;

        private ObservableWebView webview;

        public WebViewActionBarCallback(ObservableWebView webview)
        {
            this.webview = webview;
        }

        public bool OnCreateActionMode(ActionMode mode, IMenu menu)
        {
            webview.actionMode = mode;

            //MenuInflater inflater = mode.MenuInflater;
            //inflater.Inflate(Resource.Menu.highlighting_menu, menu);

            var highlightMenu = menu.Add(0, HIGHLIGHT_MENU, HIGHLIGHT_MENU, "Highlight");
            highlightMenu.SetShowAsAction(ShowAsAction.IfRoom);
            highlightMenu.SetIcon(Resource.Drawable.highligher);

            var clearHighlightMenu = menu.Add(0, CLEAR_HIGHLIGHT_MENU, CLEAR_HIGHLIGHT_MENU, "Clear Highlights");
            clearHighlightMenu.SetShowAsAction(ShowAsAction.IfRoom);
            clearHighlightMenu.SetIcon(Resource.Drawable.clear);

            return true;
        }

        public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
        {
            switch (item.ItemId)
            {
                case HIGHLIGHT_MENU:
                    webview.LoadUrl("javascript:HighlightSelection()");
                    break;
                case CLEAR_HIGHLIGHT_MENU:
                    webview.LoadUrl("javascript:ClearHighlights()");
                    break;
            }

            mode.Finish();
            return true;
        }

        public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
        {
            string js = "javascript:SetSelectedTextInfo()";
            webview.LoadUrl(js);

            return false;
        }

        public void OnDestroyActionMode(ActionMode mode)
        {
            webview.ClearFocus();

            if (webview.selectActionModeCallback != null)
            {
                webview.selectActionModeCallback.OnDestroyActionMode(mode);
            }

            webview.actionMode = null;
        }
    }

    public class WebViewGestureListener : GestureDetector.SimpleOnGestureListener
    {
        private static int SWIPE_MIN_DISTANCE = 120;
        private static int SWIPE_MAX_OFF_PATH = 250;
        private static int SWIPE_THRESHOLD_VELOCITY = 200;

        private ObservableWebView webview;

        public WebViewGestureListener(ObservableWebView webview)
        {
            this.webview = webview;
        }

        public override bool OnSingleTapUp(MotionEvent e)
        {
            if (webview.actionMode != null)
            {
                webview.actionMode.Finish();
                return true;
            }

            return false;
        }

        public override bool OnDoubleTap(MotionEvent e)
        {
            webview.ParentFragment.ShowChapterPrompt();

            return true;
        }

        public override bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            try 
            {
                //do not do anything if the swipe does not reach a certain length of distance
                if (Math.Abs(e1.GetY() - e2.GetY()) > SWIPE_MAX_OFF_PATH)
                {
                    return false;
                }
 
                // right to left swipe
                if(e1.GetX() - e2.GetX() > SWIPE_MIN_DISTANCE && Math.Abs(velocityX) > SWIPE_THRESHOLD_VELOCITY)
                {
                    webview.ParentFragment.ChangeArticle(1);
                }
                // left to right swipe
                else if (e2.GetX() - e1.GetX() > SWIPE_MIN_DISTANCE && Math.Abs(velocityX) > SWIPE_THRESHOLD_VELOCITY)
                {
                    webview.ParentFragment.ChangeArticle(-1);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return true;
        }
    }
}