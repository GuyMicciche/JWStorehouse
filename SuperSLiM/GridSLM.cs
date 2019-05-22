using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using System;

namespace SuperSLiM
{
	/// <summary>
	/// Lays out views in a grid. The number of columns can be set directly, or a minimum size can be
	/// requested. If you request a 100dip minimum column size and there is 330dip available, the layout
	/// with calculate there to be 3 columns each 130dip across.
	/// </summary>
	public class GridSLM : SectionLayoutManager
	{
		private const int AUTO_FIT = -1;
		private const int DEFAULT_NUM_COLUMNS = 1;
		public static int ID = LayoutManager.SECTION_MANAGER_GRID;
		private Context mContext;
		private int mMinimumWidth = 0;
		private int mNumColumns = 0;
		private int mColumnWidth;
		private bool mColumnsSpecified;

		public GridSLM(LayoutManager layoutManager, Context context) : base(layoutManager)
		{
			mContext = context;
		}

		public override int ComputeHeaderOffset(int firstVisiblePosition, SectionData sd, LayoutState state)
		{
			/*
			 * Work from an assumed overlap and add heights from the start until the overlap is zero or
			 * less, or the current position (or max items) is reached.
			 */
			int areaAbove = 0;
			for (int position = sd.firstPosition + 1; areaAbove < sd.headerHeight && position < firstVisiblePosition; position += mNumColumns)
			{
				// Look to see if the header overlaps with the displayed area of the mSection.
				int rowHeight = 0;
				for (int col = 0; col < mNumColumns; col++)
				{
					LayoutState.View child = state.GetView(position + col);
					MeasureChild(child, sd);
					rowHeight = Math.Max(rowHeight, mLayoutManager.GetDecoratedMeasuredHeight(child.view));
					state.CacheView(position + col, child.view);
				}
				areaAbove += rowHeight;
			}

			if (areaAbove == sd.headerHeight)
			{
				return 0;
			}
			else if (areaAbove > sd.headerHeight)
			{
				return 1;
			}
			else
			{
				return -areaAbove;
			}
		}

		public override int FillToEnd(int leadingEdge, int markerLine, int anchorPosition, SectionData sd, LayoutState state)
		{
			if (markerLine >= leadingEdge)
			{
				return markerLine;
			}

			int itemCount = state.recyclerState.ItemCount;
			if (anchorPosition >= itemCount)
			{
				return markerLine;
			}

			LayoutState.View anchor = state.GetView(anchorPosition);
			state.CacheView(anchorPosition, anchor.view);
			if (anchor.LayoutParams.FirstPosition != sd.firstPosition)
			{
				return markerLine;
			}

			int firstContentPosition = sd.hasHeader ? sd.firstPosition + 1 : sd.firstPosition;

			// Ensure the anchor is the first item in the row.
			int col = (anchorPosition - firstContentPosition) % mNumColumns;
			for (int i = 1; i <= col; i++)
			{
				// Detach and scrap attached items in this row, so we can re-lay them again. The last
				// child view in the index can be the header so we just skip past it if it last.
				for (int j = 1; j <= mLayoutManager.ChildCount; j++)
				{
					View child = mLayoutManager.GetChildAt(mLayoutManager.ChildCount - j);
					if (mLayoutManager.GetPosition(child) == anchorPosition - i)
					{
						markerLine = mLayoutManager.GetDecoratedTop(child);
						mLayoutManager.DetachAndScrapViewAt(j, state.recycler);
						break;
					}

                    LayoutManager.LayoutParams lp = (LayoutManager.LayoutParams)child.LayoutParameters;
					if (lp.FirstPosition != sd.firstPosition)
					{
						break;
					}
				}
			}
			anchorPosition = anchorPosition - col;

			// Lay out rows to end.
			for (int i = anchorPosition; i < itemCount; i += mNumColumns)
			{
				if (markerLine >= leadingEdge)
				{
					break;
				}

				LayoutState.View view = state.GetView(i);
                if (view.LayoutParams.FirstPosition != sd.firstPosition)
				{
					state.CacheView(i, view.view);
					break;
				}

				int rowHeight = fillRow(markerLine, i, LayoutManager.Direction.END, true, sd, state);
				markerLine += rowHeight;
			}

			return markerLine;
		}

		public override int FillToStart(int leadingEdge, int markerLine, int anchorPosition, SectionData sd, LayoutState state)
		{
			int firstContentPosition = sd.hasHeader ? sd.firstPosition + 1 : sd.firstPosition;

			// Check to see if we have to adjust for minimum section height. We don't if there is an
			// attached non-header view in this section.
			bool applyMinHeight = false;
			for (int i = 0; i < mLayoutManager.ChildCount; i++)
			{
				View check = mLayoutManager.GetChildAt(0);
                LayoutManager.LayoutParams checkParams = (LayoutManager.LayoutParams)check.LayoutParameters;
				if (checkParams.FirstPosition != sd.firstPosition)
				{
					applyMinHeight = true;
					break;
				}

				if (!checkParams.isHeader)
				{
					applyMinHeight = false;
					break;
				}
			}

			// _ _ ^ a b
			int col = (anchorPosition - firstContentPosition) % mNumColumns;
			for (int i = 1; i < mNumColumns - col; i++)
			{
				// Detach and scrap attached items in this row, so we can re-lay them again. The last
				// child view in the index can be the header so we just skip past it if it last.
				for (int j = 0; j < mLayoutManager.ChildCount; j++)
				{
					View child = mLayoutManager.GetChildAt(j);
                    LayoutManager.LayoutParams lp = (LayoutManager.LayoutParams)child.LayoutParameters;
					if (lp.FirstPosition != sd.firstPosition)
					{
						break;
					}

					if (mLayoutManager.GetPosition(child) == anchorPosition + i)
					{
						mLayoutManager.DetachAndScrapViewAt(j, state.recycler);
						break;
					}
				}
			}
			// Ensure the anchor is the first item in the row.
			int columnAnchorPosition = anchorPosition - col;

			// Work out offset to marker line by measuring rows from the end. If section height is less
			// than min height, then adjust marker line and then lay out items.
			int measuredPositionsMarker = -1;
			int sectionHeight = 0;
			int minHeightOffset = 0;
			if (applyMinHeight)
			{
				for (int i = columnAnchorPosition; i >= 0; i -= mNumColumns)
				{
					LayoutState.View check = state.GetView(i);
					state.CacheView(i, check.view);
                    LayoutManager.LayoutParams checkParams = check.LayoutParams;
					if (checkParams.FirstPosition != sd.firstPosition)
					{
						break;
					}

					int rowHeight = 0;
					for (int j = 0; j < mNumColumns && i + j <= anchorPosition; j++)
					{
						LayoutState.View measure = state.GetView(i + j);
						state.CacheView(i + j, measure.view);
                        LayoutManager.LayoutParams measureParams = measure.LayoutParams;
						if (measureParams.FirstPosition != sd.firstPosition)
						{
							break;
						}

						if (measureParams.isHeader)
						{
							continue;
						}

						MeasureChild(measure, sd);
						rowHeight = Math.Max(rowHeight, mLayoutManager.GetDecoratedMeasuredHeight(measure.view));
					}

					sectionHeight += rowHeight;
					measuredPositionsMarker = i;
					if (sectionHeight >= sd.minimumHeight)
					{
						break;
					}
				}

				if (sectionHeight < sd.minimumHeight)
				{
					minHeightOffset = sectionHeight - sd.minimumHeight;
					markerLine += minHeightOffset;
				}
			}

			// Lay out rows to end.
			for (int i = columnAnchorPosition; i >= 0; i -= mNumColumns)
			{
				if (markerLine - minHeightOffset < leadingEdge)
				{
					break;
				}

				LayoutState.View rowAnchor = state.GetView(i);
				state.CacheView(i, rowAnchor.view);
                LayoutManager.LayoutParams lp = rowAnchor.LayoutParams;
				if (lp.isHeader || lp.FirstPosition != sd.firstPosition)
				{
					break;
				}

				bool measureRowItems = !applyMinHeight || i < measuredPositionsMarker;
				int rowHeight = fillRow(markerLine, i, LayoutManager.Direction.START, measureRowItems, sd, state);
				markerLine -= rowHeight;
			}

			return markerLine;
		}

		public override int FinishFillToEnd(int leadingEdge, View anchor, SectionData sd, LayoutState state)
		{
			int anchorPosition = mLayoutManager.GetPosition(anchor);
			int markerLine = GetLowestEdge(sd.firstPosition, mLayoutManager.ChildCount - 1, mLayoutManager.GetDecoratedBottom(anchor));

			return FillToEnd(leadingEdge, markerLine, anchorPosition + 1, sd, state);
		}

		public override int FinishFillToStart(int leadingEdge, View anchor, SectionData sd, LayoutState state)
		{
			int anchorPosition = mLayoutManager.GetPosition(anchor);
			int markerLine = mLayoutManager.GetDecoratedTop(anchor);

			return FillToStart(leadingEdge, markerLine, anchorPosition - 1, sd, state);
		}

        public LayoutManager.LayoutParams GenerateLayoutParams(LayoutManager.LayoutParams lp)
		{
			return new LayoutParams(lp);
		}

		public RecyclerView.LayoutParams GenerateLayoutParams(Context c, IAttributeSet attrs)
		{
			return new LayoutParams(c, attrs);
		}

		public void GetEdgeStates(Rect outRect, View child, SectionData sd, RecyclerView.State state)
		{
            LayoutManager.LayoutParams lp = (LayoutManager.LayoutParams)child.LayoutParameters;
			int position = lp.ViewPosition;

			int column = (position - sd.FirstContentPosition) % mNumColumns;
			bool isLtr = mLayoutManager.LayoutDirection == ViewCompat.LayoutDirectionLtr;
			int ltrColumn = AdjustColumnForLayoutDirection(column, isLtr);

			outRect.Left = ltrColumn == 0 ? ItemDecorator.EXTERNAL : ItemDecorator.INTERNAL;
			outRect.Right = ltrColumn == mNumColumns - 1 ? ItemDecorator.EXTERNAL : ItemDecorator.INTERNAL;

			outRect.Top = position - column == sd.FirstContentPosition ? ItemDecorator.EXTERNAL : ItemDecorator.INTERNAL;
			// Reset position to left column and add num columns, if < itemcount then not last row.
			if (sd.LastContentItemFound)
			{
				outRect.Bottom = position + (mNumColumns - column) > sd.lastContentPosition ? ItemDecorator.EXTERNAL : ItemDecorator.INTERNAL;
			}
			else
			{
				outRect.Bottom = ItemDecorator.INTERNAL;
			}
		}

		public int GetLowestEdge(int sectionFirstPosition, int lastIndex, int defaultEdge)
		{
			int bottomMostEdge = 0;
			int leftPosition = mLayoutManager.Width;
			bool foundItems = false;
			// Look from end to find children that are the lowest.
			for (int i = lastIndex; i >= 0; i--)
			{
				View look = mLayoutManager.GetChildAt(i);
                LayoutManager.LayoutParams lp = (LayoutManager.LayoutParams)look.LayoutParameters;
				if (lp.FirstPosition != sectionFirstPosition)
				{
					break;
				}

				if (lp.isHeader)
				{
					continue;
				}

				if (look.Left < leftPosition)
				{
					leftPosition = look.Left;
				}
				else
				{
					break;
				}

				foundItems = true;
				bottomMostEdge = Math.Max(bottomMostEdge, mLayoutManager.GetDecoratedBottom(look));
			}

			return foundItems ? bottomMostEdge : defaultEdge;
		}

		public GridSLM Initialize(SectionData sd)
		{
			base.Initialize(sd);

			if (sd.headerParams is LayoutParams)
			{
				LayoutParams lp = (LayoutParams)sd.headerParams;
				int columnWidth = lp.ColumnWidth;
				int numColumns = lp.NumColumns;
				if (columnWidth < 0 && numColumns < 0)
				{
					numColumns = DEFAULT_NUM_COLUMNS;
				}

				if (numColumns == AUTO_FIT)
				{
					ColumnWidth = columnWidth;
				}
				else
				{
					NumColumns = numColumns;
				}
			}

			CalculateColumnWidthValues(sd);

			return this;
		}

		/// <summary>
		/// Fill a row.
		/// </summary>
		/// <param name="markerLine">      Line indicating the top edge of the row. </param>
		/// <param name="anchorPosition">  Position of the first view in the row. </param>
		/// <param name="direction">       Direction of edge to fill towards. </param>
		/// <param name="measureRowItems"> Measure the row items. </param>
		/// <param name="sd">              Section data. </param>
		/// <param name="state">           Layout state. </param>
		/// <returns> The height of the new row. </returns>
		public int fillRow(int markerLine, int anchorPosition, LayoutManager.Direction direction, bool measureRowItems, SectionData sd, LayoutState state)
		{
			int rowHeight = 0;
			LayoutState.View[] views = new LayoutState.View[mNumColumns];
			for (int i = 0; i < mNumColumns; i++)
			{
				int position = anchorPosition + i;
				if (position >= state.recyclerState.ItemCount)
				{
					break;
				}

				LayoutState.View view = state.GetView(position);
                if (view.LayoutParams.FirstPosition != sd.firstPosition)
				{
					state.CacheView(position, view.view);
					break;
				}

				if (measureRowItems)
				{
					MeasureChild(view, sd);
				}
				else
				{
					state.DecacheView(i + anchorPosition);
				}
				rowHeight = Math.Max(rowHeight, mLayoutManager.GetDecoratedMeasuredHeight(view.view));
				views[i] = view;
			}

			bool directionIsStart = direction == LayoutManager.Direction.START;
			if (directionIsStart)
			{
				markerLine -= rowHeight;
			}

			for (int i = 0; i < mNumColumns; i++)
			{
				int col = directionIsStart ? mNumColumns - i - 1 : i;
				if (views[col] == null)
				{
					continue;
				}
				LayoutChild(views[col], markerLine, col, rowHeight, sd, state);
				AddView(views[col], col + anchorPosition, direction, state);
			}

			return rowHeight;
		}

		public int ColumnWidth
		{
			set
			{
				mMinimumWidth = value;
				mColumnsSpecified = false;
			}
			get
			{
					return mColumnWidth;
			}
		}

		public int NumColumns
		{
			set
			{
				mNumColumns = value;
				mMinimumWidth = 0;
				mColumnsSpecified = true;
			}
			get
			{
					return mNumColumns;
			}
		}

		private void CalculateColumnWidthValues(SectionData sd)
		{
			int availableWidth = mLayoutManager.Width - sd.contentStart - sd.contentEnd;
			if (!mColumnsSpecified)
			{
				if (mMinimumWidth <= 0)
				{
                    mMinimumWidth = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 48, mContext.Resources.DisplayMetrics);
				}
				mNumColumns = availableWidth / Math.Abs(mMinimumWidth);
			}
			if (mNumColumns < 1)
			{
				mNumColumns = 1;
			}
			mColumnWidth = availableWidth / mNumColumns;
			if (mColumnWidth == 0)
			{
				Console.WriteLine("GridSection", "Too many columns (" + mNumColumns + ") for available width" + availableWidth + ".");
			}
		}

		/// <summary>
		/// Layout out a view for the given column in a row. Views that have a height param of
		/// MATCH_PARENT are fixed to the height of the row.
		/// </summary>
		/// <param name="child">     View to lay out. </param>
		/// <param name="top">       Line indicating the top edge of the row. </param>
		/// <param name="col">       Column view is being placed into. </param>
		/// <param name="rowHeight"> Height of the row. </param>
		/// <param name="sd">        Section data. </param>
		/// <param name="state">     Layout state. </param>
		private void LayoutChild(LayoutState.View child, int top, int col, int rowHeight, SectionData sd, LayoutState state)
		{
			int height;
            if (child.LayoutParams.Height == LayoutManager.LayoutParams.MatchParent)
			{
				height = rowHeight;
			}
			else
			{
				height = mLayoutManager.GetDecoratedMeasuredHeight(child.view);
			}
			int width = mLayoutManager.GetDecoratedMeasuredWidth(child.view);

			col = AdjustColumnForLayoutDirection(col, state.isLTR);

			int bottom = top + height;
			int left = (state.isLTR ? sd.contentStart : sd.contentEnd) + col * mColumnWidth;
			int right = left + width;

			mLayoutManager.LayoutDecorated(child.view, left, top, right, bottom);
		}

		private int AdjustColumnForLayoutDirection(int col, bool isLtr)
		{
			if (!isLtr)
			{
				col = mNumColumns - 1 - col;
			}
			return col;
		}

		/// <summary>
		/// Measure view. A view is given an area as wide as a single column with an undefined height.
		/// </summary>
		/// <param name="child"> View to measure. </param>
		/// <param name="sd">    Section data. </param>
		private void MeasureChild(LayoutState.View child, SectionData sd)
		{
			int widthOtherColumns = (mNumColumns - 1) * mColumnWidth;
			mLayoutManager.MeasureChildWithMargins(child.view, sd.marginStart + sd.marginEnd + widthOtherColumns, 0);
		}

        public class LayoutParams : LayoutManager.LayoutParams
		{

			public int mNumColumns;
            public int mColumnWidth;

			public LayoutParams(Context c, IAttributeSet attrs) 
                : base(c, attrs)
			{

				TypedArray a = c.ObtainStyledAttributes(attrs, Resource.Styleable.superslim_GridSLM);
				mNumColumns = a.GetInt(Resource.Styleable.superslim_GridSLM_slm_grid_numColumns, AUTO_FIT);
				mColumnWidth = a.GetDimensionPixelSize(Resource.Styleable.superslim_GridSLM_slm_grid_columnWidth, -1);
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

			public int ColumnWidth
			{
				get
                {
                    return mColumnWidth;
                }
                set
				{
					mColumnWidth = value;
				}
			}

			public int NumColumns
			{
                get
                {
                    return mNumColumns;
                }
				set
				{
					mNumColumns = value;
				}
			}

			public void Initialize(ViewGroup.LayoutParams other)
			{
				if (other is LayoutParams)
				{
					LayoutParams lp = (LayoutParams) other;
					mNumColumns = lp.mNumColumns;
					mColumnWidth = lp.mColumnWidth;
				}
				else
				{
					mNumColumns = AUTO_FIT;
					mColumnWidth = -1;
				}
			}
		}
	}
}