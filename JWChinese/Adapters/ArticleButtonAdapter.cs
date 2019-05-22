using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;

using Storehouse.Core;

using System;

namespace JWChinese
{
    public class ArticleButtonAdapter : BaseAdapter
    {
        private Activity context;
        private ISpanned[] articles;
        private int[] maxRowHeight;

        // Gets the context so it can be used later
        public ArticleButtonAdapter(Activity context, ISpanned[] articles)
            : base()
        {
            this.context = context;
            this.articles = articles;

            this.maxRowHeight = new int[articles.Length];
        }

        // Total number of things contained within the adapter
        public override int Count
        {
            get { return articles.Length; }
        }

        // Require for structure, not really used in my code.
        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }

        // Require for structure, not really used in my code. Can be used to get the id of an item in the adapter for manual control.
        public override long GetItemId(int position)
        {
            return position;
        }

        // Use this with AutoMeasureGridView
        public void MeasureItems(int columnWidth)
        {
            // Create measuring specs
            int widthMeasureSpec = View.MeasureSpec.MakeMeasureSpec(columnWidth, MeasureSpecMode.Exactly);
            int heightMeasureSpec = View.MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified);

            GridViewItemLayout view = new GridViewItemLayout(context);

            // Loop through each data object
            for (int index = 0; index < Count; index++)
            {
                view.SetPosition(index);
                view.UpdateItemDisplay(articles[index], index);

                // Force measuring
                view.RequestLayout();
                view.Measure(widthMeasureSpec, heightMeasureSpec);
            }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            TextView view;

            if (convertView != null)
            {
                view = (TextView)convertView;
            }
            else
            {
                Typeface face = Typeface.CreateFromAsset(context.Assets, "fonts/Roboto-Light.ttf");

                int height = context.Resources.GetDimensionPixelSize(Resource.Dimension.bible_nav_chapter_grid_height);
                int padding = context.Resources.GetDimensionPixelSize(Resource.Dimension.bible_nav_bible_book_grid_cell_padding);

                int textSize = context.Resources.GetDimensionPixelSize(Resource.Dimension.bible_nav_bible_book_text_size);
                if (App.STATE.CurrentLibrary == Library.Bible)
                {
                    textSize = context.Resources.GetDimensionPixelSize(Resource.Dimension.bible_nav_chapter_grid_text_size);
                }

                view = new TextView(context);
                //view.SetTextSize(0, context.Resources.GetDimensionPixelSize(Resource.Dimension.bible_nav_chapter_grid_text_size));
                view.SetTextSize(0, textSize);
                view.SetTextColor(context.Resources.GetColorStateList(Resource.Color.metro_button_text_style_inverse));
                view.SetTypeface(face, TypefaceStyle.Normal);
                view.SetBackgroundResource(Resource.Drawable.metro_style_inverse);
                view.Gravity = GravityFlags.Center;

                if (App.STATE.CurrentLibrary == Library.Bible)
                {
                    view.SetHeight(height);
                }
                else
                {
                    view.SetPadding(padding, padding, padding, padding);
                }
            }

            view.SetText(articles[position], TextView.BufferType.Normal);

            return view;
        }        
    }

    public class GridViewItemLayout : TextView
    {
        private static int[] MaxRowHeight;
        private static int NumColumns;
        private int Position;

        public GridViewItemLayout(Context context)
            : base(context)
        {

        }

        public GridViewItemLayout(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
        }

        public GridViewItemLayout(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
        }

        public void SetPosition(int position)
        {
            this.Position = position;
        }


        public static void InitItemLayout(int numColumns, int itemCount)
        {
            NumColumns = numColumns;
            MaxRowHeight = new int[itemCount];
        }

        public void UpdateItemDisplay(ISpanned text, int id)
        {
            try
            {
                this.SetText(text, TextView.BufferType.Normal);
                this.Id = id;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);


            if (NumColumns <= 1 || MaxRowHeight == null || App.STATE.CurrentLibrary == Library.Bible)
            {
                return;
            }

            Console.WriteLine("GridViewItemLayout OnMeasure");

            // Get the current view cell index for the grid row
            int rowIndex = Position / NumColumns;

            // If the current height is larger than previous measurements, update the array
            if (MeasuredHeight > MaxRowHeight[rowIndex])
            {
                MaxRowHeight[rowIndex] = MeasuredHeight;
            }

            // Update the dimensions of the layout to reflect the max height
            SetMeasuredDimension(MeasuredWidth, MaxRowHeight[rowIndex]);
        }
    }
}