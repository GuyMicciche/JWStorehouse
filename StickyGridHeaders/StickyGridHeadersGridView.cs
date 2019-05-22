using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.Accessibility;
using Android.Widget;
using Java.Interop;
using Java.Lang;
using Java.Lang.Reflect;
using System;
using System.Collections.Generic;

namespace StickyGridHeaders
{
	/// <summary>
	/// GridView that displays items in sections with headers that stick to the top
	/// of the view.
	/// 
	/// @author Tonic Artos, Emil Sjölander, caguilar187
	/// </summary>
	public class StickyGridHeadersGridView : GridView, AbsListView.IOnScrollListener, AdapterView.IOnItemClickListener, AdapterView.IOnItemSelectedListener, AdapterView.IOnItemLongClickListener
	{
		private static string ERROR_PLATFORM = "Error supporting platform " + Build.VERSION.SdkInt + ".";
		private const int MATCHED_STICKIED_HEADER = -2;
		private const int NO_MATCHED_HEADER = -1;
        public const int TOUCH_MODE_DONE_WAITING = 2;
        public const int TOUCH_MODE_DOWN = 0;
        public const int TOUCH_MODE_FINISHED_LONG_PRESS = -2;
        public const int TOUCH_MODE_REST = -1;
        public const int TOUCH_MODE_TAP = 1;
        public const int AUTO_FIT = -1;

        public static string TAG = Java.Lang.Class.FromType(typeof(StickyGridHeadersGridView)).SimpleName;

		private static MotionEvent.PointerCoords[] GetPointerCoords(MotionEvent e)
		{
			int n = e.PointerCount;
			MotionEvent.PointerCoords[] r = new MotionEvent.PointerCoords[n];
			for (int i = 0; i < n; i++)
			{
				r[i] = new MotionEvent.PointerCoords();
				e.GetPointerCoords(i, r[i]);
			}
			return r;
		}

		private static int[] GetPointerIds(MotionEvent e)
		{
			int n = e.PointerCount;
			int[] r = new int[n];
			for (int i = 0; i < n; i++)
			{
				r[i] = e.GetPointerId(i);
			}
			return r;
		}

		public CheckForHeaderLongPress mPendingCheckForLongPress;
		public CheckForHeaderTap mPendingCheckForTap;
		private bool mAreHeadersSticky = true;
		private Rect mClippingRect = new Rect();
		private bool mClippingToPadding;
		private bool mClipToPaddingHasBeenSet;
		private int mColumnWidth;
		private long mCurrentHeaderId = -1;
		private DataSetObserver mDataSetObserver;
		private int mHeaderBottomPosition;
		private bool mHeadersIgnorePadding;
		private int mHorizontalSpacing;
		private bool mMaskStickyHeaderRegion = true;
		private float mMotionY;

		/// <summary>
		/// Must be set from the wrapped GridView in the constructor.
		/// </summary>
		private int mNumColumns;
		private bool mNumColumnsSet;
		private int mNumMeasuredColumns = 1;
		private IOnHeaderClickListener mOnHeaderClickListener;
		private IOnHeaderLongClickListener mOnHeaderLongClickListener;
		private AdapterView.IOnItemClickListener mOnItemClickListener;
		private AdapterView.IOnItemLongClickListener mOnItemLongClickListener;
		private AdapterView.IOnItemSelectedListener mOnItemSelectedListener;
		private PerformHeaderClickRunnable mPerformHeaderClickRunnable;
		private AbsListView.IOnScrollListener mScrollListener;
        private int mScrollState = (int)ScrollState.Idle;
		private View mStickiedHeader;
		private IRunnable mTouchModeReset;
		private int mTouchSlop;
		private int mVerticalSpacing;
        public StickyGridHeadersBaseAdapterWrapper mAdapter;
        public bool mDataChanged;
        public int mMotionHeaderPosition;
        public int mTouchMode;
        public bool mHeaderChildBeingPressed = false;

        public StickyGridHeadersGridView(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {

        }

		public StickyGridHeadersGridView(Context context)
            : this(context, null)
		{
            Initialize(context);
		}

        public StickyGridHeadersGridView(Context context, IAttributeSet attrs)
            : this(context, attrs, Android.Resource.Attribute.GridViewStyle)
		{
            Initialize(context);
		}

        public StickyGridHeadersGridView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(context);
		}

        private void Initialize(Context context)
        {
            mDataSetObserver = new StickyGridHeadersGridViewObserver(this);

            base.SetOnScrollListener(this);
            VerticalFadingEdgeEnabled = false;

            if (!mNumColumnsSet)
            {
                mNumColumns = AUTO_FIT;
            }

            ViewConfiguration vc = ViewConfiguration.Get(context);
            mTouchSlop = vc.ScaledTouchSlop;
        }

        private class StickyGridHeadersGridViewObserver  : DataSetObserver
        {
            private StickyGridHeadersGridView gridView;

            public StickyGridHeadersGridViewObserver(StickyGridHeadersGridView gridView)
            {
                this.gridView = gridView;
            }

            public override void OnChanged()
            {
                gridView.Reset();
            }
            public override void OnInvalidated()
            {
                gridView.Reset();
            }
        }		

		/// <summary>
		/// Gets the header at an item position. However, the position must be that
		/// of a HeaderFiller.
		/// </summary>
		/// <param name="position"> Position of HeaderFiller. </param>
		/// <returns> Header View wrapped in HeaderFiller or null if no header was
		///         found. </returns>
		public View GetHeaderAt(int position)
		{
			if (position == MATCHED_STICKIED_HEADER)
			{
				return mStickiedHeader;
			}

			try
			{
				return (View)GetChildAt(position).Tag;
			}
			catch (System.Exception e)
			{
                Console.WriteLine(e.Message);
			}
			return null;
		}

		/// <summary>
		/// Get the currently stickied header.
		/// </summary>
		/// <returns> Current stickied header. </returns>
		public View StickiedHeader
		{
			get
			{
				return mStickiedHeader;
			}
		}

		public bool StickyHeaderIsTranscluent
		{
			get
			{
				return !mMaskStickyHeaderRegion;
			}
			set
			{
				mMaskStickyHeaderRegion = !value;
			}
		}

		public void OnItemClick(AdapterView parent, View view, int position, long id)
		{
			mOnItemClickListener.OnItemClick(parent, view, mAdapter.TranslatePosition(position).mPosition, id);
		}

		public bool OnItemLongClick(AdapterView parent, View view, int position, long id)
		{
			return mOnItemLongClickListener.OnItemLongClick(parent, view, mAdapter.TranslatePosition(position).mPosition, id);
		}

		public void OnItemSelected(AdapterView parent, View view, int position, long id)
		{
			mOnItemSelectedListener.OnItemSelected(parent, view, mAdapter.TranslatePosition(position).mPosition, id);
		}

        public void OnNothingSelected(AdapterView parent)
        {
            mOnItemSelectedListener.OnNothingSelected(parent);
        }

		public override void OnRestoreInstanceState(IParcelable state)
		{
			SavedState ss = (SavedState)state;

			base.OnRestoreInstanceState(ss.SuperState);
			mAreHeadersSticky = ss.AreHeadersSticky;

			RequestLayout();
		}

		public override IParcelable OnSaveInstanceState()
		{
			IParcelable superState = base.OnSaveInstanceState();

			SavedState ss = new SavedState((IParcelable)superState);
			ss.AreHeadersSticky = mAreHeadersSticky;

			return ss;
		}

		public void OnScroll(AbsListView view, int firstVisibleItem, int visibleItemCount, int totalItemCount)
		{
			if (mScrollListener != null)
			{
				mScrollListener.OnScroll(view, firstVisibleItem, visibleItemCount, totalItemCount);
			}

			if (Build.VERSION.SdkInt >= Build.VERSION_CODES.Froyo)
			{
				ScrollChanged(firstVisibleItem);
			}
		}

		public void OnScrollStateChanged(AbsListView view, ScrollState scrollState)
		{
			if (mScrollListener != null)
			{
				mScrollListener.OnScrollStateChanged(view, scrollState);
			}

			mScrollState = (int)scrollState;
		}

		public override bool OnTouchEvent(MotionEvent ev)
		{
            View header;

            MotionEventActions action = ev.Action;
			bool wasHeaderChildBeingPressed = mHeaderChildBeingPressed;
			if (mHeaderChildBeingPressed)
			{
				View tempHeader = GetHeaderAt(mMotionHeaderPosition);
				View headerHolder = mMotionHeaderPosition == MATCHED_STICKIED_HEADER ? tempHeader : GetChildAt(mMotionHeaderPosition);
                if (action == MotionEventActions.Up || action == MotionEventActions.Cancel)
				{
					mHeaderChildBeingPressed = false;
				}
				if (tempHeader != null)
				{
					tempHeader.DispatchTouchEvent(TransformEvent(ev, mMotionHeaderPosition));
					tempHeader.Invalidate();
					tempHeader.PostDelayed(() =>
                        {
                            Invalidate(0, headerHolder.Top, Width, headerHolder.Top + headerHolder.Height);                            
                        }, ViewConfiguration.PressedStateDuration);
					Invalidate(0, headerHolder.Top, Width, headerHolder.Top + headerHolder.Height);
				}
			}

            switch (action & MotionEventActions.Mask)
			{
                case MotionEventActions.Down:
					if (mPendingCheckForTap == null)
					{
						mPendingCheckForTap = new CheckForHeaderTap(this);
					}
					PostDelayed(mPendingCheckForTap, ViewConfiguration.TapTimeout);

					int y = (int)ev.GetY();
					mMotionY = y;
					mMotionHeaderPosition = FindMotionHeader(y);
					if (mMotionHeaderPosition == NO_MATCHED_HEADER || mScrollState == (int)ScrollState.Fling)
					{
						// Don't consume the event and pass it to super because we
						// can't handle it yet.
						break;
					}
					else
					{
						View tempHeader = GetHeaderAt(mMotionHeaderPosition);
						if (tempHeader != null)
						{
							if (tempHeader.DispatchTouchEvent(TransformEvent(ev, mMotionHeaderPosition)))
							{
								mHeaderChildBeingPressed = true;
								tempHeader.Pressed = true;
							}
							tempHeader.Invalidate();
							if (mMotionHeaderPosition != MATCHED_STICKIED_HEADER)
							{
								tempHeader = GetChildAt(mMotionHeaderPosition);
							}
							Invalidate(0, tempHeader.Top, Width, tempHeader.Top + tempHeader.Height);
						}
					}
					mTouchMode = TOUCH_MODE_DOWN;
					return true;
				case MotionEventActions.Move:
					if (mMotionHeaderPosition != NO_MATCHED_HEADER && System.Math.Abs(ev.GetY() - mMotionY) > mTouchSlop)
					{
						// Detected scroll initiation so cancel touch completion on
						// header.
						mTouchMode = TOUCH_MODE_REST;
						// if (!mHeaderChildBeingPressed) {
						header = GetHeaderAt(mMotionHeaderPosition);
						if (header != null)
						{
							header.Pressed = false;
							header.Invalidate();
						}
						Handler handler = Handler;
						if (handler != null)
						{
							handler.RemoveCallbacks(mPendingCheckForLongPress);
						}
						mMotionHeaderPosition = NO_MATCHED_HEADER;
						// }
					}
					break;
				case MotionEventActions.Up:
					if (mTouchMode == TOUCH_MODE_FINISHED_LONG_PRESS)
					{
						mTouchMode = TOUCH_MODE_REST;
						return true;
					}
					if (mTouchMode == TOUCH_MODE_REST || mMotionHeaderPosition == NO_MATCHED_HEADER)
					{
						break;
					}

					header = GetHeaderAt(mMotionHeaderPosition);
					if (!wasHeaderChildBeingPressed)
					{
						if (header != null)
						{
							if (mTouchMode != TOUCH_MODE_DOWN)
							{
								header.Pressed = false;
							}

							if (mPerformHeaderClickRunnable == null)
							{
								mPerformHeaderClickRunnable = new PerformHeaderClickRunnable(this);
							}

							PerformHeaderClickRunnable performHeaderClick = mPerformHeaderClickRunnable;
							performHeaderClick.mClickMotionPosition = mMotionHeaderPosition;
							performHeaderClick.RememberWindowAttachCount();

							if (mTouchMode == TOUCH_MODE_DOWN || mTouchMode == TOUCH_MODE_TAP)
							{
								Handler handler = Handler;
								if (handler != null)
								{
                                    if(mTouchMode == TOUCH_MODE_DOWN)
                                    {
                                        handler.RemoveCallbacks(mPendingCheckForTap);
                                    }
                                    else
                                    {
                                        handler.RemoveCallbacks(mPendingCheckForLongPress);
                                    }
								}

								if (!mDataChanged)
								{
									/*
									 * Got here so must be a tap. The long press
									 * would have triggered on the callback handler.
									 */
									mTouchMode = TOUCH_MODE_TAP;
									header.Pressed = true;
									Pressed = true;
									if (mTouchModeReset != null)
									{
										RemoveCallbacks(mTouchModeReset);
									}
									mTouchModeReset = new TouchModeResetAction(this, header, performHeaderClick);
									PostDelayed(mTouchModeReset, ViewConfiguration.PressedStateDuration);
								}
								else
								{
									mTouchMode = TOUCH_MODE_REST;
								}
							}
							else if (!mDataChanged)
							{
								performHeaderClick.Run();
							}
						}
					}
					mTouchMode = TOUCH_MODE_REST;
					return true;
			}
			return base.OnTouchEvent(ev);
		}
        
		private class TouchModeResetAction : Java.Lang.Object, IRunnable
		{
			private StickyGridHeadersGridView gridView;
			private View header;
			private PerformHeaderClickRunnable performHeaderClick;

            public TouchModeResetAction(StickyGridHeadersGridView gridView, View header, PerformHeaderClickRunnable performHeaderClick)
			{
                this.gridView = gridView;
				this.header = header;
				this.performHeaderClick = performHeaderClick;
			}

			public void Run()
			{
				gridView.mMotionHeaderPosition = NO_MATCHED_HEADER;
				gridView.mTouchModeReset = null;
				gridView.mTouchMode = TOUCH_MODE_REST;
				header.Pressed = false;
                gridView.Pressed = false;
				header.Invalidate();
                gridView.Invalidate(0, header.Top, gridView.Width, header.Height);
				if (!gridView.mDataChanged)
				{
					performHeaderClick.Run();
				}
			}
		}

		public bool PerformHeaderClick(View view, long id)
		{
			if (mOnHeaderClickListener != null)
			{
				PlaySoundEffect(SoundEffects.Click);
				if (view != null)
				{
					view.SendAccessibilityEvent(EventTypes.ViewClicked);
				}
				mOnHeaderClickListener.OnHeaderClick(this, view, id);
				return true;
			}

			return false;
		}

		public bool PerformHeaderLongPress(View view, long id)
		{
			bool handled = false;
			if (mOnHeaderLongClickListener != null)
			{
				handled = mOnHeaderLongClickListener.OnHeaderLongClick(this, view, id);
			}

			if (handled)
			{
				if (view != null)
				{
					view.SendAccessibilityEvent(EventTypes.ViewLongClicked);
				}
				PerformHapticFeedback(FeedbackConstants.LongPress);
			}

			return handled;
		}

		public override IListAdapter Adapter
		{
            get
            {
                return base.Adapter;
            }
			set
			{
				if (mAdapter != null && mDataSetObserver != null)
				{
					mAdapter.UnregisterDataSetObserver(mDataSetObserver);
				}
    
				if (!mClipToPaddingHasBeenSet)
				{
					mClippingToPadding = true;
				}
    
				IStickyGridHeadersBaseAdapter baseAdapter;
				if (value is IStickyGridHeadersBaseAdapter)
				{
					baseAdapter = (IStickyGridHeadersBaseAdapter)value;
				}
				else if (value is IStickyGridHeadersSimpleAdapter)
				{
					// Wrap up simple value to auto-generate the data we need.
					baseAdapter = new StickyGridHeadersSimpleAdapterWrapper((IStickyGridHeadersSimpleAdapter)value);
				}
				else
				{
					// Wrap up a list value so it is an value with zero headers.
					baseAdapter = new StickyGridHeadersListAdapterWrapper(value);
				}
    
				mAdapter = new StickyGridHeadersBaseAdapterWrapper(Context, this, baseAdapter);
				mAdapter.RegisterDataSetObserver(mDataSetObserver);
				Reset();
				base.Adapter = mAdapter;
			}
		}

		public bool AreHeadersSticky
		{
			set
			{
				if (value != mAreHeadersSticky)
				{
					mAreHeadersSticky = value;
					RequestLayout();
				}
			}
            get
            {
                return mAreHeadersSticky;
            }
		}

        public override void SetClipToPadding(bool clipToPadding)
        {
            base.SetClipToPadding(clipToPadding);
            mClippingToPadding = clipToPadding;
            mClipToPaddingHasBeenSet = true;
        }

        public override void SetColumnWidth(int columnWidth)
        {
            base.SetColumnWidth(columnWidth);
            mColumnWidth = columnWidth;
        }

		/// <summary>
		/// If set to true, headers will ignore horizontal padding.
		/// </summary>
		/// <param name="b"> if true, horizontal padding is ignored by headers </param>
		public bool HeadersIgnorePadding
		{
			set
			{
				mHeadersIgnorePadding = value;
			}
		}

        public override void SetHorizontalSpacing(int horizontalSpacing)
        {
            base.SetHorizontalSpacing(horizontalSpacing);
            mHorizontalSpacing = horizontalSpacing;
        }

		public override int NumColumns
		{
			set
			{
				base.NumColumns = value;
				mNumColumnsSet = true;
				this.mNumColumns = value;
				if (value != AUTO_FIT && mAdapter != null)
				{
					mAdapter.NumColumns = value;
				}
			}
		}

		public IOnHeaderClickListener OnHeaderClickListener
		{
			set
			{
				mOnHeaderClickListener = value;
			}
		}

		public IOnHeaderLongClickListener OnHeaderLongClickListener
		{
			set
			{
				if (!LongClickable)
				{
					LongClickable = true;
				}
				mOnHeaderLongClickListener = value;
			}
		}

        public void SetOnItemClickListener(AdapterView.IOnItemClickListener l)
        {
            this.mOnItemClickListener = l;
            base.OnItemClickListener = this;
        }

        public void SetOnItemLongClickListener(AdapterView.IOnItemLongClickListener l)
        {
            this.mOnItemLongClickListener = l;
            base.OnItemLongClickListener = this;
        }

        public void SetOnItemSelectedListener(AdapterView.IOnItemSelectedListener l)
        {
            this.mOnItemSelectedListener = l;
            base.OnItemSelectedListener = this;
        }

        public override void SetOnScrollListener(AbsListView.IOnScrollListener l)
        {
            this.mScrollListener = l;
        }

        public override void SetVerticalSpacing(int verticalSpacing)
        {
            base.SetVerticalSpacing(verticalSpacing);
            mVerticalSpacing = verticalSpacing;
        }

		private int FindMotionHeader(float y)
		{
			if (mStickiedHeader != null && y <= mHeaderBottomPosition)
			{
				return MATCHED_STICKIED_HEADER;
			}

			int vi = 0;
			for (int i = FirstVisiblePosition; i <= LastVisiblePosition;)
			{
				long id = GetItemIdAtPosition(i);
				if (id == StickyGridHeadersBaseAdapterWrapper.ID_HEADER)
				{
					View headerWrapper = GetChildAt(vi);

					int bottom = headerWrapper.Bottom;
					int top = headerWrapper.Top;
					if (y <= bottom && y >= top)
					{
						return vi;
					}
				}
				i += mNumMeasuredColumns;
				vi += mNumMeasuredColumns;
			}

			return NO_MATCHED_HEADER;
		}

		private int HeaderHeight
		{
			get
			{
				if (mStickiedHeader != null)
				{
					return mStickiedHeader.MeasuredHeight;
				}
				return 0;
			}
		}

		private long HeaderViewPositionToId(int pos)
		{
			if (pos == MATCHED_STICKIED_HEADER)
			{
				return mCurrentHeaderId;
			}
			return mAdapter.GetHeaderId(FirstVisiblePosition + pos);
		}

		private void MeasureHeader()
		{
			if (mStickiedHeader == null)
			{
				return;
			}

			int widthMeasureSpec;
			if (mHeadersIgnorePadding)
			{
                widthMeasureSpec = MeasureSpec.MakeMeasureSpec(Width, MeasureSpecMode.Exactly);
			}
			else
			{
                widthMeasureSpec = MeasureSpec.MakeMeasureSpec(Width - PaddingLeft - PaddingRight, MeasureSpecMode.Exactly);
			}

			int heightMeasureSpec = 0;

			ViewGroup.LayoutParams lp = mStickiedHeader.LayoutParameters;
			if (lp != null && lp.Height > 0)
			{
                heightMeasureSpec = MeasureSpec.MakeMeasureSpec(lp.Height, MeasureSpecMode.Exactly);
			}
			else
			{
                heightMeasureSpec = MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified);
			}
            mStickiedHeader.Measure(MeasureSpec.MakeMeasureSpec(0, 0), MeasureSpec.MakeMeasureSpec(0, 0));
            mStickiedHeader.Measure(widthMeasureSpec, heightMeasureSpec);

			if (mHeadersIgnorePadding)
			{
				mStickiedHeader.Layout(Left, 0, Right, mStickiedHeader.MeasuredHeight);
			}
			else
			{
                mStickiedHeader.Layout(Left + PaddingLeft, 0, Right - PaddingRight, mStickiedHeader.MeasuredHeight);
			}
		}

		private void Reset()
		{
			mHeaderBottomPosition = 0;
			SwapStickiedHeader(null);
			mCurrentHeaderId = AdapterView.InvalidRowId;
		}

		private void ScrollChanged(int firstVisibleItem)
		{
			if (mAdapter == null || mAdapter.Count == 0 || !mAreHeadersSticky)
			{
				return;
			}

			View firstItem = GetChildAt(0);
			if (firstItem == null)
			{
				return;
			}

			long newHeaderId;
			int selectedHeaderPosition = firstVisibleItem;

			int beforeRowPosition = firstVisibleItem - mNumMeasuredColumns;
			if (beforeRowPosition < 0)
			{
				beforeRowPosition = firstVisibleItem;
			}

			int secondRowPosition = firstVisibleItem + mNumMeasuredColumns;
			if (secondRowPosition >= mAdapter.Count)
			{
				secondRowPosition = firstVisibleItem;
			}

			if (mVerticalSpacing == 0)
			{
				newHeaderId = mAdapter.GetHeaderId(firstVisibleItem);
			}
			else if (mVerticalSpacing < 0)
			{
				newHeaderId = mAdapter.GetHeaderId(firstVisibleItem);
				View firstSecondRowView = GetChildAt(mNumMeasuredColumns);
				if (firstSecondRowView.Top <= 0)
				{
					newHeaderId = mAdapter.GetHeaderId(secondRowPosition);
					selectedHeaderPosition = secondRowPosition;
				}
				else
				{
					newHeaderId = mAdapter.GetHeaderId(firstVisibleItem);
				}
			}
			else
			{
				int margin = GetChildAt(0).Top;
				if (0 < margin && margin < mVerticalSpacing)
				{
					newHeaderId = mAdapter.GetHeaderId(beforeRowPosition);
					selectedHeaderPosition = beforeRowPosition;
				}
				else
				{
					newHeaderId = mAdapter.GetHeaderId(firstVisibleItem);
				}
			}

			if (mCurrentHeaderId != newHeaderId)
			{
				SwapStickiedHeader(mAdapter.GetHeaderView(selectedHeaderPosition, mStickiedHeader, this));
				MeasureHeader();
				mCurrentHeaderId = newHeaderId;
			}

			int childCount = ChildCount;
			if (childCount != 0)
			{
				View viewToWatch = null;
				int watchingChildDistance = 99999;

				// Find the next header after the stickied one.
				for (int i = 0; i < childCount; i += mNumMeasuredColumns)
				{
					View child = base.GetChildAt(i);

					int childDistance;
					if (mClippingToPadding)
					{
						childDistance = child.Top - PaddingTop;
					}
					else
					{
						childDistance = child.Top;
					}

					if (childDistance < 0)
					{
						continue;
					}

					if (mAdapter.GetItemId(GetPositionForView(child)) == StickyGridHeadersBaseAdapterWrapper.ID_HEADER && childDistance < watchingChildDistance)
					{
						viewToWatch = child;
						watchingChildDistance = childDistance;
					}
				}

				int headerHeight = HeaderHeight;

				// Work out where to draw stickied header using synchronised
				// scrolling.
				if (viewToWatch != null)
				{
					if (firstVisibleItem == 0 && base.GetChildAt(0).Top > 0 && !mClippingToPadding)
					{
						mHeaderBottomPosition = 0;
					}
					else
					{
						if (mClippingToPadding)
						{
							mHeaderBottomPosition = System.Math.Min(viewToWatch.Top, headerHeight + PaddingTop);
							mHeaderBottomPosition = mHeaderBottomPosition < PaddingTop ? headerHeight + PaddingTop : mHeaderBottomPosition;
						}
						else
						{
							mHeaderBottomPosition = System.Math.Min(viewToWatch.Top, headerHeight);
							mHeaderBottomPosition = mHeaderBottomPosition < 0 ? headerHeight : mHeaderBottomPosition;
						}
					}
				}
				else
				{
					mHeaderBottomPosition = headerHeight;
					if (mClippingToPadding)
					{
						mHeaderBottomPosition += PaddingTop;
					}
				}
			}
		}

		private void SwapStickiedHeader(View newStickiedHeader)
		{
			DetachHeader(mStickiedHeader);
			AttachHeader(newStickiedHeader);
			mStickiedHeader = newStickiedHeader;
		}

		private MotionEvent TransformEvent(MotionEvent e, int headerPosition)
		{
			if (headerPosition == MATCHED_STICKIED_HEADER)
			{
				return e;
			}

			long downTime = e.DownTime;
			long eventTime = e.EventTime;
			MotionEventActions action = e.Action;
			int pointerCount = e.PointerCount;
			int[] pointerIds = GetPointerIds(e);
			MotionEvent.PointerCoords[] pointerCoords = GetPointerCoords(e);
			MetaKeyStates metaState = e.MetaState;
			float xPrecision = e.XPrecision;
			float yPrecision = e.YPrecision;
			int deviceId = e.DeviceId;
			Edge edgeFlags = e.EdgeFlags;
			InputSourceType source = e.Source;
			MotionEventFlags flags = e.Flags;

			View headerHolder = GetChildAt(headerPosition);
			for (int i = 0; i < pointerCount;i++)
			{
				pointerCoords[i].Y -= headerHolder.Top;
			}
			MotionEvent n = MotionEvent.Obtain(downTime, eventTime, action, pointerCount, pointerIds, pointerCoords, metaState, xPrecision, yPrecision, deviceId, edgeFlags, source, flags);
			return n;
		}

		protected override void DispatchDraw(Canvas canvas)
		{
			if (Build.VERSION.SdkInt < Build.VERSION_CODES.Froyo)
			{
				ScrollChanged(FirstVisiblePosition);
			}

			bool drawStickiedHeader = mStickiedHeader != null && mAreHeadersSticky && mStickiedHeader.Visibility == ViewStates.Visible;
			int headerHeight = HeaderHeight;
			int top = mHeaderBottomPosition - headerHeight;

			// Mask the region where we will draw the header later, but only if we
			// will draw a header and masking is requested.
			if (drawStickiedHeader && mMaskStickyHeaderRegion)
			{
				if (mHeadersIgnorePadding)
				{
					mClippingRect.Left = 0;
					mClippingRect.Right = Width;
				}
				else
				{
                    mClippingRect.Left = PaddingLeft;
					mClippingRect.Right = Width - PaddingRight;
				}
				mClippingRect.Top = mHeaderBottomPosition;
				mClippingRect.Bottom = Height;

				canvas.Save();
				canvas.ClipRect(mClippingRect);
			}

			// ...and draw the grid view.
			base.DispatchDraw(canvas);

			// Find headers.
			IList<int> headerPositions = new List<int>();
			int vi = 0;
			for (int i = FirstVisiblePosition; i <= LastVisiblePosition;)
			{
				long id = GetItemIdAtPosition(i);
				if (id == StickyGridHeadersBaseAdapterWrapper.ID_HEADER)
				{
					headerPositions.Add(vi);
				}
				i += mNumMeasuredColumns;
				vi += mNumMeasuredColumns;
			}

			// Draw headers in list.
			for (int i = 0; i < headerPositions.Count; i++)
			{
				View frame = GetChildAt(headerPositions[i]);
				View header;
				try
				{
					header = (View)frame.Tag;
				}
				catch (System.Exception e)
				{
					return;
				}

				bool headerIsStickied = ((StickyGridHeaders.StickyGridHeadersBaseAdapterWrapper.HeaderFillerView)frame).HeaderId == mCurrentHeaderId && frame.Top < 0 && mAreHeadersSticky;
				if (header.Visibility != ViewStates.Visible || headerIsStickied)
				{
					continue;
				}

				int widthMeasureSpec;
				if (mHeadersIgnorePadding)
				{
                    widthMeasureSpec = MeasureSpec.MakeMeasureSpec(Width, MeasureSpecMode.Exactly);
				}
				else
				{
                    widthMeasureSpec = MeasureSpec.MakeMeasureSpec(Width - PaddingLeft - PaddingRight, MeasureSpecMode.Exactly);
				}

                int heightMeasureSpec = MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified);
                header.Measure(MeasureSpec.MakeMeasureSpec(0, 0), MeasureSpec.MakeMeasureSpec(0, 0));
				header.Measure(widthMeasureSpec, heightMeasureSpec);

				if (mHeadersIgnorePadding)
				{
					header.Layout(Left, 0, Right, frame.Height);
				}
				else
				{
					header.Layout(Left + PaddingLeft, 0, Right - PaddingRight, frame.Height);
				}

				if (mHeadersIgnorePadding)
				{
					mClippingRect.Left = 0;
					mClippingRect.Right = Width;
				}
				else
				{
					mClippingRect.Left = PaddingLeft;
					mClippingRect.Right = Width - PaddingRight;
				}

				mClippingRect.Bottom = frame.Bottom;
				mClippingRect.Top = frame.Top;
				canvas.Save();
				canvas.ClipRect(mClippingRect);
				if (mHeadersIgnorePadding)
				{
					canvas.Translate(0, frame.Top);
				}
				else
				{
					canvas.Translate(PaddingLeft, frame.Top);
				}
				header.Draw(canvas);
				canvas.Restore();
			}

			if (drawStickiedHeader && mMaskStickyHeaderRegion)
			{
				canvas.Restore();
			}
			else if (!drawStickiedHeader)
			{
				// Done.
				return;
			}

			// Draw stickied header.
			int wantedWidth;
			if (mHeadersIgnorePadding)
			{
				wantedWidth = Width;
			}
			else
			{
				wantedWidth = Width - PaddingLeft - PaddingRight;
			}
			if (mStickiedHeader.Width != wantedWidth)
			{
				int widthMeasureSpec;
				if (mHeadersIgnorePadding)
				{
                    widthMeasureSpec = MeasureSpec.MakeMeasureSpec(Width, MeasureSpecMode.Exactly);
				}
				else
				{
                    widthMeasureSpec = MeasureSpec.MakeMeasureSpec(Width - PaddingLeft - PaddingRight, MeasureSpecMode.Exactly); // Bug here
				}
                int heightMeasureSpec = MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified);
                mStickiedHeader.Measure(MeasureSpec.MakeMeasureSpec(0, 0), MeasureSpec.MakeMeasureSpec(0, 0));
				mStickiedHeader.Measure(widthMeasureSpec, heightMeasureSpec);
				if (mHeadersIgnorePadding)
				{
					mStickiedHeader.Layout(Left, 0, Right, mStickiedHeader.Height);
				}
				else
				{
					mStickiedHeader.Layout(Left + PaddingLeft, 0, Right - PaddingRight, mStickiedHeader.Height);
				}
			}

			if (mHeadersIgnorePadding)
			{
				mClippingRect.Left = 0;
				mClippingRect.Right = Width;
			}
			else
			{
				mClippingRect.Left = PaddingLeft;
				mClippingRect.Right = Width - PaddingRight;
			}
			mClippingRect.Bottom = top + headerHeight;
			if (mClippingToPadding)
			{
				mClippingRect.Top = PaddingTop;
			}
			else
			{
				mClippingRect.Top = 0;
			}

			canvas.Save();
			canvas.ClipRect(mClippingRect);

			if (mHeadersIgnorePadding)
			{
				canvas.Translate(0, top);
			}
			else
			{
				canvas.Translate(PaddingLeft, top);
			}

			if (mHeaderBottomPosition != headerHeight)
			{
				canvas.SaveLayerAlpha(0, 0, canvas.Width, canvas.Height, 255 * mHeaderBottomPosition / headerHeight, SaveFlags.All);
			}

			mStickiedHeader.Draw(canvas);

			if (mHeaderBottomPosition != headerHeight)
			{
				canvas.Restore();
			}
			canvas.Restore();
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			if (mNumColumns == AUTO_FIT)
			{
				int numFittedColumns;
				if (mColumnWidth > 0)
				{
					int gridWidth = System.Math.Max(MeasureSpec.GetSize(widthMeasureSpec) - PaddingLeft - PaddingRight, 0);
					numFittedColumns = gridWidth / mColumnWidth;
					// Calculate measured columns accounting for requested grid
					// spacing.
					if (numFittedColumns > 0)
					{
						while (numFittedColumns != 1)
						{
							if (numFittedColumns * mColumnWidth + (numFittedColumns - 1) * mHorizontalSpacing > gridWidth)
							{
								numFittedColumns--;
							}
							else
							{
								break;
							}
						}
					}
					else
					{
						// Could not fit any columns in grid width, so default to a
						// single column.
						numFittedColumns = 1;
					}
				}
				else
				{
					// Mimic vanilla GridView behaviour where there is not enough
					// information to auto-fit columns.
					numFittedColumns = 2;
				}
				mNumMeasuredColumns = numFittedColumns;
			}
			else
			{
				// There were some number of columns requested so we will try to
				// fulfil the request.
				mNumMeasuredColumns = mNumColumns;
			}

			// Update adapter with number of columns.
			if (mAdapter != null)
			{
				mAdapter.NumColumns = mNumMeasuredColumns;
			}

			MeasureHeader();

			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		}

		public void AttachHeader(View header)
		{
			if (header == null)
			{
				return;
			}

			try
			{
				Field attachInfoField = Java.Lang.Class.FromType(typeof(View)).GetDeclaredField("mAttachInfo");
				attachInfoField.Accessible = true;
				Method method = Java.Lang.Class.FromType(typeof(View)).GetDeclaredMethod("dispatchAttachedToWindow", Class.ForName("android.view.View$AttachInfo"), Java.Lang.Integer.Type);
				method.Accessible = true;
                method.Invoke(header, new Java.Lang.Object[] {attachInfoField.Get(this), (int)ViewStates.Gone});
			}
			catch (NoSuchMethodException e)
			{
				throw new RuntimePlatformSupportException(this, e);
			}
			catch (ClassNotFoundException e)
			{
				throw new RuntimePlatformSupportException(this, e);
			}
			catch (System.ArgumentException e)
			{
				throw new RuntimePlatformSupportException(this, e);
			}
			catch (IllegalAccessException e)
			{
				throw new RuntimePlatformSupportException(this, e);
			}
			catch (InvocationTargetException e)
			{
				throw new RuntimePlatformSupportException(this, e);
			}
			catch (NoSuchFieldException e)
			{
				throw new RuntimePlatformSupportException(this, e);
			}
		}

		public void DetachHeader(View header)
		{
			if (header == null)
			{
				return;
			}

			try
			{
				Method method = Java.Lang.Class.FromType(typeof(View)).GetDeclaredMethod("dispatchDetachedFromWindow");
				method.Accessible = true;
				method.Invoke(header);
			}
			catch (NoSuchMethodException e)
			{
				throw new RuntimePlatformSupportException(this, e);
			}
			catch (System.ArgumentException e)
			{
				throw new RuntimePlatformSupportException(this, e);
			}
			catch (IllegalAccessException e)
			{
				throw new RuntimePlatformSupportException(this, e);
			}
			catch (InvocationTargetException e)
			{
				throw new RuntimePlatformSupportException(this, e);
			}
		}

		public interface IOnHeaderClickListener
		{
			void OnHeaderClick(AdapterView parent, View view, long id);
		}

		public interface IOnHeaderLongClickListener
		{
			bool OnHeaderLongClick(AdapterView parent, View view, long id);
		}

		public class CheckForHeaderLongPress : WindowRunnable, IRunnable
		{
			private StickyGridHeadersGridView gridView;

            public CheckForHeaderLongPress(StickyGridHeadersGridView gridView)
                : base(gridView)
			{
                this.gridView = gridView;
			}

			public void Run()
			{
				View child = gridView.GetHeaderAt(gridView.mMotionHeaderPosition);
				if (child != null)
				{
					long longPressId = gridView.HeaderViewPositionToId(gridView.mMotionHeaderPosition);

					bool handled = false;
					if (SameWindow() && !gridView.mDataChanged)
					{
						handled = gridView.PerformHeaderLongPress(child, longPressId);
					}
					if (handled)
					{
						gridView.mTouchMode = TOUCH_MODE_FINISHED_LONG_PRESS;
                        gridView.Pressed = false;
						child.Pressed = false;
					}
					else
					{
						gridView.mTouchMode = TOUCH_MODE_DONE_WAITING;
					}
				}
			}
		}

		private class PerformHeaderClickRunnable : WindowRunnable, IRunnable
		{
            private StickyGridHeadersGridView gridView;
			public int mClickMotionPosition;

            public PerformHeaderClickRunnable(StickyGridHeadersGridView gridView)
                : base(gridView)
			{
                this.gridView = gridView;
			}

			public void Run()
			{
				// The data has changed since we posted this action to the event
				// queue, bail out before bad things happen.
                if (gridView.mDataChanged)
				{
					return;
				}

                if (gridView.mAdapter != null && gridView.mAdapter.Count > 0 && mClickMotionPosition != AdapterView.InvalidPosition && mClickMotionPosition < gridView.mAdapter.Count && SameWindow())
				{
                    View view = gridView.GetHeaderAt(mClickMotionPosition);
					// If there is no view then something bad happened, the view
					// probably scrolled off the screen, and we should cancel the
					// click.
					if (view != null)
					{
                        gridView.PerformHeaderClick(view, gridView.HeaderViewPositionToId(mClickMotionPosition));
					}
				}
			}
		}

		/// <summary>
		/// A base class for Runnables that will check that their view is still
		/// attached to the original window as when the Runnable was created.
		/// </summary>
		public class WindowRunnable : Java.Lang.Object
		{
			private StickyGridHeadersGridView gridView;

            public WindowRunnable(StickyGridHeadersGridView gridView)
			{
                this.gridView = gridView;
			}

            public int mOriginalAttachCount;

			public void RememberWindowAttachCount()
			{
                mOriginalAttachCount = gridView.WindowAttachCount;
			}

			public bool SameWindow()
			{
                return gridView.HasFocus && gridView.WindowAttachCount == mOriginalAttachCount;
			}
		}

		public class CheckForHeaderTap : Java.Lang.Object, IRunnable
		{
			private StickyGridHeadersGridView gridView;

            public CheckForHeaderTap(StickyGridHeadersGridView gridView)
			{
                this.gridView = gridView;
			}

			public void Run()
			{
				if (gridView.mTouchMode == TOUCH_MODE_DOWN)
				{
					gridView.mTouchMode = TOUCH_MODE_TAP;
					View header = gridView.GetHeaderAt(gridView.mMotionHeaderPosition);
					if (header != null && !gridView.mHeaderChildBeingPressed)
					{
						if (!gridView.mDataChanged)
						{
							header.Pressed = true;
                            gridView.Pressed = true;
                            gridView.RefreshDrawableState();

							int longPressTimeout = ViewConfiguration.LongPressTimeout;
                            bool longClickable = gridView.LongClickable;

							if (longClickable)
							{
								if (gridView.mPendingCheckForLongPress == null)
								{
									gridView.mPendingCheckForLongPress = new CheckForHeaderLongPress(gridView);
								}
								gridView.mPendingCheckForLongPress.RememberWindowAttachCount();
                                gridView.PostDelayed(gridView.mPendingCheckForLongPress, longPressTimeout);
							}
							else
							{
								gridView.mTouchMode = TOUCH_MODE_DONE_WAITING;
							}
						}
						else
						{
							gridView.mTouchMode = TOUCH_MODE_DONE_WAITING;
						}
					}
				}
			}
		}

		public class RuntimePlatformSupportException : System.Exception
		{
			private StickyGridHeadersGridView gridView;

            public const long serialVersionUID = -6512098808936536538L;

            public RuntimePlatformSupportException(StickyGridHeadersGridView gridView, System.Exception e)
                : base(ERROR_PLATFORM, e)
			{
                this.gridView = gridView;
			}
		}

		/// <summary>
		/// Constructor called from <seealso cref="#CREATOR"/>
		/// </summary>
		public class SavedState : BaseSavedState
		{
			public SavedState(IParcelable superState) 
                : base(superState)
			{

			}

			/// <summary>
			/// Constructor called from <seealso cref="#CREATOR"/>
			/// </summary>
			public SavedState(Parcel i) : base(i)
			{
				AreHeadersSticky = i.ReadByte() != 0;
			}

			public override string ToString()
			{
				return "StickyGridHeadersGridView.SavedState{" + Java.Lang.JavaSystem.IdentityHashCode(this) + " areHeadersSticky=" + AreHeadersSticky + "}";
			}

            public override void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
            {
                base.WriteToParcel(dest, flags);
                dest.WriteByte((sbyte)(AreHeadersSticky ? 1 : 0));
            }

            [ExportField("CREATOR")]
            static SavedStateCreator InitializeCreator()
            {
                return new SavedStateCreator();
            }

            class SavedStateCreator : Java.Lang.Object, IParcelableCreator
            {
                public Java.Lang.Object CreateFromParcel(Parcel i)
                {
                    return new SavedState(i);
                }

                public Java.Lang.Object[] NewArray(int size)
                {
                    return new SavedState[size];
                }
            }

            public bool AreHeadersSticky;
		}
	}
}