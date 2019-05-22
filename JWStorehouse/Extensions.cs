using Android.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JWStorehouse
{
    public static class ListExtensions
    {   
        public static void SwapObject<T>(this IList<T> list, int index1, int index2)
        {
            T temp = list[index1];
            list[index1] = list[index2];
            list[index2] = temp;
        }

        public static List<WOLArticle> ToArticleList(this ICursor cursor)
        {
            cursor.MoveToFirst();
            List<WOLArticle> articles = new List<WOLArticle>();

            try
            {
                for (bool haveRow = cursor.MoveToFirst(); haveRow; haveRow = cursor.MoveToNext())
                {
                     articles.Add(new WOLArticle()
                        {
                            ArticleContent = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleContent)),
                            ArticleLocation = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleLocation)),
                            ArticleMEPSID = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleMEPSID)),
                            ArticleTitle = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleTitle)),
                            PublicationCode = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyPublicationCode)),
                            PublicationName = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyPublicationName))                    
                        });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                cursor.Close();
            }

            return articles;
        }

        public static List<WOLArticle> ToInsightList(this ICursor cursor, string[] groupArticles)
        {
            cursor.MoveToFirst();
            List<WOLArticle> articles = new List<WOLArticle>();

            for (bool haveRow = cursor.MoveToFirst(); haveRow; haveRow = cursor.MoveToNext())
            {
                if (groupArticles.Contains(NavStruct.Parse(cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleMEPSID))).Chapter.ToString()))
                {
                    articles.Add(new WOLArticle()
                    {
                        ArticleContent = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleContent)),
                        ArticleLocation = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleLocation)),
                        ArticleMEPSID = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleMEPSID)),
                        ArticleTitle = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleTitle)),
                        PublicationCode = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyPublicationCode)),
                        PublicationName = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyPublicationName))
                    });
                }
            }

            return articles;
        }

        public static WOLArticle ToArticle(this ICursor cursor)
        {
            cursor.MoveToFirst();

            WOLArticle article = new WOLArticle();

            try
            {
                article.ArticleContent = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleContent));
                article.ArticleLocation = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleLocation));
                article.ArticleMEPSID = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleMEPSID));
                article.ArticleTitle = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleTitle));
                article.PublicationCode = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyPublicationCode));
                article.PublicationName = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyPublicationName));
                article.ArticleGroup = cursor.GetString(cursor.GetColumnIndex(JwStore.KeyArticleGroup));
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return article;
        }
    }

    public static class TExtensions
    {
        public static T SwapWith<T>(this T current, ref T other)
        {
            T tmpOther = other;
            other = current;
            return tmpOther;
        }
    }
}
