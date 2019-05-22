using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;

namespace JWChinese
{
    /// <summary>
    /// LibraryDatabase node holds the path for library databases
    /// </summary>
    public static class LibraryStorehouse
    {
        public static string English
        {
            get
            {
                return App.EnglishDatabasePath;
            }
        }
        public static string Chinese
        {
            get
            {
                return App.ChineseDatabasePath;
            }
        }
        public static string Pinyin
        {
            get
            {
                return App.PinyinDatabasePath;
            }
        }
    }
    
    /// <summary>
    /// Simple custom JW Chinese AlertDialog
    /// </summary>
    public class StorehouseDialog : AlertDialog
    {
        public StorehouseDialog(Context context, View view)
            : base(context)
        {
            RequestWindowFeature(9);
            SetContentView(view);
            Window.DecorView.SetBackgroundResource(Resource.Color.storehouse_blue);
        }
    }

    /// <summary>
    /// Custom JW Chinese TypefaceSpan
    /// </summary>
    public class CustomTypefaceSpan : TypefaceSpan
    {
        private readonly Typeface newType;

        public CustomTypefaceSpan(string family, Typeface type)
            : base(family)
        {
            newType = type;
        }

        public override void UpdateDrawState(TextPaint ds)
        {
            ApplyCustomTypeFace(ds, newType);
        }

        public override void UpdateMeasureState(TextPaint paint)
        {
            ApplyCustomTypeFace(paint, newType);
        }

        private static void ApplyCustomTypeFace(Paint paint, Typeface tf)
        {
            int oldStyle;

            Typeface old = paint.Typeface;

            if (old == null)
            {
                oldStyle = 0;
            }
            else
            {
                oldStyle = (int)old.Style;
            }

            paint.SetTypeface(tf);
        }
    }
}