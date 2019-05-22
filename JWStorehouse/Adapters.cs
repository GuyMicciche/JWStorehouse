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
using Android.Content.PM;
using Android.Text;

namespace JWStorehouse
{
    public class DownloadOptionsGridAdapter : BaseAdapter
    {
        //private System.Collections.Generic.IList<ResolveInfo> pubs;
        string[] pubs;
        private Context context;

        public DownloadOptionsGridAdapter(Context context, string[] pubs)
        {
            this.pubs = pubs;
            this.context = context;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
		{
			CheckableLayout layout;
			//ImageView image;
            TextView pub;

			if (convertView == null)
			{
                //image = new ImageView(context);
                //image.Tag = position.ToString();
                //image.SetScaleType(ImageView.ScaleType.FitCenter);
                //image.LayoutParameters = new ViewGroup.LayoutParams(50, 50);

                pub = new TextView(context);
                pub.Text = pubs[position];
                pub.SetPadding(12, 12, 12, 12);

                layout = new CheckableLayout(context);
                layout.LayoutParameters = new GridView.LayoutParams(GridView.LayoutParams.WrapContent, GridView.LayoutParams.WrapContent);
                layout.AddView(pub);
			}
			else
			{
				layout = (CheckableLayout)convertView;
                pub = (TextView)layout.GetChildAt(0);
				//image = (ImageView)layout.GetChildAt(0);
			}

            //ResolveInfo info = apps[position];
            //image.SetImageDrawable(info.ActivityInfo.LoadIcon(App.STATE.Context.PackageManager));

			return layout;
		}


        public override int Count
        {
            get
            {
                return pubs.Count();
            }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return pubs[position];
        }

        public override long GetItemId(int position)
        {
            return position;
        }
    }

    public class CheckableLayout : FrameLayout, ICheckable
    {
        private Context context;
        private bool isChecked;

        public CheckableLayout(Context context)
            : base(context)
        {
            this.context = context;
        }

        public bool Checked
        {
            set
            {
                isChecked = value;
                SetBackgroundColor(isChecked ? Android.Graphics.Color.Blue : Android.Graphics.Color.Transparent);
            }
            get
            {
                return isChecked;
            }
        }


        public void Toggle()
        {
            Checked = !isChecked;
        }

    }

    public class MultiChoiceModeListener : Java.Lang.Object, GridView.IMultiChoiceModeListener
    {
        private GridView grid;

        public MultiChoiceModeListener(GridView grid)
        {
            this.grid = grid;
        }

        public bool OnCreateActionMode(ActionMode mode, IMenu menu)
        {
            mode.Title = "Select Items";
            SetSubtitle(mode);
            return true;
        }

        public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
        {
            return true;
        }

        public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
        {
            return true;
        }

        public void OnDestroyActionMode(ActionMode mode)
        {
        }

        public void OnItemCheckedStateChanged(ActionMode mode, int position, long id, bool isChecked)
        {
            SetSubtitle(mode);
        }

        private void SetSubtitle(ActionMode mode)
        {
            int checkedCount = grid.CheckedItemCount;
            switch (checkedCount)
            {
                case 0:
                    mode.Subtitle = null;
                    Console.WriteLine("None");
                    break;

                case 1:
                    mode.Subtitle = "One item selected";
                    Console.WriteLine("One item selected");
                    break;

                default:
                    mode.Subtitle = "" + checkedCount + " items selected";
                    Console.WriteLine("" + checkedCount + " items selected");
                    break;
            }
        }

    }

    public class ChapterButtonAdapter : BaseAdapter
    {
        private Activity context;
        private string[] chapters;

        // Gets the context so it can be used later
        public ChapterButtonAdapter(Activity context, string[] chapters)
            : base()
        {
            this.context = context;
            this.chapters = chapters;
        }

        // Total number of things contained within the adapter
        public override int Count
        {
            get { return chapters.Length; }
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

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            TextView button = new TextView(context);
            button.SetText(chapters[position], TextView.BufferType.Normal);
            button.TextSize = 32;
            button.SetHeight(84);
            button.SetBackgroundResource(Resource.Drawable.metro_button_style);
            button.SetTextColor(Android.Graphics.Color.White);
            button.Gravity = GravityFlags.Center;
            button.Id = position;

            button.Click += button_Click;

            return button;
        }

        void button_Click(object sender, EventArgs e)
        {
            string chapter = (sender as TextView).Text;
            NavStruct nav = new NavStruct()
            {
                Book = App.STATE.SelectedArticle.Book,
                Chapter = int.Parse(chapter),
                Verse = 0
            };
            App.STATE.SelectedArticle = nav;
            App.STATE.Language = App.STATE.Language;
        }
    }

    public class PublicationButtonAdapter : BaseAdapter
    {
        private Activity context;
        private ISpanned[] chapters;

        // Gets the context so it can be used later
        public PublicationButtonAdapter(Activity context, ISpanned[] chapters)
            : base()
        {
            this.context = context;
            this.chapters = chapters;
        }

        // Total number of things contained within the adapter
        public override int Count
        {
            get { return chapters.Length; }
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

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            TextView button = new TextView(context);
            button.SetText(chapters[position], TextView.BufferType.Normal);
            button.TextSize = 28;
            button.SetHeight(84);
            button.SetPadding(8, 8, 8, 8);
            button.SetBackgroundResource(Resource.Drawable.metro_button_style);
            button.SetTextColor(Android.Graphics.Color.White);
            button.Gravity = GravityFlags.CenterVertical;
            button.Id = position;

            button.Click += button_Click;

            return button;
        }

        void button_Click(object sender, EventArgs e)
        {
            int index = (sender as TextView).Id;
            NavStruct nav = new NavStruct()
            {
                Book = App.STATE.SelectedArticle.Book,
                Chapter = index + 1,
                Verse = 0
            };
            App.STATE.SelectedArticle = nav;
            App.STATE.Language = App.STATE.Language;
        }
    }

    public class InsightButtonAdapter : BaseAdapter
    {
        private Activity context;
        private ISpanned[] chapters;
        private string[] articles;

        // Gets the context so it can be used later
        public InsightButtonAdapter(Activity context, ISpanned[] chapters, string[] articles)
            : base()
        {
            this.context = context;
            this.chapters = chapters;
            this.articles = articles;
        }

        // Total number of things contained within the adapter
        public override int Count
        {
            get { return chapters.Length; }
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

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            TextView button = new TextView(context);
            button.SetText(chapters[position], TextView.BufferType.Normal);
            button.TextSize = 28;
            button.SetHeight(84);
            button.SetPadding(8, 8, 8, 8);
            button.SetBackgroundResource(Resource.Drawable.metro_button_style);
            button.SetTextColor(Android.Graphics.Color.White);
            button.Gravity = GravityFlags.CenterVertical;
            button.Id = NavStruct.Parse(articles[position]).Chapter;

            button.Click += button_Click;

            return button;
        }

        void button_Click(object sender, EventArgs e)
        {
            int index = (sender as TextView).Id;
            NavStruct nav = new NavStruct()
            {
                Book = App.STATE.SelectedArticle.Book,
                Chapter = index,
                Verse = 0
            };
            App.STATE.SelectedArticle = nav;
            App.STATE.Language = App.STATE.Language;
        }
    }
}