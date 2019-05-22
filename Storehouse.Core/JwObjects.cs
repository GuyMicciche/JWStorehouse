using SQLite;

using System;
using System.Collections.Generic;
using System.Text;

namespace Storehouse.Core
{
    /// <summary>
    /// Article node holds articles of publications
    /// </summary>
    public class WOLArticle
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string PublicationCode { get; set; }
        public string PublicationName { get; set; }
        public string ArticleTitle { get; set; }
        public string ArticleNumber { get; set; }
        public string ArticleLocation { get; set; }
        public string ArticleContent { get; set; }
        public string ArticleGroup { get; set; }
        public string PublicationNameShort { get; set; }
        public string PublicationAbbreviation { get; set; }
    }

    public class LearningCharacter
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Chinese { get; set; }
        public string English { get; set; }
        public string Pinyin { get; set; }
    }

    public class Highlight
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string SelectedText { get; set; }
        public int Begin { get; set; }
        public int End { get; set; }
        public string PublicationCode { get; set; }
        public string ArticleNumber { get; set; }
    }

    /// <summary>
    /// WOLPublication node holds publication data
    /// </summary>
    public class WOLPublication
    {
        public string EnglishName { get; set; }
        public string EnglishNameShort { get; set; }
        public string ChineseName { get; set; }
        public string ChineseNameShort { get; set; }
        public string PinyinName { get; set; }
        public string PinyinNameShort { get; set; }
        public string PinyinChineseName { get; set; }
        public string PinyinChineseNameShort { get; set; }
        public string Code { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }
        public string Image { get; set; }
    }


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
    /// BibleBook node holds Bible book data
    /// </summary>
    public class BibleBook
    {
        public string Number { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Abbreviation { get; set; }
    }

    /// <summary>
    /// InsightArticle node holds articles of insight publication
    /// </summary>
    public class InsightArticle
    {
        public string Group { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
    }

    /// <summary>
    /// PublicationType node holds code of publication
    /// </summary>
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
        public static string Publication
        {
            get
            {
                return "pub";
            }
        }
    }

    public enum Storehouse
    {
        English,
        Chinese,
        Pinyin,
    };

    public enum Library
    {
        None = -1,
        Bible = 0,
        Insight = 1,
        DailyText = 2,
        Publications = 3,
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

    /// <summary>
    /// NavStruct node holds navigation formating
    /// </summary>
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

        public static NavStruct Parse(string nav)
        {
            // In this format:  1.1.1
            int book;
            int chapter;
            int verse;

            char[] chrArray = new char[] { '.' };
            string[] strArrays = nav.Split(chrArray);

            if ((int)strArrays.Length != 3)
            {
                throw new ArgumentException("Invalid format of Verse string");
            }
            if (!int.TryParse(strArrays[0], out book) || !int.TryParse(strArrays[1], out chapter) || !int.TryParse(strArrays[2], out verse))
            {
                throw new ArgumentException("Invalid format of Verse string");
            }

            return new NavStruct(book, chapter, verse);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", this.Book, this.Chapter, this.Verse);
        }
    }
}
