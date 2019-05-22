using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Views;
using System.Collections.Generic;

namespace SuperSLiM
{
	/// <summary>
	/// A generic decorator that can decorate items with drawables and/or padding in a flexible and
	/// selectable manner. <p> To control which edges are affected, flags are used to determine whether
	/// to apply the padding or drawable to a given edge. Edges can be or external as determined
	/// by the section layout manager. Drawables take precedence over padding in being applied to an
	/// edge. </p> Use the Builder.decorates* methods, or assignmentCheckers to limit the decorator to a
	/// subset of the items.
	/// </summary>
	public class ItemDecorator : RecyclerView.ItemDecoration
	{
		public const int INTERNAL = 0x01;
		public const int EXTERNAL = 0x02;
		public static readonly int ANY_EDGE = INTERNAL | EXTERNAL;
		private static readonly int DEFAULT_FLAGS = ANY_EDGE;
		public const int INSET = 0x04;
		public const int HEADER = 0x01;
		public const int CONTENT = 0x02;
		public static readonly int ANY_KIND = HEADER | CONTENT;
		internal const int UNSET = -1;
		private readonly Spacing mSpacing;
		private readonly Rect mEdgeState = new Rect();
		private readonly Edge mLeft;
		private readonly Edge mRight;
		private readonly Edge mTop;
		private readonly Edge mBottom;
		private readonly Edge mStart;
		private readonly Edge mEnd;
		private IList<AssignmentChecker> mCheckers;
		private bool initialised = false;
		private bool layoutDirectionResolved = false;

		public ItemDecorator(Builder b)
		{
            mCheckers = new List<AssignmentChecker>(b.assignments);
			mStart = new Edge(b.startDrawable, b.startDrawableFlags, b.startPadding, b.startPaddingFlags);
			mEnd = new Edge(b.endDrawable, b.endDrawableFlags, b.endPadding, b.endPaddingFlags);
			mLeft = new Edge(b.leftDrawable, b.leftDrawableFlags, b.leftPadding, b.leftPaddingFlags);
			mTop = new Edge(b.topDrawable, b.topDrawableFlags, b.topPadding, b.topPaddingFlags);
			mRight = new Edge(b.rightDrawable, b.rightDrawableFlags, b.rightPadding, b.rightPaddingFlags);
			mBottom = new Edge(b.bottomDrawable, b.bottomDrawableFlags, b.bottomPadding, b.bottomPaddingFlags);
			mSpacing = new Spacing(b);
		}

		public override void OnDrawOver(Canvas c, RecyclerView parent, RecyclerView.State state)
		{
			resolveLayoutDirection(parent);
			LayoutManager lm = (LayoutManager) parent.GetLayoutManager();

			for (int i = 0; i < lm.ChildCount; i++)
			{
				View child = lm.GetChildAt(i);
                LayoutManager.LayoutParams lp = (LayoutManager.LayoutParams)child.LayoutParameters;
				if (!assignedTo(lm.GetSectionData(lp.FirstPosition, child), lp))
				{
					continue;
				}

				lm.GetEdgeStates(mEdgeState, child, state);

				int decorLeft = lm.GetDecoratedLeft(child);
				int decorTop = lm.GetDecoratedTop(child);
				int decorRight = lm.GetDecoratedRight(child);
				int decorBottom = lm.GetDecoratedBottom(child);

				int childLeft = child.Left;
				int childTop = child.Top;
				int childRight = child.Right;
				int childBottom = child.Bottom;

				int offsetLeft = childLeft - decorLeft - lp.LeftMargin;
				int offsetTop = childTop - decorTop - lp.TopMargin;
				int offsetRight = decorRight - childRight - lp.RightMargin;
				int offsetBottom = decorBottom - childBottom - lp.BottomMargin;

				if (mLeft.hasValidDrawableFor(mEdgeState.Left))
				{
					int right = decorLeft + offsetLeft;
					int left = right - mLeft.drawable.IntrinsicWidth;

					bool excludeOffsets = (mLeft.drawableFlags & INSET) == INSET;
					int top = excludeOffsets ? decorTop + offsetTop : decorTop;
					int bottom = excludeOffsets ? decorBottom - offsetBottom : decorBottom;

					mLeft.drawable.SetBounds(left, top, right, bottom);
					mLeft.drawable.Draw(c);
				}

				if (mTop.hasValidDrawableFor(mEdgeState.Top))
				{
					int bottom = decorTop + offsetTop;
					int top = bottom - mTop.drawable.IntrinsicHeight;

					bool excludeOffsets = (mTop.drawableFlags & INSET) == INSET;
					int left = excludeOffsets ? decorLeft + offsetLeft : decorLeft;
					int right = excludeOffsets ? decorRight - offsetRight : decorRight;

					mTop.drawable.SetBounds(left, top, right, bottom);
					mTop.drawable.Draw(c);
				}

				if (mRight.hasValidDrawableFor(mEdgeState.Right))
				{
					int left = decorRight - offsetRight;
					int right = left + mRight.drawable.IntrinsicWidth;

					bool excludeOffsets = (mRight.drawableFlags & INSET) == INSET;
					int top = excludeOffsets ? decorTop + offsetTop : decorTop;
					int bottom = excludeOffsets ? decorBottom - offsetBottom : decorBottom;

					mRight.drawable.SetBounds(left, top, right, bottom);
					mRight.drawable.Draw(c);
				}

				if (mBottom.hasValidDrawableFor(mEdgeState.Bottom))
				{
					int top = decorBottom - offsetBottom;
					int bottom = top + mBottom.drawable.IntrinsicHeight;

					bool excludeOffsets = (mBottom.drawableFlags & INSET) == INSET;
					int left = excludeOffsets ? decorLeft + offsetLeft : decorLeft;
					int right = excludeOffsets ? decorRight - offsetRight : decorRight;

					mBottom.drawable.SetBounds(left, top, right, bottom);
					mBottom.drawable.Draw(c);
				}
			}

			base.OnDrawOver(c, parent, state);
		}

		public override void GetItemOffsets(Rect outRect, View view, RecyclerView parent, RecyclerView.State state)
		{
			resolveLayoutDirection(parent);
			// Check decorator is assigned to section by sfp or slm.
			LayoutManager.LayoutParams lp = (LayoutManager.LayoutParams) view.LayoutParameters;
			LayoutManager lm = (LayoutManager)parent.GetLayoutManager();
			if (!assignedTo(lm.GetSectionData(lp.FirstPosition, view), lp))
			{
				outRect.Left = 0;
				outRect.Top = 0;
				outRect.Right = 0;
				outRect.Bottom = 0;
				return;
			}

			mSpacing.GetOffsets(outRect, view, lm, state);
		}

		private bool assignedTo(SectionData sectionData, LayoutManager.LayoutParams lp)
		{
			if (mCheckers.Count == 0)
			{
				return true;
			}

			foreach (AssignmentChecker check in mCheckers)
			{
				if (check.IsAssigned(sectionData, lp))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// We can finally handle layout direction. </summary>
		/// <param name="view"> View providing layout direction. </param>
		private void resolveLayoutDirection(View view)
		{
			if (layoutDirectionResolved)
			{
				return;
			}
			layoutDirectionResolved = true;
			int layoutDirection = ViewCompat.GetLayoutDirection(view);
			mSpacing.ResolveLayoutDirection(layoutDirection);

			switch (layoutDirection)
			{
				case ViewCompat.LayoutDirectionRtl:
					if (mStart.padding != UNSET)
					{
						mRight.padding = mStart.padding;
						mRight.paddingFlags = mStart.paddingFlags;
					}
					if (mStart.drawable != null)
					{
						mRight.drawable = mStart.drawable;
						mRight.drawableFlags = mStart.drawableFlags;
					}
					if (mEnd.padding != UNSET)
					{
						mLeft.padding = mEnd.padding;
						mLeft.paddingFlags = mEnd.paddingFlags;
					}
					if (mEnd.drawable != null)
					{
						mLeft.drawable = mEnd.drawable;
						mLeft.drawableFlags = mEnd.drawableFlags;
					}
					break;
                case ViewCompat.LayoutDirectionLtr:
				default:
					if (mStart.padding != UNSET)
					{
						mLeft.padding = mStart.padding;
						mLeft.paddingFlags = mStart.paddingFlags;
					}
					if (mStart.drawable != null)
					{
						mLeft.drawable = mStart.drawable;
						mLeft.drawableFlags = mStart.drawableFlags;
					}
					if (mEnd.padding != UNSET)
					{
						mRight.padding = mEnd.padding;
						mRight.paddingFlags = mEnd.paddingFlags;
					}
					if (mEnd.drawable != null)
					{
						mRight.drawable = mEnd.drawable;
						mRight.drawableFlags = mEnd.drawableFlags;
					}
				break;
			}
		}

		/// <summary>
		/// Checks if the decorator is assigned to an item.
		/// </summary>
		public interface AssignmentChecker
		{

			/// <summary>
			/// Tests and item to see if it should be decorated.
			/// </summary>
			/// <param name="params"> Item's layout params. </param>
			/// <returns> True if the decorator should decorate this item. </returns>
			bool IsAssigned(SectionData sectionData, LayoutManager.LayoutParams lp);
		}

		internal class Edge
		{

			internal Drawable drawable;
			internal int drawableFlags;
			internal int padding;
			internal int paddingFlags;

			internal Edge(Drawable drawable, int drawableFlags, int padding, int paddingFlags)
			{
				this.drawable = drawable;
				this.drawableFlags = drawableFlags;
				this.padding = padding;
				this.paddingFlags = paddingFlags;
			}

			internal virtual bool hasValidDrawableFor(int positionMask)
			{
				return drawable != null && (drawableFlags & positionMask) == positionMask;
			}
		}

		/// <summary>
		/// Builder for decorator.
		/// </summary>
		public class Builder
		{
			public Drawable startDrawable;
			public int startDrawableFlags;
			public int startPaddingFlags;
			public Drawable endDrawable;
			public int endDrawableFlags;
			public int endPaddingFlags;
			internal List<AssignmentChecker> assignments = new List<AssignmentChecker>();
			internal int leftPadding;
			internal int leftPaddingFlags;
			internal Drawable leftDrawable;
			internal int leftDrawableFlags;
			internal int topPadding;
			internal int topPaddingFlags;
			internal Drawable topDrawable;
			internal int topDrawableFlags;
			internal int rightPadding;
			internal int rightPaddingFlags;
			internal Drawable rightDrawable;
			internal int rightDrawableFlags;
			internal int bottomPadding;
			internal int bottomPaddingFlags;
			internal Drawable bottomDrawable;
			internal int bottomDrawableFlags;
			public int startPadding = UNSET;
			internal Context mContext;

			public Builder(Context context)
			{
				mContext = context;
			}

			/// <summary>
			/// Add an assignment checker for this decorator.
			/// </summary>
			/// <param name="checker"> Checker to test whether items should be decorated. </param>
			/// <returns> Builder. </returns>
			public virtual Builder addAssignmentChecker(AssignmentChecker checker)
			{
				assignments.Add(checker);
				return this;
			}

			public virtual ItemDecorator build()
			{
				return new ItemDecorator(this);
			}

			/// <summary>
			/// Decorate items in a section.
			/// </summary>
			/// <param name="sectionFirstPosition"> First position in section to decorate items from. </param>
			/// <returns> Builder. </returns>
			public virtual Builder decorateSection(int sectionFirstPosition, int targetKind)
			{
				assignments.Add(new SectionChecker(sectionFirstPosition, targetKind));
				return this;
			}

			/// <summary>
			/// Decorate items in a section.
			/// </summary>
			/// <param name="sectionFirstPosition"> First position in section to decorate items from. </param>
			/// <returns> Builder. </returns>
			public virtual Builder decorateSection(int sectionFirstPosition)
			{
				return decorateSection(sectionFirstPosition, CONTENT);
			}

			/// <summary>
			/// Decorate items in a section managed by one of the built-in SLMs.
			/// </summary>
			/// <param name="id"> SLM id. </param>
			/// <returns> Builder. </returns>
			public virtual Builder decorateSlm(int id, int targetKind)
			{
				assignments.Add(new InternalSlmChecker(id, targetKind));
				return this;
			}

			public int endPadding = UNSET;

			/// <summary>
			/// Decorate items in a section managed by a custom SLM.
			/// </summary>
			/// <param name="key"> SLM key.. </param>
			/// <returns> Builder. </returns>
			public virtual Builder decorateSlm(string key, int targetKind)
			{
				assignments.Add(new CustomSlmChecker(key, targetKind));
				return this;
			}

			/// <summary>
			/// Decorate items in a section managed by one of the built-in SLMs.
			/// </summary>
			/// <param name="id"> SLM id. </param>
			/// <returns> Builder. </returns>
			public virtual Builder decorateSlm(int id)
			{
				return decorateSlm(id, CONTENT);
			}

			/// <summary>
			/// Decorate items in a section managed by a custom SLM.
			/// </summary>
			/// <param name="key"> SLM key.. </param>
			/// <returns> Builder. </returns>
			public virtual Builder decorateSlm(string key)
			{
				return decorateSlm(key, CONTENT);
			}

			/// <summary>
			/// Decorates a specific item.
			/// </summary>
			public virtual Builder decoratesPosition(int position)
			{
				assignments.Add(new PositionChecker(position));
				return this;
			}

			public virtual Builder setDrawableAbove(int resId)
			{
				return setDrawableAbove(resId, DEFAULT_FLAGS);
			}

			public virtual Builder setDrawableAbove(int resId, int flags)
			{
				return setDrawableAbove(mContext.Resources.GetDrawable(resId), flags);
			}

			public virtual Builder setDrawableAbove(Drawable drawable)
			{
				return setDrawableAbove(drawable, DEFAULT_FLAGS);
			}

			public virtual Builder setDrawableAbove(Drawable drawable, int flags)
			{
				topDrawable = drawable;
				topDrawableFlags = flags;
				return this;
			}

			public virtual Builder setDrawableBelow(int resId)
			{
				return setDrawableBelow(resId, DEFAULT_FLAGS);
			}

			public virtual Builder setDrawableBelow(int resId, int flags)
			{
				return setDrawableBelow(mContext.Resources.GetDrawable(resId), flags);
			}

			public virtual Builder setDrawableBelow(Drawable drawable)
			{
				return setDrawableBelow(drawable, DEFAULT_FLAGS);
			}

			public virtual Builder setDrawableBelow(Drawable drawable, int flags)
			{
				bottomDrawable = drawable;
				bottomDrawableFlags = flags;
				return this;
			}

			public virtual Builder setDrawableEnd(int resId)
			{
				return setDrawableEnd(resId, DEFAULT_FLAGS);
			}

			public virtual Builder setDrawableEnd(int resId, int flags)
			{
				return setDrawableEnd(mContext.Resources.GetDrawable(resId), flags);
			}

			public virtual Builder setDrawableEnd(Drawable drawable)
			{
				return setDrawableEnd(drawable, DEFAULT_FLAGS);
			}

			public virtual Builder setDrawableEnd(Drawable drawable, int flags)
			{
				endDrawable = drawable;
				endDrawableFlags = flags;
				return this;
			}

			public virtual Builder setDrawableLeft(int resId)
			{
				return setDrawableLeft(resId, DEFAULT_FLAGS);
			}

			public virtual Builder setDrawableLeft(int resId, int flags)
			{
				return setDrawableLeft(mContext.Resources.GetDrawable(resId), flags);
			}

			public virtual Builder setDrawableLeft(Drawable drawable)
			{
				return setDrawableLeft(drawable, DEFAULT_FLAGS);
			}

			public virtual Builder setDrawableLeft(Drawable drawable, int flags)
			{
				leftDrawable = drawable;
				leftDrawableFlags = flags;
				return this;
			}

			public virtual Builder setDrawableRight(int resId)
			{
				return setDrawableRight(resId, DEFAULT_FLAGS);
			}

			public virtual Builder setDrawableRight(int resId, int flags)
			{
				return setDrawableRight(mContext.Resources.GetDrawable(resId), flags);
			}

			public virtual Builder setDrawableRight(Drawable drawable)
			{
				return setDrawableRight(drawable, DEFAULT_FLAGS);
			}

			public virtual Builder setDrawableRight(Drawable drawable, int flags)
			{
				rightDrawable = drawable;
				rightDrawableFlags = flags;
				return this;
			}

			public virtual Builder setDrawableStart(int resId)
			{
				return setDrawableStart(resId, DEFAULT_FLAGS);
			}

			public virtual Builder setDrawableStart(int resId, int flags)
			{
				return setDrawableStart(mContext.Resources.GetDrawable(resId), flags);
			}

			public virtual Builder setDrawableStart(Drawable drawable)
			{
				return setDrawableStart(drawable, DEFAULT_FLAGS);
			}

			public virtual Builder setDrawableStart(Drawable drawable, int flags)
			{
				startDrawable = drawable;
				startDrawableFlags = flags;
				return this;
			}

			public virtual Builder setPaddingAbove(int padding)
			{
				return setPaddingAbove(padding, DEFAULT_FLAGS);
			}

			public virtual Builder setPaddingAbove(int padding, int flags)
			{
				topPadding = padding;
				topPaddingFlags = flags;
				return this;
			}

			public virtual Builder setPaddingBelow(int padding)
			{
				return setPaddingBelow(padding, DEFAULT_FLAGS);
			}

			public virtual Builder setPaddingBelow(int padding, int flags)
			{
				bottomPadding = padding;
				bottomPaddingFlags = flags;
				return this;
			}

			public virtual Builder setPaddingDimensionAbove(int resId)
			{
				return setPaddingDimensionAbove(resId, DEFAULT_FLAGS);
			}

			public virtual Builder setPaddingDimensionAbove(int resId, int flags)
			{
				return setPaddingAbove(mContext.Resources.GetDimensionPixelSize(resId), flags);
			}

			public virtual Builder setPaddingDimensionBelow(int resId)
			{
				return setPaddingDimensionBelow(resId, DEFAULT_FLAGS);
			}

			public virtual Builder setPaddingDimensionBelow(int resId, int flags)
			{
				return setPaddingBelow(mContext.Resources.GetDimensionPixelSize(resId), flags);
			}

			public virtual Builder setPaddingDimensionEnd(int resId)
			{
				return setPaddingDimensionEnd(resId, DEFAULT_FLAGS);
			}

			public virtual Builder setPaddingDimensionEnd(int resId, int flags)
			{
				return setPaddingEnd(mContext.Resources.GetDimensionPixelSize(resId), flags);
			}

			public virtual Builder setPaddingDimensionLeft(int resId)
			{
				return setPaddingDimensionLeft(resId, DEFAULT_FLAGS);
			}

			public virtual Builder setPaddingDimensionLeft(int resId, int flags)
			{
				return setPaddingLeft(mContext.Resources.GetDimensionPixelSize(resId), flags);
			}

			public virtual Builder setPaddingDimensionRight(int resId)
			{
				return setPaddingDimensionRight(resId, DEFAULT_FLAGS);
			}

			public virtual Builder setPaddingDimensionRight(int resId, int flags)
			{
				return setPaddingRight(mContext.Resources.GetDimensionPixelSize(resId), flags);
			}

			public virtual Builder setPaddingDimensionStart(int resId)
			{
				return setPaddingDimensionStart(resId, DEFAULT_FLAGS);
			}

			public virtual Builder setPaddingDimensionStart(int resId, int flags)
			{
				return setPaddingStart(mContext.Resources.GetDimensionPixelSize(resId), flags);
			}

			public virtual Builder setPaddingEnd(int padding)
			{
				return setPaddingEnd(padding, DEFAULT_FLAGS);
			}

			public virtual Builder setPaddingEnd(int padding, int flags)
			{
				endPadding = padding;
				endPaddingFlags = flags;
				return this;
			}

			public virtual Builder setPaddingLeft(int padding)
			{
				return setPaddingLeft(padding, DEFAULT_FLAGS);
			}

			public virtual Builder setPaddingLeft(int padding, int flags)
			{
				leftPadding = padding;
				leftPaddingFlags = flags;
				return this;
			}

			public virtual Builder setPaddingRight(int padding)
			{
				return setPaddingRight(padding, DEFAULT_FLAGS);
			}

			public virtual Builder setPaddingRight(int padding, int flags)
			{
				rightPadding = padding;
				rightPaddingFlags = flags;
				return this;
			}

			public virtual Builder setPaddingStart(int padding)
			{
				return setPaddingStart(padding, DEFAULT_FLAGS);
			}

			public virtual Builder setPaddingStart(int padding, int flags)
			{
				startPadding = padding;
				startPaddingFlags = flags;
				return this;
			}
		}

		internal class Spacing
		{
			internal readonly int internalTop;
			internal readonly int internalBottom;
			internal readonly int externalTop;
			internal readonly int externalBottom;
			internal int internalLeft;
			internal int internalRight;
			internal int externalLeft;
			internal int externalRight;
			internal int internalStart;
			internal int internalEnd;
			internal int externalStart;
			internal int externalEnd;

			public Spacing(Builder b)
			{
				int mask = INTERNAL;
				internalStart = CalculateOffset(b.startDrawable, b.startDrawableFlags, b.startPadding, b.startPaddingFlags, mask, false);
                internalEnd = CalculateOffset(b.endDrawable, b.endDrawableFlags, b.endPadding, b.endPaddingFlags, mask, false);
                internalLeft = CalculateOffset(b.leftDrawable, b.leftDrawableFlags, b.leftPadding, b.leftPaddingFlags, mask, false);
                internalTop = CalculateOffset(b.topDrawable, b.topDrawableFlags, b.topPadding, b.topPaddingFlags, mask, true);
                internalRight = CalculateOffset(b.rightDrawable, b.rightDrawableFlags, b.rightPadding, b.rightPaddingFlags, mask, false);
                internalBottom = CalculateOffset(b.bottomDrawable, b.bottomDrawableFlags, b.bottomPadding, b.bottomPaddingFlags, mask, true);
				
                mask = EXTERNAL;
                externalStart = CalculateOffset(b.startDrawable, b.startDrawableFlags, b.leftPadding, b.leftPaddingFlags, mask, false);
                externalEnd = CalculateOffset(b.endDrawable, b.endDrawableFlags, b.endPadding, b.endPaddingFlags, mask, false);
                externalLeft = CalculateOffset(b.leftDrawable, b.leftDrawableFlags, b.leftPadding, b.leftPaddingFlags, mask, false);
                externalTop = CalculateOffset(b.topDrawable, b.topDrawableFlags, b.topPadding, b.topPaddingFlags, mask, true);
                externalRight = CalculateOffset(b.rightDrawable, b.rightDrawableFlags, b.rightPadding, b.rightPaddingFlags, mask, false);
                externalBottom = CalculateOffset(b.bottomDrawable, b.bottomDrawableFlags, b.bottomPadding, b.bottomPaddingFlags, mask, true);
			}

			public void GetOffsets(Rect outRect, View view, LayoutManager lm, RecyclerView.State state)
			{
				// Reuse the rect to get the edge states, either or external.
				lm.GetEdgeStates(outRect, view, state);
				outRect.Left = outRect.Left == EXTERNAL ? externalLeft : internalLeft;
				outRect.Right = outRect.Right == EXTERNAL ? externalRight : internalRight;
				outRect.Top = outRect.Top == EXTERNAL ? externalTop : internalTop;
				outRect.Bottom = outRect.Bottom == EXTERNAL ? externalBottom : internalBottom;
			}

			public void ResolveLayoutDirection(int resolvedLayoutDirection)
			{
				switch (resolvedLayoutDirection)
				{
					case ViewCompat.LayoutDirectionRtl:
						if (internalStart != UNSET)
						{
							internalRight = internalStart;
						}

						if (externalStart != UNSET)
						{
							externalRight = externalStart;
						}

						if (internalEnd != UNSET)
						{
							internalLeft = internalEnd;
						}

						if (externalEnd != UNSET)
						{
							externalLeft = externalEnd;
						}
						break;
					case ViewCompat.LayoutDirectionLtr:
					default:
						if (internalStart != UNSET)
						{
							internalLeft = internalStart;
						}

						if (externalStart != UNSET)
						{
							externalLeft = externalStart;
						}

						if (internalEnd != UNSET)
						{
							internalRight = internalEnd;
						}

						if (externalEnd != UNSET)
						{
							externalRight = externalEnd;
						}
					break;
				}
			}

			public int CalculateOffset(Drawable drawable, int drawableFlags, int padding, int paddingFlags, int positionMask, bool isVerticalOffset)
			{
				bool drawableSelected = drawable != null && (drawableFlags & positionMask) == positionMask;
				bool paddingSelected = padding > 0 && (paddingFlags & positionMask) == positionMask;
				if (drawableSelected)
				{
					return isVerticalOffset ? drawable.IntrinsicHeight : drawable.IntrinsicWidth;
				}
				else if (paddingSelected)
				{
					return padding;
				}
				return 0;
			}
		}

		public class PositionChecker : AssignmentChecker
		{
			public int mPosition;

            public PositionChecker(int position)
			{
				mPosition = position;
			}

			public bool IsAssigned(SectionData sd, LayoutManager.LayoutParams lp)
			{
				return lp.ViewPosition == mPosition;
			}
		}

        public class InternalSlmChecker : AssignmentChecker
		{
            public int mSlmId;
            public int mTargetKind;

            public InternalSlmChecker(int slmId, int targetKind)
			{
				mSlmId = slmId;
				mTargetKind = targetKind;
			}

			public bool IsAssigned(SectionData sd, LayoutManager.LayoutParams lp)
			{
				int kind = lp.isHeader ? HEADER : CONTENT;
				return sd.sectionManagerKind == mSlmId && (kind & mTargetKind) != 0;
			}
		}

        public class CustomSlmChecker : AssignmentChecker
		{
            public string mSlmKey;
            public int mTargetKind;

            public CustomSlmChecker(string slmKey, int targetKind)
			{
				mSlmKey = slmKey;
				mTargetKind = targetKind;
			}

            public bool IsAssigned(SectionData sd, LayoutManager.LayoutParams lp)
			{
				int kind = lp.isHeader ? HEADER : CONTENT;
				return sd.sectionManagerKind == LayoutManager.SECTION_MANAGER_CUSTOM && (kind & mTargetKind) != 0 && TextUtils.Equals(sd.sectionManager, mSlmKey);
			}
		}

        public class SectionChecker : AssignmentChecker
		{
            public int mSfp;
            public int mTargetKind;

            public SectionChecker(int sfp, int targetKind)
			{
				mSfp = sfp;
				mTargetKind = targetKind;
			}

			public bool IsAssigned(SectionData sd, LayoutManager.LayoutParams lp)
			{
				int kind = lp.isHeader ? HEADER : CONTENT;
				return lp.FirstPosition == mSfp && (kind & mTargetKind) != 0;
			}
		}
	}
}