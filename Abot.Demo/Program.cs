
using Abot.Crawler;
using Abot.Poco;
using System;
using System.Net;
using SQLMethodsNameSpace;
using System.Collections.Generic;

namespace Abot.Demo
{
    class Program
    {
        //Consider creating a website class which holds roots, identifiers (price, title, url) and other info
        static string walmartRoot = "https://www.walmart.com/search/?query=";
        static string loblawsRoot = "https://www.loblaws.ca/search/?search-bar=";
        static string rcsRoot = "https://www.realcanadiansuperstore.ca/search/?search-bar=";

        static string walmartURLIdentifier = "/ip/";
        static string loblawsURLIdentifier = "/Food/";
        static string rcsURLIdentifier = "/Food/";

        static string walmartTitleIdentifier = "ProductTitle";
        static string walmartPriceIdentifier = "price-characteristic";

        static string rcsPriceIdentifier = "reg-price-text";
        static string rcsTitleIdentifier = "product-name";

        static string loblawsPriceIdentifier = "price__value selling-price-list__item__price selling-price-list__item__price--now-price__value";
        static string loblawsTitleIdentifier = "product-name__item product-name__item--name";

        static void Main(string[] args)
        {
            Crawl(args);
        }

        public static void Crawl(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            //We'll use this List in the foreach below
            List<Uri> URLList = new List<Uri>();

            //A list of (generic) shopping items we will search for during crawl
            List<string> genericProductNames = SQLMethods.GetGenericProductNames();

            //Clear SQL Product Table contents (from last crawl)
            SQLMethods.DropProductTable();

            foreach (string genericProductName in genericProductNames)
            {
                Uri siteUri = new Uri(walmartRoot + genericProductName);
                Uri uriToCrawl = siteUri;

                IWebCrawler crawler;
                crawler = GetManuallyConfiguredWebCrawler();
                //crawler.CrawlBag = genericProductName;

                //Subscribe to any of these asynchronous events, there are also sychronous versions of each.
                //This is where you process data about specific events of the crawl
                crawler.PageCrawlStartingAsync += crawler_ProcessPageCrawlStarting;
                crawler.PageCrawlCompletedAsync += crawler_ProcessPageCrawlCompleted;
                crawler.PageCrawlDisallowedAsync += crawler_PageCrawlDisallowed;
                crawler.PageLinksCrawlDisallowedAsync += crawler_PageLinksCrawlDisallowed;

                //This is a synchronous call
                CrawlResult result = crawler.Crawl(uriToCrawl);
            }
        }

        static void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            {
                CrawledPage crawledPage = e.CrawledPage;

                if (crawledPage.WebException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
                    Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
                else
                    Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);

                //If the URL we're searching has a Food Item URL identifier, scrape the data
                if (crawledPage.Uri.ToString().Contains(walmartURLIdentifier))
                {
                    var title = crawledPage.AngleSharpHtmlDocument.GetElementsByClassName(walmartTitleIdentifier);
                    var productName = title[0].TextContent;

                    var price = crawledPage.AngleSharpHtmlDocument.GetElementsByClassName(walmartPriceIdentifier);
                    var productPrice = price[0].ParentElement.TextContent;

                    var site = crawledPage.ParentUri.Host;
                    var category = "";

                    //Send the scraped data for this specific listing to the SQL Database
                    SQLMethods.InsertProductRecord(productName, category, productPrice, site);

                    /*Console.WriteLine("***********************************");
                    Console.WriteLine(title[0].TextContent);
                    Console.WriteLine(price[0].ParentElement.TextContent);
                    Console.WriteLine(crawledPage.ParentUri.Host);
                    Console.WriteLine("***********************************");
                    Console.WriteLine();*/
                }
            }
        }

        private static IWebCrawler GetDefaultWebCrawler()
        {
            return new PoliteWebCrawler();
        }

        private static IWebCrawler GetManuallyConfiguredWebCrawler()
        {
            //Create a config object manually
            CrawlConfiguration config = new CrawlConfiguration();
            config.CrawlTimeoutSeconds = 0;
            config.DownloadableContentTypes = "text/html, text/plain";
            config.IsExternalPageCrawlingEnabled = false;
            config.IsExternalPageLinksCrawlingEnabled = false;
            config.IsRespectRobotsDotTextEnabled = false;
            config.IsUriRecrawlingEnabled = false;
            config.MaxConcurrentThreads = 3;
            config.MaxPagesToCrawl = 30;
            config.MaxPagesToCrawlPerDomain = 0;
            config.MinCrawlDelayPerDomainMilliSeconds = 300;

            //Add you own values without modifying Abot's source code.
            //These are accessible in CrawlContext.CrawlConfuration.ConfigurationException object throughout the crawl
            config.ConfigurationExtensions.Add("Somekey1", "SomeValue1");
            config.ConfigurationExtensions.Add("Somekey2", "SomeValue2");

            //Initialize the crawler with custom configuration created above.
            //This override the app.config file values
            return new PoliteWebCrawler(config, null, null, null, null, new Core.AngleSharpHyperlinkParser(), null, null, null);
        }
        

        private static IWebCrawler GetCustomBehaviorUsingLambdaWebCrawler()
        {
            IWebCrawler crawler = GetDefaultWebCrawler();

            //Register a lambda expression that will make Abot not crawl any url that has the word "ghost" in it.
            //For example http://a.com/ghost, would not get crawled if the link were found during the crawl.
            //If you set the log4net log level to "DEBUG" you will see a log message when any page is not allowed to be crawled.
            //NOTE: This is lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPage method is run.
            crawler.ShouldCrawlPage((pageToCrawl, crawlContext) =>
            {
                if (pageToCrawl.Uri.AbsoluteUri.Contains("ghost"))
                    return new CrawlDecision { Allow = false, Reason = "Scared of ghosts" };

                return new CrawlDecision { Allow = true };
            });

            //Register a lambda expression that will tell Abot to not download the page content for any page after 5th.
            //Abot will still make the http request but will not read the raw content from the stream
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldDownloadPageContent method is run
            crawler.ShouldDownloadPageContent((crawledPage, crawlContext) =>
            {
                if (crawlContext.CrawledCount >= 5)
                    return new CrawlDecision { Allow = false, Reason = "We already downloaded the raw page content for 5 pages" };

                return new CrawlDecision { Allow = true };
            });

            //Register a lambda expression that will tell Abot to not crawl links on any page that is not internal to the root uri.
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPageLinks method is run
            crawler.ShouldCrawlPageLinks((crawledPage, crawlContext) =>
            {
                if (!crawledPage.IsInternal)
                    return new CrawlDecision { Allow = false, Reason = "We dont crawl links of external pages" };

                return new CrawlDecision { Allow = true };
            });

            return crawler;
        }

        private static Uri GetSiteToCrawl(string[] args)
        {
            string userInput = "";
            if (args.Length < 1)
            {
                System.Console.WriteLine("Please enter ABSOLUTE url to crawl:");
                userInput = System.Console.ReadLine();
            }
            else
            {
                userInput = args[0];
            }

            if (string.IsNullOrWhiteSpace(userInput))
                throw new ApplicationException("Site url to crawl is as a required parameter");

            return new Uri(userInput);
        }

        private static void PrintDisclaimer()
        {
            PrintAttentionText("The demo is configured to only crawl a total of 10 pages and will wait 1 second in between http requests. This is to avoid getting you blocked by your isp or the sites you are trying to crawl. You can change these values in the app.config or Abot.Console.exe.config file.");
        }

        private static void PrintAttentionText(string text)
        {
            ConsoleColor originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(text);
            System.Console.ForegroundColor = originalColor;
        }

        static void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            //Process data
        }


        static void crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            //Process data
        }

        static void crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            //Process data
        }
    }
}
