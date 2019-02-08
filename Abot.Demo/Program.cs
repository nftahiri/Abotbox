
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
        static string category;

        static Website walmart = new Website("https://www.walmart.com/search/?query=", "/ip/", "ProductTitle", "price-characteristic", 25);
        static Website nofrills = new Website("https://www.nofrills.ca/search/?search-bar=", category, "product-name", "reg-price-text", 100);
        static Website rcs = new Website("https://www.realcanadiansuperstore.ca/search/?search-bar=", category, "product-name", "reg-price-text", 100);
        static Website extraFoods = new Website("https://www.extrafoods.ca/search/?search-bar=", category, "product-name", "reg-price-text", 100);

        static Website siteToCrawl = new Website();

        static void Main(string[] args)
        {
            //Clean the database (previous crawl) - consider storing this in another table (have 1 backup)
            //SQLMethods.DropProductTable();

            //List<Website> sitesToCrawl = new List<Website>(new Website[] {walmart, nofrills, rcs, extraFoods});

            //Testing//
            List<Website> sitesToCrawl = new List<Website>(new Website[] {walmart});

            foreach (Website site in sitesToCrawl)
            {
                siteToCrawl = site;
                Crawl(args, site);
            }

            //Testing
            //siteToCrawl = nofrills;
            //Crawl(args, siteToCrawl);
        }

        public static void Crawl(string[] args, Website website)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            log4net.Config.XmlConfigurator.Configure();

            //We'll use this List in the foreach loops below
            List<Uri> URLList = new List<Uri>();

            //A list of (generic) shopping items we will search for during crawl
            List<string> genericProductNames = SQLMethods.GetGenericProductNames();
            
            //Testing
            //List<string> genericProductNames = new List<string>();
            //genericProductNames.Add("suspicious grocery item");

            foreach (string genericProductName in genericProductNames)
            {
                //Product category that will be stored in db = the generic item
                category = genericProductName;
                
                //Some sites can't handle spaces in URL, so replace with a '+' 
                Uri siteUri = new Uri(website.Root + genericProductName.Replace(" ", "+"));

                Uri uriToCrawl = siteUri;

                IWebCrawler crawler;

                //Walmart works well with the manually configured webcrawler
                if(website.Root.Contains("walmart"))
                    crawler = GetManuallyConfiguredWebCrawler(siteToCrawl);

                //Loblaw owned sites need some extra 'checks' in each URL, so use custom crawler in this case
                else
                    crawler = GetCustomBehaviorUsingLambdaWebCrawler();

                //Subscribe to any of these asynchronous events, there are also sychronous versions of each.
                //This is where you process data about specific events of the crawl
                crawler.PageCrawlStartingAsync += crawler_ProcessPageCrawlStarting;
                crawler.PageCrawlCompletedAsync += crawler_ProcessPageCrawlCompleted;
                crawler.PageCrawlDisallowedAsync += crawler_PageCrawlDisallowed;
                crawler.PageLinksCrawlDisallowedAsync += crawler_PageLinksCrawlDisallowed;

                //This is a synchronous call
                CrawlResult result = crawler.Crawl(uriToCrawl);
                //Console.Read();
            }
        }

        static void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            {
                string productName;
                string productNameTwo = "";
                string productPrice;
                string productCategory;
                string site;

                CrawledPage crawledPage = e.CrawledPage;

                //If the URL we're searching has a Food Item URL identifier, scrape the data
                var title = crawledPage.AngleSharpHtmlDocument.GetElementsByClassName(siteToCrawl.ProductTitleIdentifier);
                var price = crawledPage.AngleSharpHtmlDocument.GetElementsByClassName(siteToCrawl.ProductPriceIdentifier);
                if (title.Length < 1)
                    return;

                if (crawledPage.WebException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
                    Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
                else 
                    Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);
                
                
                if(crawledPage.Uri.AbsoluteUri.Contains("walmart"))
                {
                    productName = title[0].TextContent;
                    price = crawledPage.AngleSharpHtmlDocument.GetElementsByClassName(siteToCrawl.ProductPriceIdentifier);
                    productPrice = price[0].ParentElement.TextContent;
                }
                else
                {
                    productName = title[1].Children[1].TextContent;
                    productNameTwo = title[1].Children[0].InnerHtml;
                    price = crawledPage.AngleSharpHtmlDocument.GetElementsByClassName(siteToCrawl.ProductPriceIdentifier);
                    productPrice = price[0].InnerHtml;
                }

                var pluralCategory = category + "s";
                var singularCategory = category.Remove(category.Length - 1);

                if (productNameTwo.ToLower().Contains(category.ToLower()))
                    productName = productNameTwo;

                if (crawledPage.Uri.ToString().Contains(singularCategory.Replace(" ","+")) && 
                    (productName.ToLower().Contains(category.ToLower()) ||
                    productName.ToLower().Contains(singularCategory.ToLower()) ||
                    productName.ToLower().Contains(pluralCategory.ToLower())) )
                {                   

                    site = crawledPage.ParentUri.Host;
                    productCategory = category;

                    Console.WriteLine();
                    Console.WriteLine("********************************");
                    Console.WriteLine("Would insert:");
                    Console.WriteLine(productName);
                    Console.WriteLine(productPrice);
                    Console.WriteLine("********************************");
                    Console.WriteLine();

                    //Send the scraped data for this specific listing to the SQL Database
                    //SQLMethods.InsertProductRecord(productName, productCategory, productPrice, site);
                }
            }
        }

        private static IWebCrawler GetDefaultWebCrawler()
        {
            return new PoliteWebCrawler();
        }

        private static IWebCrawler GetManuallyConfiguredWebCrawler(Website site)
        {
            //Create a config object manually
            CrawlConfiguration config = new CrawlConfiguration();
            config.CrawlTimeoutSeconds = 0;
            config.DownloadableContentTypes = "text/html, text/plain";
            config.IsExternalPageCrawlingEnabled = false;
            config.IsExternalPageLinksCrawlingEnabled = false;
            config.IsRespectRobotsDotTextEnabled = false;
            config.IsUriRecrawlingEnabled = false;
            config.MaxConcurrentThreads = 10;
            config.MaxPagesToCrawl = site.NumberOfSitesToCrawl;
            config.MaxPagesToCrawlPerDomain = 0;
            config.MinCrawlDelayPerDomainMilliSeconds = 250;
            //config.HttpRequestTimeoutInSeconds = 60;
            //config.IsSendingCookiesEnabled = true;
            //config.IsSslCertificateValidationEnabled = false;
            config.HttpProtocolVersion = HttpProtocolVersion.Version12;

            //Initialize the crawler with custom configuration created above.
            //This override the app.config file values
            return new PoliteWebCrawler(config, null, null, null, null, new Core.AngleSharpHyperlinkParser(), null, null, null);
        }
        
        private static IWebCrawler GetCustomBehaviorUsingLambdaWebCrawler()
        {
            IWebCrawler crawler = GetManuallyConfiguredWebCrawler(siteToCrawl);

            //Register a lambda expression that will make Abot not crawl any url that has the word "ghost" in it.
            //For example http://a.com/ghost, would not get crawled if the link were found during the crawl.
            //If you set the log4net log level to "DEBUG" you will see a log message when any page is not allowed to be crawled.
            //NOTE: This is lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPage method is run.
            crawler.ShouldCrawlPage((pageToCrawl, crawlContext) =>
            {
                //if (!pageToCrawl.Uri.AbsoluteUri.Contains("chicken") && !pageToCrawl.Uri.AbsoluteUri.Contains("Chicken"))
                if (!pageToCrawl.Uri.AbsoluteUri.Contains(category.Replace(" ", "+")) || /*pageToCrawl.Uri.AbsoluteUri.Contains("navid")||*/ pageToCrawl.Uri.AbsoluteUri.Contains("_KG") || pageToCrawl.Uri.AbsoluteUri.Contains("_EA"))
                    return new CrawlDecision { Allow = false, Reason = "I only crawl the right pages" };

                return new CrawlDecision { Allow = true };
            });

            //Register a lambda expression that will tell Abot to not download the page content for any page after 5th.
            //Abot will still make the http request but will not read the raw content from the stream
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldDownloadPageContent method is run
            /*crawler.ShouldDownloadPageContent((crawledPage, crawlContext) =>
            {
                if (crawlContext.CrawledCount >= 5)
                    return new CrawlDecision { Allow = false, Reason = "We already downloaded the raw page content for 5 pages" };

                return new CrawlDecision { Allow = true };
            });*/

            //Register a lambda expression that will tell Abot to not crawl links on any page that is not internal to the root uri.
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPageLinks method is run
            crawler.ShouldCrawlPageLinks((crawledPage, crawlContext) =>
            {
                CrawlDecision decision = new CrawlDecision { Allow = true };
                if (crawledPage.Content.Bytes.Length < 100)
                    return new CrawlDecision { Allow = false, Reason = "Just crawl links in pages that have at least 100 bytes" };

                return decision;
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
            //PrintAttentionText("The demo is configured to only crawl a total of 10 pages and will wait 1 second in between http requests. This is to avoid getting you blocked by your isp or the sites you are trying to crawl. You can change these values in the app.config or Abot.Console.exe.config file.");
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
            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine("About to crawl link {0} which was found on page {1}", pageToCrawl.Uri.AbsoluteUri, pageToCrawl.ParentUri.AbsoluteUri);

        }


        static void crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            //CrawledPage crawledPage = e.CrawledPage;
            //Console.WriteLine("Did not crawl the links on page {0} due to {1}", crawledPage.Uri.AbsoluteUri, e.DisallowedReason);

        }

        static void crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            //PageToCrawl pageToCrawl = e.PageToCrawl;
            //Console.WriteLine("Did not crawl page {0} due to {1}", pageToCrawl.Uri.AbsoluteUri, e.DisallowedReason);
        }
    }
}
