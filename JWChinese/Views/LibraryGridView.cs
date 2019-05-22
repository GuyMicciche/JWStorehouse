using Android.Content;
using Android.Content.Res;
using Android.Util;
using Android.Views;
using Android.Widget;

using System;

namespace JWChinese
{
    public class LibraryGridView : AdapterView<LibraryGridButtonAdapter>
    {
        private LibraryGridButtonAdapter adapter = null;
        private int columnWidth = 48;
        private int numColumns = 10;
        private bool d = true;
        private int horizontalSpacing = 4;
        private int verticalSpacing = 4;
        private int measuredWidth = 0;
        private int measuredHeight = 0;

        public LibraryGridView(Context paramContext)
            : base(paramContext)
        {
        }

        public LibraryGridView(Context paramContext, IAttributeSet paramAttributeSet)
            : base(paramContext, paramAttributeSet)
        {
            TypedArray localTypedArray = paramContext.ObtainStyledAttributes(paramAttributeSet, Resource.Styleable.LibraryGridView);
            GridViewParams(localTypedArray);
            localTypedArray.Recycle();
        }

        public LibraryGridView(Context paramContext, IAttributeSet paramAttributeSet, int paramInt)
            : base(paramContext, paramAttributeSet, paramInt)
        {
            TypedArray localTypedArray = paramContext.ObtainStyledAttributes(paramAttributeSet, Resource.Styleable.LibraryGridView, paramInt, 0);
            GridViewParams(localTypedArray);
            localTypedArray.Recycle();
        }

        public void GridViewParams(TypedArray paramTypedArray)
        {
            this.HorizontalSpacing = paramTypedArray.GetDimensionPixelSize(0, 4);
            this.VerticalSpacing = paramTypedArray.GetDimensionPixelSize(1, 4);
            this.NumColumns = paramTypedArray.GetInt(3, 10);
            this.ColumnWidth = paramTypedArray.GetDimensionPixelSize(2, 48);
        }

        public override LibraryGridButtonAdapter Adapter
        {
            get
            {
                return this.adapter;
            }
            set
            {
                this.adapter = value;
                RemoveAllViewsInLayout();
                for (int i = 0; i < this.adapter.Count; i++)
                {
                    View localView = this.adapter.GetView(i, null, this);

                    ViewGroup.LayoutParams localLayoutParams = localView.LayoutParameters;
                    if (localLayoutParams == null)
                    {
                        localLayoutParams = new ViewGroup.LayoutParams(-2, -2);
                    }

                    localView.Clickable = true;
                    localView.SetOnClickListener(new LibraryGridViewListener(this, i));
                    AddViewInLayout(localView, i, localLayoutParams);
                    localView.Measure(GetChildMeasureSpec(View.MeasureSpec.MakeMeasureSpec(this.columnWidth, MeasureSpecMode.AtMost), 0, localLayoutParams.Width), GetChildMeasureSpec(View.MeasureSpec.MakeMeasureSpec(0, 0), 0, localLayoutParams.Height));
                }
                RequestLayout();
            }
        }

        public override Java.Lang.Object GetItemAtPosition(int position)
        {
            return this.adapter.GetItem(position);
        }

        public int NumColumns
        {
            get
            {
                return this.numColumns;
            }
            set
            {
                this.numColumns = value;
                RequestLayout();
            }
        }

        public int ColumnWidth
        {
            get
            {
                return this.columnWidth;
            }
            set
            {
                this.columnWidth = value;
                this.d = false;
            }
        }

        public int HorizontalSpacing
        {
            get
            {
                return this.horizontalSpacing;
            }
            set
            {
                this.horizontalSpacing = value;
                RequestLayout();
            }
        }

        public int VerticalSpacing
        {
            get
            {
                return this.verticalSpacing;
            }
            set
            {
                this.verticalSpacing = value;
                RequestLayout();
            }
        }

        public override View SelectedView
        {
            get
            {
                return null;
            }
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);

            if (this.adapter != null)
            {
                int padLeft = PaddingLeft;
                int padRight = PaddingRight;
                int padTop = PaddingTop;
                int n = 0;
                int i3 = this.columnWidth + this.horizontalSpacing;
                int i2 = 0;
                int j = padLeft;
                while (i2 < ChildCount)
                {
                    View localView = GetChildAt(i2);
                    int i = localView.MeasuredHeight;
                    if (this.numColumns != 0)
                    {
                        if (n >= this.numColumns)
                        {
                            padTop += i + this.verticalSpacing;
                            n = 0;
                            j = padLeft;
                        }
                    }
                    else if (padRight + (j + i3) > right)
                    {
                        padTop += i + this.verticalSpacing;
                        j = padLeft;
                    }
                    localView.Layout(j, padTop, j + this.columnWidth, i + padTop);
                    j += this.columnWidth + this.horizontalSpacing;
                    n++;
                    i2++;
                }
                Invalidate();
            }
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            int k = View.MeasureSpec.GetSize(widthMeasureSpec) - this.horizontalSpacing - PaddingLeft - PaddingRight;
            int i = this.columnWidth + this.horizontalSpacing;
            View localView = GetChildAt(0);
            int j;
            if (localView != null)
            {
                j = localView.MeasuredHeight;
            }
            else
            {
                j = 16;
            }
            int m = ChildCount;
            if (this.numColumns > 0)
            {
                k = Math.Max(this.numColumns, 1);
            }
            else
            {
                k = Math.Max(k / i, 1);
            }
            m = 1 + m / k;
            this.measuredWidth = (k * i + this.horizontalSpacing);
            this.measuredHeight = (m * (j + this.verticalSpacing) + this.verticalSpacing);
            SetMeasuredDimension(this.measuredWidth, this.measuredHeight);
        }

        public override void SetSelection(int position)
        {
            throw new NotImplementedException();
        }
    }

    public class LibraryGridViewListener : Java.Lang.Object, View.IOnClickListener
    {
        LibraryGridView paramLibraryGridView;
        int paramInt;

        public LibraryGridViewListener(LibraryGridView paramLibraryGridView, int paramInt)
        {
            this.paramLibraryGridView = paramLibraryGridView;
            this.paramInt = paramInt;
        }

        public void OnClick(View paramView)
        {
            paramLibraryGridView.PerformItemClick(paramView, paramInt, paramLibraryGridView.GetItemIdAtPosition(paramInt));
        }
    }
}