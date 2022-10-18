using System;
using System.Collections.Generic;
using SrcChess2.Core;

namespace SrcChess2.PgnParsing {

    /// <summary>
    /// Type of player (human of computer program)
    /// </summary>
    public enum PgnPlayerType {
        /// <summary>Player is a human</summary>
        Human,
        /// <summary>Player is a computer program</summary>
        Program
    };

    /// <summary>
    /// PGN raw game. Attributes and undecoded move list
    /// </summary>
    public class PgnGame {

        /// <summary>
        /// Attribute which has been read
        /// </summary>
        [Flags]
        private enum AttrRead {
            None        = 0,
            Event       = 1,
            Site        = 2,
            GameDate    = 4,
            Round       = 8,
            WhitePlayer = 16,
            BlackPlayer = 32,
            WhiteElo    = 64,
            BlackElo    = 128,
            GameResult  = 256,
            GameTime    = 512,
            WhiteType   = 1024,
            BlackType   = 2048,
            Fen         = 4096,
            TimeControl = 8192,
            Termination = 16384,
            WhiteSpan   = 32768,
            BlackSpan   = 65536
        }

        /// <summary>Game starting position in the PGN text file</summary>
        public  long                       StartingPos { get; set; }
        /// <summary>Game length in the PGN text file</summary>
        public  int                        Length { get; set; }
        /// <summary>Attributes</summary>
        public  Dictionary<string,string>? Attrs { get; set; }
        /// <summary>Undecoded SAN moves</summary>
        public  List<string>?              SanMoves { get; set; }
        /// <summary>Read attributes</summary>
        private AttrRead                   m_attrRead;
        /// <summary>Event</summary>
        private string?                    m_event;
        /// <summary>Site of the event</summary>
        private string?                    m_site;
        /// <summary>Date of the game</summary>
        private string?                    m_gameDate;
        /// <summary>Round</summary>
        private string?                    m_round;
        /// <summary>White Player name</summary>
        private string?                    m_whitePlayerName;
        /// <summary>Black Player name</summary>
        private string?                    m_blackPlayerName;
        /// <summary>White ELO (-1 if none)</summary>
        private int                        m_whiteElo = -1;
        /// <summary>Black ELO (-1 if none)</summary>
        private int                        m_blackElo = -1;
        /// <summary>Game result 1-0, 0-1, 1/2-1/2 or *</summary>
        private string?                    m_gameResult;
        /// <summary>White Human/program</summary>
        private PgnPlayerType              m_whitePlayerType;
        /// <summary>White Human/program</summary>
        private PgnPlayerType              m_blackPlayerType;
        /// <summary>FEN defining the board</summary>
        private string?                    m_fen;
        /// <summary>Time control</summary>
        private string?                    m_timeControl;
        /// <summary>Game termination</summary>
        private string?                    m_termination;
        /// <summary>Time span from White player</summary>
        private TimeSpan                   m_whiteTime;
        /// <summary>Time span from Black player</summary>
        private TimeSpan                   m_blackTime;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="createAttrList"> true to create an attribute list</param>
        /// <param name="createMoveList"> true to create a move list</param>
        public PgnGame(bool createAttrList, bool createMoveList) {
            Attrs      = createAttrList ? new Dictionary<string, string>(10, StringComparer.InvariantCultureIgnoreCase) : null;
            SanMoves   = createMoveList ? new List<string>(256) : null;
            m_attrRead = AttrRead.None;
        }

        /// <summary>
        /// Event
        /// </summary>
        public string? Event {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.Event) == 0) {
                    m_attrRead |= AttrRead.Event;
                    if (!Attrs.TryGetValue("Event", out m_event)) {
                        m_event = null;
                    }
                }
                return m_event;
            }
        }

        /// <summary>
        /// Site
        /// </summary>
        public string? Site {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.Site) == 0) {
                    m_attrRead |= AttrRead.Site;
                    if (!Attrs.TryGetValue("Site", out m_site)) {
                        m_site = null;
                    }
                }
                return m_site;
            }
        }

        /// <summary>
        /// Round
        /// </summary>
        public string? Round {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.Round) == 0) {
                    m_attrRead |= AttrRead.Round;
                    if (!Attrs.TryGetValue("Round", out m_round)) {
                        m_round = null;
                    }
                }
                return m_round;
            }
        }

        /// <summary>
        /// Date of the game
        /// </summary>
        public string? Date {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.GameDate) == 0) {
                    m_attrRead |= AttrRead.GameDate;
                    if (!Attrs.TryGetValue("Date", out m_gameDate)) {
                        m_gameDate = null;
                    }
                }
                return m_gameDate;
            }
        }


        /// <summary>
        /// White Player
        /// </summary>
        public string? WhitePlayerName {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.WhitePlayer) == 0) {
                    m_attrRead |= AttrRead.WhitePlayer;
                    if (!Attrs.TryGetValue("White", out m_whitePlayerName)) {
                        m_whitePlayerName = null;
                    }
                }
                return m_whitePlayerName;
            }
        }

        /// <summary>
        /// Black Player
        /// </summary>
        public string? BlackPlayerName {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.BlackPlayer) == 0) {
                    m_attrRead |= AttrRead.BlackPlayer;
                    if (!Attrs.TryGetValue("Black", out m_blackPlayerName)) {
                        m_blackPlayerName = null;
                    }
                }
                return m_blackPlayerName;
            }
        }

        /// <summary>
        /// White ELO
        /// </summary>
        public int WhiteElo {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.WhiteElo) == 0) {
                    m_attrRead |= AttrRead.WhiteElo;
                    if (!Attrs.TryGetValue("WhiteElo", out string? txtValue) || !int.TryParse(txtValue, out m_whiteElo)) {
                        m_whiteElo = -1;
                    }
                }
                return (m_whiteElo);
            }
        }

        /// <summary>
        /// Black ELO
        /// </summary>
        public int BlackElo {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.BlackElo) == 0) {
                    m_attrRead |= AttrRead.BlackElo;
                    if (!Attrs.TryGetValue("BlackElo", out string? txtValue) || !int.TryParse(txtValue, out m_blackElo)) {
                        m_blackElo = -1;
                    }
                }
                return (m_blackElo);
            }
        }

        /// <summary>
        /// Game Result
        /// </summary>
        public string? GameResult {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.GameResult) == 0) {
                    m_attrRead |= AttrRead.GameResult;
                    if (!Attrs.TryGetValue("Result", out m_gameResult)) {
                        m_gameResult = null;
                    }
                }
                return m_gameResult;
            }
        }

        /// <summary>
        /// White player type
        /// </summary>
        public PgnPlayerType WhiteType {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.WhiteType) == 0) {
                    m_attrRead |= AttrRead.WhiteType;
                    if (Attrs.TryGetValue("WhiteType", out string? value)) {
                        m_whitePlayerType = string.Compare(value, "Program", ignoreCase: true) == 0 ? PgnPlayerType.Program : PgnPlayerType.Human;
                    } else {
                        m_whitePlayerType = PgnPlayerType.Human;
                    }
                }
                return (m_whitePlayerType);
            }
        }

        /// <summary>
        /// Black player type
        /// </summary>
        public PgnPlayerType BlackType {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.BlackType) == 0) {
                    m_attrRead |= AttrRead.BlackType;
                    if (Attrs.TryGetValue("BlackType", out string? value)) {
                        m_blackPlayerType = string.Compare(value, "Program", ignoreCase: true) == 0 ? PgnPlayerType.Program : PgnPlayerType.Human;
                    } else {
                        m_blackPlayerType = PgnPlayerType.Human;
                    }
                }
                return (m_blackPlayerType);
            }
        }

        /// <summary>
        /// FEN defining the board
        /// </summary>
        public string? Fen {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.Fen) == 0) {
                    m_attrRead |= AttrRead.Fen;
                    if (Attrs == null || !Attrs.TryGetValue("Fen", out m_fen)) {
                        m_fen = null;
                    }
                }
                return m_fen;
            }
        }

        /// <summary>
        /// Time control
        /// </summary>
        public string? TimeControl {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.TimeControl) == 0) {
                    m_attrRead |= AttrRead.TimeControl;
                    if (!Attrs.TryGetValue("TimeControl", out m_timeControl)) {
                        m_timeControl = null;
                    }
                }
                return m_timeControl;
            }
        }

        /// <summary>
        /// Game termination
        /// </summary>
        public string? Termination {
            get {
                if (Attrs != null && (m_attrRead & AttrRead.Termination) == 0) {
                    m_attrRead |= AttrRead.Termination;
                    if (!Attrs.TryGetValue("Termination", out m_termination)) {
                        m_termination = null;
                    }
                }
                return m_termination;
            }
        }

        /// <summary>
        /// Initialize the proprietary time control
        /// </summary>
        private void InitPlayerSpan() {
            string?  timeControl;
            string[] timeControls;

            m_whiteTime = TimeSpan.Zero;
            m_blackTime = TimeSpan.Zero;
            timeControl = TimeControl;
            if (timeControl != null) {
                timeControls = timeControl.Split(':');
                if (timeControls.Length == 3                        &&
                    timeControls[0] == "?"                          &&
                    int.TryParse(timeControls[1], out int tick1)  &&
                    int.TryParse(timeControls[2], out int tick2)) {
                    m_whiteTime = new TimeSpan(tick1);
                    m_blackTime = new TimeSpan(tick2);
                }
            }
            m_attrRead |= AttrRead.WhiteSpan | AttrRead.BlackSpan;
        }

        /// <summary>
        /// Time used by the White player
        /// </summary>
        public TimeSpan WhiteSpan {
            get {
                if ((m_attrRead & AttrRead.WhiteSpan) == 0) {
                    m_attrRead |= AttrRead.WhiteSpan;
                    InitPlayerSpan();
                }
                return m_whiteTime;
            }
        }

        /// <summary>
        /// Time used by the Black player
        /// </summary>
        public TimeSpan BlackSpan {
            get {
                if ((m_attrRead & AttrRead.BlackSpan) == 0) {
                    m_attrRead |= AttrRead.BlackSpan;
                    InitPlayerSpan();
                }
                return m_blackTime;
            }
        }

        /// <summary>
        /// List of moves defines as an integer per move defines as StartingPos + EndingPos * 256
        /// </summary>
        public short[]? MoveList { get; set; }

        /// <summary>
        /// List of moves defines as MoveExt object
        /// </summary>
        public List<MoveExt>? MoveExtList { get; set; }

        /// <summary>
        /// Starting chessboard when defined with a FEN
        /// </summary>
        public ChessBoard? StartingChessBoard { get; set; }

        /// <summary>
        /// Starting player
        /// </summary>
        public ChessBoard.PlayerColor StartingColor { get; set; }

        /// <summary>
        /// Set default value for some properties
        /// </summary>
        public void SetDefaultValue() {
            if (WhitePlayerName == null) {
                m_whitePlayerName = "Player 1";
            }
            if (BlackPlayerName == null) {
                m_blackPlayerName = "Player 2";
            }
        }

    } // Class PgnGame
} // Namespace
