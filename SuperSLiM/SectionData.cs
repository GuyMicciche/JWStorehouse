using Android.Text;
using Android.Views;

namespace SuperSLiM
{
	public class SectionData
	{

		public readonly int firstPosition;
		public readonly bool hasHeader;
		public readonly int minimumHeight;
		public readonly string sectionManager;
		public readonly int sectionManagerKind;
		public readonly int headerWidth;
		public readonly int headerHeight;
		public readonly int contentEnd;
		public readonly int contentStart;
        public readonly int marginStart;
		public readonly int marginEnd;

        public LayoutManager.LayoutParams headerParams;

		/// <summary>
		/// Last content position should only be set once the last position has been found.
		/// </summary>
		public int lastContentPosition = -1;

		public SectionData(LayoutManager lm, View first)
		{
			int paddingStart = lm.PaddingStart;
			int paddingEnd = lm.PaddingEnd;

            headerParams = (LayoutManager.LayoutParams)first.LayoutParameters;

			if (headerParams.isHeader)
			{
				headerWidth = lm.GetDecoratedMeasuredWidth(first);
				headerHeight = lm.GetDecoratedMeasuredHeight(first);

				if (!headerParams.HeaderInline || headerParams.HeaderOverlay)
				{
					minimumHeight = headerHeight;
				}
				else
				{
					minimumHeight = 0;
				}

				if (headerParams.headerStartMarginIsAuto)
				{
					if (headerParams.HeaderStartAligned && !headerParams.HeaderOverlay)
					{
						marginStart = headerWidth;
					}
					else
					{
						marginStart = 0;
					}
				}
				else
				{
					marginStart = headerParams.headerMarginStart;
				}
				if (headerParams.headerEndMarginIsAuto)
				{
					if (headerParams.HeaderEndAligned && !headerParams.HeaderOverlay)
					{
						marginEnd = headerWidth;
					}
					else
					{
						marginEnd = 0;
					}
				}
				else
				{
					marginEnd = headerParams.headerMarginEnd;
				}
			}
			else
			{
				minimumHeight = 0;
				headerHeight = 0;
				headerWidth = 0;
				marginStart = headerParams.headerMarginStart;
				marginEnd = headerParams.headerMarginEnd;
			}

			contentEnd = marginEnd + paddingEnd;
			contentStart = marginStart + paddingStart;

			hasHeader = headerParams.isHeader;

			firstPosition = headerParams.FirstPosition;

			sectionManager = headerParams.sectionManager;
			sectionManagerKind = headerParams.sectionManagerKind;
		}

		public virtual int FirstContentPosition
		{
			get
			{
				if (hasHeader)
				{
					if (LastContentItemFound)
					{
						return lastContentPosition > firstPosition ? firstPosition + 1 : firstPosition;
					}
					else
					{
						return firstPosition + 1;
					}
				}
				return firstPosition;
			}
		}

		public int LastContentPosition
		{
			get
			{
				return lastContentPosition;
			}
		}

		public int TotalMarginWidth
		{
			get
			{
				return marginEnd + marginStart;
			}
		}

		public bool LastContentItemFound
		{
			get
			{
				return lastContentPosition != -1;
			}
		}

        public bool SameSectionManager(LayoutManager.LayoutParams lp)
		{
			return lp.sectionManagerKind == sectionManagerKind || TextUtils.Equals(lp.sectionManager, sectionManager);
		}
	}
}