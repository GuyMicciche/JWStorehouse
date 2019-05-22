using Android.App;
using Android.Content;
using Android.Database;
using Android.Database.Sqlite;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Storehouse.Core
{
    static class JwStore
    {
        #region .NET Store

        /// <summary>
        /// Query a single article from a storehouse
        /// </summary>
        /// <param name="code">Publication code to query</param>
        /// <param name="meps">MEPS id to query</param>
        /// <param name="storehouse">Storehouse to query</param>
        /// <returns>A single article</returns>
        public static WOLArticle QueryArticle(string code, NavStruct meps, string storehouse)
        {
            string number = meps.ToString();

            var db = new SQLiteConnection(storehouse);
            var query = (from a in db.Table<WOLArticle>()
                         where (a.PublicationCode.Equals(code) && a.ArticleNumber.Equals(number))
                         select a).ToList();

            return query.FirstOrDefault();
        }

        /// <summary>
        /// Query all articles in a single publication from a storehouse
        /// </summary>
        /// <param name="code">Publication code to query</param>
        /// <param name="storehouse">Storehouse to query</param>
        /// <returns>All articles in a single publication</returns>
        public static List<WOLArticle> QueryArticlesByPublication(string code, string storehouse)
        {
            var db = new SQLiteConnection(storehouse);
            var query = (from a in db.Table<WOLArticle>()
                         where (a.PublicationCode.Equals(code))
                         select a).ToList();

            return query;
        }

        /// <summary>
        /// Query all chapters in a single Bible book from a storehouse
        /// </summary>
        /// <param name="meps">MEPS id to query</param>
        /// <param name="storehouse">Storehouse to query</param>
        /// <returns>All chapters in a single Bible book</returns>
        public static List<WOLArticle> QueryArticlesByBibleBook(NavStruct meps, string storehouse)
        {
            string book = meps.Book.ToString() + ".";

            var db = new SQLiteConnection(storehouse);
            List<WOLArticle> query = db.Table<WOLArticle>().Where(a => a.PublicationCode.Equals(PublicationType.Bible) && a.ArticleNumber.StartsWith(book)).ToList();

            return query;
        }

        /// <summary>
        /// Query all articles by MEPS id in a storehouse that match articles from another storehouse 
        /// </summary>
        /// <param name="code">Publication code to query</param>
        /// <param name="articles">Articles to query</param>
        /// <param name="storehouse">Storehouse to query</param>
        /// <returns>Matching articles by MEPS</returns>
        public static List<WOLArticle> QueryMatchByMEPS(string code, List<WOLArticle> articles, string storehouse)
        {
            var db = new SQLiteConnection(storehouse);

            string[] chapters = articles.Select(a => a.ArticleNumber).ToArray();
            string[] meps = db.Query<WOLArticle>("select ArticleNumber from WOLArticle where PublicationCode = ?", code)
                .Where(a => chapters.Contains(a.ArticleNumber))
                .Select(a => a.ArticleNumber).ToArray();

            var query = db.Table<WOLArticle>().Where(a => a.PublicationCode.Equals(code) && meps.Contains(a.ArticleNumber)).ToList();

            return query;
        }

        /// <summary>
        /// Query all articles by chapter number in a storehouse that match articles from another storehouse 
        /// </summary>
        /// <param name="code">Publication code to query</param>
        /// <param name="articles">Articles to query</param>
        /// <param name="storehouse">Storehouse to query</param>
        /// <returns>Matching articles by chapter number</returns>
        public static List<WOLArticle> QueryMatchByChapters(string code, List<WOLArticle> articles, string storehouse)
        {
            var db = new SQLiteConnection(storehouse);

            string[] chapters = articles.Select(a => NavStruct.Parse(a.ArticleNumber).Chapter.ToString()).ToArray();
            string[] meps = db.Query<WOLArticle>("select ArticleNumber from WOLArticle where PublicationCode = ?", code)
                .Where(a => chapters.Contains(NavStruct.Parse(a.ArticleNumber).Chapter.ToString()))
                .Select(a => a.ArticleNumber).ToArray();

            var query = db.Table<WOLArticle>().Where(a => a.PublicationCode.Equals(code) && meps.Contains(a.ArticleNumber)).ToList();

            return query;
        }

        /// <summary>
        /// Query all articles by chapter number in a storehouse that match articles from another storehouse 
        /// </summary>
        /// <param name="code">Publication code to query</param>
        /// <param name="articles">Articles to query</param>
        /// <param name="storehouse">Storehouse to query</param>
        /// <returns>Matching articles by chapter number</returns>
        public static List<WOLArticle> QueryMatch(string code, List<WOLArticle> articles, string storehouse)
        {
            var db = new SQLiteConnection(storehouse);

            string[] meps = articles.Select(a => a.ArticleNumber.ToString()).ToArray();
            string[] chapters = db.Query<WOLArticle>("select ArticleNumber from WOLArticle where PublicationCode = ?", code).Select(a => a.ArticleNumber).ToArray();

            string matches = string.Empty;
            foreach (string m in chapters)
            {
                if (m.Contains(chapters[chapters.Count() - 1]))
                {
                    matches += "'" + m + "'";
                }
                else
                {
                    matches += "'" + m + "',";
                }
            }

            var query = db.Query<WOLArticle>("select * from WOLArticle where ArticleNumber in (" + matches + ")").ToList();
            return query.ToList();
        }

        /// <summary>
        /// Query a single insight article by MEPS id
        /// </summary>
        /// <param name="mepsId">MEPS id to query</param>
        /// <param name="storehouse">Storehouse to query</param>
        /// <returns></returns>
        public static WOLArticle QueryInsight(string mepsId, string storehouse)
        {
            var db = new SQLiteConnection(storehouse);
            WOLArticle query = db.Table<WOLArticle>().Single(a => a.PublicationCode.Equals(PublicationType.Insight) && (NavStruct.Parse(a.ArticleNumber).Chapter.ToString()).Equals(mepsId));

            return query;
        }

        /// <summary>
        /// Query all insight articles in a specific group
        /// </summary>
        /// <param name="insightGroup">Group to query</param>
        /// <param name="storehouse">Storehouse to query</param>
        /// <returns>All insight articles in the group</returns>
        public static List<WOLArticle> QueryInsightsByGroup(string insightGroup, string storehouse)
        {
            var db = new SQLiteConnection(storehouse);
            var query = (from a in db.Table<WOLArticle>()
                         where (a.PublicationCode.Equals(PublicationType.Insight) && a.ArticleGroup.Equals(insightGroup))
                         select a).ToList();
            var q = query.OrderBy(a => NavStruct.Parse(a.ArticleNumber).Verse);

            return q.ToList();
        }

        public static List<WOLArticle> QueryArticleChapterTitles(string code, string storehouse)
        {
            var db = new SQLiteConnection(storehouse);

            var query = db.Query<WOLArticle>("select ArticleTitle,ArticleNumber,ArticleLocation,ArticleGroup from WOLArticle where PublicationCode = ?", code).ToList();

            return query;
        }

        #endregion

        #region Android Store

        #endregion

        public static Expression<Func<TElement, bool>> BuildOrExpression<TElement, TValue>(Expression<Func<TElement, TValue>> valueSelector, IEnumerable<TValue> values)
        {
            if (null == valueSelector)
            {
                throw new ArgumentNullException("valueSelector");
            }
            if (null == values)
            {
                throw new ArgumentNullException("values");
            }
            ParameterExpression p = valueSelector.Parameters.Single();

            if (!values.Any())
            {
                return e => false;
            }

            var equals = values.Select(value => (Expression)Expression.Equal(valueSelector.Body, Expression.Constant(value, typeof(TValue))));
            var body = equals.Aggregate<Expression>((accumulate, equal) => Expression.Or(accumulate, equal));

            return Expression.Lambda<Func<TElement, bool>>(body, p);
        }
    }
}