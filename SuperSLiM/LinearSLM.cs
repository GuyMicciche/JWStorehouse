using Android.Views;
namespace SuperSLiM
{
	public class LinearSLM : SectionLayoutManager
	{

		public static int ID = LayoutManager.SECTION_MANAGER_LINEAR;

		public LinearSLM(LayoutManager layoutManager) : base(layoutManager)
		{
		}

		public override int ComputeHeaderOffset(int firstVisiblePosition, SectionData sd, LayoutState state)
		{
			/*
			 * Work from an assumed overlap and add heights from the start until the overlap is zero or
			 * less, or the current position (or max items) is reached.
			 */

			int areaAbove = 0;
			for (int position = sd.firstPosition + 1; areaAbove < sd.headerHeight && position < firstVisiblePosition; position++)
			{
				// Look to see if the header overlaps with the displayed area of the mSection.
				LayoutState.View child = state.GetView(position);
				measureChild(child, sd);

				areaAbove += mLayoutManager.GetDecoratedMeasuredHeight(child.view);
				state.CacheView(position, child.view);
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
			int itemCount = state.recyclerState.ItemCount;

			for (int i = anchorPosition; i < itemCount; i++)
			{
				if (markerLine >= leadingEdge)
				{
					break;
				}

				LayoutState.View next = state.GetView(i);
                LayoutManager.LayoutParams lp = next.LayoutParams;
				if (lp.FirstPosition != sd.firstPosition)
				{
					state.CacheView(i, next.view);
					break;
				}

				measureChild(next, sd);
				markerLine = layoutChild(next, markerLine, LayoutManager.Direction.END, sd, state);
				AddView(next, i, LayoutManager.Direction.END, state);
			}

			return markerLine;
		}

		public override int FillToStart(int leadingEdge, int markerLine, int anchorPosition, SectionData sd, LayoutState state)
		{
			// Check to see if we have to adjust for minimum section height. We don't if there is an
			// attached non-header view in this section.
			bool applyMinHeight = false;
			for (int i = 0; i < state.recyclerState.ItemCount; i++)
			{
				View check = mLayoutManager.GetChildAt(0);
				if (check == null)
				{
					applyMinHeight = false;
					break;
				}

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

			// Work out offset to marker line by measuring items from the end. If section height is less
			// than min height, then adjust marker line and then lay out items.
			int measuredPositionsMarker = -1;
			int sectionHeight = 0;
			int minHeightOffset = 0;
			if (applyMinHeight)
			{
				for (int i = anchorPosition; i >= 0; i--)
				{
					LayoutState.View measure = state.GetView(i);
					state.CacheView(i, measure.view);
                    LayoutManager.LayoutParams lp = measure.LayoutParams;
					if (lp.FirstPosition != sd.firstPosition)
					{
						break;
					}

					if (lp.isHeader)
					{
						continue;
					}

					measureChild(measure, sd);
					sectionHeight += mLayoutManager.GetDecoratedMeasuredHeight(measure.view);
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

			for (int i = anchorPosition; i >= 0; i--)
			{
				if (markerLine - minHeightOffset < leadingEdge)
				{
					break;
				}

				LayoutState.View next = state.GetView(i);
                LayoutManager.LayoutParams lp = next.LayoutParams;
				if (lp.isHeader)
				{
					state.CacheView(i, next.view);
					break;
				}
				if (lp.FirstPosition != sd.firstPosition)
				{
					state.CacheView(i, next.view);
					break;
				}

				if (!applyMinHeight || i < measuredPositionsMarker)
				{
					measureChild(next, sd);
				}
				else
				{
					state.DecacheView(i);
				}
				markerLine = layoutChild(next, markerLine, LayoutManager.Direction.START, sd, state);
				AddView(next, i, LayoutManager.Direction.START, state);
			}

			return markerLine;
		}

		public override int FinishFillToEnd(int leadingEdge, View anchor, SectionData sd, LayoutState state)
		{
			int anchorPosition = mLayoutManager.GetPosition(anchor);
			int markerLine = mLayoutManager.GetDecoratedBottom(anchor);

			return FillToEnd(leadingEdge, markerLine, anchorPosition + 1, sd, state);
		}

		public override int FinishFillToStart(int leadingEdge, View anchor, SectionData sd, LayoutState state)
		{
			int anchorPosition = mLayoutManager.GetPosition(anchor);
			int markerLine = mLayoutManager.GetDecoratedTop(anchor);

			return FillToStart(leadingEdge, markerLine, anchorPosition - 1, sd, state);
		}

		private int layoutChild(LayoutState.View child, int markerLine, LayoutManager.Direction direction, SectionData sd, LayoutState state)
		{
			int height = mLayoutManager.GetDecoratedMeasuredHeight(child.view);
			int width = mLayoutManager.GetDecoratedMeasuredWidth(child.view);

			int left = state.isLTR ? sd.contentStart : sd.contentEnd;
			int right = left + width;
			int top;
			int bottom;

			if (direction == LayoutManager.Direction.END)
			{
				top = markerLine;
				bottom = top + height;
			}
			else
			{
				bottom = markerLine;
				top = bottom - height;
			}
			mLayoutManager.LayoutDecorated(child.view, left, top, right, bottom);

			if (direction == LayoutManager.Direction.END)
			{
				markerLine = mLayoutManager.GetDecoratedBottom(child.view);
			}
			else
			{
				markerLine = mLayoutManager.GetDecoratedTop(child.view);
			}

			return markerLine;
		}

		private void measureChild(LayoutState.View child, SectionData sd)
		{
			mLayoutManager.MeasureChildWithMargins(child.view, sd.TotalMarginWidth, 0);
		}
	}
}