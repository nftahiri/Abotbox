using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Abot.Demo
{
    class Website
    {
        //No argument constructor
        public Website()
        {
            Root = "";
            URLIdentifier = "";
            ProductTitleIdentifier = "";
            ProductPriceIdentifier = "";
            NumberOfSitesToCrawl = 0;
        }

        // Constructor that takes arguments:
        public Website(string root, string urlIdentifier, string productTitleIdentifier, string productPriceIdentifier, int numberOfSitesToCrawl)
        {
            Root = root;
            URLIdentifier = urlIdentifier;
            ProductTitleIdentifier = productTitleIdentifier;
            ProductPriceIdentifier = productPriceIdentifier;
            NumberOfSitesToCrawl = numberOfSitesToCrawl;
        }

        public int NumberOfSitesToCrawl
        {
            get;
            set;
        }

        public string Root
        {
            get;
            set;
        }
        public string URLIdentifier
        {
            get;
            set;
        }

        public string ProductTitleIdentifier
        {
            get;
            set;
        }

        public string ProductPriceIdentifier
        {
            get;
            set;
        }
    }
}
