using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using SrcChess2.Core;
using SrcChess2.PgnParsing;

namespace SrcChess2 {

    /// <summary>
    /// Interaction logic for wndPGNParsing.xaml
    /// </summary>
    public partial class FrmCreatingBookFromPgn : Window {
        /// <summary>Task</summary>
        private Task?                   m_task;
        /// <summary>Array of file names</summary>
        private readonly string[]?      m_fileNames;
        /// <summary>Actual phase</summary>
        private PgnParser.ParsingPhase  m_phase;
        /// <summary>Book creation result</summary>
        private bool                    m_result;
        /// <summary>Private delegate</summary>
        private delegate void           DelProgressCallBack(PgnParser.ParsingPhase phase, int fileIndex, int fileCount, string? fileName, int gameDone, int gameCount);

        /// <summary>
        /// Ctor
        /// </summary>
        public FrmCreatingBookFromPgn() {
            InitializeComponent();
            Loaded   += WndPgnParsing_Loaded;
            Unloaded += WndPgnParsing_Unloaded;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public FrmCreatingBookFromPgn(string[] fileNames) : this() => m_fileNames  = fileNames;

        /// <summary>
        /// Called when the windows is loaded
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void WndPgnParsing_Loaded(object sender, RoutedEventArgs e) {
            ProgressBar.Start();
            StartProcessing();
        }

        /// <summary>
        /// Called when the windows is closing
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void WndPgnParsing_Unloaded(object sender, RoutedEventArgs e) => ProgressBar.Stop();

        /// <summary>
        /// Total number of games skipped
        /// </summary>
        public int TotalSkipped { get; private set; }

        /// <summary>
        /// Total number of games truncated
        /// </summary>
        public int TotalTruncated { get; private set; }

        /// <summary>
        /// Error if any
        /// </summary>
        public string? Error { get; private set; }

        /// <summary>
        /// Created openning book
        /// </summary>
        public Book? Book { get; private set; }

        /// <summary>
        /// Number of entries in the book
        /// </summary>
        public int BookEntryCount { get; private set; }

        /// <summary>
        /// List of moves of all games
        /// </summary>
        public List<short[]>? MoveList { get; private set; }

        /// <summary>
        /// Cancel the parsing job
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void ButCancel_Click(object sender, RoutedEventArgs e) {
            butCancel.IsEnabled = false;
            PgnParser.CancelParsingJob();
        }

        /// <summary>
        /// Progress bar
        /// </summary>
        /// <param name="phase">     Phase</param>
        /// <param name="fileIndex"> File index</param>
        /// <param name="fileCount"> File count</param>
        /// <param name="fileName">  File name</param>
        /// <param name="gameDone">  Games processed since the last call</param>
        /// <param name="gameCount"> Game count</param>
        private void WndCallBack(PgnParser.ParsingPhase phase, int fileIndex, int fileCount, string? fileName, int gameDone, int gameCount) {
            if (m_phase != phase) {
                switch (phase) {
                case PgnParser.ParsingPhase.OpeningFile:
                    ctlPhase.Content              = "Openning the file";
                    ctlFileBeingProcessed.Content = System.IO.Path.GetFileName(fileName ?? throw new ArgumentNullException(nameof(fileName)));
                    ctlStep.Content               = "";
                    break;
                case PgnParser.ParsingPhase.ReadingFile:
                    ctlPhase.Content              = "Reading the file content into memory";
                    ctlStep.Content               = "";
                    break;
                case PgnParser.ParsingPhase.RawParsing:
                    ctlPhase.Content              = "Parsing the PGN";
                    ctlStep.Content               = "0 / " + gameCount.ToString(CultureInfo.InvariantCulture) + "mb";
                    break;
                case PgnParser.ParsingPhase.Finished:
                    ctlPhase.Content              = "Done";
                    break;
                case PgnParser.ParsingPhase.CreatingBook:
                    ctlPhase.Content              = "Creating the book entries";
                    ctlFileBeingProcessed.Content = "***";
                    break;
                default:
                    break;
                }
                m_phase = phase;
            }
            switch (phase) {
            case PgnParser.ParsingPhase.OpeningFile:
                break;
            case PgnParser.ParsingPhase.ReadingFile:
                ctlPhase.Content    = "Reading the file content into memory";
                break;
            case PgnParser.ParsingPhase.RawParsing:
                ctlStep.Content = gameDone.ToString(CultureInfo.InvariantCulture) + " / " + gameCount.ToString(CultureInfo.InvariantCulture) + " mb";
                break;
            case PgnParser.ParsingPhase.CreatingBook:
                ctlStep.Content = gameDone.ToString(CultureInfo.InvariantCulture) + " / " + gameCount.ToString(CultureInfo.InvariantCulture);
                break;
            case PgnParser.ParsingPhase.Finished:
                if (PgnParser.IsJobCancelled) {
                    DialogResult = false;
                } else {
                    DialogResult = m_result;
                }
                break;
            default:
                break;
            }
        }

        /// <summary>
        /// Progress bar
        /// </summary>
        /// <param name="cookie">        Cookie</param>
        /// <param name="phase">         Phase</param>
        /// <param name="fileIndex">     File index</param>
        /// <param name="fileCount">     File count</param>
        /// <param name="fileName">      File name</param>
        /// <param name="gameProcessed"> Games processed since the last call</param>
        /// <param name="gameCount">     Game count</param>
        static void ProgressCallBack(object? cookie, PgnParser.ParsingPhase phase, int fileIndex, int fileCount, string? fileName, int gameProcessed, int gameCount) {
            FrmCreatingBookFromPgn wnd;
            DelProgressCallBack    del;

            wnd = (FrmCreatingBookFromPgn)cookie!;
            del = wnd.WndCallBack;
            wnd.Dispatcher.Invoke(del, System.Windows.Threading.DispatcherPriority.Normal, new object[] { phase, fileIndex, fileCount, fileName ?? "", gameProcessed, gameCount });
        }

        /// <summary>
        /// Create a book from a list of PGN games
        /// </summary>
        /// <returns>
        /// true if succeed, false if failed</returns>
        private bool CreateBook() {
            bool retVal;

            try {
                m_phase        = PgnParser.ParsingPhase.None;
                retVal         = PgnParser.ExtractMoveListFromMultipleFiles(m_fileNames ?? throw new InvalidOperationException("List of filenames not set"),
                                                                            (cookie, phase, fileIndex, fileCount, fileName, gameProcessed, gameCount) => { ProgressCallBack(cookie, phase, fileIndex, fileCount, fileName, gameProcessed, gameCount); },
                                                                            this,
                                                                            out List<short[]> moveList,
                                                                            out int totalSkipped,
                                                                            out int totalTruncated,
                                                                            out string? errTxt);
                MoveList       = moveList;
                TotalSkipped   = totalSkipped;
                TotalTruncated = totalTruncated;
                Error          = errTxt;
                if (retVal) {
                    Book            = new Book();
                    BookEntryCount  = Book.CreateBookList(MoveList,
                                                          minMoveCount: 30,
                                                          maxDepth: 10,
                                                          (cookie, phase, fileIndex, fileCount, fileName, gameDone, gameCount) => { ProgressCallBack(cookie, phase, fileIndex, fileCount, fileName, gameDone, gameCount); },
                    this);
                }
            } catch(Exception ex) {
                MessageBox.Show(ex.Message);
                retVal = false;
            }
            m_result = retVal;
            ProgressCallBack(this, PgnParser.ParsingPhase.Finished, fileIndex: 0, fileCount: 0, fileName: null, gameProcessed: 0, gameCount: 0);
            return retVal;
        }

        /// <summary>
        /// Start processing
        /// </summary>
        private void StartProcessing() {
            m_task = Task<bool>.Factory.StartNew(() => { return CreateBook(); });
        }
    }
}
