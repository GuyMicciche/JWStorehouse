using Android.Content;
using Android.Util;
using Android.Widget;

using Storehouse.Core;

using System;

namespace JWChinese
{
    public class AutoMeasureGridView : GridView
    {
        private int numColumnsID;
        private int numColumns = 1;

        public AutoMeasureGridView(Context context)
            : base(context)
        {

        }

        public AutoMeasureGridView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {

        }

        public AutoMeasureGridView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {

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
            }
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            if (changed)
            {
                if (NumColumns > 1 && App.STATE.CurrentLibrary != Library.Bible)
                {
                    Console.WriteLine("AutoMeasureGridView OnLayout");

                    ArticleButtonAdapter adapter = (ArticleButtonAdapter)Adapter;
                    GridViewItemLayout.InitItemLayout(NumColumns, adapter.Count);

                    int columnWidth = MeasuredWidth / NumColumns;
                    adapter.MeasureItems(columnWidth);
                }

            }
            base.OnLayout(changed, left, top, right, bottom);
        }
    }
}