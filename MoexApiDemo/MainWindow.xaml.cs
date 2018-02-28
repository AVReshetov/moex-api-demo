using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace WPF_ISS_Demo
{
    /// <inheritdoc cref="Window" />
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private MicexAuth _myAuth;
        private MicexISSClient _myClient;
        private readonly MicexISSDataHandler _myHandler = new MicexISSDataHandler();

        public MainWindow()
        {
            InitializeComponent();
        }

        public ObservableCollection<Tuple<string, string, string>> ResultGridItems { get; } = new ObservableCollection<Tuple<string, string, string>>();

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            var yesterday = DateTime.Today.AddDays(-1);
            DateTimePicker.SelectedDate = yesterday;
            DateTimePicker.DisplayDate = yesterday;
        }

        private void GetButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_myAuth == null)
            {
                MessageBox.Show("You need to authenticate in order to access the historical data");
                return;
            }
            if (_myAuth.IsRealTime() == false)
            {
                MessageBox.Show("Cannot access real-time and historical data. Try to reauthenticate.");
                return;
            }

            ResultGridItems.Clear();

            var year = DateTimePicker.DisplayDate.Year;
            var month = DateTimePicker.DisplayDate.Month;
            var day = DateTimePicker.DisplayDate.Day;
            var issDate = $"{year}-{month}-{day}";

            if (_myClient == null)
            {
                MessageBox.Show("MyClient object instance is not initialized");
                return;
            }

            // get the user's choice from the screen form
            var engineName = GetSelectedEngine();
            var marketName = GetSelectedMarket();
            var boardId = GetSelectedBoard();

            if (engineName.Length <= 0 || marketName.Length <= 0 || boardId.Length <= 0)
                return;
            _myClient.GetHistorySecurities(engineName, marketName, boardId, issDate, _myHandler);
            foreach (var row in _myHandler.HistoryStorage)
            {
                ResultGridItems.Add(new Tuple<string, string, string>(row.Item1, row.Item2.ToString(CultureInfo.CurrentCulture),
                    row.Item3.ToString()));
            }
        }

        /// <summary>
        /// perform one attempt to authenticate
        /// </summary>
        private void Btn_auth_OnClick(object sender, RoutedEventArgs e)
        {
            _myAuth = new MicexAuth(UsernameTextBox.Text, PasswordBox.Password);
            var st = "Result:";
            st = st + Environment.NewLine + "Status Code: " + _myAuth.LastStatus;
            st = st + Environment.NewLine + "Status Text: " + _myAuth.LastStatusText;
            st = st + Environment.NewLine + "Real-time: " + _myAuth.IsRealTime();
            AuthTextBox.Text = st;
            if (_myAuth.LastStatus == HttpStatusCode.OK)
            {
                if (BoardListbox.SelectedItems.Count == 1)
                {
                    GetButton.IsEnabled = true;
                }
                _myClient = new MicexISSClient(_myAuth.Cookiejar);
            }
            else
            {
                GetButton.IsEnabled = false;
            }
        }
        
        /// <summary>
        /// get the initial list of engines from the ISS server
        /// we do not care about cookies here because general data is always available to everyone
        /// </summary>
        private void Btn_engines_OnClick(object sender, RoutedEventArgs e)
        {
            if (_myClient == null)
                _myClient = new MicexISSClient();
            GetButton.IsEnabled = false;
            EngineListbox.Items.Clear();
            _myClient.GetEngines(_myHandler);
            foreach (var row in _myHandler.EnginesStorage)
            {
                EngineListbox.Items.Add(row.Value);
            }
        }

        /// <summary>
        /// user has chosen another engine
        /// </summary>
        private void EngineListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MarketListbox.Items.Clear();
            BoardListbox.Items.Clear();
            GetButton.IsEnabled = false;
            if (EngineListbox.SelectedItems.Count != 1)
                return;

            var engineName = GetSelectedEngine();
            if (engineName.Length <= 0)
                return;
            _myClient.GetMarkets(engineName, _myHandler);
            foreach (var row in _myHandler.MarketsStorage)
            {
                MarketListbox.Items.Add(row.Value);
            }
        }

        /// <summary>
        /// user has selected a board - the last required parameter
        /// </summary>
        private void BoardListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BoardListbox.Items.Count <= 0 || _myAuth == null)
                return;
            if (_myAuth.LastStatus == HttpStatusCode.OK)
                GetButton.IsEnabled = true;
        }

        /// <summary>
        /// user has chosen another market
        /// </summary>
        private void MarketListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BoardListbox.Items.Clear();
            GetButton.IsEnabled = false;
            if (MarketListbox.SelectedItems.Count != 1)
                return;
            var engineName = GetSelectedEngine();
            if (engineName.Length <= 0)
                return;
            var marketName = GetSelectedMarket();
            if (marketName.Length <= 0)
                return;
            _myClient.GetBoards(engineName, marketName, _myHandler);
            foreach (var row in _myHandler.BoardsStorage)
            {
                BoardListbox.Items.Add(row.Value);
            }
        }

        /// <summary>
        /// read the user's selection of engine from the screen form
        /// </summary>
        private string GetSelectedEngine()
        {
            var engineName = String.Empty;
            if (EngineListbox.SelectedItems.Count != 1)
                return engineName;
            var selectedEngine = EngineListbox.SelectedItem.ToString();
            foreach (var row in _myHandler.EnginesStorage)
            {
                if (row.Value != selectedEngine)
                    continue;
                engineName = row.Key;
                break;
            }
            return engineName;
        }

        /// <summary>
        /// read the user's selection of market from the screen form
        /// </summary>
        private string GetSelectedMarket()
        {
            var marketName = String.Empty;
            if (MarketListbox.SelectedItems.Count != 1)
                return marketName;
            var selectedMarket = MarketListbox.SelectedItem.ToString();
            foreach (var row in _myHandler.MarketsStorage)
            {
                if (row.Value != selectedMarket)
                    continue;
                marketName = row.Key;
                break;
            }
            return marketName;
        }

        /// <summary>
        /// read the user's selection of board from the screen form
        /// </summary>
        private string GetSelectedBoard()
        {
            var boardId = String.Empty;
            if (BoardListbox.SelectedItems.Count != 1)
                return boardId;
            var selectedBoard = BoardListbox.SelectedItem.ToString();
            foreach (var row in _myHandler.BoardsStorage)
            {
                if (row.Value != selectedBoard)
                    continue;
                boardId = row.Key;
                break;
            }
            return boardId;
        }
    }
}