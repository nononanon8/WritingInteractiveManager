using System;
using System.Collections.Generic;
using System.Text;

namespace WIMCore
{
    public static class ParseParams
    {
        // Global context.
        public static string PageTitleXPath = "/html/head/title";

        // Main interactive page.
        public static string IABaseUrl = "https://www.writing.com/main/interact/item_id/";
        //public static string IATitleXPath = "//*[@id=\"Content_Column_Inner\"]/div[4]/table/tr/td[2]/div[3]/a";
        public static string IATitleXPath = "//a[@class=\"proll\"]";
        public static string IAOwnerPath = "//*[@id=\"Content_Column_Inner\"]/div[4]/table/tr/td[2]/div[5]/span[1]/a";
        public static string IADescriptionXPath = "//*[@id=\"Content_Column_Inner\"]/div[4]/table/tr/td[2]/div[7]/big";
        public static string IAInfoTextXPath = "//*[@id=\"Content_Column_Inner\"]/div[6]/div[2]/table/tr/td";
        // Outline.
        public static string IAOutlineUrlSegment = "/action/pop_outline";
        public static string IAOutlineParentXPath = "//*[@id=\"tab1\"]/div[14]/div[2]/pre";
        public static string IAOutlineLoginXPath = "//*[@id=\"tab1\"]/div[13]/div/div[1]/big/b";

        // Chapter page.
        public static string ChAuthorXPath = "//*[@id=\"Content_Column_Inner\"]/div[5]/table/tr/td[1]/div/table/tr[1]/td/table/tr/td[1]/div[2]/div/span[2][@class=\"noselect\"]/a";
        public static string ChTextXPath = "//*[@class=\"KonaBody\"]";
        public static string ChChoiceDescrptionsXPath = "//*[@id=\"Content_Column_Inner\"]/div[5]/table/tr/td[1]/div/table/tr[1]/td/table/tr/td[1]/div[2]/div[last()]/table/tr/td/div/div[1]/p";

    }

}
