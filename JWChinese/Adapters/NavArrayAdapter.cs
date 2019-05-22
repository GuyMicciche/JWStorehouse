using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.Text;
using Android.Views;
using Android.Widget;

using System.Collections.Generic;

namespace JWChinese
{
    public class NavArrayAdapter<T> : ArrayAdapter
    {
        Activity activity;
        private int viewResourceId;
        private int textViewResourceId;
        public List<T> objects;
        private string font;
        private TypefaceStyle style;
        private bool isLeftNav;

        private TypedArray navMenuIcons;

        public NavArrayAdapter(Activity activity, int viewResourceId, int textViewResourceId, List<T> objects, string font, TypefaceStyle style, bool isLeftNav = false)
            : base(activity, viewResourceId, textViewResourceId, objects.ToArray())
        {
            this.font = font;
            this.style = style;
            this.viewResourceId = viewResourceId;
            this.textViewResourceId = textViewResourceId;
            this.activity = activity;
            this.objects = objects;
            this.isLeftNav = isLeftNav;

            navMenuIcons = activity.Resources.ObtainTypedArray(Resource.Array.nav_drawer_icons);
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            NavHolder holder = null;

            if (view == null)
            {
                LayoutInflater inflater = ((Activity)activity).LayoutInflater;
                view = inflater.Inflate(viewResourceId, parent, false);

                holder = new NavHolder();
                holder.Nav = (TextView)view.FindViewById(textViewResourceId);

                view.Tag = holder;
            }
            else
            {
                holder = (NavHolder)view.Tag;
            }

            if (isLeftNav)
            {
                holder.Nav.SetCompoundDrawablesWithIntrinsicBounds(navMenuIcons.GetDrawable(position), null, null, null);
            }

            Typeface face = Typeface.CreateFromAsset(App.STATE.Activity.Assets, "fonts/" + font + ".ttf");
            holder.Nav.SetText(Html.FromHtml(objects[position].ToString().Replace("\n", "<br/>")), TextView.BufferType.Normal);
            holder.Nav.SetTypeface(face, style);

            if (App.STATE.Language.Contains("Pinyin"))
            {
                holder.Nav.Gravity = GravityFlags.Center;
            }

            return view;
        }
        
        class NavHolder : Java.Lang.Object
        {
            public TextView Nav;
            public ImageView Image;
        }
    }
}