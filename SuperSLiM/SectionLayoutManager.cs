using Android.Content;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;

namespace SuperSLiM
{
	public abstract class SectionLayoutManager
	{
		private const int MARGIN_UNSET = -1;
		protected LayoutManager mLayoutManager;

		public SectionLayoutManager(LayoutManager layoutManager)
		{
			mLayoutManager = layoutManager;
		}

		/// <summary>
		/// Compute the offset for side aligned headers. If the height of the non-visible area of the
		/// section is taller than the header, then the header should be offscreen, in that case return
		/// any +ve number.
		/// </summary>
		/// <param name="firstVisiblePosition"> Position of first visible item in section. </param>
		/// <param name="sd">                   Section data. </param>
		/// <param name="state">                Layout state. </param>
		/// <returns> -ve number giving the distance the header should be offset before the anchor view. A
		/// +ve number indicates the header is offscreen. </returns>
		public abstract int ComputeHeaderOffset(int firstVisiblePosition, SectionData sd, LayoutState state);

		/// <summary>
		/// Fill section content towards the end.
		/// </summary>
		/// <param name="leadingEdge">    Line to fill up to. Content will not be wholly beyond this line. </param>
		/// <param name="markerLine">     Start of the section content area. </param>
		/// <param name="anchorPosition"> Adapter position for the first content item in the section. </param>
		/// <param name="sd">             Section data. </param>
		/// <param name="state">          Layout state. </param>
		/// <returns> Line to which content has been filled. </returns>
        public abstract int FillToEnd(int leadingEdge, int markerLine, int anchorPosition, SectionData sd, LayoutState state);

        public abstract int FillToStart(int leadingEdge, int markerLine, int anchorPosition, SectionData sd, LayoutState state);

		/// <summary>
		/// Find the position of the first completely visible item of this section.
		/// </summary>
		/// <param name="sectionFirstPosition"> First position of section being queried. </param>
		/// <returns> Position of first completely visible item. </returns>
		public int FindFirstCompletelyVisibleItemPosition(int sectionFirstPosition)
		{
			return mLayoutManager.GetPosition(FindFirstCompletelyVisibleView(sectionFirstPosition, false));
		}

		/// <summary>
		/// Locate the first view in this section that is completely visible. Will skip headers unless
		/// they are the only one visible.
		/// </summary>
		/// <param name="sectionFirstPosition"> First position of section being queried. </param>
		/// <param name="skipHeader">           Do not include the section header if it has one. </param>
		/// <returns> First completely visible item or null. </returns>
		public View FindFirstCompletelyVisibleView(int sectionFirstPosition, bool skipHeader)
		{
			int topEdge = mLayoutManager.ClipToPadding ? mLayoutManager.PaddingTop : 0;
			int bottomEdge = mLayoutManager.ClipToPadding ? mLayoutManager.Height - mLayoutManager.PaddingBottom : mLayoutManager.Height;

			int lookAt = 0;
			int childCount = mLayoutManager.ChildCount;
			View candidate = null;
			while (true)
			{
				if (lookAt >= childCount)
				{
					return candidate;
				}

				View view = mLayoutManager.GetChildAt(lookAt);

				bool topInside = mLayoutManager.GetDecoratedTop(view) >= topEdge;
				bool bottomInside = mLayoutManager.GetDecoratedBottom(view) <= bottomEdge;

                LayoutManager.LayoutParams lp = (LayoutManager.LayoutParams)view.LayoutParameters;
				if (sectionFirstPosition == lp.FirstPosition && topInside && bottomInside)
				{
					if (!lp.isHeader || !skipHeader)
					{
						return view;
					}
					else
					{
						candidate = view;
					}
				}
				else
				{
					// Skipped past section.
					return candidate;
				}

				lookAt += 1;
			}
		}

		/// <summary>
		/// Find the position of the first visible item of the section.
		/// </summary>
		/// <param name="sectionFirstPosition"> First position of section being queried. </param>
		/// <returns> Position of first visible item. </returns>
		public int FindFirstVisibleItemPosition(int sectionFirstPosition)
		{
			return mLayoutManager.GetPosition(FindFirstVisibleView(sectionFirstPosition, false));
		}

		/// <summary>
		/// Locate the visible view which has the earliest adapter position. Will skip headers unless
		/// they are the only one visible.
		/// </summary>
		/// <param name="sectionFirstPosition"> Position of first position of section.. </param>
		/// <param name="skipHeader">           Do not include the section header if it has one. </param>
		/// <returns> View. </returns>
		public View FindFirstVisibleView(int sectionFirstPosition, bool skipHeader)
		{
			int lookAt = 0;
			int childCount = mLayoutManager.ChildCount;
			View candidate = null;
			while (true)
			{
				if (lookAt >= childCount)
				{
					return candidate;
				}

				View view = mLayoutManager.GetChildAt(lookAt);
                LayoutManager.LayoutParams lp = (LayoutManager.LayoutParams)view.LayoutParameters;
				if (sectionFirstPosition == lp.FirstPosition)
				{
					if (!lp.isHeader || !skipHeader)
					{
						return view;
					}
					else
					{
						candidate = view;
					}
				}
				else
				{
					// Skipped past section.
					return candidate;
				}

				lookAt += 1;
			}
		}

		/// <summary>
		/// Find the position of the first visible item of this section.
		/// </summary>
		/// <param name="sectionFirstPosition"> First position of section being queried. </param>
		/// <returns> Position of first visible item. </returns>
		public int FindLastCompletelyVisibleItemPosition(int sectionFirstPosition)
		{
			return mLayoutManager.GetPosition(FindLastCompletelyVisibleView(sectionFirstPosition));
		}

		/// <summary>
		/// Locate the last view in this section that is completely visible. Will skip headers unless
		/// they are the only one visible.
		/// </summary>
		/// <param name="sectionFirstPosition"> First position of section being queried. </param>
		/// <returns> Last completely visible item or null. </returns>
		public View FindLastCompletelyVisibleView(int sectionFirstPosition)
		{
			int topEdge = mLayoutManager.ClipToPadding ? mLayoutManager.PaddingTop : 0;
			int bottomEdge = mLayoutManager.ClipToPadding ? mLayoutManager.Height - mLayoutManager.PaddingBottom : mLayoutManager.Height;

			int lookAt = mLayoutManager.ChildCount - 1;
			View candidate = null;
			while (true)
			{
				if (lookAt < 0)
				{
					return candidate;
				}

				View view = mLayoutManager.GetChildAt(lookAt);

				bool topInside = mLayoutManager.GetDecoratedTop(view) >= topEdge;
				bool bottomInside = mLayoutManager.GetDecoratedBottom(view) <= bottomEdge;

                LayoutManager.LayoutParams lp = (LayoutManager.LayoutParams)view.LayoutParameters;
				if (sectionFirstPosition == lp.FirstPosition)
				{
					if (topInside && bottomInside)
					{
						if (!lp.isHeader)
						{
							return view;
						}
						else
						{
							candidate = view;
						}
					}
				}
				else if (candidate == null)
				{
					sectionFirstPosition = lp.FirstPosition;
					continue;
				}
				else
				{
					return candidate;
				}

				lookAt -= 1;
			}
		}

		/// <summary>
		/// Find the position of the first visible item of the section.
		/// </summary>
		/// <param name="sectionFirstPosition"> First position of section being queried. </param>
		/// <returns> Position of first visible item. </returns>
		public int FindLastVisibleItemPosition(int sectionFirstPosition)
		{
			return mLayoutManager.GetPosition(FindLastVisibleView(sectionFirstPosition));
		}

		/// <summary>
		/// Locate the visible view which has the latest adapter position.
		/// </summary>
		/// <param name="sectionFirstPosition"> Section id. </param>
		/// <returns> View. </returns>
		public View FindLastVisibleView(int sectionFirstPosition)
		{
			int lookAt = mLayoutManager.ChildCount - 1;
			View candidate = null;
			while (true)
			{
				if (lookAt < 0)
				{
					return candidate;
				}

				View view = mLayoutManager.GetChildAt(lookAt);
                LayoutManager.LayoutParams lp = (LayoutManager.LayoutParams)view.LayoutParameters;
				if (sectionFirstPosition == lp.FirstPosition)
				{
					if (!lp.isHeader)
					{
						return view;
					}
					else
					{
						candidate = view;
					}
				}
				else
				{
					// Skipped past section.
					return candidate;
				}

				lookAt -= 1;
			}
		}

		/// <summary>
		/// Finish filling an already partially filled section.
		/// </summary>
		/// <param name="leadingEdge"> Line to fill up to. Content will not be wholly beyond this line. </param>
		/// <param name="anchor">      Last attached content item in this section. </param>
		/// <param name="sd">          Section data. </param>
		/// <param name="state">       Layout state. </param>
		/// <returns> Line to which content has been filled. </returns>
        public abstract int FinishFillToEnd(int leadingEdge, View anchor, SectionData sd, LayoutState state);

        public abstract int FinishFillToStart(int leadingEdge, View anchor, SectionData sd, LayoutState state);

        public LayoutManager.LayoutParams GenerateLayoutParams(LayoutManager.LayoutParams lp)
		{
			return lp;
		}

        public RecyclerView.LayoutParams GenerateLayoutParams(Context c, IAttributeSet attrs)
		{
            return new LayoutManager.LayoutParams(c, attrs);
		}

		/// <summary>
		/// Tell decorators which edges are and external. The default implementation assumes a
		/// linear list.
		/// </summary>
		/// <param name="outRect">     Rect to load with ege states. </param>
		/// <param name="child">       Child to look at. </param>
		/// <param name="sectionData"> Section data. </param>
		/// <param name="state">       State. </param>
		public void GetEdgeStates(Rect outRect, View child, SectionData sectionData, RecyclerView.State state)
		{
			outRect.Left = ItemDecorator.EXTERNAL;
			outRect.Right = ItemDecorator.EXTERNAL;
            LayoutManager.LayoutParams lp = (LayoutManager.LayoutParams)child.LayoutParameters;
			int position = lp.ViewPosition;
			outRect.Top = position == sectionData.FirstContentPosition ? ItemDecorator.EXTERNAL : ItemDecorator.INTERNAL;
			outRect.Bottom = position == sectionData.lastContentPosition ? ItemDecorator.EXTERNAL : ItemDecorator.INTERNAL;
		}

		/// <summary>
		/// Find the highest displayed edge of the section. If there is no member found then return the
		/// default edge instead.
		/// </summary>
		/// <param name="sectionFirstPosition"> Section id, position of first item in the section. </param>
		/// <param name="firstIndex">           Child index to start looking from. </param>
		/// <param name="defaultEdge">          Default value. </param>
		/// <returns> Top (attached) edge of the section. </returns>
		public int GetHighestEdge(int sectionFirstPosition, int firstIndex, int defaultEdge)
		{
			// Look from start to find children that are the highest.
			for (int i = firstIndex; i < mLayoutManager.ChildCount; i++)
			{
				View child = mLayoutManager.GetChildAt(i);
                LayoutManager.LayoutParams lp = (LayoutManager.LayoutParams)child.LayoutParameters;
				if (lp.FirstPosition != sectionFirstPosition)
				{
					break;
				}
				if (lp.isHeader)
				{
					continue;
				}
				// A more interesting layout would have to do something more here.
				return mLayoutManager.GetDecoratedTop(child);
			}
			return defaultEdge;
		}

		/// <summary>
		/// Find the lowest displayed edge of the section. If there is no member found then return the
		/// default edge instead.
		/// </summary>
		/// <param name="sectionFirstPosition"> Section id, position of first item in the section. </param>
		/// <param name="lastIndex">            Index to start looking from. Usually the index of the last
		///                             attached view in this section. </param>
		/// <param name="defaultEdge">          Default value. </param>
		/// <returns> Lowest (attached) edge of the section. </returns>
		public int GetLowestEdge(int sectionFirstPosition, int lastIndex, int defaultEdge)
		{
			// Look from end to find children that are the lowest.
			for (int i = lastIndex; i >= 0; i--)
			{
				View child = mLayoutManager.GetChildAt(i);
                LayoutManager.LayoutParams lp = (LayoutManager.LayoutParams)child.LayoutParameters;
				if (lp.FirstPosition != sectionFirstPosition)
				{
					break;
				}
				if (lp.isHeader)
				{
					continue;
				}
				// A more interesting layout would have to do something more here.
				return mLayoutManager.GetDecoratedBottom(child);
			}
			return defaultEdge;
		}

		public int HowManyMissingAbove(int firstPosition, SparseArray<bool> positionsOffscreen)
		{
			int itemsSkipped = 0;
			int itemsFound = 0;
			for (int i = firstPosition; itemsFound < positionsOffscreen.Size(); i++)
			{
				if (positionsOffscreen.Get(i, false))
				{
					itemsFound += 1;
				}
				else
				{
					itemsSkipped += 1;
				}
			}

			return itemsSkipped;
		}

		public int HowManyMissingBelow(int lastPosition, SparseArray<bool> positionsOffscreen)
		{
			int itemsSkipped = 0;
			int itemsFound = 0;
			for (int i = lastPosition; itemsFound < positionsOffscreen.Size(); i--)
			{
				if (positionsOffscreen.Get(i, false))
				{
					itemsFound += 1;
				}
				else
				{
					itemsSkipped += 1;
				}
			}

			return itemsSkipped;
		}

		public SectionLayoutManager Initialize(SectionData sd)
		{
			return this;
		}

		protected int AddView(LayoutState.View child, int position, LayoutManager.Direction direction, LayoutState state)
		{
			int addIndex;
			if (direction == LayoutManager.Direction.START)
			{
				addIndex = 0;
			}
			else
			{
				addIndex = mLayoutManager.ChildCount;
			}

			state.DecacheView(position);
			mLayoutManager.AddView(child.view, addIndex);

			return addIndex;
		}
	}
}