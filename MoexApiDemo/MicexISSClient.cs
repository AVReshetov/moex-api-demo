using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Xml.Linq;

namespace WPF_ISS_Demo
{
    /// <summary>
    /// class to perform all the interaction with the ISS server
    /// </summary>
    public class MicexISSClient
    {
        // container for all the cookies
        private readonly CookieContainer _cookiejar;

        // dictionary with all the URLs that can be used to request data from the ISS
        private readonly Dictionary<string, string> _urls = new Dictionary<string, string>();

        public MicexISSClient(CookieContainer cookies = null)
        {
            _cookiejar = cookies;
            _urls.Add("history_secs", "http://iss.moex.com/iss/history/engines/{0}/markets/{1}/boards/{2}/securities.xml?date={3}&start={4}");
            _urls.Add("engines", "http://iss.moex.com/iss/engines.xml");
            _urls.Add("markets", "http://iss.moex.com/iss/engines/{0}/markets.xml");
            _urls.Add("boards", "http://iss.moex.com/iss/engines/{0}/markets/{1}/boards.xml");
        }

        /// <summary>
        /// get raw reply from ISS as a string
        /// </summary>
        private string GetReply(string url)
        {
            try
            {
                var myWebRequest = (HttpWebRequest) WebRequest.Create(url);
                myWebRequest.CookieContainer = _cookiejar;
                var issResponse = (HttpWebResponse) myWebRequest.GetResponse();
                var myReplyStream = issResponse.GetResponseStream();
                var myStreamReader = new StreamReader(myReplyStream);
                var ret = myStreamReader.ReadToEnd();
                issResponse.Close();
                return ret;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error requesting data: " + e.Message);
                return "";
            }
        }

        /// <summary>
        /// get a particular data block from ISS XML reply
        /// </summary>
        private XElement GetDataBlock(XDocument resXml, String blockId)
        {
            var ret = XElement.Parse("<data id=\"none\"></data>");
            foreach (var el in resXml.Element("document").Elements())
            {
                var att = el.Attribute("id");
                if (att == null)
                    continue;
                if (att.Value != blockId)
                    continue;
                ret = el;
                break;
            }
            return ret;
        }

        /// <summary>
        /// get a set of rows inside a data block in the ISS XML
        /// </summary>
        private XElement GetRows(XElement elem)
        {
            var ret = XElement.Parse("<rows></rows>");
            foreach (var el in elem.Elements())
            {
                if (el.Name != "rows")
                    continue;
                ret = el;
                break;
            }
            return ret;
        }

        /// <summary>
        /// get a particular attribute of an element
        /// this is not the best solution when a lot of attributes from a single element are to be extracted
        /// </summary>
        private String GetAttribute(XElement elem, String attr)
        {
            var ret = String.Empty;
            foreach (var att in elem.Attributes())
            {
                if (!String.Equals(att.Name.ToString(), attr, StringComparison.OrdinalIgnoreCase))
                    continue;
                ret = att.Value;
                break;
            }
            return ret;
        }

        /// <summary>
        /// full cycle to get the end-of-day results
        /// only Security ID, number of trades and the official close price are stored in this example
        /// </summary>
        public void GetHistorySecurities(String engine, String market, String board, String histdate, MicexISSDataHandler myhandler)
        {
            var start = 0;
            var replyLen = 1;
            myhandler.HistoryStorage = new List<Tuple<string, double, uint>>();
            try
            {
                // it is very important to keep in mind that the server reply can be split into serveral pages, 
                // so we use the 'start' argument to the request
                while (replyLen > 0)
                {
                    replyLen = 0;

                    var fullurl = String.Format(_urls["history_secs"], engine, market, board, histdate, start);
                    var reply = GetReply(fullurl);

                    // get the data block with historical data
                    // we ignore metadata in this example
                    var history = GetDataBlock(XDocument.Parse(reply), "history");
                    if (history.HasElements)
                    {
                        var rows = GetRows(history);
                        if (rows.HasElements)
                        {
                            foreach (var el in rows.Elements())
                            {
                                var secid = GetAttribute(el, "SECID");
                                var numtrades = UInt32.Parse(GetAttribute(el, "NUMTRADES"));
                                // need to use Val instead of Convert or CDbl, because the ISS float numbers always come with. as the delimiter
                                var closeprice = 0.0;
                                if (Double.TryParse(GetAttribute(el, "LEGALCLOSEPRICE"), out var parcedDouble))
                                    closeprice = parcedDouble;
                                myhandler.Process_history(secid, closeprice, numtrades);
                            }
                            replyLen = rows.Elements().Count();
                        }
                    }
                    start = start + replyLen;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error while processing XML with history_secs: " + e.Message);
            }
        }

        /// <summary>
        /// get the list of available engines
        /// </summary>
        public void GetEngines(MicexISSDataHandler myhandler)
        {
            myhandler.EnginesStorage = new Dictionary<string, string>();
            try
            {
                var reply = GetReply(_urls["engines"]);
                var engines = GetDataBlock(XDocument.Parse(reply), "engines");
                if (!engines.HasElements)
                    return;
                var rows = GetRows(engines);
                if (!rows.HasElements)
                    return;
                foreach (var el in rows.Elements())
                {
                    myhandler.Process_engines(GetAttribute(el, "name"), GetAttribute(el, "title"));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error while processing XML with engines: " + e.Message);
            }
        }

        /// <summary>
        /// get the list of markets on a given engine
        /// </summary>
        public void GetMarkets(string engineName, MicexISSDataHandler myhandler)
        {
            myhandler.MarketsStorage = new Dictionary<string, string>();
            try
            {
                var reply = GetReply(String.Format(_urls["markets"], engineName));
                var markets = GetDataBlock(XDocument.Parse(reply), "markets");
                if (!markets.HasElements)
                    return;
                var rows = GetRows(markets);
                if (!rows.HasElements)
                    return;
                foreach (var el in rows.Elements())
                {
                    myhandler.Process_markets(GetAttribute(el, "name"), GetAttribute(el, "title"));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error while processing XML with markets: " + e.Message);
            }
        }

        /// <summary>
        /// get the list of boards on a given engine and a given market
        /// </summary>
        public void GetBoards(string engineName, string marketName, MicexISSDataHandler myhandler)
        {
            myhandler.BoardsStorage = new Dictionary<string, string>();
            try
            {
                var reply = GetReply(String.Format(_urls["boards"], engineName, marketName));
                var boards = GetDataBlock(XDocument.Parse(reply), "boards");
                if (!boards.HasElements)
                    return;
                var rows = GetRows(boards);
                if (!rows.HasElements)
                    return;
                foreach (var el in rows.Elements())
                {
                    myhandler.Process_boards(GetAttribute(el, "boardid"), GetAttribute(el, "title"));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error while processing XML with boards: " + e.Message);
            }
        }
    }
}