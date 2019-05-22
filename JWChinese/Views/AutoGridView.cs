using Android.Content;
using Android.Content.Res;
using Android.Util;
using Android.Widget;

using System;

namespace JWChinese
{
    public class AutoGridView : GridView
    {
        private int numColumnsID;
        private int previousFirstVisible;
        private int numColumns;

        public AutoGridView(Context context)
            : base(context)
        {

        }

        public AutoGridView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Init(attrs);
        }

        public AutoGridView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Init(attrs);
        }

        private void Init(IAttributeSet attrs)
        {
            int count = attrs.AttributeCount;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    string name = attrs.GetAttributeName(i);
                    if (name != null && name.Equals("numColumns"))
                    {
                        this.numColumnsID = attrs.GetAttributeResourceValue(i, 1);
                        UpdateColumns();
                        break;
                    }
                }
            }

            Console.WriteLine("numColumns set to: " + NumColumns);
        }

        private void UpdateColumns()
        {
            try
            {
                this.numColumns = Context.Resources.GetInteger(numColumnsID);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public override int NumColumns
        {
            get
            {
                return this.numColumns;
            }
            set
            {
                this.numColumns = value;
                base.NumColumns = value;

                SetSelection(previousFirstVisible);
            }
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);

            SetHeights();
        }

        protected override void OnConfigurationChanged(Configuration newConfig)
        {
            UpdateColumns();
            SetNumColumns(this.numColumns);
        }

        protected override void OnScrollChanged(int l, int t, int oldl, int oldt)
        {
            int firstVisible = FirstVisiblePosition;
            if (previousFirstVisible != firstVisible)
            {
                previousFirstVisible = firstVisible;
                SetHeights();
            }

            base.OnScrollChanged(l, t, oldl, oldt);
        }

        private void SetHeights()
        {
            try
            {
                IListAdapter adapter = Adapter;

                if ((adapter != null) && (numColumns > 0))
                {
                    //Console.WriteLine("ChildCount -> " + ChildCount + " NumColumns -> " + numColumns);

                    for (int i = 0; i < ChildCount; i += numColumns)
                    {
                        // Determine the maximum height for this row
                        int maxHeight = 0;
                        for (int j = i; j < (i + numColumns); j++)
                        {
                            TextView view = (TextView)GetChildAt(j);
                            if (view != null && view.Height > maxHeight)
                            {
                                maxHeight = view.Height;
                            }
                        }
                        // Set max height for each element in this row
                        if (maxHeight > 0)
                        {
                            for (int j = i; j < (i + numColumns); j++)
                            {
                                TextView view = (TextView)GetChildAt(j);
                                if (view != null && view.Height != maxHeight)
                                {
                                    view.SetHeight(maxHeight);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}