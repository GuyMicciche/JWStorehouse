using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;

using System;
using System.Collections.Generic;

namespace JWChinese
{
    public class LibraryGridButtonAdapter : BaseAdapter
    {
        private Activity activity;
        private List<string> items;

        public LibraryGridButtonAdapter(Activity activity, List<string> items)
            : base()
        {
            this.activity = activity;
            this.items = items;
        }

        public override int Count
        {
            get { return items.Count; }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return items[position];
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public void Add(List<string> items)
        {
            this.items = items;
            NotifyDataSetChanged();
        }

        public void Clear()
        {
            items = new List<string>();
        }

        public override void NotifyDataSetChanged()
        {
            activity.RunOnUiThread(new Action(() =>
            {
                base.NotifyDataSetChanged();
            }));
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            TextView view;

            if (convertView == null)
            {
                int height = activity.Resources.GetDimensionPixelSize(Resource.Dimension.bible_nav_bible_book_grid_height);
                if(App.STATE.Language.Contains("Pinyin"))
                {
                    height = activity.Resources.GetDimensionPixelSize(Resource.Dimension.bible_nav_chapter_grid_height);
                }

                int textSize = activity.Resources.GetDimensionPixelSize(Resource.Dimension.bible_nav_bible_book_text_size);
                int padding = activity.Resources.GetDimensionPixelSize(Resource.Dimension.bible_nav_bible_book_grid_cell_padding);

                view = new TextView(activity);
                view.SetTextSize(0, textSize);
                view.SetHeight(height);
                view.SetTextColor(activity.Resources.GetColorStateList(Resource.Color.metro_button_text_style));
                view.SetPadding(padding, 0, padding, 0);
                view.SetBackgroundResource(Resource.Drawable.metro_style);
                view.Gravity = GravityFlags.Center;

                Typeface face = Typeface.CreateFromAsset(activity.Assets, "fonts/Roboto-Regular.ttf");
                view.SetTypeface(face, TypefaceStyle.Normal);
            }
            else
            {
                view = (TextView)convertView;
            }

            view.SetText(items[position], TextView.BufferType.Normal);

            return view;
        }
    }
}