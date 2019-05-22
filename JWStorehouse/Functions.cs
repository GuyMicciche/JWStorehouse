using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Net;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace JWStorehouse
{
    public class Functions : Application
    {
        public Functions()
        {

        }

        //////////////////////////////////////////////////////////////////////////
        // WATCHTOWER ONLINE LIBRARY Section
        //////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the url of the requested article
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="mode">LanguageMode type</param>
        /// <param name="article">Article number (formatted)</param>
        /// <returns>Generated string of the url</returns>
        public string GenerateWOLLink(Language language, string mode, string article)
        {
            string link = mode + "/r" + language.R + "/lp-" + language.Lp + "/" + article;
            string url = "http://m.wol.jw.org/en/wol/" + link;

            return url;
        }

        public List<WOLArticle> GenerateWOLArticles(Language language, string storehouse, string publicationType)
        {
            List<WOLArticle> articles = new List<WOLArticle>();

            // If Pinyin, return 0 articles
            if (language.EnglishName.Contains("Pinyin"))
            {
                return articles;
            }

            string xml = string.Empty;
            string year = string.Empty;
            string bi = GetBibleBi(language);

            int book = 0;
            int chapter = 0;

            Stream stream = App.STATE.Context.Assets.Open(publicationType + ".xml");
            using (StreamReader reader = new StreamReader(stream))
            {
                xml = reader.ReadToEnd();
            }

            if (publicationType == PublicationType.Bible)
            {
                year = GetBibleYear(language);
            }

            XDocument doc = XDocument.Parse(xml);
            foreach (XElement publication in doc.Descendants("p"))
            {
                book = publication.ElementsBeforeSelf().Count();
                chapter = 0;

                foreach (XElement article in publication.Descendants("a"))
                {
                    chapter++;

                    try
                    {
                        string mode = string.Empty;
                        string url = string.Empty;
                        string code = publication.Attribute("code").Value;
                        string number = article.Value;
                        string group = string.Empty;

                        // DAILY TEXT
                        if (code == PublicationType.DailyText)
                        {
                            // In this format:  2015/1/1
                            url = GenerateWOLLink(language, ArticleType.DailyText, number);

                            number = NavStruct.DailyTextParse(number).ToString();

                            group = code;
                        }
                        // BIBLE
                        else if (code == PublicationType.Bible)
                        {
                            if (language.EnglishName.Contains("English"))
                            {
                                // In this format:  nwt/E/2013/1/1
                                url = GenerateWOLLink(language, ArticleType.Bible, number);
                            }
                            else
                            {
                                // In this format:  bi12/CHS/2001/1/1
                                url = GenerateWOLLink(language, ArticleType.Bible, number);

                                url = Regex.Replace(url, @"(.*)(nwt/E/2013/)(.*)", delegate(Match match)
                                {
                                    return match.Groups[1].Value + bi + "/" + language.Lp.ToUpper() + "/" + year + "/" + match.Groups[3].Value;
                                });
                            }

                            // In this format:  1.1.0
                            number = NavStruct.BibleParse(number).ToString();

                            Console.WriteLine(url);

                            group = code;
                        }
                        // PUBLICATIONS
                        else
                        {
                            // In this format:  1200000005
                            url = GenerateWOLLink(language, ArticleType.Publication, number);

                            number = NavStruct.SuperParse(int.Parse(publication.Attribute("number").Value), chapter).ToString();

                            group = code;
                        }

                        articles.Add(new WOLArticle
                        {
                            Storehouse = storehouse,
                            PublicationCode = code,
                            ArticleMEPSID = number,
                            ArticleURL = url,
                            ArticleGroup = group
                        });

                        // DEBUGGING >> Allow only one article from each group
                        //if (chapter > 1) { break; }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }

            return articles;
        }

        public List<WOLArticle> GenerateWOLInsightArticles(Language language, string storehouse)
        {
            InsightArticle insight;
            List<WOLArticle> articles = new List<WOLArticle>();
            List<InsightArticle> insights = new List<InsightArticle>();

            if (storehouse == Storehouse.Primary)
            {
                insights = App.STATE.PrimaryInsightArticles;
            }
            else
            {
                insights = App.STATE.SecondaryInsightArticles;
            }

            // DEBUGGING >> Allow only one article
            //for (int i = 0; i < 1; i++)
            
            // All articles
            for (int i = 0; i < insights.Count; i++)
            {
                try
                {
                    insight = insights.ElementAt(i);

                    string mode = string.Empty;
                    string group = string.Empty;
                    string code = "it";
                    string number = insights.ElementAt(i).MEPSID;
                    string url = GenerateWOLLink(language, ArticleType.Publication, number);

                    int groupnumber;

                    if (storehouse == Storehouse.Primary)
                    {
                        groupnumber = Array.IndexOf(App.STATE.PrimaryInsightGroups, insight.Group);
                    }
                    else
                    {
                        groupnumber = Array.IndexOf(App.STATE.SecondaryInsightGroups, insight.Group);
                    }

                    number = NavStruct.SuperParse(groupnumber, int.Parse(insight.MEPSID), insight.OrderNumber).ToString();

                    group = code + "-" + groupnumber;

                    articles.Add(new WOLArticle
                    {
                        Storehouse = storehouse,
                        PublicationCode = code,
                        ArticleMEPSID = number,
                        ArticleURL = url,
                        ArticleGroup = group
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            return articles;
        }

        private List<WOLArticle> GeneratePinyinPublicationLinks(string[] publications)
        {
            List<WOLArticle> pubs = new List<WOLArticle>();

            string xml = string.Empty;
            int book = 0;

            Stream stream = App.STATE.Context.Assets.Open("pinyin.xml");
            StreamReader reader = new StreamReader(stream);
            xml = reader.ReadToEnd();
            XDocument doc = XDocument.Parse(xml);

            // BIBLE
            if (publications.Contains(PublicationType.Bible))
            {
                book = 0;
                foreach (XElement element in doc.Descendants("biblebook"))
                {
                    book++;
                    pubs.Add(new WOLArticle()
                    {
                        PublicationCode = PublicationType.Bible,
                        PublicationURL = (string)element.Element("link"),
                        PublicationName = (string)element.Element("chinese"),
                        PublicationNamePinyin = (string)element.Element("pinyin"),
                        ArticleMEPSID = book.ToString()
                    });
                }
            }

            // INSIGHT
            if (publications.Contains(PublicationType.Insight))
            {
                foreach (XElement element in doc.Descendants("insight"))
                {
                    pubs.Add(new WOLArticle()
                    {
                        PublicationCode = PublicationType.Insight,
                        PublicationURL = (string)element.Element("link"),
                        PublicationName = (string)element.Element("chinese"),
                        PublicationNamePinyin = (string)element.Element("pinyin"),
                        ArticleMEPSID = (string)element.Attribute("number")
                    });
                }
            }

            // DAILY TEXT
            if (publications.Contains(PublicationType.DailyText))
            {
                foreach (XElement element in doc.Descendants("dailytext"))
                {
                    pubs.Add(new WOLArticle()
                    {
                        PublicationCode = PublicationType.DailyText,
                        PublicationURL = (string)element.Element("link"),
                        PublicationName = (string)element.Element("chinese"),
                        PublicationNamePinyin = (string)element.Element("pinyin"),
                        ArticleMEPSID = (string)element.Attribute("number")
                    });
                }
            }

            // BOOK
            if (publications.Contains(PublicationType.Books))
            {
                foreach (XElement element in doc.Descendants("pub"))
                {
                    pubs.Add(new WOLArticle()
                    {
                        PublicationCode = (string)element.Attribute("code"),
                        PublicationURL = (string)element.Element("link"),
                        PublicationName = (string)element.Element("chinese"),
                        PublicationNamePinyin = (string)element.Element("pinyin"),
                        ArticleMEPSID = (string)element.Attribute("number")
                    });
                }
            }

            return pubs;
        }

        private void StoreWOLArticle(WOLArticle article)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(article.ArticleURL);
            request.Timeout = 80000;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    string html = string.Empty;
                    string pattern = string.Empty;
                    string pubcode = article.PublicationCode;

                    Stream stream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(stream);
                    html = reader.ReadToEnd();

                    Match match;

                    //////////////////////////////////////////////////////////////////////////
                    // PUBLICATION Name
                    //////////////////////////////////////////////////////////////////////////
                    pattern = pubcode.Equals(PublicationType.DailyText) ? "(<title>)(.*?)(</title>)" : "(<li  class=\"resultDocumentPubTitle\">)(.*?)(</li>)";
                    pattern = pubcode.Equals(PublicationType.Bible) ? "(<li class=\"resultDocumentPubTitle\">)(.*?)(<ul)" : pattern;

                    match = Regex.Matches(html, pattern, RegexOptions.Singleline)[0];
                    
                    article.PublicationName = match.Groups[2].Value.Replace("&mdash;", "—");
                    article.PublicationName = new Regex("\\s+").Replace(article.PublicationName, " ");

                    //////////////////////////////////////////////////////////////////////////
                    // ARTICLE TITLE
                    //////////////////////////////////////////////////////////////////////////
                    pattern = pubcode.Equals(PublicationType.DailyText) ? "(<h2)(.*?)(</h2>)" : "(<li  class=\"resultsNavigationSelected\">)(.*?)(</li>)";
                    pattern = pubcode.Equals(PublicationType.Bible) ? "(<li class=\"resultsNavigationSelected documentLocation navChapter\">)(.*?)(</li>)" : pattern;
                    
                    match = Regex.Matches(html, pattern, RegexOptions.Singleline)[0];

                    if (pubcode.Equals(PublicationType.Bible))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(match.Groups[2].Value);
                        article.ArticleTitle = doc.InnerText;
                    }
                    else if (pubcode.Equals(PublicationType.DailyText))
                    {
                        article.ArticleTitle = match.Groups[2].Value.Replace("&nbsp;", " ").Split('>').Last();
                    }
                    else
                    {
                        article.ArticleTitle = match.Groups[2].Value.Replace("&nbsp;", " ");
                    }

                    //////////////////////////////////////////////////////////////////////////
                    // ARTICLE LOCATION
                    //////////////////////////////////////////////////////////////////////////
                    pattern = pubcode.Equals(PublicationType.DailyText) ? "(<span class=\"ref\">)(.*?)(</span>)" : "(<li  class=\"resultsNavigationSelected documentLocation navPublications\">)(.*?)(</li>)";
                    pattern = pubcode.Equals(PublicationType.Bible) ? "(<li class=\"resultsNavigationSelected documentLocation navChapter\">)(.*?)(</li>)" : pattern;
                    
                    match = Regex.Matches(html, pattern, RegexOptions.Singleline)[0];

                    if (pubcode.Equals(PublicationType.DailyText))
                    {
                        // Current year
                        article.ArticleLocation = DateTime.Now.Year.ToString();
                    }
                    else if (pubcode.Equals(PublicationType.Bible))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(match.Groups[2].Value);
                        article.ArticleLocation = doc.InnerText;
                    }
                    else
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(match.Groups[2].Value);
                        article.ArticleLocation = doc.InnerText;
                    }

                    //////////////////////////////////////////////////////////////////////////
                    // ARTICLE CONTENT
                    //////////////////////////////////////////////////////////////////////////
                    pattern = "(<article)(.*?)(>)(.*?)(</article>)";
                    
                    match = Regex.Matches(html, pattern, RegexOptions.Singleline)[0];

                    article.ArticleContent = "<article>" + match.Groups[4].Value + "</article>";

                    // Replace any nav from article, like in Daily Text
                    Match mat = Regex.Match(article.ArticleContent, "(<nav>)(.*?)(</nav>)", RegexOptions.Singleline);
                    if (mat.Success)
                    {
                        article.ArticleContent = article.ArticleContent.Replace(mat.Value, "");
                    }

                    // Replace any <input> tags
                    Regex rgx = new Regex("(<input type=)(.*?)(/>)");
                    article.ArticleContent = rgx.Replace(article.ArticleContent, " ");

                    // Replace possible pinyin tags
                    rgx = new Regex("(<span class=\"wd\"></span>)");
                    article.ArticleContent = rgx.Replace(article.ArticleContent, "");                    

                    // Replace broken links with correct mobile links
                    article.ArticleContent = article.ArticleContent.Replace("/en", "http://m.wol.jw.org/en");

                    // Generate HTML data
                    article.ArticleContent = @"<html>
                                                    <head>
                                                        <meta name='viewport' content='width=320' />
                                                        <meta http-equiv='Content-Type' content='text/html; charset=utf-8' />
                                                        <link href='css/wol.css' type='text/css' rel='stylesheet' />
                                                        <script src='js/init.js'></script>
                                                    </head>
                                                    <body class='calibre' onload='PageOnLoad()'>
                                                        <div class='body' id='content'>" + article.ArticleContent + @"</div>
                                                    </body>
                                                </html>";

                    // Delete extra spaces (aka minify)
                    article.ArticleContent = new Regex("\\s+").Replace(article.ArticleContent, " ");
                    // Replace messed up spaces
                    article.ArticleContent = new Regex("> <").Replace(article.ArticleContent, "><");

                    //////////////////////////////////////////////////////////////////////////
                    // ADD ARTICLE TO STOREHOUSE DATABASE
                    //////////////////////////////////////////////////////////////////////////
                    if (article.Storehouse == Storehouse.Primary)
                    {
                        if (!pubcode.Equals(PublicationType.Bible) && !pubcode.Equals(PublicationType.Insight) && !pubcode.Equals(PublicationType.DailyText))
                        {
                            if (!App.STATE.PrimaryBooks.Any(x => x.PublicationCode.Contains(pubcode)))
                            {
                                App.STATE.PrimaryBooks.Add(article);
                            }
                        }

                        JwStore database = new JwStore(Storehouse.Primary);
                        AddArticleToLibrary(database, article);
                    }
                    else if (article.Storehouse == Storehouse.Secondary)
                    {
                        if (!pubcode.Equals(PublicationType.Bible) && !pubcode.Equals(PublicationType.Insight) && !pubcode.Equals(PublicationType.DailyText))
                        {
                            if (!App.STATE.SecondaryBooks.Any(x => x.PublicationCode.Contains(pubcode)))
                            {
                                App.STATE.SecondaryBooks.Add(article);
                            }
                        }

                        JwStore database = new JwStore(Storehouse.Secondary);
                        AddArticleToLibrary(database, article);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void StorePinyinPublication(string storehouse, WOLArticle article, ProgressDialog progress)
        {
            string xml = string.Empty;
            int book = int.Parse(article.ArticleMEPSID);
            int chapter = 0;

            string code = article.PublicationCode;
            string url = article.PublicationURL;
            string title = article.PublicationName;
            string pinyin = article.PublicationNamePinyin;
            string number = article.ArticleMEPSID;

            JwStore database = new JwStore(storehouse);
            database.Open();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 80000;

            Console.WriteLine("Publication => " + code);

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Stream stream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(stream);
                    xml = reader.ReadToEnd();
                }

                XDocument doc = XDocument.Parse(xml);

                // BIBLE
                if (code == PublicationType.Bible)
                {
                    chapter = 0;
                    foreach (XElement element in doc.Descendants("c"))
                    {
                        chapter++;

                        // Delete extra spaces (aka minify)
                        string content = new Regex("\\s+").Replace(element.Value, " ");

                        article = new WOLArticle();
                        article.PublicationName = "Shèngjīng Xīn Shìjiè Yìběn";
                        article.PublicationCode = code;
                        article.ArticleTitle = pinyin + " " + chapter + "\n" + title + " " + chapter;
                        article.ArticleTitlePinyin = pinyin + " " + chapter;
                        article.ArticleContent = content;
                        article.ArticleLocation = title + " " + chapter;
                        article.ArticleMEPSID = NavStruct.SuperParse(book, chapter).ToString();
                        article.ArticleGroup = code;

                        long id = database.AddToLibrary(article);
                    }
                }
                // INSIGHT
                else if (code == PublicationType.Insight)
                {
                    List<InsightArticle> insights = new List<InsightArticle>();

                    if (storehouse == Storehouse.Primary)
                    {
                        insights = App.STATE.PrimaryInsightArticles;
                    }
                    else if (storehouse == Storehouse.Secondary)
                    {
                        insights = App.STATE.SecondaryInsightArticles;
                    }

                    for(int i = 0; i < insights.Count; i++)
                    {
                        XElement element = doc.Descendants("article").ElementAt(i);

                        // Delete extra spaces (aka minify)
                        string content = new Regex("\\s+").Replace(element.Element("content").Value, " ");

                        article = new WOLArticle();
                        article.PublicationCode = code;
                        article.PublicationName = pinyin + "\n" + title;
                        article.PublicationNamePinyin = pinyin;
                        article.ArticleTitle = element.Element("pinyinTitle").Value + "\n" + element.Element("title").Value;
                        article.ArticleTitlePinyin = element.Element("pinyinTitle").Value;
                        article.ArticleContent = content;
                        article.ArticleLocation = element.Element("documentLocation").Value;
                        article.ArticleMEPSID = insights.ElementAt(i).MEPSID;
                        article.ArticleGroup = code + "-" + insights.ElementAt(i).MEPSID.Split('.').First().ToString();

                        Console.WriteLine(insights.ElementAt(i).MEPSID.ToString());

                        long id = database.AddToLibrary(article);
                    }
                }
                // DAILY TEXT
                else if (code == PublicationType.DailyText)
                {
                    foreach (XElement element in doc.Descendants("article"))
                    {
                        string content = new Regex("\\s+").Replace(element.Element("content").Value, " ");

                        article = new WOLArticle();
                        article.PublicationCode = code;
                        article.PublicationName = pinyin + "\n" + title;
                        article.PublicationNamePinyin = pinyin;
                        article.ArticleTitle = element.Element("pinyinTitle").Value + "\n" + element.Element("title").Value;
                        article.ArticleTitlePinyin = element.Element("pinyinTitle").Value;
                        article.ArticleContent = content;
                        article.ArticleLocation = element.Element("documentLocation").Value;
                        article.ArticleMEPSID = NavStruct.DailyTextParse(element.Element("documentLocation").Value).ToString();
                        article.ArticleGroup = code;

                        long id = database.AddToLibrary(article);
                    }
                }
                // PUBLICATION
                else
                {
                    chapter = 0;
                    foreach (XElement element in doc.Descendants("article"))
                    {
                        chapter++;

                        // Delete extra spaces (aka minify)
                        string content = new Regex("\\s+").Replace(element.Element("content").Value, " ");

                        article = new WOLArticle();
                        article.PublicationCode = code;
                        article.PublicationName = pinyin + "\n" + title;
                        article.PublicationNamePinyin = pinyin;
                        article.ArticleTitle = element.Element("pinyinTitle").Value + "\n" + element.Element("title").Value;
                        article.ArticleTitlePinyin = element.Element("pinyinTitle").Value;
                        article.ArticleContent = content;
                        article.ArticleLocation = element.Element("documentLocation").Value;
                        article.ArticleMEPSID = NavStruct.SuperParse(book, chapter).ToString();
                        article.ArticleGroup = code;                       

                        if (storehouse == Storehouse.Primary)
                        {
                            if (!App.STATE.PrimaryBooks.Any(x => x.PublicationCode.Equals(code)))
                            {
                                App.STATE.PrimaryBooks.Add(article);
                            }
                        }
                        else if (storehouse == Storehouse.Secondary)
                        {
                            if (!App.STATE.SecondaryBooks.Any(x => x.PublicationCode.Equals(code)))
                            {
                                App.STATE.SecondaryBooks.Add(article);
                            }
                        }

                        long id = database.AddToLibrary(article);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("OH NO! -> " + ex.ToString());
            }

            database.Close();
        }

        public void AddArticleToLibrary(JwStore database, WOLArticle article)
        {
            database.Open();

            long id = database.AddToLibrary(article);

            if (article.Storehouse == Storehouse.Primary)
            {
                //Console.WriteLine(DatabaseUtils.QueryNumEntries(database.Database, LibraryDatabaseType.Primary) + " rows.");
            }
            else if (article.Storehouse == Storehouse.Secondary)
            {
                //Console.WriteLine(DatabaseUtils.QueryNumEntries(database.Database, LibraryDatabaseType.Secondary) + " rows.");
            }

            database.Close();
        }

        public string GetBibleYear(Language language)
        {
            string url = "http://m.wol.jw.org/en/wol/lv/r" + language.R + "/lp-" + language.Lp + "/0/0";
            string html = string.Empty;

            string pattern = "(<span class=\"details\">)(\\d+)(</span>)";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 80000;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Stream stream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(stream);
                    html = reader.ReadToEnd();

                    foreach (Match match in Regex.Matches(html, pattern, RegexOptions.Singleline))
                    {
                        return match.Groups[2].Value.ToString();
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return string.Empty;
        }

        public string GetBibleBi(Language language)
        {
            string url = "http://wol.jw.org/en/wol/h/r" + language.R + "/lp-" + language.Lp;
            string html = string.Empty;

            string pattern = "(<li id=\"menuBible\")(.*?)(<a href=\"/)(.*?)(\">)";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 80000;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Stream stream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(stream);
                    html = reader.ReadToEnd();

                    MatchCollection matchCollection = Regex.Matches(html, pattern, RegexOptions.Singleline);
                    Match match = matchCollection[0];

                    string[] temp = match.Groups[4].Value.ToString().Split('/');

                    return temp[temp.Count() - 3];
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return string.Empty;
        }

        public string[] DownloadInsightGroups(Language language)
        {
            List<string> groups = new List<string>();

            string last = (language.EnglishName.Contains("English")) ? "3" : "2";
            string url = "http://m.wol.jw.org/en/wol/lv/r" + language.R + "/lp-" + language.Lp + "/0/" + last;
            string html = string.Empty;

            string pattern = "(<span class=\"title\">)(.*?)(</span>)";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 80000;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Stream stream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(stream);
                    html = reader.ReadToEnd();

                    foreach (Match match in Regex.Matches(html, pattern, RegexOptions.Singleline))
                    {
                        groups.Add(match.Groups[2].Value.ToString());
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return groups.ToArray();
        }

        public string[] DownloadInsightGroupLinks(Language language)
        {
            List<string> links = new List<string>();

            string last = (language.EnglishName.Contains("English")) ? "3" : "2";
            string url = "http://m.wol.jw.org/en/wol/lv/r" + language.R + "/lp-" + language.Lp + "/0/" + last;
            string html = string.Empty;

            string pattern = "(<li  role=\"presentation\">)(.*?)(0/)(\\d+)(\">)";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 80000;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Stream stream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(stream);
                    html = reader.ReadToEnd();

                    foreach (Match match in Regex.Matches(html, pattern, RegexOptions.Singleline))
                    {
                        links.Add(match.Groups[4].Value.ToString());
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return links.ToArray();
        }

        public List<BibleBook> DownloadBibleBookNames(Language language)
        {
            List<BibleBook> books = new List<BibleBook>();

            if (language.EnglishName.Contains("Pinyin"))
            {
                string xml = string.Empty;
                Stream stream = App.STATE.Context.Assets.Open("pinyin.xml");
                using (StreamReader reader = new StreamReader(stream))
                {
                    xml = reader.ReadToEnd();
                }

                XDocument doc = XDocument.Parse(xml);
                foreach (XElement element in doc.Descendants("biblebook"))
                {
                    books.Add(new BibleBook
                    {
                        Number = element.ElementsBeforeSelf().Count().ToString(),
                        Name = (string)element.Element("pinyin") + "\n" + (string)element.Element("chinese"),
                        Abbreviation = (string)element.Element("pinyin").Attribute("abbr") + "\n" + (string)element.Element("chinese").Attribute("abbr")
                    });
                }

                return books;
            }


            string year = GetBibleYear(language);
            string bi = GetBibleBi(language);
            string last = (language.EnglishName.Contains("English")) ? "nwt/E/2013" : bi + "/" + language.Lp.ToUpper() + "/" + year;
            string url = "http://m.wol.jw.org/en/wol/binav/r" + language.R + "/lp-" + language.Lp + "/" + last;
            string html = string.Empty;

            string pattern = "(<li class=\"book\">)(.*?)(data-bookid=\")(\\d+)(\">)(<span class=\"name\">)(.*?)(</span>)(<span class=\"abbreviation\">)(.*?)(</span>)";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 80000;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Stream stream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(stream);
                    html = reader.ReadToEnd();

                    foreach (Match match in Regex.Matches(html, pattern, RegexOptions.Singleline))
                    {
                        books.Add(new BibleBook()
                        {
                            Number = match.Groups[4].Value.ToString(),
                            Name = match.Groups[7].Value.ToString(),
                            Abbreviation = match.Groups[10].Value.ToString()
                        });
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return books;
        }

        public List<InsightArticle> DownloadInsightArticleNames(Language language, string library)
        {
            ServicePointManager.DefaultConnectionLimit = 10000;
            //ServicePointManager.FindServicePoint(uri).ConnectionLimit = 10000;

            List<InsightArticle> articles = new List<InsightArticle>();

            if (language.EnglishName.Contains("Pinyin"))
            {
                string[] meps;
                string[] chineseNames;
                string[] pinyinNames;
                string xml = string.Empty;
                XDocument doc;
                Stream stream;

                // MEPS
                stream = App.STATE.Context.Assets.Open("it-pinyin-meps.xml");
                using (StreamReader reader = new StreamReader(stream))
                {
                    xml = reader.ReadToEnd();
                }
                doc = XDocument.Parse(xml);
                meps = doc.Descendants("a").Select(x => x.Value.ToString()).ToArray();

                // Chinese names
                stream = App.STATE.Context.Assets.Open("it-pinyin-chinese-names.xml");
                using (StreamReader reader = new StreamReader(stream))
                {
                    xml = reader.ReadToEnd();
                }
                doc = XDocument.Parse(xml);
                chineseNames = doc.Descendants("a").Select(x => x.Value.ToString()).ToArray();

                // Pinyin names
                stream = App.STATE.Context.Assets.Open("it-pinyin-pinyin-names.xml");
                using (StreamReader reader = new StreamReader(stream))
                {
                    xml = reader.ReadToEnd();
                }
                doc = XDocument.Parse(xml);
                pinyinNames = doc.Descendants("a").Select(x => x.Value.ToString()).ToArray();


                for (var i = 0; i < meps.Count(); i++)
                {
                    articles.Add(new InsightArticle
                    {
                        MEPSID = meps[i],
                        Title = ToUpper(pinyinNames[i]) + "\n" + chineseNames[i]
                    });
                    //Console.WriteLine(ToUpper(pinyinNames[i]) + "\n" + chineseNames[i]);
                }

                return articles;
            }

            string[] groups = DownloadInsightGroups(language);
            string[] grouplinks = DownloadInsightGroupLinks(language);

            for (var i = 0; i < grouplinks.Count(); i++)
            {
                string group = groups[i];
                //string group = i.ToString();
                string grouplink = grouplinks[i];

                string url = "http://m.wol.jw.org/en/wol/lv/r" + language.R + "/lp-" + language.Lp + "/0/" + grouplink;
                string html = string.Empty;

                string pattern = "(<li  role=\"presentation\"><a href=\")(.*?)(\"><span class=\"title\">)(.*?)(</span>)";

                WebClient webClient = new WebClient();

                try
                {
                    using (Stream responseData = webClient.OpenRead(url))
                    {
                        StreamReader reader = new StreamReader(responseData);
                        html = reader.ReadToEnd();

                        int j = 0;
                        foreach (Match match in Regex.Matches(html, pattern, RegexOptions.Singleline))
                        {
                            string[] temp = match.Groups[2].Value.ToString().Split('/');
                            string articleLink = temp[temp.Count() - 1];
                            string title = match.Groups[4].Value.ToString();

                            articles.Add(new InsightArticle()
                            {
                                Title = title,
                                Group = group,
                                MEPSID = articleLink,
                                OrderNumber = j
                            });

                            j++;
                        }
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            if (library == Storehouse.Primary)
            {
                App.STATE.PrimaryInsightGroups = groups;
            }
            else if (library == Storehouse.Secondary)
            {
                App.STATE.SecondaryInsightGroups = groups;
            }

            return articles;
        }

        //////////////////////////////////////////////////////////////////////////
        // LANGUAGE Section
        //////////////////////////////////////////////////////////////////////////

        public List<Language> GetAvailableLanguages()
        {
            string xml = string.Empty;
            List<Language> languages = new List<Language>();

            Stream stream = App.STATE.Context.Assets.Open("languages.xml");
            using (StreamReader reader = new StreamReader(stream))
            {
                xml = reader.ReadToEnd();
            }

            XDocument doc = XDocument.Parse(xml);
            foreach (XElement element in doc.Descendants("l"))
            {
                languages.Add(new Language
                {
                    R = element.Attribute("r").Value,
                    Lp = element.Attribute("c").Value,
                    EnglishName = element.Attribute("n").Value,
                    LanguageName = element.Attribute("ln").Value,
                    Name = element.Value
                });
            }

            return languages;
        }

        public bool[] IsLanguagesDownloaded(string[] available)
        {
            bool[] downloaded = new bool[available.Length];
            string[] languages = available;

            for (var i = 0; i < languages.Length; i++)
            {
                var dir = new Java.IO.File(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal));
                var folder = new Java.IO.File(dir, languages[i]);
                downloaded[i] = folder.IsDirectory;
            }

            return downloaded;
        }

        //////////////////////////////////////////////////////////////////////////
        // DIALOG Section
        //////////////////////////////////////////////////////////////////////////

        public Dialog DownloadDuelLanguagePackDialog(Context context)
        {
            string[] available = GetAvailableLanguages().Select(s => s.Name).ToArray();
            string[] languages = GetAvailableLanguages().Select(s => s.EnglishName).ToArray();

            LayoutInflater inflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            View view = inflater.Inflate(Resource.Layout.LanguagePackDialogView, null);

            // Checkbox adapter
            Intent intent = new Intent(Intent.ActionMain, null);
            intent.AddCategory(Intent.CategoryLauncher);
            //System.Collections.Generic.IList<ResolveInfo> pubs = App.STATE.Context.PackageManager.QueryIntentActivities(intent, PackageInfoFlags.Activities);
            ExpandableHeightListView list = view.FindViewById<ExpandableHeightListView>(Resource.Id.downloadOptionsListView);
            list.Expanded = true;
            list.SetSelector(Resource.Drawable.metro_abs_selectablelistitem_style);
            //grid.Adapter = new DownloadOptionsGridAdapter(context, context.Resources.GetStringArray(Resource.Array.LibraryNavigation));
            list.Adapter = new ArrayAdapter<string>(context, Android.Resource.Layout.SimpleListItemMultipleChoice, context.Resources.GetStringArray(Resource.Array.LibraryNavigation));
            list.ChoiceMode = ChoiceMode.Multiple;

            //if (((int)Android.OS.Build.VERSION.SdkInt) >= 11)
            //{
            //    grid.ChoiceMode = ChoiceMode.Multiple;
            //}

            //grid.SetMultiChoiceModeListener(new MultiChoiceModeListener(grid));
            list.ItemClick += grid_ItemClick;

            // IF USING SPINNER
            //ArrayAdapter languageAdapter = new ArrayAdapter(context, Android.Resource.Layout.SimpleSpinnerItem, available);
            //languageAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

            //var primary = view.FindViewById<Spinner>(Resource.Id.primaryLanguageSpinner);
            //primary.Adapter = languageAdapter;

            //var secondary = view.FindViewById<Spinner>(Resource.Id.secondaryLanguageSpinner);
            //secondary.Adapter = languageAdapter;

            // IF USING AUTOCOMPLETETEXTVIEW
            // Default 31 is ENGLISH
            int primPos = -1;
            int secPos = -1;
            ArrayAdapter autoCompleteAdapter = new ArrayAdapter(context, Android.Resource.Layout.SimpleDropDownItem1Line, available);

            var prim = view.FindViewById<AutoCompleteTextView>(Resource.Id.primaryLanguageAutoComplete);
            prim.Hint = "Enter language...";
            prim.Adapter = autoCompleteAdapter;
            prim.ItemClick += (sender, e) =>
                {
                    primPos = Array.IndexOf(available, ((TextView)e.View).Text);
                };
            var sec = view.FindViewById<AutoCompleteTextView>(Resource.Id.secondaryLanguageAutoComplete);
            sec.Hint = "Enter language...";
            sec.Adapter = autoCompleteAdapter;
            sec.ItemClick += (sender, e) =>
                {
                    secPos = Array.IndexOf(available, ((TextView)e.View).Text);
                };

            var builder = new StorehouseDialogBuilder(context);
            builder.SetView(view);
            builder.SetTitle("Available Languages");
            builder.SetIcon(Resource.Drawable.Icon);
            builder.SetTitleTextColor(Resource.Color.storehouse_white);
            builder.SetTitleBackgroundColor(Android.Resource.Color.Transparent);
            builder.SetCancelable(false);
            builder.SetPositiveButton("Ok", (sender, e) =>
            {
                if(primPos < 0 && secPos < 0)
                {
                    Toast.MakeText(App.STATE.Activity, "Invalid languages. Please try again.", ToastLength.Long).Show();
                    DownloadDuelLanguagePackDialog(context).Show();
                    return;
                }

                if(PublicationsToDownload(list).Count() < 1)
                {
                    Toast.MakeText(App.STATE.Activity, "Invalid selection. Please try again.", ToastLength.Long).Show();
                    DownloadDuelLanguagePackDialog(context).Show();
                    return;
                }

                DateTime time = DateTime.Now;
                BackgroundWorker worker = new BackgroundWorker();

                //Language primaryLanguage = GetAvailableLanguages().Single(x => x.Name.Equals(primary.SelectedItem.ToString()));
                //Language secondaryLanguage = GetAvailableLanguages().Single(x => x.Name.Equals(secondary.SelectedItem.ToString()));

                Language primaryLanguage = GetAvailableLanguages().ElementAt(primPos);
                Language secondaryLanguage = GetAvailableLanguages().ElementAt(secPos);

                ProgressDialog progress = DownloadingProgressDialog(context, "Opening storehouse . . .", "Connecting . . .", delegate
                {
                    if (worker.IsBusy)
                    {
                        worker.CancelAsync();
                    }
                });

                progress.Show();

                worker.WorkerSupportsCancellation = true;
                worker.DoWork += (object o, DoWorkEventArgs ev) =>
                {
                    //////////////////////////////////////////////////////////////////////////
                    // PRE-DOWNLOAD ACTIONS
                    //////////////////////////////////////////////////////////////////////////
                    List<WOLArticle> articles = new List<WOLArticle>();
                    string[] publications = PublicationsToDownload(list);
                    int count = publications.Count();

                    // Rest progress
                    progress.Progress = 0;
                    progress.Max = 4;
                    App.STATE.Activity.RunOnUiThread(() =>
                    {
                        progress.SetTitle("Collecting Information  . . .");
                        progress.SetMessage("Requires a lot of time. Please be patient.");
                    });
                    
                    if (publications.Contains(PublicationType.Bible))
                    {
                        App.STATE.PrimaryBibleBooks = DownloadBibleBookNames(primaryLanguage);
                        progress.Progress++;
                        App.STATE.SecondaryBibleBooks = DownloadBibleBookNames(secondaryLanguage);
                        progress.Progress++;
                    }                    
                    if (publications.Contains(PublicationType.Insight))
                    {
                        App.STATE.PrimaryInsightArticles = DownloadInsightArticleNames(primaryLanguage, Storehouse.Primary);
                        progress.Progress++;
                        App.STATE.SecondaryInsightArticles = DownloadInsightArticleNames(secondaryLanguage, Storehouse.Secondary);
                        progress.Progress++;
                    }

                    App.STATE.PrimaryBooks = new List<WOLArticle>();
                    App.STATE.SecondaryBooks = new List<WOLArticle>();


                    //////////////////////////////////////////////////////////////////////////
                    // CHECK FOR PINYIN
                    //////////////////////////////////////////////////////////////////////////
                    // Reset time
                    time = DateTime.Now;

                    // Rest progress
                    progress.Progress = 0;
                    progress.Max = 0;
                    App.STATE.Activity.RunOnUiThread(() =>
                    {
                        progress.SetTitle("Collecting Information  . . .");
                        progress.SetMessage("Preparing to download Chinese Pinyin articles. Please be patient.");
                    });

                    if (primaryLanguage.EnglishName.Contains("Pinyin"))
                    {
                        List<WOLArticle> PinyinArticles = GeneratePinyinPublicationLinks(publications);
                        progress.Max = PinyinArticles.Count();

                        foreach (var pinyin in PinyinArticles)
                        {
                            App.STATE.Activity.RunOnUiThread(() =>
                            {
                                TimeSpan timeRemaining = TimeSpan.FromTicks(DateTime.Now.Subtract(time).Ticks * (progress.Max - (progress.Progress + 1)) / (progress.Progress + 1));
                                progress.SetTitle("Downloading . . .");
                                progress.SetMessage("Transferring Chinese Pinyin Library publications to storehouse.\n\n" + pinyin.PublicationNamePinyin + "\n\nAbout " + timeRemaining.ToString(@"h\:mm\:ss") + " remaining.");
                            });

                            StorePinyinPublication(Storehouse.Primary, pinyin, progress);
                            progress.Progress++;
                        }
                    }                    
                    if (secondaryLanguage.EnglishName.Contains("Pinyin"))
                    {
                        List<WOLArticle> PinyinArticles = GeneratePinyinPublicationLinks(publications);
                        progress.Max = PinyinArticles.Count();

                        foreach (var pinyin in PinyinArticles)
                        {
                            App.STATE.Activity.RunOnUiThread(() =>
                            {
                                TimeSpan timeRemaining = TimeSpan.FromTicks(DateTime.Now.Subtract(time).Ticks * (progress.Max - (progress.Progress + 1)) / (progress.Progress + 1));
                                progress.SetTitle("Downloading . . .");
                                progress.SetMessage("Transferring Chinese Pinyin Library articles to storehouse.\n\n" + pinyin.PublicationNamePinyin + "\n\nAbout " + timeRemaining.ToString(@"h\:mm\:ss") + " remaining.");
                            });

                            StorePinyinPublication(Storehouse.Secondary, pinyin, progress);
                            progress.Progress++;
                        }
                    }


                    //////////////////////////////////////////////////////////////////////////
                    // ANY OTHER LANGUAGE
                    //////////////////////////////////////////////////////////////////////////
                    // Rest progress
                    progress.Progress = 0;
                    progress.Max = (count > 0) ? count * 2 : 0;

                    // Generate all article links
                    foreach (var publication in publications)
                    {
                        if (!primaryLanguage.EnglishName.Contains("Pinyin"))
                        {
                            progress.Progress++;
                            App.STATE.Activity.RunOnUiThread(() =>
                            {
                                progress.SetTitle("Opening Storehouse  . . .");
                                progress.SetMessage("Gathering articles from Watchtower ONLINE Library. [" + publication + "]\n\n" + primaryLanguage.Name);
                            });

                            if(publication != "it")
                            {
                                Console.WriteLine("NOT INSIGHT!");
                                articles.AddRange(GenerateWOLArticles(primaryLanguage, Storehouse.Primary, publication));
                            }
                            else
                            {
                                Console.WriteLine("INSIGHT!");
                                articles.AddRange(GenerateWOLInsightArticles(primaryLanguage, Storehouse.Primary));
                            }
                        }

                        if (!secondaryLanguage.EnglishName.Contains("Pinyin"))
                        {
                            progress.Progress++;
                            App.STATE.Activity.RunOnUiThread(() =>
                            {
                                progress.SetTitle("Opening Storehouse  . . .");
                                progress.SetMessage("Gathering articles from Watchtower ONLINE Library. [" + publication + "]\n\n" + secondaryLanguage.Name);
                            });

                            if (publication != "it")
                            {
                                articles.AddRange(GenerateWOLArticles(secondaryLanguage, Storehouse.Secondary, publication));
                            }
                            else
                            {
                                articles.AddRange(GenerateWOLInsightArticles(secondaryLanguage, Storehouse.Secondary));
                            }
                        }
                    }

                    // Rest progress
                    progress.Progress = 0;
                    progress.Max = (articles.Count > 0) ? articles.Count : 0;

                    //////////////////////////////////////////////////////////////////////////
                    // INITIALIZE DOWNLOAD PARADIGM
                    //////////////////////////////////////////////////////////////////////////     
                    // Reset time
                    time = DateTime.Now;

                    // Download all articles
                    foreach (WOLArticle article in articles)
                    {
                        if (worker.CancellationPending)
                        {
                            JwStore database = new JwStore(Storehouse.Primary);
                            database.Open();
                            database.DeleteAllTables();
                            database.Close();

                            database = new JwStore(Storehouse.Secondary);
                            database.Open();
                            database.DeleteAllTables();
                            database.Close();

                            ev.Cancel = true;

                            return;
                        }

                        StoreWOLArticle(article);

                        progress.Progress++;
                        App.STATE.Activity.RunOnUiThread(() =>
                        {
                            progress.SetTitle("Downloading . . .");
                            TimeSpan timeRemaining = TimeSpan.FromTicks(DateTime.Now.Subtract(time).Ticks * (progress.Max - (progress.Progress + 1)) / (progress.Progress + 1));
                            progress.SetMessage("Transferring Watchtower ONLINE Library articles to storehouse.\n\n" + article.PublicationName + " — " + article.ArticleTitle + "\n\nAbout " + timeRemaining.ToString(@"h\:mm\:ss") + " remaining.");
                        });
                    }                    

                    //////////////////////////////////////////////////////////////////////////
                    // DOWNLOAD COMPLETE
                    //////////////////////////////////////////////////////////////////////////
                    progress.Dismiss();                    
                    App.STATE.Activity.RunOnUiThread(() =>
                    {
                        // Reset the drawer to show only what was downloaded
                        MainLibraryActivity activity = context as MainLibraryActivity;
                        List<Library> libraries = new List<Library>();
                        foreach (string item in publications)
                        {
                            if (item == PublicationType.Bible)
                            {
                                libraries.Add(Library.Bible);
                            }
                            else if (item == PublicationType.Insight)
                            {
                                libraries.Add(Library.Insight);
                            }
                            else if (item == PublicationType.DailyText)
                            {
                                libraries.Add(Library.DailyText);
                            }
                            else if (item == PublicationType.Books)
                            {
                                libraries.Add(Library.Books);
                            }
                        }

                        //activity.list.Adapter = new ArrayAdapter<string>(this, Resource.Layout.DrawerListItem, Resources.GetStringArray(Resource.Array.LibraryNavigation));
                        App.STATE.Libraries = libraries;
                        App.STATE.CurrentLibrary = libraries.FirstOrDefault();

                        List<string> navigation = new List<string>();
                        string[] nav = App.STATE.Context.Resources.GetStringArray(Resource.Array.LibraryNavigation);
                        foreach (Library m in libraries)
                        {
                            navigation.Add(nav[(int)m]);
                        }
                        activity.list.Adapter = new ArrayAdapter<string>(context, Resource.Layout.DrawerListItem, navigation);

                        Toast.MakeText(App.STATE.Activity, "Transfer complete. Storehouse updated.", ToastLength.Long).Show();
                    });

                    App.STATE.PrimaryLanguage = primaryLanguage;
                    App.STATE.SecondaryLanguage = secondaryLanguage;
                    App.STATE.Language = primaryLanguage.EnglishName;

                    App.STATE.SaveUserPreferences();
                };
                worker.RunWorkerCompleted += (object o, RunWorkerCompletedEventArgs a) =>
                {
                    progress.Dismiss();
                    App.STATE.PrimaryLanguage = primaryLanguage;
                    App.STATE.SecondaryLanguage = secondaryLanguage;
                    App.STATE.Language = primaryLanguage.EnglishName;
                    Toast.MakeText(App.STATE.Activity, "Download complete!", ToastLength.Long).Show();
                };

                worker.RunWorkerAsync();
            });

            builder.SetNegativeButton("Cancel", delegate
            {
                Toast.MakeText(App.STATE.Activity, "Nothing happened. Please restart the application, or go to MENU > RESET LIBRARY.", ToastLength.Long).Show();
            });

            return builder.Create();
        }

        public Dialog PresentationDialog(Context context, string title, string content, bool external = false)
        {
            // Decide what dialog theme to use
            ContextThemeWrapper wrapper = new ContextThemeWrapper(context, GetDialogTheme());

            AlertDialog.Builder dialog = new AlertDialog.Builder(wrapper);
            dialog.SetTitle(title);
            dialog.SetIcon(Resource.Drawable.Icon);
            dialog.SetNegativeButton("X",
                (o, args) =>
                {
                    // Close dialog
                });

            LayoutInflater inflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            View view = inflater.Inflate(Resource.Layout.DialogWebView, null);
            WebView presentationWebView = view.FindViewById<WebView>(Resource.Id.dialogWebView);

            StorehouseWebViewClient client = new StorehouseWebViewClient(external);
            presentationWebView.SetWebViewClient(client);
            presentationWebView.Settings.JavaScriptEnabled = true;
            presentationWebView.Settings.BuiltInZoomControls = true;
            presentationWebView.VerticalScrollBarEnabled = false;
            presentationWebView.Settings.DefaultFontSize = GetWebViewTextSize(App.STATE.SeekBarTextSize);
            presentationWebView.LoadDataWithBaseURL("file:///android_asset/", content, "text/html", "utf-8", null);

            dialog.SetView(view);

            return dialog.Create();
        }

        public Dialog PresentationUrlDialog(Activity activity, string url)
        {
            // Decide what dialog theme to use
            ContextThemeWrapper context = new ContextThemeWrapper(activity, GetDialogTheme());

            AlertDialog.Builder dialog = new AlertDialog.Builder(context);
            dialog.SetTitle("Watchtower ONLINE Library");
            dialog.SetIcon(Resource.Drawable.Icon);
            dialog.SetNegativeButton("X",
                (o, args) =>
                {
                    // Close dialog
                });

            LayoutInflater inflater = (LayoutInflater)activity.GetSystemService(Context.LayoutInflaterService);
            View layout = inflater.Inflate(Resource.Layout.DialogWebView, null);
            WebView presentationWebView = layout.FindViewById<WebView>(Resource.Id.dialogWebView);

            StorehouseWebViewClient client = new StorehouseWebViewClient();
            presentationWebView.SetWebViewClient(client);
            presentationWebView.Settings.JavaScriptEnabled = true;
            presentationWebView.Settings.BuiltInZoomControls = true;
            presentationWebView.VerticalScrollBarEnabled = false;
            presentationWebView.Settings.DefaultFontSize = App.STATE.SeekBarTextSize;

            presentationWebView.LoadUrl(url);

            dialog.SetView(layout);

            return dialog.Create();
        }

        private string[] PublicationsToDownload(ListView grid)
        {
            string[] publicationTypes = App.STATE.Context.Resources.GetStringArray(Resource.Array.PublicationType);

            var checkedItems = grid.CheckedItemPositions;
            string[] selected = new string[checkedItems.Size()];
            if (checkedItems != null)
            {
                for (int i = 0; i < checkedItems.Size(); i++)
                {
                    if (checkedItems.ValueAt(i))
                    {
                        selected[i] = publicationTypes[checkedItems.KeyAt(i)];
                    }
                }
            }

            return selected;
        }

        void grid_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            //GridView grid = (sender as GridView);

            //var checkedItems = grid.CheckedItemPositions;
            //string[] selected = new string[checkedItems.Size()];
            //if (checkedItems != null)
            //{
            //    for (int i = 0; i < checkedItems.Size(); i++)
            //    {
            //        if (checkedItems.ValueAt(i))
            //        {
            //            string x = grid.Adapter.GetItem(checkedItems.KeyAt(i)).ToString();
            //            selected[i] = x;
            //            Console.WriteLine(x + " is selected.");
            //        }
            //    }
            //}
            //Console.WriteLine((sender as GridView).CheckedItemCount);
        }

        public Dialog DownloadLanguagePackDialog(Context context)
        {
            string[] available = GetAvailableLanguages().Select(s => s.Name).ToArray();
            string[] names = GetAvailableLanguages().Select(s => s.EnglishName).ToArray();
            bool[] isDownloaded = IsLanguagesDownloaded(names);

            var builder = new AlertDialog.Builder(context);
            builder.SetIcon(Resource.Drawable.Icon);
            builder.SetTitle("Download Language");
            builder.SetCancelable(false);
            builder.SetMultiChoiceItems(available, isDownloaded, (sender, e) =>
            {
                int index = e.Which;

                isDownloaded[index] = e.IsChecked;
            });

            builder.SetPositiveButton("Ok", (sender, e) =>
            {

            });
            builder.SetNegativeButton("Cancel", delegate
            {

            });

            return builder.Create();
        }

        public ProgressDialog DownloadingProgressDialog(Context context, string title, string message, EventHandler<DialogClickEventArgs> cancel = null)
        {
            View customTitle = View.Inflate(context, Resource.Layout.DialogTitle, null);
            TextView alertTitle = customTitle.FindViewById<TextView>(Resource.Id.alertTitle);
            ImageView alertIcon = customTitle.FindViewById<ImageView>(Resource.Id.icon);

            alertTitle.Text = title;
            alertIcon.SetImageResource(Resource.Drawable.Icon);

            //ContextThemeWrapper wrapper = new ContextThemeWrapper(context, Resource.Style.Theme_Storehouse_AlertDialogStyle);
            ProgressDialog progress = new ProgressDialog(context);
            progress.SetMessage(message);
            progress.SetCustomTitle(customTitle);
            progress.SetButton("Cancel", cancel);
            progress.SetProgressStyle(ProgressDialogStyle.Horizontal);
            progress.SetCancelable(false);
            progress.SetCanceledOnTouchOutside(false);
            //((ViewGroup)progress.Window.DecorView).GetChildAt(0).SetBackgroundColor(Color.Black);

            return progress;
        }

        //////////////////////////////////////////////////////////////////////////
        // OTHER Section
        //////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Calculates the eta.
        /// </summary>
        /// <param name="processStarted">When the process started</param>
        /// <param name="totalElements">How many items are being processed</param>
        /// <param name="processedElements">How many items are done</param>
        /// <returns>A string representing the time left</returns>
        private string CalculateEta(DateTime processStarted, int totalElements, int processedElements)
        {
            int itemsPerSecond = processedElements / (int)(processStarted - DateTime.Now).TotalSeconds;

            if (itemsPerSecond <= 0)
            {
                return new TimeSpan(0, 0, 0).ToString();
            }

            int secondsRemaining = (totalElements - processedElements) / itemsPerSecond;

            return new TimeSpan(0, 0, secondsRemaining).ToString();
        }

        public List<InsightArticle> GetInsightArticlesByGroup(int group)
        {
            string[] groups = App.STATE.PrimaryInsightGroups;
            List<InsightArticle> articles = App.STATE.PrimaryInsightArticles;

            return articles.Where(a => a.Group == groups.ElementAt(group).ToString()).OrderBy(a => a.OrderNumber).ToList();
        }

        public string[] GetAllPublicationCodes(string publicationType)
        {
            string xml = string.Empty;
            List<string> codes = new List<string>();

            Stream stream = App.STATE.Context.Assets.Open(publicationType + ".xml");
            using (StreamReader reader = new StreamReader(stream))
            {
                xml = reader.ReadToEnd();
            }

            XDocument doc = XDocument.Parse(xml);
            foreach (XElement publication in doc.Descendants("p"))
            {
                codes.Add(publication.Attribute("code").Value);
            }

            return codes.ToArray();
        }

        public int GetWebViewTextSize(int multiplier)
        {
            int size = (multiplier * App.STATE.TextSizeMultiplier) + App.STATE.TextSizeBase;

            return size;
        }

        public int GetDialogTheme()
        {
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.IceCreamSandwich)
            {
                return Resource.Style.Theme_Storehouse_AlertDialogStyle;
            }
            else if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Honeycomb)
            {
                return Android.Resource.Style.ThemeHoloLightDialog;
            }
            else
            {
                return Android.Resource.Style.ThemeDialog;
            }
        }

        public string FormatDateTime(DateTime input)
        {
            return input.ToString(@"yyyy.M.d");
        }

        public bool ConnectedToNetwork(Context context)
        {
            bool connected = false;

            ConnectivityManager connectivityManager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
            if (connectivityManager != null)
            {
                bool mobileNetwork = false;
                bool wifiNetwork = false;
                bool wimaxNetwork = false;

                bool mobileNetworkConnected = false;
                bool wifiNetworkConnected = false;
                bool wimaxNetworkConnected = false;

                NetworkInfo mobileInfo = connectivityManager.GetNetworkInfo(ConnectivityType.Mobile);
                NetworkInfo wifiInfo = connectivityManager.GetNetworkInfo(ConnectivityType.Wifi);
                NetworkInfo wimaxInfo = connectivityManager.GetNetworkInfo(ConnectivityType.Wimax);

                if (mobileInfo != null)
                {
                    mobileNetwork = mobileInfo.IsAvailable;
                    //Console.WriteLine("Is mobile available?  " + mobileNetwork);
                }

                if (wifiInfo != null)
                {
                    wifiNetwork = wifiInfo.IsAvailable;
                    //Console.WriteLine("Is WiFi available?  " + wifiNetwork);
                }

                if (wimaxInfo != null)
                {
                    wimaxNetwork = wimaxInfo.IsAvailable;
                    //Console.WriteLine("Is WiMAX available?  " + wimaxNetwork);
                }

                if (wifiNetwork || mobileNetwork || wimaxNetwork)
                {
                    mobileNetworkConnected = (mobileInfo != null) ? mobileInfo.IsConnectedOrConnecting : false;
                    wifiNetworkConnected = (wifiInfo != null) ? wifiInfo.IsConnectedOrConnecting : false;
                    wimaxNetworkConnected = (wimaxInfo != null) ? wimaxInfo.IsConnectedOrConnecting : false;
                }

                connected = (mobileNetworkConnected || wifiNetworkConnected || wimaxNetworkConnected);

                //Console.WriteLine("Is mobile connected?  " + mobileNetworkConnected);
                //Console.WriteLine("Is WiFi connected?  " + wifiNetworkConnected);
                //Console.WriteLine("Is WiMAX connected?  " + wimaxNetworkConnected);
            }

            Console.WriteLine("Is this device connected?  " + connected);

            return connected;
        }

        public SpannableString CreateSpanString(string text, Color color)
        {
            var span = new SpannableString(text);
            span.SetSpan(new ForegroundColorSpan(color), 0, span.Length(), 0);

            return span;
        }

        /// <summary>
        /// Remove digits from string.
        /// </summary>
        public string RemoveDigits(string key)
        {
            return Regex.Replace(key, @"\d", "");
        }

        public string ToUpper(string value)
        {
            char[] array = value.ToCharArray();
            // Handle the first letter in the string.
            if (array.Length >= 1)
            {
                if (char.IsLower(array[0]))
                {
                    array[0] = char.ToUpper(array[0]);
                }
            }
            // Scan through the letters, checking for spaces.
            // ... Uppercase the lowercase letters following spaces.
            for (int i = 1; i < array.Length; i++)
            {
                //if ((array[i - 1] == ' ') || (array[i - 1] == '—') || (array[i - 1] == '-') || (array[i - 1] == '/'))
                //{
                if (!Char.IsLetter(array[i - 1]))
                {
                    if (char.IsLower(array[i]))
                    {
                        array[i] = char.ToUpper(array[i]);
                    }
                }
            }
            return new string(array);
        }

        /// <summary>
        /// Remove HTML tags from string
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public string RemoveHTMLTags(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }
    }
}