
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SrcChess2.Core;

namespace SrcChess2.FicsInterface {

    /// <summary>
    /// Game description
    /// </summary>
    public class FicsGame {

        /// <summary>
        /// Type of games supported by FICS server
        /// </summary>
        public enum FicsGameType {
            /// <summary>Blitz</summary>
            Blitz,
            /// <summary>Fast blitz</summary>
            Lightning,
            /// <summary>Untimed</summary>
            Untimed,
            /// <summary>Examined</summary>
            Examined,
            /// <summary>Standard game</summary>
            Standard,
            /// <summary>Wild variant</summary>
            Wild,
            /// <summary>Atomic variant</summary>
            Atomic,
            /// <summary>Crazyhouse variant</summary>
            Crazyhouse,
            /// <summary>Bughouse variant</summary>
            Bughouse,
            /// <summary>Losers variant</summary>
            Losers,
            /// <summary>Suicide variant</summary>
            Suicide,
            /// <summary>Non standard</summary>
            NonStandard
        }

        /// <summary>Game ID</summary>
        public int GameId { get; private set; }
        /// <summary>White Rating (-1 = unregistred, 0 = Unrated)</summary>
        public int WhiteRating { get; private set; }
        /// <summary>Name of the white player</summary>
        public string WhitePlayerName { get; private set; } = "";
        /// <summary>Black Rating (-1 = unregistred, 0 = Unrated)</summary>
        public int BlackRating { get; private set; }
        /// <summary>Name of the black player</summary>
        public string BlackPlayerName { get; private set; } = "";
        /// <summary>Game type</summary>
        public FicsGameType GameType { get; private set; }
        /// <summary>true if rated game</summary>
        public bool IsRated { get; private set; }
        /// <summary>true if private</summary>
        public bool IsPrivate { get; private set; }
        /// <summary>Time for each player for the game</summary>
        public int PlayerTimeInMin { get; private set; }
        /// <summary>Time add to the total game per move</summary>
        public int IncTimeInSec { get; private set; }
        /// <summary>White time span</summary>
        public TimeSpan WhiteTimeSpan { get; private set; }
        /// <summary>Black time span</summary>
        public TimeSpan BlackTimeSpan { get; private set; }
        /// <summary>Current White material strength</summary>
        public int WhiteMaterialPoint { get; private set; }
        /// <summary>Current Black material strength</summary>
        public int BlackMaterialPoint { get; private set; }
        /// <summary>Player making the next move</summary>
        public ChessBoard.PlayerColor NextMovePlayer { get; private set; }
        /// <summary>Count for the next move</summary>
        public int NextMoveCount { get; private set; }

        /// <summary>
        /// Gets rating in human form
        /// </summary>
        /// <param name="rating"> Rating</param>
        /// <returns>
        /// Rating
        /// </returns>
        public static string GetHumanRating(int rating) {
            string retVal;

            if (rating == -1) {
                retVal = "Guest";
            } else if (rating == 0) {
                retVal = "Not Rated";
            } else {
                retVal = rating.ToString(CultureInfo.InvariantCulture);
            }
            return (retVal);
        }

        /// <summary>
        /// Convert rating to string
        /// </summary>
        /// <param name="rating"> Rating</param>
        /// <returns>
        /// String
        /// </returns>
        private static string CnvRating(int rating) {
            string retVal;

            if (rating == -1) {
                retVal = "++++";
            } else if (rating == 0) {
                retVal = "----";
            } else {
                retVal = rating.ToString(CultureInfo.InvariantCulture).PadLeft(4);
            }
            return (retVal);
        }

        /// <summary>
        /// Convert player's name
        /// </summary>
        /// <param name="playerName"> Player's name</param>
        /// <returns>
        /// Normalized player name
        /// </returns>
        private static string CnvPlayerName(string playerName) => playerName.Length < 10 ? playerName.PadRight(10) : playerName[..10];

        /// <summary>
        /// Convert time to string
        /// </summary>
        /// <param name="span"> Span</param>
        /// <returns>
        /// String
        /// </returns>
        private static string TimeToString(TimeSpan span)
            => span.Hours != 0 ? $"{span.Hours.ToString(CultureInfo.InvariantCulture)}:{span.Minutes.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}:{span.Seconds.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}" :
                                 $"{span.Minutes.ToString(CultureInfo.InvariantCulture) }:{span.Seconds.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}";

        /// <summary>
        /// Convert the game into string
        /// </summary>
        /// <returns>
        /// String representation
        /// </returns>
        public override string ToString() {
            StringBuilder strb;

            strb = new StringBuilder(128);
            strb.Append(GameId.ToString(CultureInfo.InvariantCulture).PadLeft(3));
            strb.Append(' ');
            strb.Append(CnvRating(WhiteRating));
            strb.Append(' ');
            strb.Append(CnvPlayerName(WhitePlayerName));
            strb.Append(' ');
            strb.Append(CnvRating(BlackRating));
            strb.Append(' ');
            strb.Append(CnvPlayerName(BlackPlayerName));
            strb.Append(' ');
            strb.Append('[');
            strb.Append(IsPrivate ? 'p' : ' ');
            strb.Append(GameTypeToChar(GameType));
            strb.Append(IsRated ? 'r' : 'u');
            strb.Append(PlayerTimeInMin.ToString(CultureInfo.InvariantCulture).PadLeft(3));
            strb.Append(' ');
            strb.Append(IncTimeInSec.ToString(CultureInfo.InvariantCulture).PadLeft(3));
            strb.Append(']');
            strb.Append(' ');
            strb.Append(TimeToString(WhiteTimeSpan).PadLeft(6));
            strb.Append(" -");
            strb.Append(TimeToString(BlackTimeSpan).PadLeft(6));
            strb.Append(' ');
            strb.Append('(');
            strb.Append(WhiteMaterialPoint.ToString(CultureInfo.InvariantCulture).PadLeft(2));
            strb.Append('-');
            strb.Append(BlackMaterialPoint.ToString(CultureInfo.InvariantCulture).PadLeft(2));
            strb.Append(')');
            strb.Append(' ');
            strb.Append(NextMovePlayer == ChessBoard.PlayerColor.White ? 'W' : 'B');
            strb.Append(':');
            strb.Append(NextMoveCount.ToString(CultureInfo.InvariantCulture).PadLeft(3));;
            return strb.ToString();
        }

        /// <summary>
        /// Skip the next character
        /// </summary>
        /// <param name="str"> String</param>
        /// <param name="pos"> Position in the string</param>
        /// <returns>
        /// Character
        /// </returns>
        private static char GetNextChar(string str, ref int pos) {
            char retVal = '\0';
            int  length;

            length = str.Length;
            if (pos < length) {
                retVal = str[pos++];
            }
            return retVal;
        }

        /// <summary>
        /// Skip the next non-white character
        /// </summary>
        /// <param name="str"> String</param>
        /// <param name="pos"> Position in the string</param>
        /// <returns>
        /// Next non white character
        /// </returns>
        private static char GetNextNonWhiteChar(string str, ref int pos) {
            char retVal = '\0';
            int  length;

            length = str.Length;
            while (pos < length && Char.IsWhiteSpace(str[pos])) {
                pos++;
            }
            if (pos < length) {
                retVal = str[pos++];
            }
            return retVal;
        }

        /// <summary>
        /// Gets the next token
        /// </summary>
        /// <param name="str"> String</param>
        /// <param name="pos"> Position in the string</param>
        /// <returns>
        /// Next string token. Can be empty
        /// </returns>
        private static string GetNextToken(string str, ref int pos) {
            StringBuilder strb;
            int           length;

            length = str.Length;
            strb   = new StringBuilder(32);
            while (pos < length && Char.IsWhiteSpace(str[pos])) {
                pos++;
            }
            while (pos < length && !Char.IsWhiteSpace(str[pos])) {
                strb.Append(str[pos++]);
            }
            return strb.ToString();
        }

        /// <summary>
        /// Gets the next digit token
        /// </summary>
        /// <param name="str"> String</param>
        /// <param name="pos"> Position in the string</param>
        /// <returns>
        /// Next string token. Can be empty
        /// </returns>
        private static string GetNextDigitToken(string str, ref int pos) {
            StringBuilder strb;
            int           length;

            length = str.Length;
            strb   = new StringBuilder(32);
            while (pos < length && char.IsWhiteSpace(str[pos])) {
                pos++;
            }
            while (pos < length && char.IsDigit(str[pos])) {
                strb.Append(str[pos++]);
            }
            return strb.ToString();
        }

        /// <summary>
        /// Gets a token included between a starting and ending character
        /// </summary>
        /// <param name="str">         String</param>
        /// <param name="pos">         Current position in string</param>
        /// <param name="startingChr"> Starting character</param>
        /// <param name="endingChr">   Ending character</param>
        /// <returns>
        /// Enclosed string or empty if none
        /// </returns>
        private static string GetNextEnclosedToken(string str, ref int pos, char startingChr, char endingChr) {
            StringBuilder strb;
            int           length;

            length = str.Length;
            strb   = new StringBuilder(32);
            while (pos < length && char.IsWhiteSpace(str[pos])) {
                pos++;
            }
            if (pos < length && str[pos] == startingChr) {
                pos++;
                while (pos < length && str[pos] != endingChr) {
                    strb.Append(str[pos++]);
                }
                if (pos < length) {
                    pos++;
                } else {
                    strb.Clear();
                }
            }
            return strb.ToString();
        }

        /// <summary>
        /// Parse the type of the game
        /// </summary>
        /// <param name="chr">         Character specifying the game type</param>
        /// <param name="isSupported"> Return false if the game type is not supported</param>
        /// <returns>
        /// Game type
        /// </returns>
        public static FicsGameType ParseGameType(char chr, ref bool isSupported) {
            FicsGameType retVal;

            switch (chr) {
            case 'b':
                retVal      = FicsGameType.Blitz;
                break;
            case 'e':
                retVal      = FicsGameType.Examined;
                isSupported = false;
                break;
            case 'l':
                retVal      = FicsGameType.Lightning;
                break;
            case 'n':
                retVal      = FicsGameType.NonStandard;
                break;
            case 's':
                retVal      = FicsGameType.Standard;
                break;
            case 'u':
                retVal      = FicsGameType.Untimed;
                break;
            case 'w':
                retVal      = FicsGameType.Wild;
                break;
            case 'x':
                retVal      = FicsGameType.Atomic;
                isSupported = false;
                break;
            case 'z':
                retVal      = FicsGameType.Crazyhouse;
                isSupported = false;
                break;
            case 'B':
                retVal      = FicsGameType.Bughouse;
                isSupported = false;
                break;
            case 'L':
                retVal      = FicsGameType.Losers;
                isSupported = false;
                break;
            case 'S':
                retVal      = FicsGameType.Suicide;
                isSupported = false;
                break;
            default:
                retVal      = FicsGameType.NonStandard;
                isSupported = false;
                break;
            }
            return retVal;
        }

        /// <summary>
        /// Convert a game type to its corresponding character
        /// </summary>
        /// <param name="gameType"> Character specifying the game type</param>
        /// <returns>
        /// Character representing this game type
        /// </returns>
        public static char GameTypeToChar(FicsGameType gameType)
            => gameType switch {
                FicsGameType.Blitz       => 'b',
                FicsGameType.Examined    => 'e',
                FicsGameType.Lightning   => 'l',
                FicsGameType.NonStandard => 'n',
                FicsGameType.Standard    => 's',
                FicsGameType.Untimed     => 'u',
                FicsGameType.Wild        => 'w',
                FicsGameType.Atomic      => 'x',
                FicsGameType.Crazyhouse  => 'z',
                FicsGameType.Bughouse    => 'B',
                FicsGameType.Losers      => 'L',
                FicsGameType.Suicide     => 'S',
                _                        => 'n',
            };

        /// <summary>
        /// Parsing player rating
        /// </summary>
        /// <param name="rating"> Rating</param>
        /// <returns>
        /// Rating value
        /// </returns>
        private static int ParseRating(string rating) {
            int retVal;

            if (rating.StartsWith("+")) {
                retVal = -1;
            } else if (rating.StartsWith("-")) {
                retVal = 0;
            } else {
                retVal = int.Parse(rating);
            }
            return retVal;
        }

        /// <summary>
        /// Parse a player clock time
        /// </summary>
        /// <param name="str"> String to parse</param>
        /// <returns>
        /// Time span
        /// </returns>
        public static TimeSpan ParseTime(string str) {
            TimeSpan span;
            string[] arr;
            string[] arrMS;

            arr = str.Split(':');
            if (arr.Length == 3) {
                arrMS = arr[2].Split('.');
                if (arrMS.Length == 1) {
                    span = new TimeSpan(int.Parse(arr[0], CultureInfo.InvariantCulture),
                                        int.Parse(arr[1], CultureInfo.InvariantCulture),
                                        int.Parse(arr[2], CultureInfo.InvariantCulture));
                } else {
                    span = new TimeSpan(0,
                                        int.Parse(arr[0],   CultureInfo.InvariantCulture),
                                        int.Parse(arr[1],   CultureInfo.InvariantCulture),
                                        int.Parse(arrMS[0], CultureInfo.InvariantCulture),
                                        int.Parse(arrMS[1], CultureInfo.InvariantCulture));
                }
            } else {
                arrMS = arr[1].Split('.');
                if (arrMS.Length == 1) {
                    span = new TimeSpan(0,
                                        int.Parse(arr[0], CultureInfo.InvariantCulture),
                                        int.Parse(arr[1], CultureInfo.InvariantCulture));
                } else {
                    span = new TimeSpan(0,
                                        0,
                                        int.Parse(arr[0],   CultureInfo.InvariantCulture),
                                        int.Parse(arrMS[0], CultureInfo.InvariantCulture),
                                        int.Parse(arrMS[1], CultureInfo.InvariantCulture));
                }
            }
            return span;
        }

        /// <summary>
        /// Parse the player
        /// </summary>
        /// <param name="chr">  Character specifying the player</param>
        /// <returns>
        /// Player
        /// </returns>
        private static ChessBoard.PlayerColor ParsePlayer(char chr) {
            ChessBoard.PlayerColor retVal;

            if (chr == 'B') {
                retVal = ChessBoard.PlayerColor.Black;
            } else if (chr == 'W') {
                retVal = ChessBoard.PlayerColor.White;
            } else {
                throw new ArgumentException($"Invalid player - {chr}");
            }
            return retVal;
        }

        /// <summary>
        /// Chesks if the line is the last line of a game list
        /// </summary>
        /// <param name="line"> Line to check</param>
        /// <returns>
        /// true / false
        /// </returns>
        public static bool IsLastGameLine(string line) => line.EndsWith(" games displayed.");

        /// <summary>
        /// Parse a game string coming from the games command
        /// </summary>
        /// <param name="str">         Line containing the game information</param>
        /// <param name="isSupported"> Returned false if the game type is not actually supported</param>
        /// <returns>
        /// Game or null if cannot be parsed
        /// </returns>
        public static FicsGame? ParseGameLine(string str, out bool isSupported) {
            FicsGame? retVal;
            int       pos;
            int       enclosedPos;
            string    tok;
            string    enclosedStr;
            char      chr;
            bool      isExam;

            isSupported = true;
            if (string.IsNullOrWhiteSpace(str)) {
                retVal  = null;
                isSupported  = false;
            } else {
                try {
                    isExam = false;
                    pos    = 0;
                    tok    = GetNextToken(str, ref pos);
                    if (!int.TryParse(tok, out int gameId)) {
                        retVal  = null;
                    } else {
                        retVal = new FicsGame {
                            GameId = gameId
                        };
                        tok    = GetNextToken(str, ref pos);
                        isExam = tok.StartsWith("(");
                        if (isExam || tok == "games") {
                            retVal      = null;
                            isSupported = false;
                        } else {
                            retVal.WhiteRating     = ParseRating(tok);
                            retVal.WhitePlayerName = GetNextToken(str, ref pos);
                            retVal.BlackRating     = ParseRating(GetNextToken(str, ref pos));
                            retVal.BlackPlayerName = GetNextToken(str, ref pos);
                            enclosedStr            = GetNextEnclosedToken(str, ref pos, '[', ']');
                            if (string.IsNullOrEmpty(enclosedStr)) {
                                retVal      = null;
                                isSupported = false;
                            } else {
                                enclosedPos = 0;
                                chr         = GetNextChar(enclosedStr, ref enclosedPos);
                                if (chr != ' ' && chr != 'p') {
                                    retVal      = null;
                                    isSupported = false;
                                } else {
                                    retVal.IsPrivate       = (chr == 'p');
                                    retVal.GameType        = ParseGameType(GetNextChar(enclosedStr, ref enclosedPos), ref isSupported);
                                    retVal.IsRated         = GetNextChar(enclosedStr, ref enclosedPos) == 'r';
                                    retVal.PlayerTimeInMin = int.Parse(GetNextToken(enclosedStr, ref enclosedPos));
                                    retVal.IncTimeInSec    = int.Parse(GetNextToken(enclosedStr, ref enclosedPos));
                                    retVal.WhiteTimeSpan   = ParseTime(GetNextToken(str, ref pos));
                                    GetNextNonWhiteChar(str, ref pos);
                                    retVal.BlackTimeSpan = ParseTime(GetNextToken(str, ref pos));
                                    enclosedStr          = GetNextEnclosedToken(str, ref pos, '(', ')');
                                    if (string.IsNullOrEmpty(enclosedStr)) {
                                        retVal      = null;
                                        isSupported = false;
                                    } else {
                                        enclosedPos               = 0;
                                        retVal.WhiteMaterialPoint = int.Parse(GetNextDigitToken(enclosedStr, ref enclosedPos));
                                        GetNextNonWhiteChar(enclosedStr, ref enclosedPos);
                                        retVal.BlackMaterialPoint = int.Parse(GetNextDigitToken(enclosedStr, ref enclosedPos));
                                        retVal.NextMovePlayer     = ParsePlayer(GetNextToken(str, ref pos)[0]);
                                        retVal.NextMoveCount      = int.Parse(GetNextToken(str, ref pos));
                                    }
                                }
                            }
                        }
                    }
                } catch(Exception) {
                    retVal = null;
                }
            }
            return retVal;
        }

        /// <summary>
        /// Parse move found on a line
        /// </summary>
        /// <param name="moveIndex"> Move index</param>
        /// <param name="line">      Line of data</param>
        /// <param name="whiteMove"> White move</param>
        /// <param name="whiteTime"> White time for the move</param>
        /// <param name="blackMove"> Black move if any</param>
        /// <param name="blackTime"> Black move time if any</param>
        /// <param name="errTxt">    Error if any</param>
        /// <returns>
        /// true if succeed, false if error, null if eof
        /// </returns>
        public static bool? ParseMoveLine(int          moveIndex,
                                          string       line,
                                          out string   whiteMove,
                                          out TimeSpan whiteTime,
                                          out string   blackMove,
                                          out TimeSpan blackTime,
                                          out string?  errTxt) {
            bool?  retVal;
            string tok;
            int    posInLine = 0;

            whiteTime = TimeSpan.Zero;
            blackTime = TimeSpan.Zero;
            whiteMove = "";
            blackMove = "";
            errTxt    = null;
            try {
                if (line.Trim().StartsWith("{")) {
                    retVal = null;
                } else {
                    tok = GetNextDigitToken(line, ref posInLine);
                    if (int.TryParse(tok, out int intVal) && intVal == moveIndex && GetNextChar(line, ref posInLine) == '.') {
                        whiteMove = GetNextToken(line, ref posInLine);
                        tok       = GetNextToken(line, ref posInLine).Replace("(", "").Replace(")", "");
                        whiteTime = ParseTime(tok);
                        tok       = GetNextToken(line, ref posInLine);
                        if (!string.IsNullOrEmpty(tok)) {
                            blackMove   = tok;
                            tok         = GetNextToken(line, ref posInLine).Replace("(", "").Replace(")", "");
                            blackTime   = ParseTime(tok);
                        }
                        retVal = true;
                    } else {
                        errTxt = "Illegal move number";
                        retVal = false;
                    }
                }
            } catch(Exception) {
                errTxt = $"Unable to parse move line - {line}";
                retVal  = false;
            }
            return (retVal);
        }

        /// <summary>
        /// Parse a list of moves
        /// </summary>
        /// <param name="gameId">       Game ID</param>
        /// <param name="lines">        List of lines containing the move list</param>
        /// <param name="timeSpanList"> List of time span or null if not wanted</param>
        /// <param name="errTxt">       Error if any</param>
        /// <returns>
        /// List of moves or null if error
        /// </returns>
        public static List<string>? ParseMoveList(int gameId, List<string> lines, List<TimeSpan>? timeSpanList, out string? errTxt) {
            List<string>? retVal;
            int           moveIndex;
            int           lineCount;
            int           lineIndex;
            string        startingWith;
            bool?         result;

            errTxt       = null;
            lineCount    = lines.Count;
            lineIndex    = 0;
            startingWith = $"Movelist for game {gameId.ToString(CultureInfo.InvariantCulture)}:";
            while (lineIndex < lineCount && !lines[lineIndex].StartsWith(startingWith)) {
                lineIndex++;
            }
            while (lineIndex < lineCount && !lines[lineIndex].StartsWith("---- ")) {
                lineIndex++;
            }
            if (lineIndex == lineCount) {
                retVal = null;
                errTxt = "Move list not found";
            } else {
                lineIndex++;
                retVal    = new List<string>((lineCount - lineIndex + 1) * 2);
                moveIndex = 0;
                result    = true;
                while (lineIndex < lineCount && result == true) {
                    result = ParseMoveLine(++moveIndex,
                                           lines[lineIndex++],
                                           out string whiteMove,
                                           out TimeSpan whiteTime,
                                           out string blackMove,
                                           out TimeSpan blackTime,
                                           out errTxt);
                    if (result == true) {
                        retVal.Add(whiteMove);
                        timeSpanList?.Add(whiteTime);
                        if (blackMove != null) {
                            retVal.Add(blackMove);
                            timeSpanList?.Add(blackTime);
                        }
                    }
                }
                if (result == false) {
                    retVal = null;
                }
            }
            return retVal;
        }
    }
}
