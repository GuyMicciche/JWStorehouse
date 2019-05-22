using Android.Content;
using Android.Content.Res;
using Android.Database;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using System;
using System.Collections.Generic;

namespace JWStorehouse
{
    public class HeaderFooterGridView : GridView
    {
        private const string TAG = "HeaderFooterGridView";

        /// <summary>
        /// A class that represents a fixed view in a list, for example a header at the top
        /// or a footer at the bottom.
        /// </summary>
        private class FixedViewInfo
        {
            /// <summary>
            /// The view to add to the grid
            /// </summary>
            public View view;
            public ViewGroup viewContainer;
            /// <summary>
            /// The data backing the view. This is returned from <seealso cref="ListAdapter#getItem(int)"/>.
            /// </summary>
            public Java.Lang.Object data;
            /// <summary>
            /// <code>true</code> if the fixed view should be selectable in the grid
            /// </summary>
            public bool isSelectable;
        }

        private List<FixedViewInfo> mHeaderViewInfos = new List<FixedViewInfo>();

        private List<FixedViewInfo> mFooterViewInfos = new List<FixedViewInfo>();

        private int mRequestedNumColumns;

        private int mPreviousFirstVisible;

        private int mNumColumnsID;
        private int mNumColmuns = 1;

        private bool doSetHeight = true;

        private void initHeaderGridView()
        {
            base.SetClipChildren(false);
        }

        public HeaderFooterGridView(Context context)
            : base(context)
        {
            initHeaderGridView();
        }

        public HeaderFooterGridView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Init(attrs);
            initHeaderGridView();
        }

        public HeaderFooterGridView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Init(attrs);
            initHeaderGridView();
        }

        private void Init(IAttributeSet attrs)
        {
            int count = attrs.AttributeCount;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    string name = attrs.GetAttributeName(i);

                    if (name != null && name.Equals("numColumns"))
                    {
                        this.mNumColumnsID = attrs.GetAttributeResourceValue(i, 1);
                        UpdateColumns();
                        break;
                    }
                }
            }
            Console.WriteLine("numColumns set to: " + NumColumns);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            if (mRequestedNumColumns != -1)
            {
                mNumColmuns = mRequestedNumColumns;
            }
            if (mNumColmuns <= 0)
            {
                mNumColmuns = 1;
            }

            IListAdapter adapter = (IListAdapter)Adapter;
            if (adapter != null && adapter is HeaderFooterViewGridAdapter)
            {
                ((HeaderFooterViewGridAdapter)adapter).NumColumns = NumColumns;
            }
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);

            SetHeights(doSetHeight);
        }

        private void UpdateColumns()
        {
            //this.mNumColmuns = Context.Resources.GetInteger(mNumColumnsID);
        }

        protected override void OnConfigurationChanged(Configuration newConfig)
        {
            UpdateColumns();
            NumColumns = this.mNumColmuns;
        }

        protected override void OnScrollChanged(int l, int t, int oldl, int oldt)
        {
            int firstVisible = FirstVisiblePosition;
            if (mPreviousFirstVisible != firstVisible)
            {
                mPreviousFirstVisible = firstVisible;
                SetHeights(doSetHeight);
            }

            base.OnScrollChanged(l, t, oldl, oldt);
        }

        public bool DoSetHeight
        {
            get
            {
                return doSetHeight;
            }
            set
            {
                doSetHeight = value;
            }
        }

        private void SetHeights(bool doSetHeight)
        {
            if(doSetHeight)
            {
                SetHeights();
            }
        }

        public void SetHeights()
        {
            try
            {
                IListAdapter adapter = Adapter;

                if ((adapter != null) && (mNumColmuns > 0))
                {
                    Console.WriteLine("ChildCount -> " + ChildCount + " NumColumns -> " + mNumColmuns);

                    for (int i = 0; i < ChildCount; i += mNumColmuns)
                    {
                        // Determine the maximum height for this row
                        int maxHeight = 0;
                        for (int j = i; j < (i + mNumColmuns); j++)
                        {
                            TextView view = (TextView)GetChildAt(j);
                            if (view != null && view.Height > maxHeight)
                            {
                                maxHeight = view.Height;
                            }
                        }
                        // Set max height for each element in this row
                        if (maxHeight > 0)
                        {
                            for (int j = i; j < (i + mNumColmuns); j++)
                            {
                                TextView view = (TextView)GetChildAt(j);
                                if (view != null && view.Height != maxHeight)
                                {
                                    view.SetHeight(maxHeight);
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public override bool ClipChildren
        {
            get
            {
                return false;
            }
        }

        public void AddHeaderView(View v, Java.Lang.Object data, bool isSelectable)
        {
            IListAdapter adapter = (IListAdapter)Adapter;

            if (adapter != null && !(adapter is HeaderFooterViewGridAdapter))
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
                ((HeaderFooterViewGridAdapter)adapter).notifyDataSetChanged();
            }
        }

        public void AddHeaderView(View v)
        {
            AddHeaderView(v, null, true);
        }

        public void AddFooterView(View v, Java.Lang.Object data, bool isSelectable)
        {
            IListAdapter adapter = (IListAdapter)Adapter;

            if (adapter != null && !(adapter is HeaderFooterViewGridAdapter))
            {
                throw new IllegalStateException("Cannot add footer view to grid -- setAdapter has already been called.");
            }

            FixedViewInfo info = new FixedViewInfo();
            FrameLayout fl = new FullWidthFixedViewLayout(this, Context);
            fl.AddView(v);
            info.view = v;
            info.viewContainer = fl;
            info.data = data;
            info.isSelectable = isSelectable;
            mFooterViewInfos.Add(info);

            // in the case of re-adding a header view, or adding one later on,
            // we need to notify the observer
            if (adapter != null)
            {
                ((HeaderFooterViewGridAdapter)adapter).notifyDataSetChanged();
            }
        }

        public void AddFooterView(View v)
        {
            AddFooterView(v, null, true);
        }

        public int HeaderViewCount
        {
            get
            {
                return mHeaderViewInfos.Count;
            }
        }

        public int FooterViewCount
        {
            get
            {
                return mFooterViewInfos.Count;
            }
        }

        public bool RemoveHeaderView(View v)
        {
            if (mHeaderViewInfos.Count > 0)
            {
                bool result = false;
                IListAdapter adapter = (IListAdapter)Adapter;
                if (adapter != null && ((HeaderFooterViewGridAdapter)adapter).RemoveHeader(v))
                {
                    result = true;
                }
                RemoveFixedViewInfo(v, mHeaderViewInfos);
                return result;
            }
            return false;
        }

        public bool RemoveFooterView(View v)
        {
            if (mFooterViewInfos.Count > 0)
            {
                bool result = false;
                IListAdapter adapter = (IListAdapter)Adapter;
                if (adapter != null && ((HeaderFooterViewGridAdapter)adapter).RemoveFooter(v))
                {
                    result = true;
                }
                RemoveFixedViewInfo(v, mFooterViewInfos);
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
                if (mHeaderViewInfos.Count > 0 || mFooterViewInfos.Count > 0)
                {
                    HeaderFooterViewGridAdapter hadapter = new HeaderFooterViewGridAdapter(mHeaderViewInfos, mFooterViewInfos, (IListAdapter)value);
                    int numColumns = NumColumns;
                    if (numColumns > 1)
                    {
                        hadapter.NumColumns = numColumns;
                    }
                    base.Adapter = hadapter;
                }
                else
                {
                    base.Adapter = value;
                }
            }
        }

        private class FullWidthFixedViewLayout : FrameLayout
        {
            private HeaderFooterGridView outerInstance;

            public FullWidthFixedViewLayout(HeaderFooterGridView outerInstance, Context context)
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

        public override int NumColumns
        {
            set
            {
                base.NumColumns = value;
                this.mNumColmuns = value;

                // Store specified value for less than Honeycomb.
                this.mRequestedNumColumns = value;

                SetSelection(mPreviousFirstVisible);
            }
            get
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
                {
                    return base.NumColumns;
                }

                // Return value for less than Honeycomb.
                return this.mNumColmuns;
            }
        }

        /// <summary>
        /// ListAdapter used when a HeaderFooterGridView has header views. This ListAdapter
        /// wraps another one and also keeps track of the header views and their
        /// associated data objects.
        /// <p>This is intended as a base class; you will probably not need to
        /// use this class directly in your own code.
        /// </summary>
        private class HeaderFooterViewGridAdapter : Java.Lang.Object, IWrapperListAdapter, IFilterable
        {

            // This is used to notify the container of updates relating to number of columns
            // or headers changing, which changes the number of placeholders needed
            private DataSetObservable mDataSetObservable = new DataSetObservable();

            private IListAdapter mAdapter;
            private int mNumColumns = 1;

            // This ArrayList is assumed to NOT be null.
            private List<FixedViewInfo> mHeaderViewInfos;

            private List<FixedViewInfo> mFooterViewInfos;

            private bool mAreAllFixedViewsSelectable;

            private bool mIsFilterable;

            public HeaderFooterViewGridAdapter(List<FixedViewInfo> headerViewInfos, List<FixedViewInfo> footerViewInfos, IListAdapter adapter)
            {
                mAdapter = adapter;
                mIsFilterable = adapter is IFilterable;

                if (headerViewInfos == null)
                {
                    throw new System.ArgumentException("headerViewInfos cannot be null");
                }
                if (footerViewInfos == null)
                {
                    throw new System.ArgumentException("footerViewInfos cannot be null");
                }
                mHeaderViewInfos = headerViewInfos;
                mFooterViewInfos = footerViewInfos;

                mAreAllFixedViewsSelectable = (AreAllListInfosSelectable(mHeaderViewInfos) && AreAllListInfosSelectable(mFooterViewInfos));
            }

            public int HeadersCount
            {
                get
                {
                    return mHeaderViewInfos.Count;
                }
            }

            public int FootersCount
            {
                get
                {
                    return mFooterViewInfos.Count;
                }
            }

            public bool IsEmpty
            {
                get
                {
                    return (mAdapter == null || mAdapter.IsEmpty) && HeadersCount == 0 && FootersCount == 0;
                }
            }

            public int NumColumns
            {
                set
                {
                    //if (value < 1)
                    //{
                    //    throw new System.ArgumentException("Number of columns must be 1 or more");
                    //}
                    if (mNumColumns != value)
                    {
                        mNumColumns = value;
                        notifyDataSetChanged();
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

                        mAreAllFixedViewsSelectable = (AreAllListInfosSelectable(mHeaderViewInfos) && AreAllListInfosSelectable(mFooterViewInfos));

                        mDataSetObservable.NotifyChanged();
                        return true;
                    }
                }

                return false;
            }

            public bool RemoveFooter(View v)
            {
                for (int i = 0; i < mFooterViewInfos.Count; i++)
                {
                    FixedViewInfo info = mFooterViewInfos[i];
                    if (info.view == v)
                    {
                        mFooterViewInfos.RemoveAt(i);

                        mAreAllFixedViewsSelectable = (AreAllListInfosSelectable(mHeaderViewInfos) && AreAllListInfosSelectable(mFooterViewInfos));

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
                        //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                        //ORIGINAL LINE: final int lastRowItemCount = (mAdapter.getCount() % mNumColumns);
                        int lastRowItemCount = (mAdapter.Count % mNumColumns);
                        //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                        //ORIGINAL LINE: final int emptyItemCount = ((lastRowItemCount == 0) ? 0 : mNumColumns - lastRowItemCount);
                        int emptyItemCount = ((lastRowItemCount == 0) ? 0 : mNumColumns - lastRowItemCount);
                        return (HeadersCount * mNumColumns) + mAdapter.Count + emptyItemCount + (FootersCount * mNumColumns);
                    }
                    else
                    {
                        return (HeadersCount * mNumColumns) + (FootersCount * mNumColumns);
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
                // Header (negative positions will throw an ArrayIndexOutOfBoundsException)
                int numHeadersAndPlaceholders = HeadersCount * mNumColumns;
                if (position < numHeadersAndPlaceholders)
                {
                    return (position % mNumColumns == 0) && mHeaderViewInfos[position / mNumColumns].isSelectable;
                }

                // Adapter
                if (position < numHeadersAndPlaceholders + mAdapter.Count)
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int adjPosition = position - numHeadersAndPlaceholders;
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
                }

                // Empty item
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int lastRowItemCount = (mAdapter.getCount() % mNumColumns);
                int lastRowItemCount = (mAdapter.Count % mNumColumns);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int emptyItemCount = ((lastRowItemCount == 0) ? 0 : mNumColumns - lastRowItemCount);
                int emptyItemCount = ((lastRowItemCount == 0) ? 0 : mNumColumns - lastRowItemCount);
                if (position < numHeadersAndPlaceholders + mAdapter.Count + emptyItemCount)
                {
                    return false;
                }

                // Footer
                int numFootersAndPlaceholders = FootersCount * mNumColumns;
                if (position < numHeadersAndPlaceholders + mAdapter.Count + emptyItemCount + numFootersAndPlaceholders)
                {
                    return (position % mNumColumns == 0) && mFooterViewInfos[(position - numHeadersAndPlaceholders - mAdapter.Count - emptyItemCount) / mNumColumns].isSelectable;
                }

                throw new System.IndexOutOfRangeException(position.ToString());
            }

            public Java.Lang.Object GetItem(int position)
            {
                // Header (negative positions will throw an ArrayIndexOutOfBoundsException)
                int numHeadersAndPlaceholders = HeadersCount * mNumColumns;
                if (position < numHeadersAndPlaceholders)
                {
                    if (position % mNumColumns == 0)
                    {
                        return mHeaderViewInfos[position / mNumColumns].data;
                    }
                    return null;
                }

                // Adapter
                if (position < numHeadersAndPlaceholders + mAdapter.Count)
                {
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
                }

                int lastRowItemCount = (mAdapter.Count % mNumColumns);
                int emptyItemCount = ((lastRowItemCount == 0) ? 0 : mNumColumns - lastRowItemCount);
                if (position < numHeadersAndPlaceholders + mAdapter.Count + emptyItemCount)
                {
                    return null;
                }

                // Footer
                int numFootersAndPlaceholders = FootersCount * mNumColumns;
                if (position < numHeadersAndPlaceholders + mAdapter.Count + emptyItemCount + numFootersAndPlaceholders)
                {
                    if (position % mNumColumns == 0)
                    {
                        return mFooterViewInfos[(position - numHeadersAndPlaceholders - mAdapter.Count - emptyItemCount) / mNumColumns].data;
                    }
                }

                throw new System.IndexOutOfRangeException(position.ToString());
            }

            public long GetItemId(int position)
            {
                int numHeadersAndPlaceholders = HeadersCount * mNumColumns;
                if (mAdapter != null)
                {
                    if (position >= numHeadersAndPlaceholders && position < numHeadersAndPlaceholders + mAdapter.Count)
                    {
                        int adjPosition = position - numHeadersAndPlaceholders;
                        int adapterCount = mAdapter.Count;
                        if (adjPosition < adapterCount)
                        {
                            return mAdapter.GetItemId(adjPosition);
                        }
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
                // Header (negative positions will throw an ArrayIndexOutOfBoundsException)
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
                        convertView = new View(parent.Context);
                        // We need to do this because GridView uses the height of the last item
                        // in a row to determine the height for the entire row.
                        convertView.Visibility = ViewStates.Invisible;
                        convertView.SetMinimumHeight(headerViewContainer.Height);
                        return convertView;
                    }
                }

                // Adapter
                if (position < numHeadersAndPlaceholders + mAdapter.Count)
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int adjPosition = position - numHeadersAndPlaceholders;
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
                }

                // Empty item
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int lastRowItemCount = (mAdapter.getCount() % mNumColumns);
                int lastRowItemCount = (mAdapter.Count % mNumColumns);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int emptyItemCount = ((lastRowItemCount == 0) ? 0 : mNumColumns - lastRowItemCount);
                int emptyItemCount = ((lastRowItemCount == 0) ? 0 : mNumColumns - lastRowItemCount);
                if (position < numHeadersAndPlaceholders + mAdapter.Count + emptyItemCount)
                {
                    // We need to do this because GridView uses the height of the last item
                    // in a row to determine the height for the entire row.
                    // TODO Current implementation may not be enough in the case of 3 or more column. May need to be careful on the INVISIBLE View height.
                    convertView = mAdapter.GetView(mAdapter.Count - 1, convertView, parent);
                    convertView.Visibility = ViewStates.Invisible;
                    return convertView;
                }

                // Footer
                int numFootersAndPlaceholders = FootersCount * mNumColumns;
                if (position < numHeadersAndPlaceholders + mAdapter.Count + emptyItemCount + numFootersAndPlaceholders)
                {
                    View footerViewContainer = mFooterViewInfos[(position - numHeadersAndPlaceholders - mAdapter.Count - emptyItemCount) / mNumColumns].viewContainer;
                    if (position % mNumColumns == 0)
                    {
                        return footerViewContainer;
                    }
                    else
                    {
                        convertView = new View(parent.Context);
                        // We need to do this because GridView uses the height of the last item
                        // in a row to determine the height for the entire row.
                        convertView.Visibility = ViewStates.Invisible;
                        convertView.SetMinimumHeight(footerViewContainer.Height);
                        return convertView;
                    }
                }

                throw new System.IndexOutOfRangeException(position.ToString());
            }

            public int GetItemViewType(int position)
            {
                int numHeadersAndPlaceholders = HeadersCount * mNumColumns;
                if (position < numHeadersAndPlaceholders && (position % mNumColumns != 0))
                {
                    // Placeholders get the last view type number
                    return mAdapter != null ? mAdapter.ViewTypeCount : 1;
                }
                if (mAdapter != null && position >= numHeadersAndPlaceholders && position < numHeadersAndPlaceholders + mAdapter.Count)
                {
                    int adjPosition = position - numHeadersAndPlaceholders;
                    int adapterCount = mAdapter.Count;
                    if (adjPosition < adapterCount)
                    {
                        return mAdapter.GetItemViewType(adjPosition);
                    }
                }
                int numFootersAndPlaceholders = FootersCount * mNumColumns;
                if (mAdapter != null && position < numHeadersAndPlaceholders + mAdapter.Count + numFootersAndPlaceholders)
                {
                    return mAdapter != null ? mAdapter.ViewTypeCount : 1;
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

            public void notifyDataSetChanged()
            {
                mDataSetObservable.NotifyChanged();
            }
        }
    }
}