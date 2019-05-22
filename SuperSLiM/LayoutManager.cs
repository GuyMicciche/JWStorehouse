using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Util;
using Android.Views;
using Java.Interop;
using Java.Lang;
using System;
using System.Collections.Generic;

namespace SuperSLiM
{
	/// <summary>
	/// A LayoutManager that lays out mSection headers with optional stickiness and uses a map of
	/// sections to view layout managers to layout items.
	/// </summary>
	public class LayoutManager : RecyclerView.LayoutManager
	{
		internal const int SECTION_MANAGER_CUSTOM = -1;
		internal const int SECTION_MANAGER_LINEAR = 0x01;
		internal const int SECTION_MANAGER_GRID = 0x02;
		internal const int SECTION_MANAGER_STAGGERED_GRID = 0x03;
		private const int NO_POSITION_REQUEST = -1;
		private int mRequestPosition = NO_POSITION_REQUEST;
		private readonly SectionLayoutManager mLinearSlm;
		private readonly SectionLayoutManager mGridSlm;
		private Rect mRect = new Rect();
		private int mRequestPositionOffset = 0;
		private Dictionary<string, SectionLayoutManager> mSlms;
		private bool mSmoothScrollEnabled = true;
		private SparseArray<SectionData> mSectionDataCache = new SparseArray<SectionData>();

		public LayoutManager(Context context)
		{
			mLinearSlm = new LinearSLM(this);
			mGridSlm = new GridSLM(this, context);
            mSlms = new Dictionary<string, SectionLayoutManager>();
		}

		public LayoutManager(Builder builder)
		{
			mLinearSlm = new LinearSLM(this);
			mGridSlm = new GridSLM(this, builder.context);
			mSlms = builder.slms;
		}

		/// <summary>
		/// Add a section layout manager to those that can be used to lay out items.
		/// </summary>
		/// <param name="key"> Key to match that to be set in <seealso cref="LayoutParams#setSlm(String)"/>. </param>
		/// <param name="slm"> SectionLayoutManager to add. </param>
		public void AddSlm(string key, SectionLayoutManager slm)
		{
			mSlms[key] = slm;
		}

		/// <summary>
		/// Find the position of the first completely visible item.
		/// </summary>
		/// <returns> Position of first completely visible item. </returns>
		public virtual View FindFirstCompletelyVisibleItem()
		{
			View firstVisibleView = null;
			SectionData sd = null;
			for (int i = 0; i < ChildCount - 1; i++)
			{
				LayoutParams lp = (LayoutParams)GetChildAt(0).LayoutParameters;
				SectionLayoutManager slm = GetSlm(lp);

				firstVisibleView = slm.FindFirstCompletelyVisibleView(lp.FirstPosition, false);
				if (firstVisibleView != null)
				{
					break;
				}
			}
			if (firstVisibleView == null)
			{
				return null;
			}

			int firstVisiblePosition = GetPosition(firstVisibleView);
			if (firstVisiblePosition == sd.firstPosition || firstVisiblePosition > sd.firstPosition + 1)
			{
				// Header doesn't matter.
				return firstVisibleView;
			}

			// Maybe the header is completely visible.
			View first = FindAttachedHeaderOrFirstViewForSection(sd.firstPosition, 0, Direction.START);
			if (first == null)
			{
				return firstVisibleView;
			}

			int topEdge = ClipToPadding ? PaddingTop : 0;
			int bottomEdge = ClipToPadding ? Height - PaddingBottom : Height;

			int firstTop = GetDecoratedTop(first);
			int firstBottom = GetDecoratedBottom(first);

			if (firstTop < topEdge || bottomEdge < firstBottom)
			{
				return firstVisibleView;
			}

			if (firstBottom <= GetDecoratedTop(firstVisibleView))
			{
				return first;
			}

			LayoutParams firstParams = (LayoutParams)first.LayoutParameters;
			if ((!firstParams.HeaderInline || firstParams.HeaderOverlay) && firstTop == GetDecoratedTop(firstVisibleView))
			{
				return first;
			}

			return firstVisibleView;
		}

		/// <summary>
		/// Find the position of the first completely visible item.
		/// </summary>
		/// <returns> Position of first completely visible item. </returns>
		public int FindFirstCompletelyVisibleItemPosition()
		{
			return GetPosition(FindFirstCompletelyVisibleItem());
		}

		/// <summary>
		/// Find the position of the first visible item.
		/// </summary>
		/// <returns> Position of first visible item. </returns>
		public View FindFirstVisibleItem()
		{
			LayoutParams lp = (LayoutParams)GetChildAt(0).LayoutParameters;
			SectionLayoutManager slm = GetSlm(lp);
			View firstVisibleView = slm.FindFirstVisibleView(lp.FirstPosition, false);
			int position = GetPosition(firstVisibleView);
			if (position > lp.FirstPosition + 1 || position == lp.FirstPosition)
			{
				return firstVisibleView;
			}
			View first = FindAttachedHeaderOrFirstViewForSection(lp.FirstPosition, 0, Direction.START);
			if (first == null)
			{
				return firstVisibleView;
			}

			if (GetDecoratedBottom(first) <= GetDecoratedTop(firstVisibleView))
			{
				return first;
			}

			LayoutParams firstParams = (LayoutParams)first.LayoutParameters;
			if ((!firstParams.HeaderInline || firstParams.HeaderOverlay) && GetDecoratedTop(first) == GetDecoratedTop(firstVisibleView))
			{
				return first;
			}

			return firstVisibleView;
		}

		/// <summary>
		/// Find the position of the first visible item.
		/// </summary>FindLastCompletelyVisibleView
		/// <returns> Position of first visible item. </returns>
		public int FindFirstVisibleItemPosition()
		{
			return GetPosition(FindFirstVisibleItem());
		}

		/// <summary>
		/// Find the position of the last completely visible item.
		/// </summary>
		/// <returns> Position of last completely visible item. </returns>
		public View FindLastCompletelyVisibleItem()
		{
			LayoutParams lp = (LayoutParams)GetChildAt(ChildCount - 1).LayoutParameters;
			SectionLayoutManager slm = GetSlm(lp);

			return slm.FindLastCompletelyVisibleView(lp.FirstPosition);
		}

		/// <summary>
		/// Find the position of the last completely visible item.
		/// </summary>
		/// <returns> Position of last completely visible item. </returns>
		public int FindLastCompletelyVisibleItemPosition()
		{
			LayoutParams lp = (LayoutParams)GetChildAt(ChildCount - 1).LayoutParameters;
			SectionLayoutManager slm = GetSlm(lp);

			return slm.FindLastCompletelyVisibleItemPosition(lp.FirstPosition);
		}

		/// <summary>
		/// Find the position of the last visible item.
		/// </summary>
		/// <returns> Position of last visible item. </returns>
		public View FindLastVisibleItem()
		{
			LayoutParams lp = (LayoutParams)GetChildAt(ChildCount - 1).LayoutParameters;
			SectionLayoutManager slm = GetSlm(lp);

			return slm.FindLastVisibleView(lp.FirstPosition);
		}

		/// <summary>
		/// Find the position of the last visible item.
		/// </summary>
		/// <returns> Position of last visible item. </returns>
		public int FindLastVisibleItemPosition()
		{
			LayoutParams lp = (LayoutParams)GetChildAt(ChildCount - 1).LayoutParameters;
			SectionLayoutManager slm = GetSlm(lp);

			return slm.FindLastVisibleItemPosition(lp.FirstPosition);
		}

		public void GetEdgeStates(Rect outRect, View child, RecyclerView.State state)
		{
			LayoutParams lp = (LayoutParams)child.LayoutParameters;
			if (lp.isHeader)
			{
				if (LayoutDirection == ViewCompat.LayoutDirectionLtr)
				{
					outRect.Left = lp.HeaderStartAligned ? ItemDecorator.EXTERNAL : ItemDecorator.INTERNAL;
					outRect.Right = lp.HeaderStartAligned ? ItemDecorator.INTERNAL : ItemDecorator.EXTERNAL;
				}
				else
				{
					outRect.Right = lp.HeaderStartAligned ? ItemDecorator.EXTERNAL : ItemDecorator.INTERNAL;
					outRect.Left = lp.HeaderStartAligned ? ItemDecorator.INTERNAL : ItemDecorator.EXTERNAL;
				}
				outRect.Top = lp.ViewPosition == 0 ? ItemDecorator.EXTERNAL : ItemDecorator.INTERNAL;
				outRect.Bottom = lp.ViewPosition == state.ItemCount - 1 ? ItemDecorator.EXTERNAL : ItemDecorator.INTERNAL;
				return;
			}
			SectionData sd = GetSectionDataInternal(lp.FirstPosition, child);
			SectionLayoutManager slm = GetSlm(sd);
			slm.GetEdgeStates(outRect, child, sd, state);
		}

		private SectionData GetSectionDataInternal(int sfp, View view)
		{
			SectionData sd = mSectionDataCache.Get(sfp);
			if (sd == null)
			{
				sd = new SectionData(this, view);
				mSectionDataCache.Put(sfp, sd);
			}
			return sd;
		}

		/// <summary>
		/// Get section data.
		/// </summary>
		/// <param name="sfp">  Section id. First position of section. </param>
		/// <param name="view"> View to create new section data if non is found. </param>
		/// <returns> Section data. </returns>
		public SectionData GetSectionData(int sfp, View view)
		{
			SectionData sd = mSectionDataCache.Get(sfp);
			if (sd == null)
			{
				sd = new SectionData(this, view);
			}
			return sd;

		}

		public bool SmoothScrollEnabled
		{
			get
			{
				return mSmoothScrollEnabled;
			}
			set
			{
				mSmoothScrollEnabled = value;
			}
		}


		public override void OnLayoutChildren(RecyclerView.Recycler recycler, RecyclerView.State state)
		{
			int itemCount = state.ItemCount;
			if (itemCount == 0)
			{
				DetachAndScrapAttachedViews(recycler);
				return;
			}

			int requestedPosition;
			int borderLine;

			if (mRequestPosition != NO_POSITION_REQUEST)
			{
				requestedPosition = System.Math.Min(mRequestPosition, itemCount - 1);
				mRequestPosition = NO_POSITION_REQUEST;
				borderLine = mRequestPositionOffset;
				mRequestPositionOffset = 0;
			}
			else
			{
				View anchorView = AnchorChild;
				requestedPosition = anchorView == null ? 0 : GetPosition(anchorView);
				borderLine = GetBorderLine(anchorView, Direction.END);
			}

			DetachAndScrapAttachedViews(recycler);
			ClearSectionDataCache();

			LayoutState layoutState = new LayoutState(this, recycler, state);
			int bottomLine = LayoutChildren(requestedPosition, borderLine, layoutState);

			FixOverscroll(bottomLine, layoutState);
		}

		public override RecyclerView.LayoutParams GenerateDefaultLayoutParams()
		{
            return new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
		}

        public override RecyclerView.LayoutParams GenerateLayoutParams(ViewGroup.LayoutParams lp)
		{
			LayoutParams newlp = new LayoutParams(lp);
            newlp.Width = LayoutParams.MatchParent;
            newlp.Height = LayoutParams.MatchParent;

            return GetSlm(newlp).GenerateLayoutParams(newlp);
		}

		public override RecyclerView.LayoutParams GenerateLayoutParams(Context c, IAttributeSet attrs)
		{
			// Just so we don't build layout params multiple times.

			bool isString;
			TypedArray a = c.ObtainStyledAttributes(attrs, Resource.Styleable.superslim_LayoutManager);
			if (Build.VERSION.SdkInt < Build.VERSION_CODES.Lollipop)
			{
                TypedValue value = new TypedValue();
                a.GetValue(Resource.Styleable.superslim_LayoutManager_slm_section_layoutManager, value);
				isString = value.Type == DataType.String;
			}
			else
			{
                isString = a.GetType(Resource.Styleable.superslim_LayoutManager_slm_section_layoutManager) == (int)DataType.String;
			}
			string sectionManager = null;
			int sectionManagerKind;
			if (isString)
			{
                sectionManager = a.GetString(Resource.Styleable.superslim_LayoutManager_slm_section_layoutManager);
				if (TextUtils.IsEmpty(sectionManager))
				{
					sectionManagerKind = SECTION_MANAGER_LINEAR;
				}
				else
				{
					sectionManagerKind = SECTION_MANAGER_CUSTOM;
				}
			}
			else
			{
                sectionManagerKind = a.GetInt(Resource.Styleable.superslim_LayoutManager_slm_section_layoutManager, SECTION_MANAGER_LINEAR);
			}
			a.Recycle();

			return GetSlm(sectionManagerKind, sectionManager).GenerateLayoutParams(c, attrs);
		}

		public override int ScrollVerticallyBy(int dy, RecyclerView.Recycler recycler, RecyclerView.State state)
		{
			int numChildren = ChildCount;
			if (numChildren == 0)
			{
				return 0;
			}

			LayoutState layoutState = new LayoutState(this, recycler, state);

			Direction direction = dy > 0 ? Direction.END : Direction.START;
			bool isDirectionEnd = direction == Direction.END;
			int height = Height;

			int leadingEdge = isDirectionEnd ? height + dy : dy;

			// Handle situation where total content height is less than the view height. We only
			// have to handle the end direction because we never over scroll the top or lay out
			// from the bottom up.
			if (isDirectionEnd)
			{
				View end = AnchorAtEnd;
				LayoutParams lp = (LayoutParams)end.LayoutParameters;
				SectionData sd = GetSectionData(lp.FirstPosition, end);
				SectionLayoutManager slm = GetSlm(sd);
				int endEdge = slm.GetLowestEdge(lp.FirstPosition, ChildCount - 1, leadingEdge);
				if (endEdge < height - PaddingBottom && GetPosition(end) == (state.ItemCount - 1))
				{
					return 0;
				}
			}

			int fillEdge = FillUntil(leadingEdge, direction, layoutState);

			int delta;
			if (isDirectionEnd)
			{
				// Add padding so we scroll to inset area at scroll end.
				int fillDelta = fillEdge - height + PaddingBottom;
				delta = fillDelta < dy ? fillDelta : dy;
			}
			else
			{
				int fillDelta = fillEdge - PaddingTop;
				delta = fillDelta > dy ? fillDelta : dy;
			}

			if (delta != 0)
			{
				OffsetChildrenVertical(-delta);

				TrimTail(isDirectionEnd ? Direction.START : Direction.END, layoutState);
			}

			layoutState.recycleCache();

			return delta;
		}

		public override bool CanScrollVertically()
		{
			return true;
		}

		public override void ScrollToPosition(int position)
		{
			if (position < 0 || ItemCount <= position)
			{
				Console.WriteLine("SuperSLiM.LayoutManager", "Ignored scroll to " + position + " as it is not within the item range 0 - " + ItemCount);
				return;
			}

			mRequestPosition = position;
			RequestLayout();
		}

		public override void SmoothScrollToPosition(RecyclerView recyclerView, RecyclerView.State state, int position)
		{
			if (position < 0 || ItemCount <= position)
			{
				Console.WriteLine("SuperSLiM.LayoutManager", "Ignored smooth scroll to " + position + " as it is not within the item range 0 - " + ItemCount);
				return;
			}

			// Temporarily disable sticky headers.
			RequestLayout();

            recyclerView.Handler.Post(() =>
                {
                    LinearSmoothScroller smoothScroller = new LinearSmoothScrollerAnonymousInnerClassHelper(this, recyclerView.Context);
                    smoothScroller.TargetPosition = position;
                    StartSmoothScroll(smoothScroller);
                });
		}

        private class LinearSmoothScrollerAnonymousInnerClassHelper : LinearSmoothScroller
        {
            private readonly SuperSLiM.LayoutManager outerInstance;

            public LinearSmoothScrollerAnonymousInnerClassHelper(SuperSLiM.LayoutManager outerInstance, Context context)
                : base(context)
            {
                this.outerInstance = outerInstance;
            }

            protected override void OnChildAttachedToWindow(View child)
            {
                base.OnChildAttachedToWindow(child);
            }

            protected override void OnStop()
            {
                base.OnStop();
                // Turn sticky headers back on.
                outerInstance.RequestLayout();
            }

            protected override int VerticalSnapPreference
            {
                get
                {
                    return LinearSmoothScroller.SnapToStart;
                }
            }

            public override int CalculateDyToMakeVisible(View view, int snapPreference)
            {
                RecyclerView.LayoutManager layoutManager = LayoutManager;
                if (!layoutManager.CanScrollVertically())
                {
                    return 0;
                }
                RecyclerView.LayoutParams lp = (RecyclerView.LayoutParams)view.LayoutParameters;
                int top = layoutManager.GetDecoratedTop(view) - lp.TopMargin;
                int bottom = layoutManager.GetDecoratedBottom(view) + lp.BottomMargin;
                int start = outerInstance.GetPosition(view) == 0 ? layoutManager.PaddingTop : 0;
                int end = layoutManager.Height - layoutManager.PaddingBottom;
                int dy = CalculateDtToFit(top, bottom, start, end, snapPreference);
                return dy == 0 ? 1 : dy;
            }

            public override PointF ComputeScrollVectorForPosition(int tarGetPosition)
            {
                if (ChildCount == 0)
                {
                    return null;
                }

                return new PointF(0, outerInstance.GetDirectionToPosition(tarGetPosition));
            }
        }

		public override int GetDecoratedMeasuredWidth(View child)
		{
			ViewGroup.MarginLayoutParams lp = (ViewGroup.MarginLayoutParams) child.LayoutParameters;
			return base.GetDecoratedMeasuredWidth(child) + lp.LeftMargin + lp.RightMargin;
		}

		public override int GetDecoratedMeasuredHeight(View child)
		{
			ViewGroup.MarginLayoutParams lp = (ViewGroup.MarginLayoutParams) child.LayoutParameters;
			return base.GetDecoratedMeasuredHeight(child) + lp.TopMargin + lp.BottomMargin;
		}

		public override void LayoutDecorated(View child, int left, int top, int right, int bottom)
		{
			ViewGroup.MarginLayoutParams lp = (ViewGroup.MarginLayoutParams) child.LayoutParameters;
			base.LayoutDecorated(child, left + lp.LeftMargin, top + lp.TopMargin, right - lp.RightMargin, bottom - lp.BottomMargin);
		}

		public override int GetDecoratedLeft(View child)
		{
			ViewGroup.MarginLayoutParams lp = (ViewGroup.MarginLayoutParams) child.LayoutParameters;
			return base.GetDecoratedLeft(child) - lp.LeftMargin;
		}

		public override int GetDecoratedTop(View child)
		{
			ViewGroup.MarginLayoutParams lp = (ViewGroup.MarginLayoutParams) child.LayoutParameters;
			return base.GetDecoratedTop(child) - lp.TopMargin;
		}

		public override int GetDecoratedRight(View child)
		{
			ViewGroup.MarginLayoutParams lp = (ViewGroup.MarginLayoutParams) child.LayoutParameters;
			return base.GetDecoratedRight(child) + lp.RightMargin;
		}

		public override int GetDecoratedBottom(View child)
		{
			ViewGroup.MarginLayoutParams lp = (ViewGroup.MarginLayoutParams) child.LayoutParameters;
			return base.GetDecoratedBottom(child) + lp.BottomMargin;
		}

		public override void OnAdapterChanged(RecyclerView.Adapter oldAdapter, RecyclerView.Adapter newAdapter)
		{
			RemoveAllViews();
		}

		public override void OnItemsUpdated(RecyclerView recyclerView, int positionStart, int itemCount)
		{
			base.OnItemsUpdated(recyclerView, positionStart, itemCount);

			View first =GetChildAt(0);
			View last =GetChildAt(ChildCount - 1);
			if (positionStart + itemCount <= GetPosition(first))
			{
				return;
			}

			if (positionStart <= GetPosition(last))
			{
				RequestLayout();
			}
		}

		public override int ComputeVerticalScrollExtent(RecyclerView.State state)
		{
			if (state.ItemCount == 0)
			{
				return 0;
			}

			if (!mSmoothScrollEnabled)
			{
				return ChildCount;
			}

			float contentInView = ChildCount;

			// Work out fraction of content lost off top and bottom.
			contentInView -= GetFractionOfContentAbove(state, true);
			contentInView -= GetFractionOfContentBelow(state, true);

			return (int)(contentInView / state.ItemCount * Height);
		}

		public override int ComputeVerticalScrollOffset(RecyclerView.State state)
		{
			if (state.ItemCount == 0)
			{
				return 0;
			}

			View child =GetChildAt(0);
			if (!mSmoothScrollEnabled)
			{
				return GetPosition(child);
			}

			float contentAbove = GetPosition(child);
			contentAbove += GetFractionOfContentAbove(state, false);
			return (int)(contentAbove / state.ItemCount * Height);
		}

		public override int ComputeVerticalScrollRange(RecyclerView.State state)
		{
			if (!mSmoothScrollEnabled)
			{
				return state.ItemCount;
			}

			return Height;
		}

		public override IParcelable OnSaveInstanceState()
		{
			SavedState state = new SavedState();
			View view = AnchorChild;
			if (view == null)
			{
				state.anchorPosition = 0;
				state.anchorOffset = 0;
			}
			else
			{
				state.anchorPosition = GetPosition(view);
				state.anchorOffset = GetDecoratedTop(view);
			}
			return state;
		}

		public override void OnRestoreInstanceState(IParcelable state)
		{
			mRequestPosition = ((SavedState) state).anchorPosition;
			mRequestPositionOffset = ((SavedState) state).anchorOffset;
			RequestLayout();
		}

		public void MeasureHeader(View header)
		{
			// Width to leave for the mSection to which this header belongs. Only applies if the
			// header is being laid out adjacent to the mSection.
			int unavailableWidth = 0;
			LayoutParams lp = (LayoutParams)header.LayoutParameters;
			int recyclerWidth = Width - PaddingStart - PaddingEnd;
			if (!lp.HeaderOverlay)
			{
				if (lp.HeaderStartAligned && !lp.headerStartMarginIsAuto)
				{
					unavailableWidth = recyclerWidth - lp.headerMarginStart;
				}
				else if (lp.HeaderEndAligned && !lp.headerEndMarginIsAuto)
				{
					unavailableWidth = recyclerWidth - lp.headerMarginEnd;
				}
			}
			MeasureChildWithMargins(header, unavailableWidth, 0);
		}

		private void AttachHeaderForStart(View header, int leadingEdge, SectionData sd, LayoutState state)
		{
			if (state.GetCachedView(sd.firstPosition) != null && GetDecoratedBottom(header) > leadingEdge)
			{
				AddView(header, FindLastIndexForSection(sd.firstPosition) + 1);
				state.DecacheView(sd.firstPosition);
	//        } else {
	//            detachView(header);
	//            attachView(header, findLastIndexForSection(sd.firstPosition) + 1);
			}
		}

		private int BinarySearchForLastPosition(int min, int max, int sfp)
		{
			if (max < min)
			{
				return -1;
			}

			int mid = min + (max - min) / 2;

			View candidate =GetChildAt(mid);
			LayoutParams lp = (LayoutParams)candidate.LayoutParameters;
			if (lp.FirstPosition < sfp)
			{
				return BinarySearchForLastPosition(mid + 1, max, sfp);
			}

			if (lp.FirstPosition > sfp || lp.isHeader)
			{
				return BinarySearchForLastPosition(min, mid - 1, sfp);
			}

			if (mid == ChildCount - 1)
			{
				return mid;
			}

			View next = GetChildAt(mid + 1);
			LayoutParams newlp = (LayoutParams)next.LayoutParameters;
            if (newlp.FirstPosition != sfp)
			{
				return mid;
			}

            if (newlp.isHeader)
			{
				if (mid + 1 == ChildCount - 1)
				{
					return mid;
				}

				next =GetChildAt(mid + 2);
                newlp = (LayoutParams)next.LayoutParameters;
                if (newlp.FirstPosition != sfp)
				{
					return mid;
				}
			}

			return BinarySearchForLastPosition(mid + 1, max, sfp);
		}

		private void ClearSectionDataCache()
		{
			mSectionDataCache.Clear();
		}

		/// <summary>
		/// Fill out the next section as far as possible. The marker line is used as a start line to
		/// position content from. If necessary, room for headers is given before laying out the section
		/// content. However, headers are always added to an index after the section content.
		/// </summary>
		/// <param name="leadingEdge"> Line to fill up to. Content will not be wholly beyond this line. </param>
		/// <param name="markerLine">  Start line to begin placing content at. </param>
		/// <param name="state">       Layout state. </param>
		/// <returns> Line to which content has been filled. </returns>
		private int FillNextSectionToEnd(int leadingEdge, int markerLine, LayoutState state)
		{
			if (markerLine >= leadingEdge)
			{
				return markerLine;
			}

			View last = AnchorAtEnd;
			int anchorPosition = GetPosition(last) + 1;

			if (anchorPosition >= state.recyclerState.ItemCount)
			{
				return markerLine;
			}

			LayoutState.View header = state.GetView(anchorPosition);
			LayoutParams headerParams = header.LayoutParams;
			SectionData sd;
			if (headerParams.isHeader)
			{
				MeasureHeader(header.view);
				sd = GetSectionDataInternal(anchorPosition, header.view);
				markerLine = LayoutHeaderTowardsEnd(header.view, markerLine, sd, state);
				anchorPosition += 1;
			}
			else
			{
				state.CacheView(anchorPosition, header.view);
				sd = GetSectionDataInternal(anchorPosition, header.view);
			}

			if (anchorPosition < state.recyclerState.ItemCount)
			{
				SectionLayoutManager slm = GetSlm(sd);
				markerLine = slm.FillToEnd(leadingEdge, markerLine, anchorPosition, sd, state);
				UpdateSectionDataAfterFillToEnd(sd, state);
			}

			if (sd.hasHeader)
			{
				AddView(header.view);
				if (header.wasCached)
				{
					state.DecacheView(sd.firstPosition);
				}
				markerLine = System.Math.Max(GetDecoratedBottom(header.view), markerLine);
			}

			return FillNextSectionToEnd(leadingEdge, markerLine, state);
		}

		/// <summary>
		/// Fill the next section towards the start edge.
		/// </summary>
		/// <param name="leadingEdge"> Line to fill up to. Content will not be wholly beyond this line. </param>
		/// <param name="markerLine">  Start line to begin placing content at. </param>
		/// <param name="state">       Layout state. </param>
		/// <returns> Line content was filled up to. </returns>
		private int fillNextSectionToStart(int leadingEdge, int markerLine, LayoutState state)
		{
			if (markerLine < leadingEdge)
			{
				return markerLine;
			}

			View preAnchor = AnchorAtStart;
			LayoutParams preAnchorParams = (LayoutParams)preAnchor.LayoutParameters;
			View first = FindAttachedHeaderOrFirstViewForSection(preAnchorParams.FirstPosition, 0, Direction.START);
			int anchorPosition;
			if (first != null)
			{
				anchorPosition = GetPosition(first) - 1;
			}
			else
			{
				anchorPosition = GetPosition(preAnchor) - 1;
			}

			if (anchorPosition < 0)
			{
				return markerLine;
			}

			LayoutState.View anchor = state.GetView(anchorPosition);
			LayoutParams anchorParams = anchor.LayoutParams;

			// Now we are in our intended section to fill.
			int sfp = anchorParams.FirstPosition;

			// Setup section data.
			View header = GetHeaderOrFirstViewForSection(sfp, Direction.START, state);
			LayoutParams headerParams = (LayoutParams)header.LayoutParameters;
			if (headerParams.isHeader)
			{
				MeasureHeader(header);
			}
			SectionData sd = GetSectionDataInternal(sfp, header);
			sd.lastContentPosition = anchorPosition;

			// Fill out section.
			SectionLayoutManager slm = GetSlm(sd);
			int sectionBottom = markerLine;
			if (anchorPosition >= 0)
			{
				markerLine = slm.FillToStart(leadingEdge, markerLine, anchorPosition, sd, state);
			}

			// Lay out and attach header.
			if (sd.hasHeader)
			{
				int headerOffset = 0;
				if (!sd.headerParams.HeaderInline || sd.headerParams.HeaderOverlay)
				{
					View firstVisibleView = slm.FindFirstVisibleView(sd.firstPosition, true);
					if (firstVisibleView == null)
					{
						headerOffset = 0;
					}
					else
					{
						headerOffset = slm.ComputeHeaderOffset(GetPosition(firstVisibleView), sd, state);
					}
				}
				markerLine = LayoutHeaderTowardsStart(header, leadingEdge, markerLine, headerOffset, sectionBottom, sd, state);

				AttachHeaderForStart(header, leadingEdge, sd, state);
			}

			return fillNextSectionToStart(leadingEdge, markerLine, state);
		}

		/// <summary>
		/// Fill the space between the last content item and the leadingEdge.
		/// </summary>
		/// <param name="leadingEdge"> Line to fill up to. Content will not be wholly beyond this line. </param> </param>
		/// <param name="state">       Layout state.  <returns> Line to which content has been filled. If the line
		///                    is before the leading edge then the end of the data set has been reached. </returns>
		private int FillToEnd(int leadingEdge, LayoutState state)
		{
			View anchor = AnchorAtEnd;

			LayoutParams anchorParams = (LayoutParams) anchor.LayoutParameters;
			int sfp = anchorParams.FirstPosition;
			View first = GetHeaderOrFirstViewForSection(sfp, Direction.END, state);
			SectionData sd = GetSectionDataInternal(sfp, first);

			SectionLayoutManager slm = GetSlm(sd);
			int markerLine = slm.FinishFillToEnd(leadingEdge, anchor, sd, state);
			UpdateSectionDataAfterFillToEnd(sd, state);

			View header = FindAttachedHeaderForSectionFromEnd(sd.firstPosition);
			markerLine = UpdateHeaderForEnd(header, markerLine);

			if (markerLine <= leadingEdge)
			{
				markerLine = FillNextSectionToEnd(leadingEdge, markerLine, state);
			}

			return markerLine;
		}

		/// <summary>
		/// Fill towards the start edge.
		/// </summary>
		/// <param name="leadingEdge"> Line to fill up to. Content will not be wholly beyond this line. </param>
		/// <param name="state">       Layout state. </param>
		/// <returns> Line content was filled up to. </returns>
		private int FillToStart(int leadingEdge, LayoutState state)
		{
			View anchor = AnchorAtStart;

			LayoutParams anchorParams = (LayoutParams) anchor.LayoutParameters;
			int sfp = anchorParams.FirstPosition;
			View first = GetHeaderOrFirstViewForSection(sfp, Direction.START, state);
			SectionData sd = GetSectionDataInternal(sfp, first);

			SectionLayoutManager slm = GetSlm(sd);

			int markerLine;
			int anchorPosition = GetPosition(anchor);
			if (anchorPosition == sd.firstPosition)
			{
				markerLine = GetDecoratedBottom(anchor);
			}
			else
			{
				if (anchorPosition - 1 == sd.firstPosition && sd.hasHeader)
				{
					// Already at first content position, so no more to do.
					markerLine = GetDecoratedTop(anchor);
				}
				else
				{
					markerLine = slm.FinishFillToStart(leadingEdge, anchor, sd, state);
				}
			}

			markerLine = UpdateHeaderForStart(first, leadingEdge, markerLine, sd, state);

			if (markerLine >= leadingEdge)
			{
				markerLine = fillNextSectionToStart(leadingEdge, markerLine, state);
			}

			return markerLine;
		}

		/// <summary>
		/// Fill up to a line in a given direction.
		/// </summary>
		/// <param name="leadingEdge"> Line to fill up to. Content will not be wholly beyond this line. </param>
		/// <param name="direction">   Direction fill will be taken towards. </param>
		/// <param name="layoutState"> Layout state. </param>
		/// <returns> Line to which content has been filled. If the line is before the leading edge then
		/// the end of the data set has been reached. </returns>
		private int FillUntil(int leadingEdge, Direction direction, LayoutState layoutState)
		{
			if (direction == Direction.START)
			{
				return FillToStart(leadingEdge, layoutState);
			}
			else
			{
				return FillToEnd(leadingEdge, layoutState);
			}
		}

		/// <summary>
		/// Find a view that is the header for the specified section. Looks in direction specified from
		/// opposite end.
		/// </summary>
		/// <param name="sfp">  Section to look for header inside of. Search is expected to start inside the
		///             section so it must be at the matching end specified by the direction. </param>
		/// <param name="from"> Edge to start looking from. </param>
		/// <returns> Null if no header found, otherwise the header view. </returns>
		private View FindAttachedHeaderForSection(int sfp, Direction from)
		{
			if (from == Direction.END)
			{
				return FindAttachedHeaderForSectionFromEnd(sfp);
			}
			else
			{
				return FindAttachedHeaderForSectionFromStart(0, ChildCount - 1, sfp);
			}
		}

		/// <summary>
		/// The header is almost guaranteed to be at the end so just use look there.
		/// </summary>
		/// <param name="sfp"> Section identifier. </param>
		/// <returns> Header, or null if not found. </returns>
		private View FindAttachedHeaderForSectionFromEnd(int sfp)
		{
			for (int i = ChildCount - 1; i >= 0; i--)
			{
				View child =GetChildAt(i);
				LayoutParams lp = (LayoutParams)child.LayoutParameters;
				if (lp.FirstPosition != sfp)
				{
					break;
				}
				else if (lp.isHeader)
				{
					return child;
				}
			}

			return null;
		}

		/// <summary>
		/// The header is most likely at the end of the section but we don't know where that is so use
		/// binary search.
		/// </summary>
		/// <param name="min"> min </param>
		/// <param name="max"> max </param>
		/// <param name="sfp"> Section identifier. </param>
		/// <returns> Header, or null if not found. </returns>
		private View FindAttachedHeaderForSectionFromStart(int min, int max, int sfp)
		{
			if (max < min)
			{
				return null;
			}

			int mid = min + (max - min) / 2;

			View candidate =GetChildAt(mid);
			LayoutParams lp = (LayoutParams)candidate.LayoutParameters;
			if (lp.FirstPosition != sfp)
			{
				return FindAttachedHeaderForSectionFromStart(min, mid - 1, sfp);
			}

			if (lp.isHeader)
			{
				return candidate;
			}

			return FindAttachedHeaderForSectionFromStart(mid + 1, max, sfp);
		}

		/// <summary>
		/// Find header or, if it cannot be found, the first view for a section.
		/// </summary>
		/// <param name="sfp">        Section to look for header inside of. Search is expected to start inside
		///                   the section so it must be at the matching end specified by the direction. </param>
		/// <param name="startIndex"> Index to start looking from. </param>
		/// <param name="from">       Edge to start looking from. </param>
		/// <returns> Null if no header or first item found, otherwise the found view. </returns>
		private View FindAttachedHeaderOrFirstViewForSection(int sfp, int startIndex, Direction from)
		{
			int childIndex = startIndex;
			int step = from == Direction.START ? 1 : -1;
			for (; 0 <= childIndex && childIndex < ChildCount; childIndex += step)
			{
				View child =GetChildAt(childIndex);

				if (GetPosition(child) == sfp)
				{
					return child;
				}
				LayoutParams lp = (LayoutParams)child.LayoutParameters;
				if (lp.FirstPosition != sfp)
				{
					break;
				}
			}

			return null;
		}

		private int FindLastIndexForSection(int sfp)
		{
			return BinarySearchForLastPosition(0, ChildCount - 1, sfp);
		}

		private void FixOverscroll(int bottomLine, LayoutState state)
		{
			if (!IsOverscrolled(state))
			{
				return;
			}

			// Shunt content down to the bottom of the screen.
			int delta = Height - PaddingBottom - bottomLine;
			OffsetChildrenVertical(delta);

			// Fill back towards the top.
			int topLine = FillToStart(0, state);

			if (topLine > PaddingTop)
			{
				// Not enough content to fill all the way back up so we shunt it back up.
				OffsetChildrenVertical(PaddingTop - topLine);
			}
		}

		/// <summary>
		/// Find an anchor to fill to end from.
		/// </summary>
		/// <returns> Non-header view closest to the end edge. </returns>
		private View AnchorAtEnd
		{
			get
			{
				if (ChildCount == 1)
				{
					return GetChildAt(0);
				}
				View candidate =GetChildAt(ChildCount - 1);
				LayoutParams candidateParams = (LayoutParams)candidate.LayoutParameters;
				if (candidateParams.isHeader)
				{
					// Try one above.
					View check =GetChildAt(ChildCount - 2);
					LayoutParams checkParams = (LayoutParams)check.LayoutParameters;
					if (checkParams.FirstPosition == candidateParams.FirstPosition)
					{
						candidate = check;
					}
				}
				return candidate;
			}
		}

		/// <summary>
		/// Get the first view in the section that intersects the start edge. Only returns the header if
		/// it is the last one displayed.
		/// </summary>
		/// <returns> View in section at start edge. </returns>
		private View AnchorAtStart
		{
			get
			{
				View child =GetChildAt(0);
				LayoutParams lp = (LayoutParams)child.LayoutParameters;
				int sfp = lp.FirstPosition;
    
				if (!lp.isHeader)
				{
					return child;
				}
    
				int i = 1;
				if (i < ChildCount)
				{
					View candidate =GetChildAt(i);
					LayoutParams candidateParams = (LayoutParams)candidate.LayoutParameters;
					if (candidateParams.FirstPosition == sfp)
					{
						return candidate;
					}
				}
    
				return child;
			}
		}

		/// <summary>
		/// Find the first view in the hierarchy that can act as an anchor.
		/// </summary>
		/// <returns> The anchor view, or null if no view is a valid anchor. </returns>
		private View AnchorChild
		{
			get
			{
				if (ChildCount == 0)
				{
					return null;
				}
    
				View child =GetChildAt(0);
				LayoutParams lp = (LayoutParams)child.LayoutParameters;
				int sfp = lp.FirstPosition;
    
				View first = FindAttachedHeaderOrFirstViewForSection(sfp, 0, Direction.START);
				if (first == null)
				{
					return child;
				}
    
				LayoutParams firstParams = (LayoutParams)first.LayoutParameters;
				if (!firstParams.isHeader)
				{
					return child;
				}
    
				if (firstParams.HeaderInline && !firstParams.HeaderOverlay)
				{
					if (GetDecoratedBottom(first) <= GetDecoratedTop(child))
					{
						return first;
					}
					else
					{
						return child;
					}
				}
    
				if (GetDecoratedTop(child) < GetDecoratedTop(first))
				{
					return child;
				}
    
				if (sfp + 1 == GetPosition(child))
				{
					return first;
				}
    
				return child;
    
			}
		}

		/// <summary>
		/// Work out the borderline from the given anchor view and the intended direction to fill the
		/// view hierarchy.
		/// </summary>
		/// <param name="anchorView"> Anchor view to determine borderline from. </param>
		/// <param name="direction">  Direction fill will be taken towards. </param>
		/// <returns> Borderline. </returns>
		private int GetBorderLine(View anchorView, Direction direction)
		{
			int borderline;
			if (anchorView == null)
			{
				if (direction == Direction.START)
				{
					borderline = PaddingBottom;
				}
				else
				{
					borderline = PaddingTop;
				}
			}
			else if (direction == Direction.START)
			{
				borderline = GetDecoratedBottom(anchorView);
			}
			else
			{
				borderline = GetDecoratedTop(anchorView);
			}
			return borderline;
		}

		private SectionData GetCachedSectionData(View child)
		{
			LayoutParams lp = (LayoutParams)child.LayoutParameters;
			return mSectionDataCache.Get(lp.FirstPosition);
		}

		private int GetDirectionToPosition(int tarGetPosition)
		{
			LayoutParams lp = (LayoutParams)GetChildAt(0).LayoutParameters;
			View startSectionFirstView = GetSlm(lp).FindFirstVisibleView(lp.FirstPosition, true);
			return tarGetPosition < GetPosition(startSectionFirstView) ? - 1 : 1;
		}

		private float GetFractionOfContentAbove(RecyclerView.State state, bool ignorePosition)
		{
			float fractionOffscreen = 0;

			View child = GetChildAt(0);

			int anchorPosition = GetPosition(child);
			int numBeforeAnchor = 0;

			float top = GetDecoratedTop(child);
			float bottom = GetDecoratedBottom(child);
			if (bottom < 0)
			{
				fractionOffscreen = 1;
			}
			else if (0 <= top)
			{
				fractionOffscreen = 0;
			}
			else
			{
				float height = GetDecoratedMeasuredHeight(child);
				fractionOffscreen = -top / height;
			}

			SectionData sd = GetCachedSectionData(child);
			if (sd.headerParams.isHeader && sd.headerParams.HeaderInline)
			{
				// Header must not be stickied as it is not attached after section items.
				return fractionOffscreen;
			}

			// Run through all views in the section and add up values offscreen.
			int firstPosition = -1;
			SparseArray<bool> positionsOffscreen = new SparseArray<bool>();
			for (int i = 1; i < ChildCount; i++)
			{
				child =GetChildAt(i);
				LayoutParams lp = (LayoutParams)child.LayoutParameters;
				if (lp.FirstPosition != sd.firstPosition)
				{
					break;
				}

				int position = GetPosition(child);
				if (!ignorePosition && position < anchorPosition)
				{
					numBeforeAnchor += 1;
				}

				top = GetDecoratedTop(child);
				bottom = GetDecoratedBottom(child);
				if (bottom < 0)
				{
					fractionOffscreen += 1;
				}
				else if (0 <= top)
				{
					continue;
				}
				else
				{
					float height = GetDecoratedMeasuredHeight(child);
					fractionOffscreen += -top / height;
				}

				if (!lp.isHeader)
				{
					if (firstPosition == -1)
					{
						firstPosition = position;
					}
					positionsOffscreen.Put(position, true);
				}
			}

			return fractionOffscreen - numBeforeAnchor - GetSlm(sd).HowManyMissingAbove(firstPosition, positionsOffscreen);
		}

		private float GetFractionOfContentBelow(RecyclerView.State state, bool ignorePosition)
		{
			float parentHeight = Height;
			View child =GetChildAt(ChildCount - 1);

			int anchorPosition = GetPosition(child);
			int countAfter = 0;

			SectionData sd = GetCachedSectionData(child);

			float fractionOffscreen = 0;
			int lastPosition = -1;
			SparseArray<bool> positionsOffscreen = new SparseArray<bool>();
			// Run through all views in the section and add up values offscreen.
			for (int i = 1; i <= ChildCount; i++)
			{
				child =GetChildAt(ChildCount - i);
				LayoutParams lp = (LayoutParams)child.LayoutParameters;
				if (lp.FirstPosition != sd.firstPosition)
				{
					break;
				}

				int position = GetPosition(child);
				if (!lp.isHeader && !ignorePosition && position > anchorPosition)
				{
					countAfter += 1;
				}

				float bottom = GetDecoratedBottom(child);
				float top = GetDecoratedTop(child);
				if (bottom <= parentHeight)
				{
					continue;
				}
				else if (parentHeight < top)
				{
					fractionOffscreen += 1;
				}
				else
				{
					float height = GetDecoratedMeasuredHeight(child);
					fractionOffscreen += (bottom - parentHeight) / height;
				}

				if (!lp.isHeader)
				{
					if (lastPosition == -1)
					{
						lastPosition = position;
					}
					positionsOffscreen.Put(position, true);
				}
			}

			return fractionOffscreen - countAfter - GetSlm(sd).HowManyMissingBelow(lastPosition, positionsOffscreen);
		}

		private View GetHeaderOrFirstViewForSection(int sfp, Direction direction, LayoutState state)
		{
			View view = FindAttachedHeaderOrFirstViewForSection(sfp, direction == Direction.START ? 0 : ChildCount - 1, direction);
			if (view == null)
			{
				LayoutState.View stateView = state.GetView(sfp);
				view = stateView.view;
				if (stateView.LayoutParams.isHeader)
				{
					MeasureHeader(stateView.view);
				}
				state.CacheView(sfp, view);
			}
			return view;
		}

		private SectionLayoutManager GetSlm(int kind, string key)
		{
			if (kind == SECTION_MANAGER_CUSTOM)
			{
				return mSlms[key];
			}
			else if (kind == SECTION_MANAGER_LINEAR)
			{
				return mLinearSlm;
			}
			else if (kind == SECTION_MANAGER_GRID)
			{
				return mGridSlm;
			}
			else if (kind == SECTION_MANAGER_STAGGERED_GRID)
			{
				throw new NotYetImplementedSlmException(this, kind);
			}
			else
			{
				throw new UnknownSectionLayoutException(this, kind);
			}
		}

		private SectionLayoutManager GetSlm(LayoutParams lp)
		{
			if (lp.sectionManagerKind == SECTION_MANAGER_CUSTOM)
			{
				return mSlms[lp.sectionManager];
			}
			else if (lp.sectionManagerKind == SECTION_MANAGER_LINEAR)
			{
				return mLinearSlm;
			}
			else if (lp.sectionManagerKind == SECTION_MANAGER_GRID)
			{
				return mGridSlm;
			}
			else if (lp.sectionManagerKind == SECTION_MANAGER_STAGGERED_GRID)
			{
				throw new NotYetImplementedSlmException(this, lp.sectionManagerKind);
			}
			else
			{
				throw new UnknownSectionLayoutException(this, lp.sectionManagerKind);
			}
		}

		private SectionLayoutManager GetSlm(SectionData sd)
		{
			SectionLayoutManager slm;
			if (sd.headerParams.sectionManagerKind == SECTION_MANAGER_CUSTOM)
			{
				slm = mSlms[sd.sectionManager];
				if (slm == null)
				{
					throw new UnknownSectionLayoutException(this, sd.sectionManager);
				}
			}
			else if (sd.headerParams.sectionManagerKind == SECTION_MANAGER_LINEAR)
			{
				slm = mLinearSlm;
			}
			else if (sd.headerParams.sectionManagerKind == SECTION_MANAGER_GRID)
			{
				slm = mGridSlm;
			}
			else if (sd.headerParams.sectionManagerKind == SECTION_MANAGER_STAGGERED_GRID)
			{
				throw new NotYetImplementedSlmException(this, sd.headerParams.sectionManagerKind);
			}
			else
			{
				throw new UnknownSectionLayoutException(this, sd.headerParams.sectionManagerKind);
			}

			return slm.Initialize(sd);
		}

		private bool IsOverscrolled(LayoutState state)
		{
			int itemCount = state.recyclerState.ItemCount;

			if (ChildCount == 0)
			{
				return false;
			}

			View lastVisibleView = FindLastCompletelyVisibleItem();
			if (lastVisibleView == null)
			{
				lastVisibleView =GetChildAt(ChildCount - 1);
			}

			bool reachedBottom = GetPosition(lastVisibleView) == itemCount - 1;
			if (!reachedBottom || GetDecoratedBottom(lastVisibleView) >= Height - PaddingBottom)
			{
				return false;
			}

			View firstVisibleView = FindFirstCompletelyVisibleItem();
			if (firstVisibleView == null)
			{
				firstVisibleView =GetChildAt(0);
			}

			bool reachedTop = GetPosition(firstVisibleView) == 0 && GetDecoratedTop(firstVisibleView) == PaddingTop;
			return !reachedTop;
		}

		/// <summary>
		/// Layout views from the top.
		/// </summary>
		/// <param name="anchorPosition"> Position to start laying out from. </param> </param>
		/// <param name="state">          Layout state.  <returns> Line to which content has been filled. If the
		///                       line is before the leading edge then the end of the data set has been </returns>
		private int LayoutChildren(int anchorPosition, int borderLine, LayoutState state)
		{
			int height = Height;

			LayoutState.View anchor = state.GetView(anchorPosition);
			state.CacheView(anchorPosition, anchor.view);

			int sfp = anchor.LayoutParams.FirstPosition;
			LayoutState.View first = state.GetView(sfp);
			MeasureHeader(first.view);
			state.CacheView(sfp, first.view);

			SectionData sd = GetSectionDataInternal(sfp, first.view);

			SectionLayoutManager slm = GetSlm(sd);
			// Layout header
			int markerLine = borderLine;
			int contentPosition = anchorPosition;
			if (sd.hasHeader && anchorPosition == sd.firstPosition)
			{
				markerLine = LayoutHeaderTowardsEnd(first.view, borderLine, sd, state);
				contentPosition += 1;
			}

			// Layout first section to end.
			markerLine = slm.FillToEnd(height, markerLine, contentPosition, sd, state);

			if (sd.hasHeader && anchorPosition != sd.firstPosition)
			{
				int offset = slm.ComputeHeaderOffset(contentPosition, sd, state);
				LayoutHeaderTowardsStart(first.view, 0, borderLine, offset, markerLine, sd, state);
			}
			else
			{
				markerLine = System.Math.Max(markerLine, GetDecoratedBottom(first.view));
			}

			if (sd.hasHeader && GetDecoratedBottom(first.view) > 0)
			{
				AddView(first.view);
				state.DecacheView(sd.firstPosition);
			}

			// Layout the rest.
			markerLine = FillNextSectionToEnd(height, markerLine, state);

			return markerLine;
		}

		/// <summary>
		/// Layout header for fill to end.
		/// </summary>
		/// <param name="header">     Header to be laid out. </param>
		/// <param name="markerLine"> Start of section. </param>
		/// <param name="sd">         Section data. </param>
		/// <param name="state">      Layout state. </param>
		/// <returns> Line at which to start filling out the section's content. </returns>
		private int LayoutHeaderTowardsEnd(View header, int markerLine, SectionData sd, LayoutState state)
		{
			Rect r = setHeaderRectSides(mRect, sd, state);

			r.Top = markerLine;
			r.Bottom = r.Top + sd.headerHeight;

			if (sd.headerParams.HeaderInline && !sd.headerParams.HeaderOverlay)
			{
				markerLine = r.Bottom;
			}

			if (sd.headerParams.HeaderSticky && r.Top < 0)
			{
				r.Top = 0;
				r.Bottom = r.Top + sd.headerHeight;
			}

			LayoutDecorated(header, r.Left, r.Top, r.Right, r.Bottom);

			return markerLine;
		}

		/// <summary>
		/// Layout header towards start edge.
		/// </summary>
		/// <param name="header">      Header to be laid out. </param>
		/// <param name="leadingEdge"> Leading edge to align sticky headers against. </param>
		/// <param name="markerLine">  Bottom edge of the header. </param>
		/// <param name="sd">          Section data. </param>
		/// <param name="state">       Layout state. </param>
		/// <returns> Top of the section including the header. </returns>
		private int LayoutHeaderTowardsStart(View header, int leadingEdge, int markerLine, int offset, int sectionBottom, SectionData sd, LayoutState state)
		{
			Rect r = setHeaderRectSides(mRect, sd, state);

			if (sd.headerParams.HeaderInline && !sd.headerParams.HeaderOverlay)
			{
				r.Bottom = markerLine;
				r.Top = r.Bottom - sd.headerHeight;
			}
			else if (offset <= 0)
			{
				r.Top = markerLine + offset;
				r.Bottom = r.Top + sd.headerHeight;
			}
			else
			{
				r.Bottom = leadingEdge;
				r.Top = r.Bottom - sd.headerHeight;
			}

			if (sd.headerParams.HeaderSticky && r.Top < leadingEdge && sd.firstPosition != state.recyclerState.TargetScrollPosition)
			{
				r.Top = leadingEdge;
				r.Bottom = r.Top + sd.headerHeight;
				if (sd.headerParams.HeaderInline && !sd.headerParams.HeaderOverlay)
				{
					markerLine -= sd.headerHeight;
				}
			}

			if (r.Bottom > sectionBottom)
			{
				r.Bottom = sectionBottom;
				r.Top = r.Bottom - sd.headerHeight;
			}

			LayoutDecorated(header, r.Left, r.Top, r.Right, r.Bottom);

			return System.Math.Min(r.Top, markerLine);
		}

		private Rect setHeaderRectSides(Rect r, SectionData sd, LayoutState state)
		{
			int paddingLeft = PaddingLeft;
			int paddingRight = PaddingRight;

			if (sd.headerParams.HeaderEndAligned)
			{
				// Position header from end edge.
				if (!sd.headerParams.HeaderOverlay && !sd.headerParams.headerEndMarginIsAuto && sd.marginEnd > 0)
				{
					// Position inside end margin.
					if (state.isLTR)
					{
						r.Left = Width - sd.marginEnd - paddingRight;
						r.Right = r.Left + sd.headerWidth;
					}
					else
					{
						r.Right = sd.marginEnd + paddingLeft;
						r.Left = r.Right - sd.headerWidth;
					}
				}
				else if (state.isLTR)
				{
					r.Right = Width - paddingRight;
					r.Left = r.Right - sd.headerWidth;
				}
				else
				{
					r.Left = paddingLeft;
					r.Right = r.Left + sd.headerWidth;
				}
			}
			else if (sd.headerParams.HeaderStartAligned)
			{
				// Position header from start edge.
				if (!sd.headerParams.HeaderOverlay && !sd.headerParams.headerStartMarginIsAuto && sd.marginStart > 0)
				{
					// Position inside start margin.
					if (state.isLTR)
					{
						r.Right = sd.marginStart + paddingLeft;
						r.Left = r.Right - sd.headerWidth;
					}
					else
					{
						r.Left = Width - sd.marginStart - paddingRight;
						r.Right = r.Left + sd.headerWidth;
					}
				}
				else if (state.isLTR)
				{
					r.Left = paddingLeft;
					r.Right = r.Left + sd.headerWidth;
				}
				else
				{
					r.Right = Width - paddingRight;
					r.Left = r.Right - sd.headerWidth;
				}
			}
			else
			{
				// Header is not aligned to a directed edge and assumed to fill the width available.
				r.Left = paddingLeft;
				r.Right = r.Left + sd.headerWidth;
			}

			return r;
		}

		/// <summary>
		/// Trim content wholly beyond the end edge.
		/// </summary>
		/// <param name="state"> Layout state. </param>
		private void trimEnd(LayoutState state)
		{
			int height = Height;
			for (int i = ChildCount - 1; i >= 0; i--)
			{
				View child =GetChildAt(i);
				if (GetDecoratedTop(child) >= height)
				{
					RemoveAndRecycleView(child, state.recycler);
				}
				else
				{
					if (!((LayoutParams)child.LayoutParameters).isHeader)
					{
						break;
					}
				}
			}
		}

		/// <summary>
		/// Trim content wholly beyond the start edge.
		/// </summary>
		/// <param name="state"> Layout state. </param>
		private void trimStart(LayoutState state)
		{
			// Find the first view visible on the screen.
			View anchor = null;
			int anchorIndex = 0;
			for (int i = 0; i < ChildCount; i++)
			{
				View look =GetChildAt(i);
				if (GetDecoratedBottom(look) > 0)
				{
					anchor = look;
					anchorIndex = i;
					break;
				}
			}

			if (anchor == null)
			{
				DetachAndScrapAttachedViews(state.recycler);
				return;
			}

			LayoutParams anchorParams = (LayoutParams)anchor.LayoutParameters;
			if (anchorParams.isHeader)
			{
				for (int i = anchorIndex - 1; i >= 0; i--)
				{
					View look =GetChildAt(i);
					LayoutParams lookParams = (LayoutParams)look.LayoutParameters;
					if (lookParams.FirstPosition == anchorParams.FirstPosition)
					{
						anchor = look;
						anchorParams = lookParams;
						anchorIndex = i;
						break;
					}
				}
			}

			for (int i = 0; i < anchorIndex; i++)
			{
				RemoveAndRecycleViewAt(0, state.recycler);
			}

			int sfp = anchorParams.FirstPosition;

			View header = FindAttachedHeaderForSection(sfp, Direction.START);
			if (header != null)
			{
				if (GetDecoratedTop(header) < 0)
				{
					UpdateHeaderForTrimFromStart(header);
				}

				if (GetDecoratedBottom(header) < 0)
				{
					RemoveAndRecycleView(header, state.recycler);
				}
			}
		}

		/// <summary>
		/// Trim all content wholly beyond the direction edge. If the direction is START, then update the
		/// header of the section intersecting the top edge.
		/// </summary>
		/// <param name="direction"> Direction of edge to trim against. </param>
		/// <param name="state">     Layout state. </param>
		private void TrimTail(Direction direction, LayoutState state)
		{
			if (direction == Direction.START)
			{
				trimStart(state);
			}
			else
			{
				trimEnd(state);
			}
		}

		/// <summary>
		/// Find the header for this section, if any, and move it to be attached after the section's
		/// content items. Updates the line showing the end of the section.
		/// </summary>
		/// <param name="header">     Header to update. </param>
		/// <param name="markerLine"> End of the section as given by the SLM. </param>
		/// <returns> The end of the section including the header. </returns>
		private int UpdateHeaderForEnd(View header, int markerLine)
		{
			if (header == null)
			{
				return markerLine;
			}

			// Just keep headers at the end.
			DetachView(header);
			AttachView(header, -1);

			return System.Math.Max(markerLine, GetDecoratedBottom(header));
		}

		/// <summary>
		/// Update header for an already existing section when filling towards the start.
		/// </summary>
		/// <param name="header">      Header to update. </param>
		/// <param name="leadingEdge"> Leading edge to align sticky headers against. </param>
		/// <param name="markerLine">  Start of section. </param>
		/// <param name="sd">          Section data. </param>
		/// <param name="state">       Layout state. </param>
		/// <returns> Updated line for the start of the section content including the header. </returns>
		private int UpdateHeaderForStart(View header, int leadingEdge, int markerLine, SectionData sd, LayoutState state)
		{
			if (!sd.hasHeader)
			{
				return markerLine;
			}

			SectionLayoutManager slm = GetSlm(sd);
			int sli = FindLastIndexForSection(sd.firstPosition);
			int sectionBottom = Height;
			for (int i = sli == -1 ? 0 : sli; i < ChildCount; i++)
			{
				View view =GetChildAt(i);
				LayoutParams lp = (LayoutParams)view.LayoutParameters;
				if (lp.FirstPosition != sd.firstPosition)
				{
					View first = FindAttachedHeaderOrFirstViewForSection(lp.FirstPosition, i, Direction.START);
					if (first == null)
					{
						sectionBottom = GetDecoratedTop(view);
					}
					else
					{
						sectionBottom = GetDecoratedTop(first);
					}
					break;
				}
			}
			int offset = 0;
			if (!sd.headerParams.HeaderInline || sd.headerParams.HeaderOverlay)
			{
				View firstVisibleView = slm.FindFirstVisibleView(sd.firstPosition, true);
				if (firstVisibleView == null)
				{
					offset = 0;
				}
				else
				{
					offset = slm.ComputeHeaderOffset(GetPosition(firstVisibleView), sd, state);
				}
			}

			markerLine = LayoutHeaderTowardsStart(header, leadingEdge, markerLine, offset, sectionBottom, sd, state);

			AttachHeaderForStart(header, leadingEdge, sd, state);

			return markerLine;
		}

		private void UpdateHeaderForTrimFromStart(View header)
		{
			LayoutParams lp = (LayoutParams)header.LayoutParameters;
			if (!lp.HeaderSticky)
			{
				return;
			}

			int sfp = lp.FirstPosition;
			int slp = FindLastIndexForSection(sfp);
			if (slp == -1)
			{
				return;
			}

			SectionLayoutManager slm = GetSlm(lp);
			int sectionBottom = slm.GetLowestEdge(sfp, slp, Height);
			int sectionTop = slm.GetHighestEdge(sfp, 0, 0);

			int height = GetDecoratedMeasuredHeight(header);
			if ((lp.HeaderInline && !lp.HeaderOverlay) || (sectionBottom - sectionTop) > height)
			{
				int left = GetDecoratedLeft(header);
				int right = GetDecoratedRight(header);

				int top = 0;
				int bottom = top + height;

				if (bottom > sectionBottom)
				{
					bottom = sectionBottom;
					top = bottom - height;
				}

				LayoutDecorated(header, left, top, right, bottom);
			}
		}

		private void UpdateSectionDataAfterFillToEnd(SectionData sd, LayoutState state)
		{
			// Check to see if we reached the end of the section so we can update the section data with
			// the last section position.
			if (sd.lastContentPosition != -1)
			{
				return;
			}

			View finishFillEndView = AnchorAtEnd;
			int endPosition = GetPosition(finishFillEndView);
			if (endPosition == state.recyclerState.ItemCount - 1)
			{
				sd.lastContentPosition = endPosition;
			}
			else
			{
				int nextPosition = endPosition + 1;
				LayoutState.View next = state.GetView(nextPosition);
				state.CacheView(nextPosition, next.view);

				if (next.LayoutParams.FirstPosition != sd.firstPosition)
				{
					sd.lastContentPosition = endPosition;
				}
			}
		}

		public enum Direction
		{
			START,
			END,
			NONE
		}

		public class Builder
		{
			public Context context;
			public Dictionary<string, SectionLayoutManager> slms = new Dictionary<string, SectionLayoutManager>();

			public Builder(Context context)
			{
				this.context = context;
			}

			public Builder AddSlm(string key, SectionLayoutManager slm)
			{
				slms[key] = slm;
				return this;
			}

			public LayoutManager Build()
			{
				return new LayoutManager(this);
			}
		}

		public class LayoutParams : RecyclerView.LayoutParams
		{
			public const int HEADER_INLINE = 0x01;
			public const int HEADER_ALIGN_START = 0x02;
			public const int HEADER_ALIGN_END = 0x04;
			public const int HEADER_OVERLAY = 0x08;
			public const int HEADER_STICKY = 0x10;

			public const bool DEFAULT_IS_HEADER = false;
            public static readonly int NO_FIRST_POSITION = -0x01;
            public static readonly int DEFAULT_HEADER_MARGIN = -0x01;
            public static readonly int DEFAULT_HEADER_DISPLAY = HEADER_INLINE | HEADER_STICKY;

			public bool isHeader;

			public int headerDisplay;
			public int headerMarginEnd;
			public int headerMarginStart;

			public bool headerStartMarginIsAuto;
			public bool headerEndMarginIsAuto;

            public string sectionManager;

            public int sectionManagerKind;
            public int mFirstPosition;

			public LayoutParams(int width, int height) : base(width, height)
			{

				isHeader = DEFAULT_IS_HEADER;
				sectionManagerKind = SECTION_MANAGER_LINEAR;
			}

			public LayoutParams(Context c, IAttributeSet attrs) : base(c, attrs)
			{

				TypedArray a = c.ObtainStyledAttributes(attrs, Resource.Styleable.superslim_LayoutManager);
				isHeader = a.GetBoolean(Resource.Styleable.superslim_LayoutManager_slm_isHeader, false);
				headerDisplay = a.GetInt(Resource.Styleable.superslim_LayoutManager_slm_headerDisplay, DEFAULT_HEADER_DISPLAY);
				mFirstPosition = a.GetInt(Resource.Styleable.superslim_LayoutManager_slm_section_firstPosition, NO_FIRST_POSITION);

				// Header margin types can be dimension or integer (enum).
				if (Build.VERSION.SdkInt < Build.VERSION_CODES.Lollipop)
				{
                    TypedValue value = new TypedValue();
					a.GetValue(Resource.Styleable.superslim_LayoutManager_slm_section_headerMarginStart, value);
                    LoadHeaderStartMargin(a, (int)value == (int)DataType.Dimension);

					a.GetValue(Resource.Styleable.superslim_LayoutManager_slm_section_headerMarginEnd, value);
                    LoadHeaderEndMargin(a, (int)value == (int)DataType.Dimension);

					a.GetValue(Resource.Styleable.superslim_LayoutManager_slm_section_layoutManager, value);
                    LoadSlm(a, (int)value == (int)DataType.String);
				}
				else
				{
					bool isDimension;
                    isDimension = a.GetType(Resource.Styleable.superslim_LayoutManager_slm_section_headerMarginStart) == (int)DataType.Dimension;
					LoadHeaderStartMargin(a, isDimension);

                    isDimension = a.GetType(Resource.Styleable.superslim_LayoutManager_slm_section_headerMarginEnd) == (int)DataType.Dimension;
					LoadHeaderEndMargin(a, isDimension);

                    bool isString = a.GetType(Resource.Styleable.superslim_LayoutManager_slm_section_layoutManager) == (int)DataType.Dimension;
					LoadSlm(a, isString);
				}

				a.Recycle();
			}

			public LayoutParams(ViewGroup.MarginLayoutParams other) 
                : base(other)
			{
                Initialize(other);
			}

			public LayoutParams(ViewGroup.LayoutParams other)
                : base(other)
			{
				Initialize(other);
			}

			public bool AreHeaderFlagsSet(int flags)
			{
				return (headerDisplay & flags) == flags;
			}

			/// <summary>
			/// Set the first position for the section to which this param's item belongs.
			/// </summary>
			/// <param name="firstPosition"> First position of section for this param's item. Must be {@literal
			///                      <=} 0 or an InvalidFirstPositionException runtime exception will be
			///                      thrown. </param>
			public int FirstPosition
			{
				set
				{
					if (value < 0)
					{
						throw new InvalidFirstPositionException(this);
					}
					mFirstPosition = value;
				}
				get
				{
					if (mFirstPosition == NO_FIRST_POSITION)
					{
						throw new MissingFirstPositionException(this);
					}
					return mFirstPosition;
				}
			}


			public bool HeaderEndAligned
			{
				get
				{
					return (headerDisplay & HEADER_ALIGN_END) != 0;
				}
			}

			public bool HeaderInline
			{
				get
				{
					return (headerDisplay & HEADER_INLINE) != 0;
				}
			}

			public bool HeaderOverlay
			{
				get
				{
					return (headerDisplay & HEADER_OVERLAY) != 0;
				}
			}

			public bool HeaderStartAligned
			{
				get
				{
					return (headerDisplay & HEADER_ALIGN_START) != 0;
				}
			}

			public bool HeaderSticky
			{
				get
				{
					return (headerDisplay & HEADER_STICKY) != 0;
				}
			}

			/// <summary>
			/// Set the layout manager for this section to a custom implementation. This custom SLM must
			/// be registered via <seealso cref="#addSlm(String, SectionLayoutManager)"/>.
			/// </summary>
			/// <param name="key"> Identifier for a registered custom SLM to layout this section out with. </param>
			public void SetSlm(string key)
			{
				sectionManagerKind = SECTION_MANAGER_CUSTOM;
				sectionManager = key;
			}

			/// <summary>
			/// Set the layout manager for this section to one of the default implementations.
			/// </summary>
			/// <param name="id"> Kind of SLM to use. </param>
			public void SetSlm(int id)
			{
			    sectionManagerKind = id;
			}

			private void Initialize(ViewGroup.LayoutParams other)
			{
				if (other is LayoutParams)
				{
					LayoutParams lp = (LayoutParams) other;
					isHeader = lp.isHeader;
					headerDisplay = lp.headerDisplay;
					mFirstPosition = lp.mFirstPosition;
					sectionManager = lp.sectionManager;
					sectionManagerKind = lp.sectionManagerKind;
					headerMarginEnd = lp.headerMarginEnd;
					headerMarginStart = lp.headerMarginStart;
					headerEndMarginIsAuto = lp.headerEndMarginIsAuto;
					headerStartMarginIsAuto = lp.headerStartMarginIsAuto;
				}
				else
				{
					isHeader = DEFAULT_IS_HEADER;
					headerDisplay = DEFAULT_HEADER_DISPLAY;
					headerMarginEnd = DEFAULT_HEADER_MARGIN;
					headerMarginStart = DEFAULT_HEADER_MARGIN;
					headerStartMarginIsAuto = true;
					headerEndMarginIsAuto = true;
					sectionManagerKind = SECTION_MANAGER_LINEAR;
				}
			}

			public void LoadHeaderEndMargin(TypedArray a, bool isDimension)
			{
				if (isDimension)
				{
					headerEndMarginIsAuto = false;
					headerMarginEnd = a.GetDimensionPixelSize(Resource.Styleable.superslim_LayoutManager_slm_section_headerMarginEnd, 0);
				}
				else
				{
					headerEndMarginIsAuto = true;
				}
			}

			public void LoadHeaderStartMargin(TypedArray a, bool isDimension)
			{
				if (isDimension)
				{
					headerStartMarginIsAuto = false;
					headerMarginStart = a.GetDimensionPixelSize(Resource.Styleable.superslim_LayoutManager_slm_section_headerMarginStart, 0);
				}
				else
				{
					headerStartMarginIsAuto = true;
				}
			}

			public void LoadSlm(TypedArray a, bool isString)
			{
				if (isString)
				{
					sectionManager = a.GetString(Resource.Styleable.superslim_LayoutManager_slm_section_layoutManager);
					if (TextUtils.IsEmpty(sectionManager))
					{
						sectionManagerKind = SECTION_MANAGER_LINEAR;
					}
					else
					{
						sectionManagerKind = SECTION_MANAGER_CUSTOM;
					}
				}
				else
				{
					sectionManagerKind = a.GetInt(Resource.Styleable.superslim_LayoutManager_slm_section_layoutManager, SECTION_MANAGER_LINEAR);
				}
			}

			private class MissingFirstPositionException : Java.Lang.Exception
			{
				private readonly LayoutManager.LayoutParams outerInstance;

                public MissingFirstPositionException(LayoutManager.LayoutParams outerInstance)
                    : base("Missing section first position.")
				{
					this.outerInstance = outerInstance;
				}
			}

            private class InvalidFirstPositionException : Java.Lang.Exception
			{
                private readonly LayoutManager.LayoutParams outerInstance;

                public InvalidFirstPositionException(LayoutManager.LayoutParams outerInstance)
                    : base("Invalid section first position given.")
				{
					this.outerInstance = outerInstance;
				}
			}
		}

		public class SavedState : Java.Lang.Object, IParcelable
		{
            [ExportField("CREATOR")]
            static SavedStateCreator InititalizeCreator()
            {
                return new SavedStateCreator();
            }

            private class SavedStateCreator : Java.Lang.Object, IParcelableCreator
			{
                public SavedStateCreator()
				{

				}

                public Java.Lang.Object CreateFromParcel(Parcel i)
				{
					return new SavedState(i);
				}

                public Java.Lang.Object[] NewArray(int size)
				{
					return new SavedState[size];
				}
			}

			public int anchorPosition;
			public int anchorOffset;

			public SavedState()
			{

			}

            public SavedState(Parcel i)
			{
				anchorPosition = i.ReadInt();
				anchorOffset = i.ReadInt();
			}

			public int DescribeContents()
			{
				return 0;
			}

			public void WriteToParcel(Parcel o, ParcelableWriteFlags flags)
			{
				o.WriteInt(anchorPosition);
				o.WriteInt(anchorOffset);
			}
		}

		private class NotYetImplementedSlmException : Java.Lang.Exception
		{
			private readonly LayoutManager outerInstance;


			public NotYetImplementedSlmException(LayoutManager outerInstance, int id) : base("SLM not yet implemented " + id + ".")
			{
				this.outerInstance = outerInstance;
			}
		}

        private class UnknownSectionLayoutException : Java.Lang.Exception
		{
			private readonly LayoutManager outerInstance;


			public UnknownSectionLayoutException(LayoutManager outerInstance, string key) : base("No registered layout for id " + key + ".")
			{
				this.outerInstance = outerInstance;
			}

			public UnknownSectionLayoutException(LayoutManager outerInstance, int id) : base("No built-in layout known by id " + id + ".")
			{
				this.outerInstance = outerInstance;
			}
		}
	}
}