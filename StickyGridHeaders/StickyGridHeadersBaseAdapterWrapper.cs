using Android.Content;
using Android.Database;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace StickyGridHeaders
{
	/// <summary>
	/// Adapter wrapper to insert extra views and otherwise hack around GridView to
	/// add sections and headers.
	/// 
	/// @author Tonic Artos
	/// </summary>
	public class StickyGridHeadersBaseAdapterWrapper : BaseAdapter
	{
		private const int sNumViewTypes = 3;

		public static int ID_FILLER = -0x02;
        public static int ID_HEADER = -0x01;
        public static int ID_HEADER_FILLER = -0x03;
        public static int POSITION_FILLER = -0x01;
        public static int POSITION_HEADER = -0x02;
        public static int POSITION_HEADER_FILLER = -0x03;
        public const int VIEW_TYPE_FILLER = 0x00;
        public const int VIEW_TYPE_HEADER = 0x01;
        public const int VIEW_TYPE_HEADER_FILLER = 0x02;

		private Context mContext;
		private int mCount;
		private bool mCounted = false;

        private DataSetObserver mDataSetObserver;		
		private StickyGridHeadersGridView mGridView;
		private View mLastHeaderViewSeen;
		private View mLastViewSeen;
		private int mNumColumns = 1;

		private IStickyGridHeadersBaseAdapter mDelegate { get; set; }

        public StickyGridHeadersBaseAdapterWrapper(Context context, StickyGridHeadersGridView gridView, object adapter)
		{
            mDataSetObserver = new StickyGridHeadersBaseAdapterWrapperObserver(this);

			mContext = context;
            mDelegate = adapter as IStickyGridHeadersBaseAdapter;
			mGridView = gridView;
            ((IStickyGridHeadersBaseAdapter)adapter).RegisterDataSetObserver(mDataSetObserver);
		}

        private class StickyGridHeadersBaseAdapterWrapperObserver : DataSetObserver
        {
            private StickyGridHeadersBaseAdapterWrapper adapter;

            public StickyGridHeadersBaseAdapterWrapperObserver(StickyGridHeadersBaseAdapterWrapper adapter)
            {
                this.adapter = adapter;
            }

            public override void OnChanged()
            {
                adapter.UpdateCount();
            }

            public override void OnInvalidated()
            {
                adapter.mCounted = false;
            }
        }

		public override bool AreAllItemsEnabled()
		{
			return false;
		}

		public override int Count
		{
			get
			{
				if (mCounted)
				{
					return mCount;
				}
				mCount = 0;
				int numHeaders = mDelegate.NumHeaders;
				if (numHeaders == 0)
				{
					mCount = mDelegate.Count;
					mCounted = true;
					return mCount;
				}
    
				for (int i = 0; i < numHeaders; i++)
				{
					// Pad count with space for header and trailing filler in header
					// group.
					mCount += mDelegate.GetCountForHeader(i) + UnFilledSpacesInHeaderGroup(i) + mNumColumns;
				}
				mCounted = true;
				return mCount;
			}
		}

		/// <summary>
		/// Get the data item associated with the specified position in the data set.
		/// <p>
		/// Since this wrapper inserts fake entries to fill out items grouped by
		/// header and also spaces to insert headers into some positions will return
		/// null.
		/// </p>
		/// </summary>
		/// <param name="position"> Position of the item whose data we want within the
		///            adapter's data set. </param>
		/// <returns> The data at the specified position. </returns>
		public override Java.Lang.Object GetItem(int position)
		{
			Position adapterPosition = TranslatePosition(position);
			if (adapterPosition.mPosition == POSITION_FILLER || adapterPosition.mPosition == POSITION_HEADER)
			{
				// Fake entry in view.
				return null;
			}

			return mDelegate.GetItem(adapterPosition.mPosition);
		}

		public override long GetItemId(int position)
		{
			Position adapterPosition = TranslatePosition(position);
			if (adapterPosition.mPosition == POSITION_HEADER)
			{
				return ID_HEADER;
			}
			if (adapterPosition.mPosition == POSITION_FILLER)
			{
				return ID_FILLER;
			}
			if (adapterPosition.mPosition == POSITION_HEADER_FILLER)
			{
				return ID_HEADER_FILLER;
			}
			return mDelegate.GetItemId(adapterPosition.mPosition);
		}

		public override int GetItemViewType(int position)
		{
			Position adapterPosition = TranslatePosition(position);
			if (adapterPosition.mPosition == POSITION_HEADER)
			{
				return VIEW_TYPE_HEADER;
			}
			if (adapterPosition.mPosition == POSITION_FILLER)
			{
				return VIEW_TYPE_FILLER;
			}
			if (adapterPosition.mPosition == POSITION_HEADER_FILLER)
			{
				return VIEW_TYPE_HEADER_FILLER;
			}
			int itemViewType = mDelegate.GetItemViewType(adapterPosition.mPosition);
			if (itemViewType == (int)ItemViewType.Ignore)
			{
				return itemViewType;
			}
			return itemViewType + sNumViewTypes;
		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			Position adapterPosition = TranslatePosition(position);
			if (adapterPosition.mPosition == POSITION_HEADER)
			{
				HeaderFillerView v = GetHeaderFillerView(adapterPosition.mHeader, convertView, parent);
				View view = mDelegate.GetHeaderView(adapterPosition.mHeader, (View)v.Tag, parent);
				mGridView.DetachHeader((View) v.Tag);
				v.Tag = view;
				mGridView.AttachHeader(view);
				convertView = v;
				mLastHeaderViewSeen = v;
				v.ForceLayout();
			}
			else if (adapterPosition.mPosition == POSITION_HEADER_FILLER)
			{
				convertView = GetFillerView(convertView, parent, mLastHeaderViewSeen);
				convertView.ForceLayout();
			}
			else if (adapterPosition.mPosition == POSITION_FILLER)
			{
				convertView = GetFillerView(convertView, parent, mLastViewSeen);
			}
			else
			{
				convertView = mDelegate.GetView(adapterPosition.mPosition, convertView, parent);
				mLastViewSeen = convertView;
			}

			return convertView;
		}

		public override int ViewTypeCount
		{
			get
			{
				return mDelegate.ViewTypeCount + sNumViewTypes;
			}
		}

		/// <returns> the adapter wrapped by this adapter. </returns>
		public IStickyGridHeadersBaseAdapter WrappedAdapter
		{
			get
			{
				return mDelegate;
			}
		}

        public override bool HasStableIds
        {
            get
            {
                return mDelegate.HasStableIds;
            }
        }

        public override bool IsEmpty
        {
            get
            {
                return mDelegate.IsEmpty;
            }
        }

		public override bool IsEnabled(int position)
		{
			Position adapterPosition = TranslatePosition(position);
			if (adapterPosition.mPosition == POSITION_FILLER || adapterPosition.mPosition == POSITION_HEADER)
			{
				return false;
			}

			return mDelegate.IsEnabled(adapterPosition.mPosition);
		}

		public override void RegisterDataSetObserver(DataSetObserver observer)
		{
			base.RegisterDataSetObserver(observer);

			mDelegate.RegisterDataSetObserver(observer);
		}

		public int NumColumns
		{
			set
			{
				mNumColumns = value;
				mCounted = false;
				// notifyDataSetChanged();
			}
		}

		public override void UnregisterDataSetObserver(DataSetObserver observer)
		{
			base.UnregisterDataSetObserver(observer);
			mDelegate.UnregisterDataSetObserver(observer);
		}

		private FillerView GetFillerView(View convertView, ViewGroup parent, View lastViewSeen)
		{
			FillerView fillerView = (FillerView)convertView;
			if (fillerView == null)
			{
				fillerView = new FillerView(this, mContext);
			}

			fillerView.MeasureTarget = lastViewSeen;

			return fillerView;
		}

		private HeaderFillerView GetHeaderFillerView(int headerPosition, View convertView, ViewGroup parent)
		{
			HeaderFillerView headerFillerView = (HeaderFillerView)convertView;
			if (headerFillerView == null)
			{
				headerFillerView = new HeaderFillerView(this, mContext);
			}

			return headerFillerView;
		}

		/// <summary>
		/// Counts the number of items that would be need to fill out the last row in
		/// the group of items with the given header.
		/// </summary>
		/// <param name="header"> Header set of items are grouped by. </param>
		/// <returns> The count of unfilled spaces in the last row. </returns>
		private int UnFilledSpacesInHeaderGroup(int header)
		{
			//If mNumColumns is equal to zero we will have a divide by 0 exception
			if (mNumColumns == 0)
			{
				return 0;
			}

			int remainder = mDelegate.GetCountForHeader(header) % mNumColumns;
			return remainder == 0 ? 0 : mNumColumns - remainder;
		}

        public long GetHeaderId(int position)
		{
			return TranslatePosition(position).mHeader;
		}

        public View GetHeaderView(int position, View convertView, ViewGroup parent)
		{
			if (mDelegate.NumHeaders == 0)
			{
				return null;
			}

			return mDelegate.GetHeaderView(TranslatePosition(position).mHeader, convertView, parent);
		}

		public Position TranslatePosition(int position)
		{
			int numHeaders = mDelegate.NumHeaders;
			if (numHeaders == 0)
			{
				if (position >= mDelegate.Count)
				{
					return new Position(POSITION_FILLER, 0);
				}
				return new Position(position, 0);
			}

			// Translate GridView position to Adapter position.
			int adapterPosition = position;
			int place = position;
			int i;

			for (i = 0; i < numHeaders; i++)
			{
				int sectionCount = mDelegate.GetCountForHeader(i);

				// Skip past fake items making space for header in front of
				// sections.
				if (place == 0)
				{
					// Position is first column where header will be.
					return new Position(POSITION_HEADER, i);
				}
				place -= mNumColumns;
				if (place < 0)
				{
					// Position is a fake so return null.
					return new Position(POSITION_HEADER_FILLER, i);
				}
				adapterPosition -= mNumColumns;

				if (place < sectionCount)
				{
					return new Position(adapterPosition, i);
				}

				// Skip past section end of section row filler;
				int filler = UnFilledSpacesInHeaderGroup(i);
				adapterPosition -= filler;
				place -= sectionCount + filler;

				if (place < 0)
				{
					// Position is a fake so return null.
					return new Position(POSITION_FILLER, i);
				}
			}

			// Position is a fake.
			return new Position(POSITION_FILLER, i);
		}

        public void UpdateCount()
		{
			mCount = 0;
			int numHeaders = mDelegate.NumHeaders;
			if (numHeaders == 0)
			{
				mCount = mDelegate.Count;
				mCounted = true;
				return;
			}

			for (int i = 0; i < numHeaders; i++)
			{
				mCount += mDelegate.GetCountForHeader(i) + mNumColumns;
			}
			mCounted = true;
		}

		/// <summary>
		/// Simple view to fill space in grid view.
		/// 
		/// @author Tonic Artos
		/// </summary>
        public class FillerView : View
		{
			private StickyGridHeadersBaseAdapterWrapper adapter;

            public View mMeasureTarget;

            public FillerView(StickyGridHeadersBaseAdapterWrapper adapter, Context context)
                : base(context)
			{
                this.adapter = adapter;
			}

            public FillerView(StickyGridHeadersBaseAdapterWrapper adapter, Context context, IAttributeSet attrs)
                : base(context, attrs)
			{
                this.adapter = adapter;
			}

            public FillerView(StickyGridHeadersBaseAdapterWrapper adapter, Context context, IAttributeSet attrs, int defStyle) 
                : base(context, attrs, defStyle)
			{
                this.adapter = adapter;
			}

			public View MeasureTarget
			{
				set
				{
					mMeasureTarget = value;
				}
			}

			protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
			{
				heightMeasureSpec = MeasureSpec.MakeMeasureSpec(mMeasureTarget.MeasuredHeight, MeasureSpecMode.Exactly);
				base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
			}
		}

		/// <summary>
		/// A view to hold the section header and measure the header row height
		/// correctly.
		/// 
		/// @author Tonic Artos
		/// </summary>
        public class HeaderFillerView : FrameLayout
		{
			private StickyGridHeadersBaseAdapterWrapper adapter;

			private int mHeaderId;

			public HeaderFillerView(StickyGridHeadersBaseAdapterWrapper adapter, Context context) : base(context)
			{
				this.adapter = adapter;
			}

            public HeaderFillerView(StickyGridHeadersBaseAdapterWrapper adapter, Context context, IAttributeSet attrs)
                : base(context, attrs)
			{
                this.adapter = adapter;
			}

            public HeaderFillerView(StickyGridHeadersBaseAdapterWrapper adapter, Context context, IAttributeSet attrs, int defStyle) 
                : base(context, attrs, defStyle)
			{
                this.adapter = adapter;
			}

			public int HeaderId
			{
				get
				{
					return mHeaderId;
				}
				set
				{
					mHeaderId = value;
				}
			}

			protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
			{
				View v = (View)Tag;
				ViewGroup.LayoutParams lp = v.LayoutParameters;
				if (lp == null)
				{
                    lp = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);
                    v.LayoutParameters = lp;
				}
				if (v.Visibility != ViewStates.Gone)
				{
					int heightSpec = GetChildMeasureSpec(MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified), 0, lp.Height);
					int widthSpec = GetChildMeasureSpec(MeasureSpec.MakeMeasureSpec(adapter.mGridView.Width, MeasureSpecMode.Exactly), 0, lp.Width);
					v.Measure(widthSpec, heightSpec);
				}
				SetMeasuredDimension(MeasureSpec.GetSize(widthMeasureSpec), v.MeasuredHeight);
			}
		}

        public class HeaderHolder
		{
            public View mHeaderView;
		}

        public class Position
		{
			public int mHeader;
			public int mPosition;

            public Position(int position, int header)
			{
				mPosition = position;
				mHeader = header;
			}
		}
	}
}