using Android.Content;
using Android.Database;
using Android.Util;
using Android.Views;
using Android.Widget;

using Java.Lang;

using System.Collections.Generic;

namespace JWChinese
{
    public class HeaderGridView : GridView
    {
        private const string TAG = "HeaderGridView";

        private class FixedViewInfo
        {
            public View view;
            public ViewGroup viewContainer;
            public Java.Lang.Object data;
            public bool isSelectable;
        }
        private List<FixedViewInfo> mHeaderViewInfos = new List<FixedViewInfo>();

        private void InitHeaderGridView()
        {
            base.SetClipChildren(false);
        }

        public HeaderGridView(Context context)
            : base(context)
        {
            InitHeaderGridView();
        }

        public HeaderGridView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            InitHeaderGridView();
        }

        public HeaderGridView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            InitHeaderGridView();
        }
        
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            IListAdapter adapter = (IListAdapter)Adapter;
            if (adapter != null && adapter is HeaderViewGridAdapter)
            {
                ((HeaderViewGridAdapter)adapter).NumColumns = NumColumns;
            }
        }

        public override bool ClipChildren
        {
            get
            {
                return base.ClipChildren;
            }
        }
        
        public void AddHeaderView(View v, Java.Lang.Object data, bool isSelectable)
        {
            IListAdapter adapter = (IListAdapter)Adapter;
            if (adapter != null && !(adapter is HeaderViewGridAdapter))
            {
                throw new IllegalStateException("Cannot add header view to grid -- setAdapter has already been called.");
            }
            FixedViewInfo info = new FixedViewInfo();
            FrameLayout fl = new FullWidthFixedViewLayout(this, Context);
            fl.AddView(v);
            info.view = v;
            info.viewContainer = fl;
            info.data = data;
            info.isSelectable = isSelectable;
            mHeaderViewInfos.Add(info);
            if (adapter != null)
            {
                ((HeaderViewGridAdapter)adapter).NotifyDataSetChanged();
            }
        }
        public void AddHeaderView(View v)
        {
            AddHeaderView(v, null, true);
        }
        public int HeaderViewCount
        {
            get
            {
                return mHeaderViewInfos.Count;
            }
        }

        public bool RemoveHeaderView(View v)
        {
            if (mHeaderViewInfos.Count > 0)
            {
                bool result = false;
                IListAdapter adapter = (IListAdapter)Adapter;
                if (adapter != null && ((HeaderViewGridAdapter)adapter).RemoveHeader(v))
                {
                    result = true;
                }
                RemoveFixedViewInfo(v, mHeaderViewInfos);
                return result;
            }
            return false;
        }

        private void RemoveFixedViewInfo(View v, List<FixedViewInfo> list)
        {
            int len = list.Count;
            for (int i = 0; i < len; ++i)
            {
                FixedViewInfo info = list[i];
                if (info.view == v)
                {
                    list.RemoveAt(i);
                    break;
                }
            }
        }
        public override IListAdapter Adapter
        {
            set
            {
                if (mHeaderViewInfos.Count > 0)
                {
                    HeaderViewGridAdapter hadapter = new HeaderViewGridAdapter(mHeaderViewInfos, (IListAdapter)value);
                    int numColumns = NumColumns;
                    if (numColumns > 1)
                    {
                        hadapter.NumColumns = numColumns;
                    }
                    base.Adapter = (IListAdapter)hadapter;
                }
                else
                {
                    base.Adapter = value;
                }
            }
        }
        private class FullWidthFixedViewLayout : FrameLayout
        {
            private HeaderGridView outerInstance;

            public FullWidthFixedViewLayout(HeaderGridView outerInstance, Context context)
                : base(context)
            {
                this.outerInstance = outerInstance;
            }
            protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
            {
                int targetWidth = outerInstance.MeasuredWidth - outerInstance.PaddingLeft - outerInstance.PaddingRight;
                widthMeasureSpec = MeasureSpec.MakeMeasureSpec(targetWidth, MeasureSpec.GetMode(widthMeasureSpec));
                base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            }
        }

        private class HeaderViewGridAdapter : Java.Lang.Object, IWrapperListAdapter, IFilterable
        {
            private DataSetObservable mDataSetObservable = new DataSetObservable();
            private IListAdapter mAdapter;
            private int mNumColumns = 1;
            private List<FixedViewInfo> mHeaderViewInfos;
            private bool mAreAllFixedViewsSelectable;
            private bool mIsFilterable;
            public HeaderViewGridAdapter(List<FixedViewInfo> headerViewInfos, IListAdapter adapter)
            {
                mAdapter = adapter;
                mIsFilterable = adapter is IFilterable;
                if (headerViewInfos == null)
                {
                    throw new System.ArgumentException("headerViewInfos cannot be null");
                }
                mHeaderViewInfos = headerViewInfos;
                mAreAllFixedViewsSelectable = AreAllListInfosSelectable(mHeaderViewInfos);
            }

            public void RegisterDataSetObserver(DataSetObserver observer)
            {
                mDataSetObservable.RegisterObserver(observer);
                if (mAdapter != null)
                {
                    mAdapter.RegisterDataSetObserver(observer);
                }
            }

            public void UnregisterDataSetObserver(DataSetObserver observer)
            {
                mDataSetObservable.UnregisterObserver(observer);
                if (mAdapter != null)
                {
                    mAdapter.UnregisterDataSetObserver(observer);
                }
            }

            public int HeadersCount
            {
                get
                {
                    return mHeaderViewInfos.Count;
                }
            }

            public bool IsEmpty
            {
                get
                {
                    return (mAdapter == null || mAdapter.IsEmpty) && HeadersCount == 0;
                }
            }
            public int NumColumns
            {
                set
                {
                    if (value < 1)
                    {
                        throw new System.ArgumentException("Number of columns must be 1 or more");
                    }
                    if (mNumColumns != value)
                    {
                        mNumColumns = value;
                        NotifyDataSetChanged();
                    }
                }
            }
            private bool AreAllListInfosSelectable(List<FixedViewInfo> infos)
            {
                if (infos != null)
                {
                    foreach (FixedViewInfo info in infos)
                    {
                        if (!info.isSelectable)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            public bool RemoveHeader(View v)
            {
                for (int i = 0; i < mHeaderViewInfos.Count; i++)
                {
                    FixedViewInfo info = mHeaderViewInfos[i];
                    if (info.view == v)
                    {
                        mHeaderViewInfos.RemoveAt(i);
                        mAreAllFixedViewsSelectable = AreAllListInfosSelectable(mHeaderViewInfos);
                        mDataSetObservable.NotifyChanged();
                        return true;
                    }
                }
                return false;
            }
            public int Count
            {
                get
                {
                    if (mAdapter != null)
                    {
                        return HeadersCount * mNumColumns + mAdapter.Count;
                    }
                    else
                    {
                        return HeadersCount * mNumColumns;
                    }
                }
            }
            public bool AreAllItemsEnabled()
            {
                if (mAdapter != null)
                {
                    return mAreAllFixedViewsSelectable && mAdapter.AreAllItemsEnabled();
                }
                else
                {
                    return true;
                }
            }
            public bool IsEnabled(int position)
            {
                int numHeadersAndPlaceholders = HeadersCount * mNumColumns;
                if (position < numHeadersAndPlaceholders)
                {
                    return (position % mNumColumns == 0) && mHeaderViewInfos[position / mNumColumns].isSelectable;
                }
                int adjPosition = position - numHeadersAndPlaceholders;
                int adapterCount = 0;
                if (mAdapter != null)
                {
                    adapterCount = mAdapter.Count;
                    if (adjPosition < adapterCount)
                    {
                        return mAdapter.IsEnabled(adjPosition);
                    }
                }
                throw new System.IndexOutOfRangeException(position.ToString());
            }
            public Java.Lang.Object GetItem(int position)
            {
                int numHeadersAndPlaceholders = HeadersCount * mNumColumns;
                if (position < numHeadersAndPlaceholders)
                {
                    if (position % mNumColumns == 0)
                    {
                        return mHeaderViewInfos[position / mNumColumns].data;
                    }
                    return null;
                }
                int adjPosition = position - numHeadersAndPlaceholders;
                int adapterCount = 0;
                if (mAdapter != null)
                {
                    adapterCount = mAdapter.Count;
                    if (adjPosition < adapterCount)
                    {
                        return mAdapter.GetItem(adjPosition);
                    }
                }
                throw new System.IndexOutOfRangeException(position.ToString());
            }
            public long GetItemId(int position)
            {
                int numHeadersAndPlaceholders = HeadersCount * mNumColumns;
                if (mAdapter != null && position >= numHeadersAndPlaceholders)
                {
                    int adjPosition = position - numHeadersAndPlaceholders;
                    int adapterCount = mAdapter.Count;
                    if (adjPosition < adapterCount)
                    {
                        return mAdapter.GetItemId(adjPosition);
                    }
                }
                return -1;
            }
            public bool HasStableIds
            {
                get
                {
                    if (mAdapter != null)
                    {
                        return mAdapter.HasStableIds;
                    }
                    return false;
                }
            }

            public View GetView(int position, View convertView, ViewGroup parent)
            {
                int numHeadersAndPlaceholders = HeadersCount * mNumColumns;
                if (position < numHeadersAndPlaceholders)
                {
                    View headerViewContainer = mHeaderViewInfos[position / mNumColumns].viewContainer;
                    if (position % mNumColumns == 0)
                    {
                        return headerViewContainer;
                    }
                    else
                    {
                        if (convertView == null)
                        {
                            convertView = new View(parent.Context);
                        }
                        convertView.Visibility = ViewStates.Invisible;
                        convertView.SetMinimumHeight(headerViewContainer.Height);
                        return convertView;
                    }
                }
                int adjPosition = position - numHeadersAndPlaceholders;
                int adapterCount = 0;
                if (mAdapter != null)
                {
                    adapterCount = mAdapter.Count;
                    if (adjPosition < adapterCount)
                    {
                        return mAdapter.GetView(adjPosition, convertView, parent);
                    }
                }
                throw new System.IndexOutOfRangeException(position.ToString());
            }
            public int GetItemViewType(int position)
            {
                int numHeadersAndPlaceholders = HeadersCount * mNumColumns;
                if (position < numHeadersAndPlaceholders && (position % mNumColumns != 0))
                {
                    return mAdapter != null ? mAdapter.ViewTypeCount : 1;
                }
                if (mAdapter != null && position >= numHeadersAndPlaceholders)
                {
                    int adjPosition = position - numHeadersAndPlaceholders;
                    int adapterCount = mAdapter.Count;
                    if (adjPosition < adapterCount)
                    {
                        return mAdapter.GetItemViewType(adjPosition);
                    }
                }
                return AdapterView.ItemViewTypeHeaderOrFooter;
            }
            public int ViewTypeCount
            {
                get
                {
                    if (mAdapter != null)
                    {
                        return mAdapter.ViewTypeCount + 1;
                    }
                    return 2;
                }
            }
            public void registerDataSetObserver(DataSetObserver observer)
            {
                mDataSetObservable.RegisterObserver(observer);
                if (mAdapter != null)
                {
                    mAdapter.RegisterDataSetObserver(observer);
                }
            }
            public void unregisterDataSetObserver(DataSetObserver observer)
            {
                mDataSetObservable.UnregisterObserver(observer);
                if (mAdapter != null)
                {
                    mAdapter.UnregisterDataSetObserver(observer);
                }
            }
            public Filter Filter
            {
                get
                {
                    if (mIsFilterable)
                    {
                        return ((IFilterable)mAdapter).Filter;
                    }
                    return null;
                }
            }
            public IListAdapter WrappedAdapter
            {
                get
                {
                    return mAdapter;
                }
            }
            public void NotifyDataSetChanged()
            {
                mDataSetObservable.NotifyChanged();
            }
        }
    }
}