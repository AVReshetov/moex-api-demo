using System;
using System.Collections.Generic;

namespace WPF_ISS_Demo
{
    /// <summary>
    /// class to process and store data that is being downloaded from the ISS server
    /// </summary>
    public class MicexISSDataHandler
    {
        public List<Tuple<string, double, uint>> HistoryStorage = new List<Tuple<string, double, uint>>();
        public Dictionary<string, string> EnginesStorage = new Dictionary<string, string>();
        public Dictionary<string, string> MarketsStorage = new Dictionary<string, string>();
        public Dictionary<string, string> BoardsStorage = new Dictionary<string, string>();

        /// <summary>
        /// for the requests of historical end-of-day results
        /// </summary>
        public void Process_history(string secid, double closeprice, uint numtrades)
        {
            HistoryStorage.Add(new Tuple<string, double, uint>(secid, closeprice, numtrades));
        }

        /// <summary>
        /// for the request of the list of markets on a given engine
        /// </summary>
        public void Process_engines(string engineName, string engineTitle)
        {
            EnginesStorage.Add(engineName, engineTitle);
        }

        /// <summary>
        /// for the request of the list of boards on a given engine and a given market
        /// </summary>
        public void Process_markets(string marketName, string marketTitle)
        {
            MarketsStorage.Add(marketName, marketTitle);
        }

        /// <summary>
        /// for the request of the list of boards on a given engine and a given market
        /// </summary>
        public void Process_boards(string boardId, string boardTitle)
        {
            BoardsStorage.Add(boardId, boardTitle);
        }
    }
}