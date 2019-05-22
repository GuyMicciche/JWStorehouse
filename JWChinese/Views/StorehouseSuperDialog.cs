using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;

using Java.Lang;

using System;

namespace JWChinese
{
    /// <summary>
    /// Advanced custom JW Chinese AlertDialog
    /// </summary>
    public class StorehouseSuperDialog : AlertDialog.Builder
    {
        private Context context;

        private TextView title;
        private TextView message;
        private ImageView icon;

        private View customTitle;
        private View customView;

        public StorehouseSuperDialog(Context context)
            : base(context)
        {
            this.context = context;
            Init();
        }

        public StorehouseSuperDialog(Context context, int theme)
            : base(context, theme)
        {
            this.context = context;
            Init();
        }

        private void Init()
        {
            customTitle = View.Inflate(context, Resource.Layout.DialogTitle, null);
            title = customTitle.FindViewById<TextView>(Resource.Id.alertTitle);
            icon = customTitle.FindViewById<ImageView>(Resource.Id.icon);

            customView = View.Inflate(context, Resource.Layout.DialogMessage, null);
            message = customView.FindViewById<TextView>(Resource.Id.message);            

            Typeface face = Typeface.CreateFromAsset(context.Assets, "fonts/Roboto-Regular.ttf");
            title.SetTypeface(face, TypefaceStyle.Normal);
            face = Typeface.CreateFromAsset(context.Assets, "fonts/Roboto-Light.ttf");
            message.SetTypeface(face, TypefaceStyle.Normal);

            SetCustomTitle(customTitle);
            SetView(customView);
        }

        //public override AlertDialog Create()
        //{
        //    AlertDialog dialog = base.Create();

        //    dialog.SetCustomTitle(customTitle);
        //    dialog.SetView(customView);

        //    return dialog;
        //}

        public override AlertDialog Show()
        {
            AlertDialog dialog = base.Show();

            dialog.GetButton((int)DialogButtonType.Positive).SetBackgroundResource(Resource.Drawable.metro_abs_selectablelistitem_style);
            dialog.GetButton((int)DialogButtonType.Negative).SetBackgroundResource(Resource.Drawable.metro_abs_selectablelistitem_style);

            try
            {
                // Title divider
                int id = context.Resources.GetIdentifier("titleDivider", "id", "android");
                View view = dialog.FindViewById(id);
                view.SetBackgroundColor(context.Resources.GetColor(Resource.Color.storehouse_blue_dark));
                
                // Title divider top
                id = context.Resources.GetIdentifier("titleDividerTop", "id", "android");
                view = dialog.FindViewById(id);
                view.SetBackgroundColor(context.Resources.GetColor(Resource.Color.storehouse_blue_dark));

                // Custom panel
                id = context.Resources.GetIdentifier("customPanel", "id", "android");
                view = dialog.FindViewById(id);
                view.SetMinimumHeight(0);
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return dialog;
        }
        
        public void SetTitleBackgroundColor(int resId)
        {
            title.SetBackgroundResource(resId);
        }

        public void SetTitleTextColor(Color color)
        {
            title.SetTextColor(color);
        }

        public void SetTitleTextSize(float size)
        {
            title.TextSize = size;
        }

        public void SetMessageBackgroundColor(int resId)
        {
            message.SetBackgroundResource(resId);
        }

        public void SetMessageTextColor(Color color)
        {
            message.SetTextColor(color);
        }

        public void SetMessageTextSize(float size)
        {
            message.TextSize = size;
        }

        public override AlertDialog.Builder SetTitle(int textResId)
        {
            title.SetText(textResId);
            return this;
        }

        public override AlertDialog.Builder SetTitle(ICharSequence text)
        {
            title.SetText(text, TextView.BufferType.Normal);
            return this;
        }

        public override AlertDialog.Builder SetMessage(int textResId)
        {
            message.SetText(textResId);
            return this;
        }

        public override AlertDialog.Builder SetMessage(ICharSequence text)
        {
            message.SetText(text, TextView.BufferType.Normal);
            return this;
        }

        public override AlertDialog.Builder SetIcon(int drawableResId)
        {
            icon.SetImageResource(drawableResId);
            return this;
        }

        public override AlertDialog.Builder SetIcon(Drawable drawable)
        {
            icon.SetImageDrawable(drawable);
            return this;
        }

        public override AlertDialog.Builder SetView(View view)
        {
            return base.SetView(view);
        }
    }
}