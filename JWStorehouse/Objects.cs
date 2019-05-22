using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Text.Style;
using Android.Webkit;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Android.Util;
using Android.App;
using Android.Views;

namespace JWStorehouse
{
    /// <summary>
    /// Language node holds language data
    /// </summary>
    public class Language
    {
        public string R { get; set; }
        public string Lp { get; set; }
        public string EnglishName { get; set; }
        public string LanguageName { get; set; }
        public string Name { get; set; }
    }   

    /// <summary>
    /// Publication node holds publication info
    /// </summary>
    public class Publication
    {
        public int Page { get; set; }
        public int PageMax { get; set; }
    }

    /// <summary>
    /// BibleBook node holds Bible book data
    /// </summary>
    public class BibleBook
    {
        public string Number { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
    }

    /// <summary>
    /// Article node holds articles of publications
    /// </summary>
    public class WOLArticle
    {
        public string Storehouse { get; set; }
        public string PublicationCode { get; set; }
        public string PublicationName { get; set; }
        public string PublicationNamePinyin { get; set; }
        public string PublicationURL { get; set; }
        public string ArticleMEPSID { get; set; }
        public string ArticleURL { get; set; }
        public string ArticleTitle { get; set; }
        public string ArticleTitlePinyin { get; set; }
        public string ArticleLocation { get; set; }
        public string ArticleContent { get; set; }
        public string ArticleGroup { get; set; }
    }

    /// <summary>
    /// InsightArticle node holds articles of insight publication
    /// </summary>
    public class InsightArticle
    {
        public string Group { get; set; }
        public string Title { get; set; }
        public string MEPSID { get; set; }
        public int OrderNumber { get; set; }
    }

    public static class ArticleType
    {
        public static string Bible
        {
            get
            {
                return "b";
            }
        }
        public static string DailyText
        {
            get
            {
                return "dt";
            }
        }
        public static string Publication
        {
            get
            {
                return "d";
            }
        }
    }

    public static class PublicationType
    {
        public static string Bible
        {
            get
            {
                return "nwt";
            }
        }
        public static string Insight
        {
            get
            {
                return "it";
            }
        }
        public static string DailyText
        {
            get
            {
                return "es";
            }
        }
        public static string Books
        {
            get
            {
                return "books";
            }
        }
        //public static string Brochures
        //{
        //    get
        //    {
        //        return "brochures";
        //    }
        //}
        //public static string Tracts
        //{
        //    get
        //    {
        //        return "tracts";
        //    }
        //}
        //public static string Watchtower
        //{
        //    get
        //    {
        //        return "watchtower";
        //    }
        //}
        //public static string Awake
        //{
        //    get
        //    {
        //        return "awake";
        //    }
        //}
        //public static string Yearbooks
        //{
        //    get
        //    {
        //        return "yearbooks";
        //    }
        //}
    }   

    public enum Library
    {
        None = -1,
        Bible = 0,
        Insight = 1,
        DailyText = 2,
        Books = 3,
        Brochures = 4,
        Tracts = 5,
        Watchtower = 6,
        Awake = 7,
        Yearbooks = 8,
    };

    public enum ScreenMode
    {
        Duel = 0,
        Navigation = 1,
        Single = 2,
    };

    public struct NavStruct
    {
        public int Book;
        public int Chapter;
        public int Verse;

        public NavStruct(int book, int chapter, int verse)
        {
            this.Book = book;
            this.Chapter = chapter;
            this.Verse = verse;
        }

        public static NavStruct Parse(string verse)
        {
            // In this format:  1.1.1
            int num;
            int num1;
            int num2;
            char[] chrArray = new char[] { '.' };
            string[] strArrays = verse.Split(chrArray);
            if ((int)strArrays.Length != 3)
            {
                throw new ArgumentException("Invalid format of Verse string");
            }
            if (!int.TryParse(strArrays[0], out num) || !int.TryParse(strArrays[1], out num1) || !int.TryParse(strArrays[2], out num2))
            {
                throw new ArgumentException("Invalid format of Verse string");
            }
            return new NavStruct(num, num1, num2);
        }

        public static NavStruct BibleParse(string articleCode)
        {
            // In this format:  nwt/E/2013/1/1
            int num;
            int num1;
            char[] chrArray = new char[] { '/' };
            string[] strArrays = articleCode.Split(chrArray);
            if ((int)strArrays.Length != 5)
            {
                throw new ArgumentException("Invalid format of Verse string");
            }
            if (!int.TryParse(strArrays[3], out num) || !int.TryParse(strArrays[4], out num1))
            {
                throw new ArgumentException("Invalid format of Verse string");
            }
            return new NavStruct(num, num1, 0);
        }

        public static NavStruct DailyTextParse(string date)
        {
            // In this format:  2014/1/1
            int num;
            int num1;
            int num2;
            char[] chrArray = new char[] { '/' };
            string[] strArrays = date.Split(chrArray);
            if ((int)strArrays.Length != 3)
            {
                throw new ArgumentException("Invalid format of Verse string");
            }
            if (!int.TryParse(strArrays[0], out num) || !int.TryParse(strArrays[1], out num1) || !int.TryParse(strArrays[2], out num2))
            {
                throw new ArgumentException("Invalid format of Verse string");
            }
            return new NavStruct(num, num1, num2);
        }

        public static NavStruct SuperParse(int book, int chapter, int verse = 0)
        {
            return new NavStruct(book, chapter, verse);
        }

        public static NavStruct InsightParse(int group, int article)
        {
            return new NavStruct(group, article, 0);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", this.Book, this.Chapter, this.Verse);
        }
    }

    public class SortByPublicationCode : IComparer<WOLArticle>
    {
        public int Compare(WOLArticle a, WOLArticle b)
        {
            if (int.Parse(a.PublicationCode) > int.Parse(b.PublicationCode)) return 1;
            else if (int.Parse(a.PublicationCode) < int.Parse(b.PublicationCode)) return -1;
            else return 0;
        }
    }

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

    public class StorehouseDialogBuilder : AlertDialog.Builder
    {
        private Context context;

        private TextView title = null;
        private TextView message = null;
        private ImageView icon = null;

        public StorehouseDialogBuilder(Context context)
            : base(context)
	    {

            this.context = context;

		    View customTitle = View.Inflate(context, Resource.Layout.DialogTitle, null);
		    title = customTitle.FindViewById<TextView>(Resource.Id.alertTitle);
		    icon = customTitle.FindViewById<ImageView>(Resource.Id.icon);
		    SetCustomTitle(customTitle);
 
		    View customMessage = View.Inflate(context, Resource.Layout.DialogMessage, null);
		    message = customMessage.FindViewById<TextView>(Resource.Id.message);
		    SetView(customMessage);

            Typeface face = Typeface.CreateFromAsset(context.Assets, "fonts/Roboto-Regular.ttf");
            title.SetTypeface(face, TypefaceStyle.Normal);
            face = Typeface.CreateFromAsset(context.Assets, "fonts/Roboto-Light.ttf");
            message.SetTypeface(face, TypefaceStyle.Normal);
	    }

        public void SetTitleBackgroundColor(int resId)
        {
            title.SetBackgroundResource(resId);
        }

        public void SetTitleTextColor(int resId)
        {
            title.SetTextColor(Color.White);
        }

        public override AlertDialog.Builder SetTitle(int textResId)
        {
            title.SetText(textResId);
            return this;
        }

        public override AlertDialog.Builder SetTitle(Java.Lang.ICharSequence text)
        {
            title.SetText(text, TextView.BufferType.Normal);
            return this;
        }

        public override AlertDialog.Builder SetMessage(int textResId)
        {
            message.SetText(textResId);
            return this;
        }

        public override AlertDialog.Builder SetMessage(Java.Lang.ICharSequence text)
        {
            message.SetText(text, TextView.BufferType.Normal);
            return this;
        }

        public override AlertDialog.Builder SetIcon(int drawableResId)
        {
            this.icon.SetImageResource(drawableResId);
            return this;
        }

        public override AlertDialog.Builder SetIcon(Android.Graphics.Drawables.Drawable icon)
        {
            this.icon.SetImageDrawable(icon);
            return this;
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