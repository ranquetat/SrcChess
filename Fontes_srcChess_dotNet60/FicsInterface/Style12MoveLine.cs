using System;
using System.Collections.Generic;
using SrcChess2.Core;

namespace SrcChess2.FicsInterface {

    /// <summary>
    /// Termination
    /// </summary>
    public enum TerminationCode {
        /// <summary>On going</summary>
        None              = 0,
        /// <summary>White win</summary>
        WhiteWin          = 1,
        /// <summary>Black win</summary>
        BlackWin          = 2,
        /// <summary>Draw</summary>
        Draw              = 3,
        /// <summary>Terminated</summary>
        Terminated        = 4,
        /// <summary>Terminated with error</summary>
        TerminatedWithErr = 5
    }


    /// <summary>
    /// Represent a parsed line of observed game move in style 12 (raw for interface)
    /// </summary>
    public class Style12MoveLine {

        /// <summary>Relation with the game</summary>
        public enum RelationWithGameType {
            /// <summary>isolated position, such as for "ref 3" or the "sposition" command</summary>
            IsolatedPosition      = -3,
            /// <summary>I am observing game being examined</summary>
            ObservingExaminedGame = -2,
            /// <summary>I am the examiner of this game</summary>
            Examiner              = 2,
            /// <summary>I am playing, it is my opponent's move</summary>
            PlayerOpponentMove    = -1,
            /// <summary>I am playing and it is my move</summary>
            PlayerMyMove          = 1,
            /// <summary>I am observing a game being played</summary>
            Observer              = 0
        }

        /// <summary>Board represented by the line</summary>
        public ChessBoard.PieceType[]    Board { get; }
        /// <summary>Color of the next player</summary>
        public ChessBoard.PlayerColor    NextMovePlayer { get; private set; }
        /// <summary>Board state mask</summary>
        public ChessBoard.BoardStateMask BoardStateMask { get; private set; }
        /// <summary>Number of irreversible moves</summary>
        public int                       IrreversibleMoveCount { get; private set; }
        /// <summary>Game ID</summary>
        public int                       GameId { get; private set; }
        /// <summary>Name of white player</summary>
        public string?                   WhitePlayerName { get; private set; }
        /// <summary>Name of black player</summary>
        public string?                   BlackPlayerName { get; private set; }
        /// <summary>Relation with the game</summary>
        public RelationWithGameType      RelationWithGame { get; private set; }
        /// <summary>Initial time</summary>
        public int                       InitialTime { get; private set; }
        /// <summary>Incremented time</summary>
        public int                       IncrementTime { get; private set; }
        /// <summary>White material strength</summary>
        public int                       WhiteMaterial { get; private set; }
        /// <summary>Black material strength</summary>
        public int                       BlackMaterial { get; private set; }
        /// <summary>White remaining time in second</summary>
        public int                       WhiteRemainingTime { get; private set; }
        /// <summary>Black remaining time in second</summary>
        public int                       BlackRemainingTime { get; private set; }
        /// <summary>Move number</summary>
        public int                       MoveNumber { get; private set; }
        /// <summary>Last move represent in verbose mode ( PIECE '/' StartPosition - EndingPosition )</summary>
        public string?                   LastMoveVerbose { get; private set; }
        /// <summary>Time used to make this move</summary>
        public TimeSpan                  LastMoveSpan { get; private set; }
        /// <summary>Last move represent using SAN</summary>
        public string?                   LastMoveSan { get; private set; }
        /// <summary>true if black in the bottom</summary>
        public bool                      IsFlipped { get; private set; }
        /// <summary>true if clock is ticking</summary>
        public bool                      IsClockTicking { get; set; }
        /// <summary>Lag in millisecond</summary>
        public int                       LagInMS { get; set; }

        /// <summary>
        /// Ctor
        /// </summary>
        public Style12MoveLine() => Board = new ChessBoard.PieceType[64];

        /// <summary>
        /// Number of half move count
        /// </summary>
        public int HalfMoveCount => (MoveNumber * 2) - (NextMovePlayer == ChessBoard.PlayerColor.White ? 2 : 1);

        /// <summary>
        /// Gets line part
        /// </summary>
        /// <param name="line"> Get line part</param>
        /// <returns>
        /// Parts
        /// </returns>
        static private string[]? GetLineParts(string line) {
            string[]? retVal;

            if (line.StartsWith("<12> ")) {
                retVal = line.Split(' ');
                if (retVal.Length < 31) {
                    retVal = null;
                }
            } else {
                retVal = null;
            }
            return retVal;
        }

        /// <summary>
        /// Returns if the line text represent a style 12 move line
        /// </summary>
        /// <param name="line">  Line to check</param>
        /// <returns>
        /// true or false
        /// </returns>
        static public bool IsStyle12Line(string line) => GetLineParts(line) != null;

        /// <summary>
        /// Decode the piece represent by a character
        /// </summary>
        /// <param name="chr">       Character to decode</param>
        /// <param name="pieceType"> Resulting piece type</param>
        /// <returns>
        /// true if succeed, false if error
        /// </returns>
        public static bool DecodePiece(char chr, out ChessBoard.PieceType pieceType) {
            bool retVal;

            if (chr == '-') {
                retVal    = true;
                pieceType = ChessBoard.PieceType.None;
            } else {
                pieceType = chr switch {
                    '-' => ChessBoard.PieceType.None,
                    'P' => ChessBoard.PieceType.Pawn   | ChessBoard.PieceType.White,
                    'N' => ChessBoard.PieceType.Knight | ChessBoard.PieceType.White,
                    'B' => ChessBoard.PieceType.Bishop | ChessBoard.PieceType.White,
                    'R' => ChessBoard.PieceType.Rook   | ChessBoard.PieceType.White,
                    'Q' => ChessBoard.PieceType.Queen  | ChessBoard.PieceType.White,
                    'K' => ChessBoard.PieceType.King   | ChessBoard.PieceType.White,
                    'p' => ChessBoard.PieceType.Pawn   | ChessBoard.PieceType.Black,
                    'n' => ChessBoard.PieceType.Knight | ChessBoard.PieceType.Black,
                    'b' => ChessBoard.PieceType.Bishop | ChessBoard.PieceType.Black,
                    'r' => ChessBoard.PieceType.Rook   | ChessBoard.PieceType.Black,
                    'q' => ChessBoard.PieceType.Queen  | ChessBoard.PieceType.Black,
                    'k' => ChessBoard.PieceType.King   | ChessBoard.PieceType.Black,
                    _   => (ChessBoard.PieceType)255
                };
                retVal = pieceType != ChessBoard.PieceType.None;
            }
            return retVal;
        }

        /// <summary>
        /// Set a board state mask depending on the passed value
        /// </summary>
        /// <param name="value"> Value (must be 0 or 1)</param>
        /// <param name="mask">  Mask to add if 1</param>
        /// <returns>
        /// true if ok, false if error
        /// </returns>
        private bool SetBoardStateMask(string value, ChessBoard.BoardStateMask mask) {
            bool retVal;

            switch (value) {
            case "0":
                retVal          = true;
                break;
            case "1":
                retVal          = true;
                BoardStateMask |= mask;
                break;
            default:
                retVal          = false;
                break;
            }
            return retVal;
        }

        /// <summary>
        /// Check if an move termination as been issued
        /// </summary>
        /// <param name="line">               Line to parse</param>
        /// <param name="gameId">             Game id</param>
        /// <param name="terminationComment"> Termination comment if any</param>
        /// <param name="errTxt">             Error if any</param>
        /// <returns></returns>
        static public TerminationCode IsMoveTermination(string line, out int gameId, out string terminationComment, out string? errTxt) {
            TerminationCode retVal = TerminationCode.None;
            int             startIndex;
            int             endIndex;
            string[]        parts;
            
            //{Game 378 (OlegM vs. Chessnull) Chessnull forfeits on time} 1-0
            terminationComment = "";
            gameId             = 0;
            errTxt             = null;
            line               = line.Trim();
            if (line.StartsWith("{Game ")) {
                parts = line.Split(' ');
                if (int.TryParse(parts[1], out gameId)) {
                    switch (parts[^1]) {
                    case "1/2-1/2":
                        retVal = TerminationCode.Draw;
                        break;
                    case "1-0":
                        retVal = TerminationCode.WhiteWin;
                        break;
                    case "0-1":
                        retVal = TerminationCode.BlackWin;
                        break;
                    case "*":
                        retVal = TerminationCode.Terminated;
                        break;
                    default:
                        retVal = TerminationCode.TerminatedWithErr;
                        errTxt = $"Unknown termination character '{parts[^1]}'";
                        break;
                    }
                    if (retVal != TerminationCode.TerminatedWithErr) {
                        startIndex = line.IndexOf(") ");
                        endIndex   = line.IndexOf("}");
                        if (startIndex != -1 && endIndex != -1) {
                            terminationComment = line.Substring(startIndex + 2, endIndex - startIndex - 2);
                        }
                    }
                }
            } else if (line.StartsWith("Removing game ") && int.TryParse(line.Split(' ')[2], out gameId)) {
                retVal = TerminationCode.Terminated;
            }
            return retVal;
        }

        /// <summary>
        /// Parse a line
        /// </summary>
        /// <param name="line">               Line to parse</param>
        /// <param name="gameId">             Game ID</param>
        /// <param name="terminationCode">    Termination code if error or if game has ended</param>
        /// <param name="terminationComment"> Termination comment if any</param>
        /// <param name="errTxt">             Returned error if any. null if no error detected</param>
        /// <returns>
        /// Line or null if not a style12 line or error
        /// </returns>
        static public Style12MoveLine? ParseLine(string line, out int gameId, out TerminationCode terminationCode, out string terminationComment, out string? errTxt) {
            Style12MoveLine? retVal;
            string[]?        parts;
            string           fenLine;
            int              lineIndex;
            int              pos;
            int[]            intVals;

            terminationCode = IsMoveTermination(line, out gameId, out terminationComment, out errTxt);
            if (terminationCode != TerminationCode.None) {
                retVal = null;
            } else {
                parts = GetLineParts(line);
                if (parts == null) {
                    retVal = null;
                } else {
                    retVal    = new Style12MoveLine();
                    pos       = 63;
                    lineIndex = 0;
                    intVals   = new int[11];
                    while (lineIndex < 8 && errTxt == null) {
                        fenLine = parts[lineIndex + 1];
                        if (fenLine.Length != 8) {
                            errTxt = "Illegal board definition - bad FEN line size";
                        } else {
                            foreach (char chr in fenLine) {
                                if (DecodePiece(chr, out ChessBoard.PieceType pieceType)) {
                                    retVal.Board[pos--] = pieceType;
                                } else {
                                    errTxt = $"Illegal board definition - Unknown piece specification '{chr}'";
                                    break;
                                }
                            }
                        }
                        lineIndex++;
                    }
                    if (errTxt == null) {
                        switch(parts[9]) {
                        case "B":
                            retVal.NextMovePlayer = ChessBoard.PlayerColor.Black;
                            break;
                        case "W":
                            retVal.NextMovePlayer = ChessBoard.PlayerColor.White;
                            break;
                        default:
                            errTxt = "Next move player not 'B' or 'W'";
                            break;
                        }
                        if (errTxt == null) {
                            if (!retVal.SetBoardStateMask(parts[11], ChessBoard.BoardStateMask.WRCastling) ||
                                !retVal.SetBoardStateMask(parts[12], ChessBoard.BoardStateMask.WLCastling) ||
                                !retVal.SetBoardStateMask(parts[13], ChessBoard.BoardStateMask.BRCastling) ||
                                !retVal.SetBoardStateMask(parts[14], ChessBoard.BoardStateMask.BLCastling) ||
                                !int.TryParse(parts[15], out intVals[0])                                   ||
                                !int.TryParse(parts[16], out intVals[1])                                   ||
                                !int.TryParse(parts[19], out intVals[2])                                   ||
                                !int.TryParse(parts[20], out intVals[3])                                   ||
                                !int.TryParse(parts[21], out intVals[4])                                   ||
                                !int.TryParse(parts[22], out intVals[5])                                   ||
                                !int.TryParse(parts[23], out intVals[6])                                   ||
                                !int.TryParse(parts[24], out intVals[7])                                   ||
                                !int.TryParse(parts[25], out intVals[8])                                   ||
                                !int.TryParse(parts[26], out intVals[9])                                   ||
                                !int.TryParse(parts[30], out intVals[10])) {
                                errTxt = "Illegal value in field.";
                            } else if (intVals[2]  < -3 ||
                                       intVals[2]  > 2  ||
                                       intVals[3]  < 0  ||
                                       intVals[9]  < 0  ||
                                       intVals[10] < 0  ||
                                       intVals[10] > 1) {
                                errTxt = "Field value out of range.";
                            } else {
                                retVal.WhitePlayerName       = parts[17];
                                retVal.BlackPlayerName       = parts[18];
                                retVal.IrreversibleMoveCount = intVals[0];
                                retVal.GameId                = intVals[1];
                                retVal.RelationWithGame      = (RelationWithGameType)intVals[2];
                                retVal.InitialTime           = intVals[3];
                                retVal.IncrementTime         = intVals[4];
                                retVal.WhiteMaterial         = intVals[5];
                                retVal.BlackMaterial         = intVals[6];
                                retVal.WhiteRemainingTime    = intVals[7];
                                retVal.BlackRemainingTime    = intVals[8];
                                retVal.MoveNumber            = intVals[9];
                                retVal.LastMoveVerbose       = parts[27];
                                retVal.LastMoveSpan          = FicsGame.ParseTime(parts[28].Replace("(", "").Replace(")",""));
                                retVal.LastMoveSan           = parts[29];
                                retVal.IsFlipped             = (intVals[9] == 1);
                                gameId                       = retVal.GameId;
                            }
                        }
                        if (errTxt == null) {
                            if (parts.Length >= 33                      &&
                                int.TryParse(parts[31], out intVals[0]) &&
                                int.TryParse(parts[32], out intVals[1])) {
                                retVal.IsClockTicking = (intVals[0] == 1);
                                retVal.LagInMS        = intVals[1];   
                            } else {
                                retVal.IsClockTicking = true;
                                retVal.LagInMS        = 0;
                            }
                        }
                    }
                    if (errTxt != null) {
                        retVal   = null;
                        terminationCode = TerminationCode.TerminatedWithErr;
                    }
                }
            }
            return retVal;
        }

        /// <summary>
        /// Parse the receiving line info
        /// </summary>
        /// <param name="gameId">             ID of the game being listened to</param>
        /// <param name="lines">              List of lines to parse</param>
        /// <param name="lineQueue">          Queue where to register parsed lines</param>
        /// <param name="terminationComment"> Termination comment if any</param>
        /// <param name="errTxt">             Error if any, null if none</param>
        /// <returns>
        /// Termination code
        /// </returns>
        static public TerminationCode ParseStyle12Lines(int gameId, List<string> lines, Queue<Style12MoveLine> lineQueue, out string terminationComment, out string? errTxt) {
            TerminationCode  retVal = TerminationCode.None;
            Style12MoveLine? line;

            errTxt             = null;
            terminationComment = "";
            foreach (string textLine in lines) {
                line = ParseLine(textLine, out int foundGameId, out retVal, out terminationComment, out errTxt);
                if (foundGameId == gameId) {
                    if (line != null) {
                        lineQueue.Enqueue(line);
                    } else if (retVal != TerminationCode.None) {
                        break;
                    }
                }
            }
            return retVal;
        }
    }
}
