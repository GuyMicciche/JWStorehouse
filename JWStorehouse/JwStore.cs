using Android.Content;
using Android.Database;
using Android.Database.Sqlite;
using Android.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JWStorehouse
{
    public class JwStore
    {
        public SQLiteDatabase Database;

        private DatabaseHelper Helper;
        private string Type;

        public static string KeyPublicationCode = "PublicationCode";
        public static string KeyPublicationName = "PublicationName";
        public static string KeyArticleTitle = "ArticleTitle";
        public static string KeyArticleMEPSID = "ArticleNumber";
        public static string KeyArticleLocation = "ArticleLocation";
        public static string KeyArticleContent = "ArticleContent";
        public static string KeyArticleGroup = "ArticleGroup";
        public static string KeyArticleURL = "ArticleURL";


        public JwStore(string storehouse)
        {
            this.Type = storehouse;
        }

        public JwStore Open()
        {
            this.Helper = new DatabaseHelper(App.STATE.Context, Type, 2);
            this.Database = this.Helper.WritableDatabase;

            return this;
        }

        public void Close()
        {
            this.Helper.Close();
        }

        public void DeleteAllTables()
        {
            Helper.OnUpgrade(Database, 2, 2);
        }

        public long AddToLibrary(WOLArticle article)
        {
            var initialValues = new ContentValues();
            initialValues.Put(KeyPublicationCode, (article.PublicationCode != null) ? article.PublicationCode : "");
            initialValues.Put(KeyPublicationName, (article.PublicationName != null) ? article.PublicationName : "");
            initialValues.Put(KeyArticleTitle, article.ArticleTitle);
            initialValues.Put(KeyArticleMEPSID, article.ArticleMEPSID);
            initialValues.Put(KeyArticleLocation, article.ArticleLocation);
            initialValues.Put(KeyArticleContent, article.ArticleContent);
            initialValues.Put(KeyArticleGroup, article.ArticleGroup);
            initialValues.Put(KeyArticleURL, article.ArticleURL);

            return this.Database.Insert(Type, null, initialValues);
        }

        /// <summary>
        /// Get a Bible chapter from JwStore
        /// </summary>
        /// <param name="meps">MEPS Id</param>
        /// <returns>ICursor Bible chapter</returns>
        public ICursor QueryBible(NavStruct meps)
        {        
            ICursor cursor = this.Database.Query(
                true, Type,
                new[] { KeyPublicationCode, KeyPublicationName, KeyArticleTitle, KeyArticleMEPSID, KeyArticleLocation, KeyArticleContent, KeyArticleGroup },
                KeyPublicationCode + "=? AND " + KeyArticleMEPSID + "=?",
                new string[] { "nwt", meps.ToString() },
                null, null, "_id", null);

            if (cursor != null)
            {
                cursor.MoveToFirst();
            }
            return cursor;
        }

        /// <summary>
        /// Get a Daily Text day from JwStore
        /// </summary>
        /// <param name="day">Day</param>
        /// <returns>ICursor Daily Text day</returns>
        public ICursor QueryDailyText(string day)
        {
            ICursor cursor = this.Database.Query(
                true, Type,
                new[] { KeyPublicationCode, KeyPublicationName, KeyArticleTitle, KeyArticleMEPSID, KeyArticleLocation, KeyArticleContent, KeyArticleGroup },
                KeyArticleMEPSID + "=?",
                new string[] { day },
                null, null, "_id", null);

            if (cursor != null)
            {
                cursor.MoveToFirst();
            }
            return cursor;
        }

        /// <summary>
        /// Get an Insight article from JwStore
        /// </summary>
        /// <param name="meps">MEPS Id</param>
        /// <returns>ICursor Insight article</returns>
        public ICursor QueryInsight(NavStruct meps)
        {
            ICursor cursor = this.Database.Query(
                true, Type,
                new[] { KeyPublicationCode, KeyPublicationName, KeyArticleTitle, KeyArticleMEPSID, KeyArticleLocation, KeyArticleContent, KeyArticleGroup },
                KeyPublicationCode + "=? AND " + KeyArticleMEPSID + " LIKE ?",
                new string[] {"it", "%" + meps.Chapter.ToString() + "%" },
                null, null, "_id", null);

            if (cursor != null)
            {
                cursor.MoveToFirst();
            }
            return cursor;
        }

        public ICursor QueryAllInsightsByGroup(NavStruct meps)
        {
            string group = "it-" + meps.Book.ToString();

            ICursor cursor = this.Database.Query(
                true, Type,
                new[] { KeyPublicationCode, KeyPublicationName, KeyArticleTitle, KeyArticleMEPSID, KeyArticleLocation, KeyArticleContent, KeyArticleGroup },
                KeyPublicationCode + "=? AND " + KeyArticleGroup + "=?",
                new string[] { "it", group },
                null, null, "_id", null);

            if (cursor != null)
            {
                cursor.MoveToFirst();
            }
            return cursor;
        }

        /// <summary>
        /// Get a Publication chapter from JwStore
        /// </summary>
        /// <param name="meps">MEPS Id</param>
        /// <returns>ICursor Publication chapter</returns>
        public ICursor QueryPublication(NavStruct meps)
        {
            ICursor cursor = this.Database.Query(
                true, Type,
                new[] { KeyPublicationCode, KeyPublicationName, KeyArticleTitle, KeyArticleMEPSID, KeyArticleLocation, KeyArticleContent, KeyArticleGroup },
                KeyPublicationCode + "!=? AND " + KeyPublicationCode + "!=? AND " + KeyPublicationCode + "!=? AND " + KeyArticleMEPSID + "=?",
                new string[] { "nwt", "it", "es", meps.ToString() },
                null, null, "_id", null);

            if (cursor != null)
            {
                cursor.MoveToFirst();
            }
            return cursor;
        }

        public ICursor QueryMatchingArticles(List<string> articles)
        {
            string selection = "SELECT * FROM " + Type + " WHERE " + KeyPublicationCode + "='it' AND";
            string[] selectionArgs = articles.Select(a => "%." + a + ".%").ToArray();
            
            foreach(var article in articles)
            {
                selection += " ArticleNumber LIKE ?";
                if(article != articles.Last())
                {
                    selection += " OR";
                }
            }

            ICursor cursor = this.Database.RawQuery(selection, selectionArgs);

            if (cursor != null)
            {
                cursor.MoveToFirst();
            }
            return cursor;
        }

        /// <summary>
        /// Get all chapters from Publication
        /// </summary>
        /// <param name="pubicationCode">Publication code</param>
        /// <returns>ICursor all chapters from Publication</returns>
        public ICursor QueryArticles(string pubicationCode)
        {
            ICursor cursor = this.Database.Query(
                true, Type,
                new[] { KeyPublicationCode, KeyPublicationName, KeyArticleTitle, KeyArticleMEPSID, KeyArticleLocation, KeyArticleContent, KeyArticleGroup },
                KeyPublicationCode + "=?",
                new string[] { pubicationCode },
                null, null, "_id", null);

            if (cursor != null)
            {
                cursor.MoveToFirst();
            }
            return cursor;
        }

        /// <summary>
        /// Get all chapters from Bible book
        /// </summary>
        /// <param name="bookName">Bible book name</param>
        /// <returns>ICursor all chapters from Bible book</returns>
        public ICursor QueryAllArticlesByBibleChapter(string bookName)
        {
            ICursor cursor = this.Database.Query(
                true, Type,
                new[] { KeyPublicationCode, KeyPublicationName, KeyArticleTitle, KeyArticleMEPSID, KeyArticleLocation, KeyArticleContent, KeyArticleGroup },
                KeyArticleTitle + " LIKE ?",
                new string[] { "%" + App.FUNCTIONS.RemoveDigits(bookName)+ "%" },
                null, null, "_id", null);

            if (cursor != null)
            {
                cursor.MoveToFirst();
            }
            return cursor;
        }
    }

    public class DatabaseHelper : SQLiteOpenHelper
    {
        private const string PrimaryDatabaseCreate =
            "create table PrimaryLibrary ("
            + "_id integer primary key autoincrement, "
            + "PublicationCode text not null, "
            + "PublicationName text not null, "
            + "ArticleTitle text not null, "
            + "ArticleNumber text not null, "
            + "ArticleLocation text not null, "
            + "ArticleContent text not null, "
            + "ArticleGroup text not null, "
            + "ArticleURL text not null);";

        private const string SecondarDatabaseCreate =
            "create table SecondaryLibrary ("
            + "_id integer primary key autoincrement, "
            + "PublicationCode text not null, "
            + "PublicationName text not null, "
            + "ArticleTitle text not null, "
            + "ArticleNumber text not null, "
            + "ArticleLocation text not null, "
            + "ArticleContent text not null, "
            + "ArticleGroup text not null, "
            + "ArticleURL text not null);";

        private string DatabaseType;

        internal DatabaseHelper(Context context, string databaseType, int version)
            : base(context, databaseType, null, version)
        {
            this.DatabaseType = databaseType;
        }

        public override void OnCreate(SQLiteDatabase db)
        {
            if (DatabaseType == Storehouse.Primary)
            {
                db.ExecSQL(PrimaryDatabaseCreate);
            }
            else if (DatabaseType == Storehouse.Secondary)
            {
                db.ExecSQL(SecondarDatabaseCreate);
            }
        }

        public override void OnUpgrade(SQLiteDatabase db, int oldVersion, int newVersion)
        {
            db.ExecSQL("DROP TABLE IF EXISTS PrimaryLibrary");
            db.ExecSQL("DROP TABLE IF EXISTS SecondaryLibrary");
            this.OnCreate(db);
        }
    }

    public static class Storehouse
    {
        public static string Primary
        {
            get
            {
                return "PrimaryLibrary";
            }
        }
        public static string Secondary
        {
            get
            {
                return "SecondaryLibrary";
            }
        }
    }
}
