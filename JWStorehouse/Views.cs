using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Content.Res;
using Android.Util;

namespace JWStorehouse
{
    public class LibraryGridView : AdapterView
    {
        public IListAdapter adapter = null;
        public int columnWidth = 48;
        public int numColumns = 10;
        public bool d = true;
        public int horizontalSpacing = 4;
        public int verticalSpacing = 4;
        public int measuredWidth = 0;
        public int measuredHeight = 0;

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

        public void SetAdapter(ArrayAdapter adapter)
        {
            RawAdapter = adapter;
        }

        protected override Java.Lang.Object RawAdapter
        {
            get
            {
                return this.adapter.JavaCast<Java.Lang.Object>();
            }
            set
            {
                this.adapter = value.JavaCast<global::Android.Widget.IListAdapter>();
                RemoveAllViewsInLayout();
                for (int i = 0; i < ((IListAdapter)this.adapter).Count; i++)
                {
                    View localView = ((IListAdapter)this.adapter).GetView(i, null, this);

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
                int i1 = PaddingLeft;
                int k = PaddingRight;
                int m = PaddingTop;
                int n = 0;
                int i3 = this.columnWidth + this.horizontalSpacing;
                int i2 = 0;
                int j = i1;
                while (i2 < ChildCount)
                {
                    View localView = GetChildAt(i2);
                    int i = localView.MeasuredHeight;
                    if (this.numColumns != 0)
                    {
                        if (n >= this.numColumns)
                        {
                            m += i + this.verticalSpacing;
                            n = 0;
                            j = i1;
                        }
                    }
                    else if (k + (j + i3) > right)
                    {
                        m += i + this.verticalSpacing;
                        j = i1;
                    }
                    localView.Layout(j, m, j + this.columnWidth, i + m);
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

    public class ExpandableHeightGridView : GridView
    {
        bool expanded = false;

        public ExpandableHeightGridView(Context context)
            : base(context)
        {
        }

        public ExpandableHeightGridView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
        }

        public ExpandableHeightGridView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
        }

        public bool IsExpanded()
        {
            return expanded;
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            // HACK! TAKE THAT ANDROID!
            if (IsExpanded())
            {
                // Calculate entire height by providing a very large height hint.
                // View.MEASURED_SIZE_MASK represents the largest height possible.
                int expandSpec = MeasureSpec.MakeMeasureSpec(MeasuredSizeMask, MeasureSpecMode.AtMost);
                base.OnMeasure(widthMeasureSpec, expandSpec);

                LayoutParameters.Height = MeasuredHeight;
            }
            else
            {
                base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            }
        }

        public void SetExpanded(bool expanded)
        {
            this.expanded = expanded;
        }
    }

    public class ExpandableHeightListView : ListView
    {
        bool expanded = false;

        public ExpandableHeightListView(Context context)
            : base(context)
        {
        }

        public ExpandableHeightListView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
        }

        public ExpandableHeightListView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
        }

        public bool IsExpanded()
        {
            return expanded;
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            // HACK! TAKE THAT ANDROID!
            if (IsExpanded())
            {
                // Calculate entire height by providing a very large height hint.
                // View.MEASURED_SIZE_MASK represents the largest height possible.
                int expandSpec = MeasureSpec.MakeMeasureSpec(MeasuredSizeMask, MeasureSpecMode.AtMost);
                base.OnMeasure(widthMeasureSpec, expandSpec);

                LayoutParameters.Height = MeasuredHeight;
            }
            else
            {
                base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            }
        }

        public bool Expanded
        {
            get
            {
                return this.expanded;
            }
            set
            {
                this.expanded = value;
            }
        }
    }
}