using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using SrcChess2.Core;

namespace SrcChess2.PgnParsing {
    //
    //  PGN BNF
    //
    //  <PGN-database>           ::= {<PGN-game>}
    //  <PGN-game>               ::= <tag-section> <movetext-section>
    //  <tag-section>            ::= {<tag-pair>}
    //  <tag-pair>               ::= '[' <tag-name> <tag-value> ']'
    //  <tag-name>               ::= <identifier>
    //  <tag-value>              ::= <string>
    //  <movetext-section>       ::= <element-sequence> <game-termination>
    //  <element-sequence>       ::= {<element>}
    //  <element>                ::= <move-number-indication> | <SAN-move> | <numeric-annotation-glyph>
    //  <move-number-indication> ::= Integer {'.'}
    //  <recursive-variation>    ::= '(' <element-sequence> ')'
    //  <game-termination>       ::= '1-0' | '0-1' | '1/2-1/2' | '*'

    /// <summary>
    /// Parser exception
    /// </summary>
    [Serializable]
    public class PgnParserException : System.Exception {
        /// <summary>Code which is in error</summary>
        public string?  CodeInError { get; }
        /// <summary>Array of move position</summary>
        public short[]? MoveList { get; set; } = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="errTxt">      Error Message</param>
        /// <param name="codeInError"> Code in error</param>
        /// <param name="ex">          Inner exception</param>
        public PgnParserException(string? errTxt, string? codeInError, Exception? ex) : base(errTxt, ex) { CodeInError = codeInError; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="errTxt">       Error Message</param>
        /// <param name="codeInError">  Code in error</param>
        public PgnParserException(string? errTxt, string? codeInError) : this(errTxt, codeInError, ex: null) {}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="errTxt">       Error Message</param>
        public PgnParserException(string? errTxt) : this(errTxt, codeInError: "", ex: null) {}

        /// <summary>
        /// Constructor
        /// </summary>
        public PgnParserException() : this(errTxt: "", codeInError: "", ex: null) {}

        /// <summary>
        /// Unserialize additional data
        /// </summary>
        /// <param name="info">    Serialization Info</param>
        /// <param name="context"> Context Info</param>
        protected PgnParserException(SerializationInfo info, StreamingContext context) : base(info, context) {
            CodeInError = info.GetString("CodeInError");
            MoveList    = info.GetValue("MoveList", typeof(short[])) as short[];
        }

        /// <summary>
        /// Serialize the additional data
        /// </summary>
        /// <param name="info">    Serialization Info</param>
        /// <param name="context"> Context Info</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("CodeInError", CodeInError);
            info.AddValue("MoveList", MoveList);
        }
    } // Class PgnParserException

    /// <summary>
    /// Class implementing the parsing of a PGN document. PGN is a standard way of recording chess games.
    /// </summary>
    public class PgnParser {

        /// <summary>
        /// Parsing Phase
        /// </summary>
        public enum ParsingPhase {
            /// <summary>No phase set yet</summary>
            None         = 0,
            /// <summary>Openning a file</summary>
            OpeningFile  = 1,
            /// <summary>Reading the file content into memory</summary>
            ReadingFile  = 2,
            /// <summary>Raw parsing the PGN file</summary>
            RawParsing   = 3,
            /// <summary>Creating the book</summary>
            CreatingBook = 10,
            /// <summary>Processing is finished</summary>
            Finished     = 255
        }

        /// <summary>true to cancel the parsing job</summary>
        private static bool         m_isJobCancelled;
        /// <summary>Board use to play as we decode</summary>
        private readonly ChessBoard m_chessBoard;
        /// <summary>true to diagnose the parser. This generate exception when a move cannot be resolved</summary>
        private readonly bool       m_isDiagnoseOn;
        /// <summary>PGN Lexical Analyser</summary>
        private PgnLexical?         m_pgnLexical;

        /// <summary>
        /// Class Ctor
        /// </summary>
        /// <param name="isDiagnoseOn"> true to diagnose the parser</param>
        public PgnParser(bool isDiagnoseOn) {
            m_chessBoard   = new ChessBoard();
            m_isDiagnoseOn = isDiagnoseOn;
            m_pgnLexical   = null;
        }

        /// <summary>
        /// Class Ctor
        /// </summary>
        /// <param name="chessBoard"> Chessboard to use</param>
        public PgnParser(ChessBoard chessBoard) {
            m_chessBoard   = chessBoard;
            m_isDiagnoseOn = false;
            m_pgnLexical   = null;
        }

        /// <summary>
        /// Initialize the parser using the content of a PGN file
        /// </summary>
        /// <param name="fileName"> File name</param>
        /// <returns>true if succeed, false if failed</returns>
        public bool InitFromFile(string fileName) {
            bool retVal;

            m_pgnLexical ??= new PgnLexical();
            retVal         = m_pgnLexical.InitFromFile(fileName);
            return retVal;
        }

        /// <summary>
        /// Initialize the parser using a PGN text
        /// </summary>
        /// <param name="text">  PGN Text</param>
        public void InitFromString(string text) {
            m_pgnLexical ??= new PgnLexical();
            m_pgnLexical.InitFromString(text);
        }

        /// <summary>
        /// Initialize from a PGN buffer object
        /// </summary>
        /// <param name="pgnLexical">    PGN Lexical Analyser</param>
        public void InitFromPgnBuffer(PgnLexical pgnLexical) => m_pgnLexical = pgnLexical;

        /// <summary>
        /// PGN buffer
        /// </summary>
        public PgnLexical? PgnLexical => m_pgnLexical;

        /// <summary>
        /// Return the code of the current game
        /// </summary>
        /// <returns>
        /// Current game
        /// </returns>
        private string GetCodeInError(long startPos, int length) => m_pgnLexical!.GetStringAtPos(startPos, length)!;

        /// <summary>
        /// Return the code of the current game
        /// </summary>
        /// <param name="tok">  Token</param>
        /// <returns>
        /// Current game
        /// </returns>
        private string GetCodeInError(PgnLexical.Token tok) => m_pgnLexical!.GetStringAtPos(tok.StartPos, tok.Size)!;

        /// <summary>
        /// Return the code of the current game
        /// </summary>
        /// <param name="pgnGame">    PGN game</param>
        /// <returns>
        /// Current game
        /// </returns>
        private string GetCodeInError(PgnGame pgnGame) => GetCodeInError(pgnGame.StartingPos, pgnGame.Length);

        /// <summary>
        /// Callback for 
        /// </summary>
        /// <param name="cookie">        Callback cookie</param>
        /// <param name="phase">         Parsing phase OpeningFile,ReadingFile,RawParsing,AnalysingMoves</param>
        /// <param name="fileIndex">     File index</param>
        /// <param name="fileCount">     Number of files to parse</param>
        /// <param name="fileName">      File name</param>
        /// <param name="gameProcessed"> Game processed since the last update</param>
        /// <param name="gameCount">     Game count</param>
        public delegate void DelProgressCallBack(object? cookie, ParsingPhase phase, int fileIndex, int fileCount, string? fileName, int gameProcessed, int gameCount);

        /// <summary>
        /// Decode a move
        /// </summary>
        /// <param name="pgnGame">  PGN game</param>
        /// <param name="pos">      Move position</param>
        /// <param name="startCol"> Returns the starting column found in move if specified (-1 if not)</param>
        /// <param name="startRow"> Returns the starting row found in move if specified (-1 if not)</param>
        /// <param name="endPos">   Returns the ending position of the move</param>
        private void DecodeMove(PgnGame pgnGame, string pos, out int startCol, out int startRow, out int endPos) {
            char chr1;
            char chr2;
            char chr3;
            char chr4;

            switch(pos.Length) {
            case 2:
                chr1 = pos[0];
                chr2 = pos[1];
                if (chr1 < 'a' || chr1 > 'h' ||
                    chr2 < '1' || chr2 > '8') {
                    throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
                }
                startCol = -1;
                startRow = -1;
                endPos   = (7 - (chr1 - 'a')) + ((chr2 - '1') << 3);
                break;
            case 3:
                chr1 = pos[0];
                chr2 = pos[1];
                chr3 = pos[2];
                if (chr1 >= 'a' && chr1 <= 'h') {
                    startCol = 7 - (chr1 - 'a');
                    startRow = -1;
                } else if (chr1 >= '1' && chr1 <= '8') {
                    startCol = -1;
                    startRow = (chr1 - '1');
                } else {
                    throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
                }
                if (chr2 < 'a' || chr2 > 'h' ||
                    chr3 < '1' || chr3 > '8') {
                    throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
                }
                endPos = (7 - (chr2 - 'a')) + ((chr3 - '1') << 3);
                break;
            case 4:
                chr1 = pos[0];
                chr2 = pos[1];
                chr3 = pos[2];
                chr4 = pos[3];
                if (chr1 < 'a' || chr1 > 'h' ||
                    chr2 < '1' || chr2 > '8' ||
                    chr3 < 'a' || chr3 > 'h' ||
                    chr4 < '1' || chr4 > '8') {
                    throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
                }
                startCol = 7 - (chr1 - 'a');
                startRow = (chr2 - '1');
                endPos   = (7 - (chr3 - 'a')) + ((chr4 - '1') << 3);
                break;
            default:
                throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
            }
        }

        /// <summary>
        /// Find a castle move
        /// </summary>
        /// <param name="pgnGame">         PGN game</param>
        /// <param name="playerColor">     Color moving</param>
        /// <param name="isShortCastling"> true for short, false for long</param>
        /// <param name="truncatedCount">  Truncated count</param>
        /// <param name="moveTxt">         Move text</param>
        /// <param name="move">            Returned moved if found</param>
        /// <returns>
        /// Moving position (Starting Position + Ending Position * 256) or -1 if error
        /// </returns>
        private short FindCastling(PgnGame pgnGame, ChessBoard.PlayerColor playerColor, bool isShortCastling, ref int truncatedCount, string moveTxt, ref MoveExt move) {
            short       retVal = -1;
            int         deltaWanted;
            int         delta;
            List<Move>  movePosList;

            movePosList = m_chessBoard.EnumMoveList(playerColor);
            deltaWanted = isShortCastling ? 2 : -2;
            foreach (Move moveTmp in movePosList) {
                if ((moveTmp.Type & Move.MoveType.MoveTypeMask) == Move.MoveType.Castle) {
                    delta = ((int)moveTmp.StartPos & 7) - ((int)moveTmp.EndPos & 7);
                    if (delta == deltaWanted) {
                        retVal = (short)(moveTmp.StartPos + (moveTmp.EndPos << 8));
                        move   = new MoveExt(moveTmp);
                        m_chessBoard.DoMove(move);
                    }
                }
            }
            if (retVal == -1) {
                if (m_isDiagnoseOn) {
                    throw new PgnParserException($"Unable to find compatible move - {moveTxt}", GetCodeInError(pgnGame));
                }
                truncatedCount++;
            }
            return retVal;
        }

        /// <summary>
        /// Find a move using the specification
        /// </summary>
        /// <param name="pgnGame">        PGN game</param>
        /// <param name="playerColor">    Color moving</param>
        /// <param name="pieceType">      Piece moving</param>
        /// <param name="startCol">       Starting column of the move or -1 if not specified</param>
        /// <param name="startRow">       Starting row of the move or -1 if not specified</param>
        /// <param name="endPos">         Ending position of the move</param>
        /// <param name="moveType">       Type of move. Use for discriminating between different pawn promotion.</param>
        /// <param name="moveTxt">        Move text</param>
        /// <param name="truncatedCount"> Truncated count</param>
        /// <param name="move">           Move position</param>
        /// <returns>
        /// Moving position (Starting Position + Ending Position * 256) or -1 if error
        /// </returns>
        private short FindPieceMove(PgnGame pgnGame, ChessBoard.PlayerColor playerColor, ChessBoard.PieceType pieceType, int startCol, int startRow, int endPos, Move.MoveType moveType, string moveTxt, ref int truncatedCount, ref MoveExt move) {
            short      retVal = -1;
            List<Move> movePosList;
            int        col;
            int        row;
            
            pieceType   |= playerColor == ChessBoard.PlayerColor.Black ? ChessBoard.PieceType.Black : ChessBoard.PieceType.White;
            movePosList  = m_chessBoard.EnumMoveList(playerColor);
            foreach (Move tmpMove in movePosList) {
                if ((int)tmpMove.EndPos == endPos && m_chessBoard[(int)tmpMove.StartPos] == pieceType) {
                    if (moveType == Move.MoveType.Normal || (tmpMove.Type & Move.MoveType.MoveTypeMask) == moveType) {
                        col = (int)tmpMove.StartPos & 7;
                        row = (int)tmpMove.StartPos >> 3;
                        if ((startCol == -1 || startCol == col) &&
                            (startRow == -1 || startRow == row)) {
                            if (retVal != -1) {
                                throw new PgnParserException($"More then one piece found for this move - {moveTxt}", GetCodeInError(pgnGame));
                            }
                            move   = new MoveExt(tmpMove);
                            retVal = (short)((int)tmpMove.StartPos + ((int)tmpMove.EndPos << 8));
                            m_chessBoard.DoMove(move);
                        }
                    }
                }
            }            
            if (retVal == -1) {
                if (m_isDiagnoseOn) {
                    throw new PgnParserException($"Unable to find compatible move - {moveTxt}", GetCodeInError(pgnGame));
                }
                truncatedCount++;
            }
            return retVal;
        }

        /// <summary>
        /// Convert a SAN position into a moving position
        /// </summary>
        /// <param name="pgnGame">        PGN game</param>
        /// <param name="playerColor">    Color moving</param>
        /// <param name="moveText">       Move text</param>
        /// <param name="pos">            Returned moving position (-1 if error, Starting position + Ending position * 256</param>
        /// <param name="truncatedCount"> Truncated count</param>
        /// <param name="move">           Move position</param>
        private void CnvSanMoveToPosMove(PgnGame pgnGame, ChessBoard.PlayerColor playerColor, string moveText, out short pos, ref int truncatedCount, ref MoveExt move) {
            string               pureMove;
            int                  index;
            ChessBoard.PieceType pieceType;
            Move.MoveType        moveType;
            
            moveType = Move.MoveType.Normal;
            pos      = 0;
            pureMove = moveText.Replace("x", "").Replace("#", "").Replace("ep","").Replace("+", "");
            index    = pureMove.IndexOf('=');
            if (index != -1) {
                if (pureMove.Length > index + 1) {
                    switch(pureMove[index+1]) {
                    case 'Q':
                        moveType = Move.MoveType.PawnPromotionToQueen;
                        break;
                    case 'R':
                        moveType = Move.MoveType.PawnPromotionToRook;
                        break;
                    case 'B':
                        moveType = Move.MoveType.PawnPromotionToBishop;
                        break;
                    case 'N':
                        moveType = Move.MoveType.PawnPromotionToKnight;
                        break;
                    case 'P':
                        moveType = Move.MoveType.PawnPromotionToPawn;
                        break;
                    default:
                        pos = -1;
                        truncatedCount++;
                        break;
                    }
                    if (pos != -1) {
                        pureMove = pureMove[..index];
                    }
                } else {
                    pos = -1;
                    truncatedCount++;
                }
            }
            if (pos == 0) {
                if (pureMove == "O-O") {
                    pos = FindCastling(pgnGame, playerColor, isShortCastling: true, ref truncatedCount, moveText, ref move);
                } else if (pureMove == "O-O-O") {
                    pos = FindCastling(pgnGame, playerColor, isShortCastling: false, ref truncatedCount, moveText, ref move);
                } else {
                    int ofs = 1;
                    switch (pureMove[0]) {
                    case 'K':   // King
                        pieceType = ChessBoard.PieceType.King;
                        break;
                    case 'N':   // Knight
                        pieceType = ChessBoard.PieceType.Knight;
                        break;
                    case 'B':   // Bishop
                        pieceType = ChessBoard.PieceType.Bishop;
                        break;
                    case 'R':   // Rook
                        pieceType = ChessBoard.PieceType.Rook;
                        break;
                    case 'Q':   // Queen
                        pieceType = ChessBoard.PieceType.Queen;
                        break;
                    default:    // Pawn
                        pieceType = ChessBoard.PieceType.Pawn;
                        ofs   = 0;
                        break;
                    }
                    DecodeMove(pgnGame, pureMove[ofs..], out int startCol, out int startRow, out int endPos);
                    pos = FindPieceMove(pgnGame, playerColor, pieceType, startCol, startRow, endPos, moveType, moveText, ref truncatedCount, ref move);
                }
            }
        }

        /// <summary>
        /// Convert a list of SAN positions into a moving positions
        /// </summary>
        /// <param name="pgnGame">        PGN game</param>
        /// <param name="colorToPlay">    Color to play</param>
        /// <param name="rawMoveList">    Array of PGN moves</param>
        /// <param name="moves">          Returned array of moving position (Starting Position + Ending Position * 256)</param>
        /// <param name="movePosList">    Returned the list of move if not null</param>
        /// <param name="truncatedCount"> Truncated count</param>
        private void CnvSanMoveToPosMove(PgnGame pgnGame, ChessBoard.PlayerColor colorToPlay, List<string> rawMoveList, out short[] moves, List<MoveExt>? movePosList, ref int truncatedCount) {
            List<short> shortMoveList;
            MoveExt     move;

            move          = new MoveExt(ChessBoard.PieceType.None, 0, 0, Move.MoveType.Normal, "", -1, -1, 0, 0);
            shortMoveList = new List<short>(256);
            try {
                foreach (string moveTxt in rawMoveList) {
                    CnvSanMoveToPosMove(pgnGame, colorToPlay, moveTxt, out short pos, ref truncatedCount, ref move);
                    if (pos != -1) {
                        shortMoveList.Add(pos);
                        movePosList?.Add(move);
                        colorToPlay = (colorToPlay == ChessBoard.PlayerColor.Black) ? ChessBoard.PlayerColor.White : ChessBoard.PlayerColor.Black;
                    } else {
                        break;
                    }
                }
            } catch(PgnParserException ex) {
                ex.MoveList = shortMoveList.ToArray();
                throw;
            }
            moves = shortMoveList.ToArray();
        }

        /// <summary>
        /// Parse FEN definition into a board representation
        /// </summary>
        /// <param name="fenTxt">         FEN</param>
        /// <param name="colorToMove">    Return the color to move</param>
        /// <param name="boardStateMask"> Return the mask of castling info</param>
        /// <param name="enPassantPos">   Return the en passant position or 0 if none</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        private bool ParseFen(string fenTxt, out ChessBoard.PlayerColor colorToMove, out ChessBoard.BoardStateMask boardStateMask, out int enPassantPos) {
            bool                 retVal = true;
            string[]             cmds;
            string[]             rows;
            string               cmd;
            int                  pos;
            int                  linePos;
            int                  blankCount;
            ChessBoard.PieceType pieceType;
            
            boardStateMask = (ChessBoard.BoardStateMask)0;
            enPassantPos   = 0;
            colorToMove    = ChessBoard.PlayerColor.White;
            cmds           = fenTxt.Split(' ');
            if (cmds.Length != 6) {
                retVal = false;
            } else {
                rows = cmds[0].Split('/');
                if (rows.Length != 8) {
                    retVal = false;
                } else {
                    pos = 63;
                    foreach (string row in rows) {
                        linePos = 0;
                        foreach (char chr in row) {
                            pieceType = ChessBoard.PieceType.None;
                            switch(chr) {
                            case 'P':
                                pieceType = ChessBoard.PieceType.Pawn   | ChessBoard.PieceType.White;
                                break;
                            case 'N':
                                pieceType = ChessBoard.PieceType.Knight | ChessBoard.PieceType.White;
                                break;
                            case 'B':
                                pieceType = ChessBoard.PieceType.Bishop | ChessBoard.PieceType.White;
                                break;
                            case 'R':
                                pieceType = ChessBoard.PieceType.Rook   | ChessBoard.PieceType.White;
                                break;
                            case 'Q':
                                pieceType = ChessBoard.PieceType.Queen  | ChessBoard.PieceType.White;
                                break;
                            case 'K':
                                pieceType = ChessBoard.PieceType.King   | ChessBoard.PieceType.White;
                                break;
                            case 'p':
                                pieceType = ChessBoard.PieceType.Pawn   | ChessBoard.PieceType.Black;
                                break;
                            case 'n':
                                pieceType = ChessBoard.PieceType.Knight | ChessBoard.PieceType.Black;
                                break;
                            case 'b':
                                pieceType = ChessBoard.PieceType.Bishop | ChessBoard.PieceType.Black;
                                break;
                            case 'r':
                                pieceType = ChessBoard.PieceType.Rook   | ChessBoard.PieceType.Black;
                                break;
                            case 'q':
                                pieceType = ChessBoard.PieceType.Queen  | ChessBoard.PieceType.Black;
                                break;
                            case 'k':
                                pieceType = ChessBoard.PieceType.King   | ChessBoard.PieceType.Black;
                                break;
                            default:
                                if (chr >= '1' && chr <= '8') {
                                     blankCount = int.Parse(chr.ToString());
                                     if (blankCount + linePos <= 8) {
                                        for (int i = 0; i < blankCount; i++) {
                                            m_chessBoard[pos--] = ChessBoard.PieceType.None;
                                        }
                                        linePos += blankCount;
                                    }
                                } else {
                                    retVal = false;
                                }
                                break;
                            }
                            if (retVal && pieceType != ChessBoard.PieceType.None) {
                                if (linePos < 8) {
                                    m_chessBoard[pos--] = pieceType;
                                    linePos++;
                                } else {
                                    retVal = false;
                                }
                            }
                        }
                        if (linePos != 8) {
                            retVal = false;
                        }
                    }
                    if (retVal) {
                        cmd = cmds[1];
                        if (cmd == "w") {
                            colorToMove = ChessBoard.PlayerColor.White;
                        } else if (cmd == "b") {
                            colorToMove = ChessBoard.PlayerColor.Black;
                        } else {
                            retVal = false;
                        }
                        cmd = cmds[2];
                        if (cmd != "-") {
                            for (int i = 0; i < cmd.Length; i++) {
                                boardStateMask |= cmd[i] switch {
                                    'K' => ChessBoard.BoardStateMask.WRCastling,
                                    'Q' => ChessBoard.BoardStateMask.WLCastling,
                                    'k' => ChessBoard.BoardStateMask.BRCastling,
                                    'q' => ChessBoard.BoardStateMask.BLCastling,
                                     _  => 0
                                };
                            }
                        }
                        cmd = cmds[3];
                        if (cmd == "-") {
                            enPassantPos = 0;
                        } else {
                            enPassantPos = PgnUtil.GetSquareIdFromPgn(cmd);
                            if (enPassantPos == -1) {
                                enPassantPos = 0;
                            }
                        }
                    }
                }
            }
            return retVal;
        }

        /// <summary>
        /// Parse FEN definition into a board representation
        /// </summary>
        /// <param name="fenTxt">           FEN</param>
        /// <param name="startingColor">    Return the color to move</param>
        /// <param name="chessBoard">       Return the chess board represented by this FEN</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        public bool ParseFen(string fenTxt, out ChessBoard.PlayerColor startingColor, out ChessBoard? chessBoard) {
            bool retVal;

            m_chessBoard.OpenDesignMode();
            retVal      = ParseFen(fenTxt, out startingColor, out ChessBoard.BoardStateMask boardMask, out int enPassantPos);
            m_chessBoard.CloseDesignMode(startingColor, boardMask, enPassantPos);
            chessBoard  = retVal ? m_chessBoard.Clone() : null;
            return retVal;
        }

        /// <summary>
        /// Parse PGN moves
        /// </summary>
        /// <param name="sanMoveList">      Returned list of attributes for this game. Can be null to skip move section</param>
        /// <param name="isFen">            true if FEN present</param>
        /// <param name="isBadMoveFound">   true if a bad move has been found</param>
        /// <remarks>
        ///     movetext-section        ::= element-sequence game-termination
        ///     element-sequence        ::= {element}
        ///     element                 ::= move-number-indication | SAN-move | numeric-annotation-glyph
        ///     move-number-indication  ::= Integer {'.'}
        ///     recursive-variation     ::= '(' element-sequence ')'
        ///     game-termination        ::= '1-0' | '0-1' | '1/2-1/2' | '*'
        ///  </remarks>
        private void ParseMoves(List<string>? sanMoveList, bool isFen, out bool isBadMoveFound) {
            int              plyIndex;
            PgnLexical.Token tok;

            plyIndex       = 2;
            tok            = m_pgnLexical!.GetNextToken();
            isBadMoveFound = false;
            switch(tok.Type) {
            case PgnLexical.TokenType.Integer:
            case PgnLexical.TokenType.Symbol:
            case PgnLexical.TokenType.Nag:
                while (tok.Type != PgnLexical.TokenType.Eof && tok.Type != PgnLexical.TokenType.Termination) {
                    switch(tok.Type) {
                    case PgnLexical.TokenType.Integer:
                        if (!isFen && tok.IntValue != plyIndex / 2) {
                            throw new PgnParserException("Bad move number", GetCodeInError(tok));
                        }
                        break;
                    case PgnLexical.TokenType.Dot:
                        break;
                    case PgnLexical.TokenType.Symbol:
                        if (sanMoveList != null) {
                            sanMoveList.Add(tok.StrValue);
                        }
                        plyIndex++;
                        break;
                    case PgnLexical.TokenType.UnknownToken:
                        if (sanMoveList != null) {
                            sanMoveList.Add(tok.StrValue);
                        }
                        plyIndex++;
                        isBadMoveFound = true;
                        break;
                    case PgnLexical.TokenType.Nag:
                        break;
                    }
                    tok = m_pgnLexical.GetNextToken();
                }
                if (tok.Type != PgnLexical.TokenType.Eof) {
                    m_pgnLexical.AssumeToken(PgnLexical.TokenType.Termination, tok);
                }
                break;
            case PgnLexical.TokenType.Termination:
                break;
            default:
                m_pgnLexical.PushToken(tok);
                break;
            }
        }

        /// <summary>
        /// Parse PGN attributes
        /// </summary>
        /// <param name="attrs">    Returned list of attributes for this game</param>
        /// <returns>
        /// Attribute dictionary
        /// </returns>
        /// <remarks>
        ///     tag-section     ::= {tag-pair}
        ///     tag-pair        ::= '[' tag-name tag-value ']'
        ///     tag-name        ::= identifier
        ///     tag-value       ::= string
        /// </remarks>
        private Dictionary<string, string>? ParseAttrs(Dictionary<string, string>? attrs) {
            Dictionary<string, string>? retVal;
            PgnLexical.Token            tok;
            PgnLexical.Token            tokName;
            PgnLexical.Token            tokValue;

            retVal  = attrs;
            tok     = m_pgnLexical!.GetNextToken();
            while (tok.Type == PgnLexical.TokenType.OpenSBracket) {
                tokName  = m_pgnLexical.AssumeToken(PgnLexical.TokenType.Symbol);
                tokValue = m_pgnLexical.AssumeToken(PgnLexical.TokenType.String);
                m_pgnLexical.AssumeToken(PgnLexical.TokenType.CloseSBracket);
                if (retVal == null && tokName.StrValue == "FEN") {
                    retVal = new Dictionary<string, string>();
                }
                retVal?.Add(tokName.StrValue, tokValue.StrValue);
                tok = m_pgnLexical.GetNextToken();
            }
            m_pgnLexical.PushToken(tok);
            return retVal;
        }

        /// <summary>
        /// Parse a PGN text
        /// </summary>
        /// <param name="isAttrList">     Game to be filled with attributes and moves</param>
        /// <param name="isMoveList">     Game to be filled with attributes and moves</param>
        /// <param name="isBadMoveFound"> true if a bad move has been found</param>
        /// <returns>
        /// PGN game or null if none found
        /// </returns>
        /// <remarks>
        ///     PGN-game        ::= tag-section movetext-section
        ///     tag-section     ::= tag-pair
        /// </remarks>
        private PgnGame? ParsePgn(bool isAttrList, bool isMoveList, out bool isBadMoveFound) {
            PgnGame?         pgnGame;
            PgnLexical.Token tok;
            string           game;

            isBadMoveFound = false;
            tok            = m_pgnLexical!.PeekToken();
            if (tok.Type == PgnLexical.TokenType.Eof) {
                pgnGame = null;
            } else {
                pgnGame = new PgnGame(isAttrList, isMoveList) {
                    StartingPos = tok.StartPos
                };
                game = GetCodeInError(pgnGame.StartingPos, 1);
                if (game[0] != '[') {
                    System.Windows.MessageBox.Show($"Oops! Game doesn't begin with '[' Pos={pgnGame.StartingPos}");
                }
                if (pgnGame.Attrs != null) {
                    pgnGame.Attrs.Clear();
                }
                if (pgnGame.SanMoves != null) {
                    pgnGame.SanMoves.Clear();
                }
                pgnGame.Attrs = ParseAttrs(pgnGame.Attrs);
                ParseMoves(pgnGame.SanMoves, pgnGame.Fen != null, out isBadMoveFound);
                tok = m_pgnLexical.PeekToken();
                pgnGame.Length = (int)(tok.StartPos - pgnGame.StartingPos);
            }
            return pgnGame;
        }

        /// <summary>
        /// Analyze the PGN games to find the non-ambiguous move list
        /// </summary>
        /// <param name="pgnGame">             Game being analyzed</param>
        /// <param name="ignoreMoveListIfFEN"> Ignore the move list if FEN is found</param>
        /// <param name="fillMoveExtList">     Fills the move extended list if true</param>
        /// <param name="skipCount">           Number of games skipped</param>
        /// <param name="truncatedCount">      Number of games truncated</param>
        /// <param name="errTxt">              Error if any</param>
        /// <returns>
        /// false if invalid board
        /// </returns>
        /// <remarks>
        /// 
        /// The parser understand an extended version of the [TimeControl] tag:
        /// 
        ///     [TimeControl "?:123:456"]   where 123 = white tick count, 456 = black tick count (100 nano-sec unit)
        ///
        /// The parser also understand the following standard tags:
        /// 
        ///     [White] [Black] [FEN] [WhiteType] [BlackType]
        /// 
        /// </remarks>
        public bool AnalyzePgn(PgnGame     pgnGame,
                               bool        ignoreMoveListIfFEN,
                               bool        fillMoveExtList,
                               ref int     skipCount,
                               ref int     truncatedCount,
                               out string? errTxt) {
            bool                   retVal = true;
            string?                fen;
            List<string>?          rawMoveList;
            ChessBoard.PlayerColor startingColor;
            ChessBoard?            startingChessBoard;

            errTxt = null;
            if (m_pgnLexical == null) {
                throw new MethodAccessException("Must initialize the parser first");
            }
            startingChessBoard = null;
            startingColor      = ChessBoard.PlayerColor.White;
            fen                = pgnGame.Fen;
            rawMoveList        = pgnGame.SanMoves;
            m_chessBoard.ResetBoard();
            if (fen != null) {
                retVal = ParseFen(fen, out startingColor, out startingChessBoard);
                if (retVal) {
                    pgnGame.StartingColor      = startingColor;
                    pgnGame.StartingChessBoard = startingChessBoard;
                } else {
                    errTxt = "Error parsing the FEN attribute";
                }
            }
            pgnGame.MoveExtList = fillMoveExtList ? new List<MoveExt>(ignoreMoveListIfFEN ? 0 : 256) : null;
            if (retVal && rawMoveList != null && !(fen != null && ignoreMoveListIfFEN)) {
                if (rawMoveList.Count == 0 && startingChessBoard == null) {
                    skipCount++;
                }
                try {
                    CnvSanMoveToPosMove(pgnGame, startingColor, rawMoveList, out short[] moves, pgnGame.MoveExtList, ref truncatedCount);
                    pgnGame.MoveList = moves;
                } catch(PgnParserException ex) {
                    errTxt    = $"{ex.Message}\r\n\r\n{ex.CodeInError}";
                    retVal     = false;
                }
            }
            return retVal;
        }

        /// <summary>
        /// Parse if its a FEN line. FEN have only one line and must have 7 '/' which is highly improbable for a PGN text
        /// </summary>
        /// <param name="startingColor">    Return the color to move</param>
        /// <param name="chessBoard">       Return the chessboard represent by this FEN</param>
        /// <returns>
        /// true if its a FEN text, false if not
        /// </returns>
        private bool ParseIfFenLine(out ChessBoard.PlayerColor startingColor, out ChessBoard? chessBoard) {
            bool    retVal = false;

            startingColor  = ChessBoard.PlayerColor.White;
            chessBoard      = null;
            if (m_pgnLexical!.IsOnlyFen()) {
                retVal = ParseFen(m_pgnLexical.GetStringAtPos(0, (int)m_pgnLexical.TextSize)! , out startingColor, out chessBoard);
            }
            return retVal;
        }

        /// <summary>
        /// Parse a single PGN/FEN game
        /// </summary>
        /// <param name="ignoreMoveListIfFen"> Ignore the move list if FEN is found</param>
        /// <param name="skipCount">           Number of games skipped</param>
        /// <param name="truncatedCount">      Number of games truncated</param>
        /// <param name="pgnGame">             Returned PGN game</param>
        /// <param name="errTxt">              Error if any</param>
        /// <returns>
        /// false if the board specified by FEN is invalid.
        /// </returns>
        public bool ParseSingle(bool         ignoreMoveListIfFen,
                                out int      skipCount,
                                out int      truncatedCount,
                                out PgnGame? pgnGame,
                                out string?  errTxt) {
            bool retVal;

            errTxt = null;
            if (m_pgnLexical == null) {
                throw new MethodAccessException("Must initialize the parser first");
            }
            skipCount      = 0;
            truncatedCount = 0;
            if (ParseIfFenLine(out ChessBoard.PlayerColor startingColor, out ChessBoard? startingChessBoard)) {
                pgnGame = new PgnGame(createAttrList: true, createMoveList: true) {
                    StartingColor      = startingColor,
                    StartingChessBoard = startingChessBoard
                };
                retVal = true;
                pgnGame.SetDefaultValue();
            } else {
                pgnGame = ParsePgn(isAttrList: true, isMoveList: true, out bool isBadMoveFound);
                if (pgnGame != null) {
                    if (isBadMoveFound) {
                        throw new PgnParserException($"PGN contains a bad move\r\n\r\n{GetCodeInError(pgnGame)}");
                    }
                    pgnGame.SetDefaultValue();
                    retVal = AnalyzePgn(pgnGame,
                                        ignoreMoveListIfFen,
                                        fillMoveExtList: true,
                                        ref skipCount,
                                        ref truncatedCount,
                                        out errTxt);
                } else {
                    pgnGame = new PgnGame(createAttrList: true, createMoveList: true);
                    retVal  = true;
                    pgnGame.SetDefaultValue();
                }
            }
            return retVal;
        }

        /// <summary>
        /// Gets the list of all raw PGN in the specified text
        /// </summary>
        /// <param name="getAttrList">  true to create attributes list</param>
        /// <param name="getMoveList">  true to create move list</param>
        /// <param name="skippedCount"> Number of game which has been skipped because of bad move</param>
        /// <param name="callback">     Callback</param>
        /// <param name="cookie">       Cookie for callback</param>
        public List<PgnGame> GetAllRawPgn(bool getAttrList, bool getMoveList, out int skippedCount, DelProgressCallBack? callback, object? cookie) {
            List<PgnGame> retVal;
            PgnGame?      pgnGame;
            int           bufferCount;
            int           bufferPos;
            int           oldBufferPos;

            skippedCount = 0;
            if (m_pgnLexical == null) {
                throw new MethodAccessException("Must initialize the parser first");
            }
            m_isJobCancelled = false;
            retVal           = new List<PgnGame>(1000000);
            bufferCount      = m_pgnLexical.BufferCount;
            bufferPos        = 0;
            oldBufferPos     = 0;
            callback?.Invoke(cookie, ParsingPhase.RawParsing, fileIndex: 0, fileCount: 0, fileName: null, gameProcessed: 0, bufferCount);
            while (!m_isJobCancelled && (pgnGame = ParsePgn(getAttrList, getMoveList, out bool isBadMoveFound)) != null && !m_isJobCancelled) {
                if (isBadMoveFound) {
                    skippedCount++;
                } else {
                    if (pgnGame.SanMoves == null || pgnGame.SanMoves.Count != 0) {
                        retVal.Add(pgnGame);
                    }
                    if (callback != null) {
                        bufferPos  = m_pgnLexical.CurrentBufferPos;
                        if (bufferPos != oldBufferPos) {
                            oldBufferPos = bufferPos;
                            if ((bufferPos % 100) == 0) {
                                callback(cookie, ParsingPhase.RawParsing, 0, 0, null, bufferPos, bufferCount);
                            }
                        }
                    }
                }
            }
            callback?.Invoke(cookie, ParsingPhase.RawParsing, fileIndex: 0, fileCount: 0, fileName: null, m_pgnLexical.CurrentBufferPos, bufferCount);
            return (retVal);
        }

        /// <summary>
        /// Gets the list of all raw PGN in the specified text
        /// </summary>
        /// <param name="getMoveList">  true to create move list</param>
        /// <param name="getAttrList">  true to create attributes list</param>
        /// <param name="skippedCount"> Number of games skipped because of bad moves</param>
        public List<PgnGame> GetAllRawPgn(bool getAttrList, bool getMoveList, out int skippedCount) => GetAllRawPgn(getAttrList, getMoveList, out skippedCount, callback: null, cookie: null);

        /// <summary>
        /// Analyze the games in the list in multiple threads
        /// </summary>
        /// <param name="pgnGames">       List of games</param>
        /// <param name="skipCount">      Skip count</param>
        /// <param name="truncatedCount"> Truncated count</param>
        /// <param name="threadCount">    Thread count</param>
        /// <param name="errTxt">         Error if any</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        private bool AnalyzeInParallel(List<PgnGame> pgnGames, ref int skipCount, ref int truncatedCount, int threadCount, out string? errTxt) {
            bool    retVal              = true;
            int     localSkipCount      = 0;
            int     localTruncatedCount = 0;
            string? localErrTxt         = null;

            Parallel.For(0, threadCount, (threadIndex) => {
                PgnGame   pgnGame;
                int       index;
                int       start;
                int       gamePerThread;
                int       skipCount = 0;
                int       truncatedCount = 0;
                PgnParser parser;

                parser        = new PgnParser(false);
                parser.InitFromPgnBuffer(m_pgnLexical!);
                gamePerThread = pgnGames.Count / threadCount;
                start         = threadIndex * gamePerThread;
                index         = start;
                while (index < start + gamePerThread && !m_isJobCancelled) {
                    pgnGame = pgnGames[index];
                    if (parser.AnalyzePgn(pgnGame,
                                          ignoreMoveListIfFEN: true,
                                          fillMoveExtList: false,
                                          ref skipCount,
                                          ref truncatedCount,
                                          out string? tmpErrTxt)) {
                        lock (pgnGames) {
                            localSkipCount      += skipCount;
                            localTruncatedCount += truncatedCount;
                        }
                    } else {
                        lock(pgnGames) {
                            localErrTxt = tmpErrTxt ?? "unknown";
                            CancelParsingJob();
                            retVal = false;
                        }
                    }
                    index++;
                }
            });
            skipCount      += localSkipCount;
            truncatedCount += localTruncatedCount;
            errTxt          = localErrTxt;
            return retVal;
        }

        /// <summary>
        /// Parse a PGN text file. The move list are returned as a list of array of int. Each int encoding the starting position in the first 8 bits and the ending position in the second 8 bits
        /// </summary>
        /// <param name="movesList">      List of moves</param>
        /// <param name="callback">       Delegate callback (can be null)</param>
        /// <param name="cookie">         Cookie for the callback</param>
        /// <param name="skipCount">      Number of games skipped</param>
        /// <param name="truncatedCount"> Number of games truncated</param>
        /// <param name="errTxt">         Error if any</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        public bool ParseAllPgnMoveList(List<short[]> movesList, DelProgressCallBack? callback, object? cookie, out int skipCount, out int truncatedCount, out string? errTxt) {
            bool          retVal = true;
            List<PgnGame> pgnGameList;
            PgnGame?      pgnGame;
            int           threadCount;
            int           bufferCount;
            int           bufferPos;
            int           oldBufferPos;
            bool          isBadMoveFound;
            const int     gamePerThread = 4096;
            int           batchSize;
            int           textSizeInMb;

            errTxt = null;
            if (m_pgnLexical == null) {
                throw new MethodAccessException("Must initialize the parser first");
            }
            skipCount      = 0;
            truncatedCount = 0;
            if (m_isJobCancelled) {
                retVal = false;
            } else {
                threadCount  = Environment.ProcessorCount;
                bufferCount  = m_pgnLexical.BufferCount;
                oldBufferPos = 0;
                textSizeInMb = (int)(m_pgnLexical.TextSize / 1048576);
                callback?.Invoke(cookie, ParsingPhase.RawParsing, fileIndex: 0, fileCount: 0, fileName: null, gameProcessed: 0, textSizeInMb);
                if (threadCount == 1) {
                    while (retVal && !m_isJobCancelled && (pgnGame = ParsePgn(isAttrList: false, isMoveList: true, out isBadMoveFound)) != null) {
                        if (isBadMoveFound || pgnGame.Fen != null) {
                            skipCount++;
                        } else if (pgnGame.SanMoves != null && pgnGame.SanMoves.Count != 0) {
                            retVal = AnalyzePgn(pgnGame,
                                                ignoreMoveListIfFEN: true,
                                                fillMoveExtList: false,
                                                ref skipCount,
                                                ref truncatedCount,
                                                out errTxt);
                            if (retVal) {
                                if (pgnGame.MoveList != null) {
                                    movesList.Add(pgnGame.MoveList);
                                }
                                if (callback != null) {
                                    bufferPos = m_pgnLexical.CurrentBufferPos;
                                    if (bufferPos != oldBufferPos) {
                                        oldBufferPos = bufferPos;
                                        if ((bufferPos % 100) == 0) {
                                            callback(cookie, ParsingPhase.RawParsing, fileIndex: 0, fileCount: 0, fileName: null, bufferPos, textSizeInMb);
                                        }
                                    }
                                }
                                m_pgnLexical.FlushOldBuffer();
                            }
                        }
                    }
                } else {
                    batchSize   = threadCount * gamePerThread;
                    pgnGameList = new List<PgnGame>(batchSize);
                    while (retVal && !m_isJobCancelled && (pgnGame = ParsePgn(isAttrList: false, isMoveList: true, out isBadMoveFound)) != null) {
                        if (isBadMoveFound && pgnGame.Fen != null) {
                            skipCount++;
                        } else if (pgnGame.SanMoves != null && pgnGame.SanMoves.Count != 0) {
                            pgnGameList.Add(pgnGame);
                            if (pgnGameList.Count == batchSize) {
                                retVal = AnalyzeInParallel(pgnGameList, ref skipCount, ref truncatedCount, threadCount, out errTxt);
                                if (retVal) {
                                    foreach (PgnGame pgnGameTmp in pgnGameList) {
                                        if (pgnGameTmp.MoveList != null) {
                                            movesList.Add(pgnGameTmp.MoveList);
                                        }
                                    }
                                    pgnGameList.Clear();
                                    m_pgnLexical.FlushOldBuffer();
                                    if (callback != null) {
                                        bufferPos  = m_pgnLexical.CurrentBufferPos;
                                        if (bufferPos != oldBufferPos) {
                                            oldBufferPos = bufferPos;
                                            callback(cookie, ParsingPhase.RawParsing, fileIndex: 0, fileCount: 0, fileName: null, bufferPos, textSizeInMb);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (retVal) {
                        foreach (PgnGame pgnGameTmp in pgnGameList) {
                            retVal = AnalyzePgn(pgnGameTmp,
                                                ignoreMoveListIfFEN: true,
                                                fillMoveExtList: false,
                                                ref skipCount,
                                                ref truncatedCount,
                                                out errTxt);
                            if (retVal) {
                                if (pgnGameTmp.MoveList != null) {
                                    movesList.Add(pgnGameTmp.MoveList);
                                }
                            }
                        }
                    }
                }
                callback?.Invoke(cookie, ParsingPhase.RawParsing, fileIndex: 0, fileCount: 0, fileName: null, bufferCount, textSizeInMb);
            }
            return retVal;
        }

        /// <summary>
        /// Parse a series of PGN games
        /// </summary>
        /// <param name="fileNames">      Array of file name</param>
        /// <param name="callback">       Delegate callback (can be null)</param>
        /// <param name="cookie">         Cookie for the callback</param>
        /// <param name="movesList">      List of move list array</param>
        /// <param name="totalSkipped">   Number of games skipped because of error</param>
        /// <param name="totalTruncated"> Number of games truncated</param>
        /// <param name="errTxt">         Returned error if return value is false</param>
        /// <returns>true if succeed, false if error</returns>
        public static bool ExtractMoveListFromMultipleFiles(string[] fileNames, DelProgressCallBack callback, object? cookie, out List<short[]> movesList, out int totalSkipped, out int totalTruncated, out string? errTxt) {
            bool      retVal = true;
            int       fileIndex;
            string    fileName;
            PgnParser parser;

            m_isJobCancelled = false;
            totalSkipped     = 0;
            totalTruncated   = 0;
            errTxt           = null;
            fileIndex        = 0;
            movesList        = new List<short[]>(1000000);
            while (fileIndex < fileNames.Length && errTxt == null && !m_isJobCancelled) {
                fileName = fileNames[fileIndex++];
                callback?.Invoke(cookie, ParsingPhase.OpeningFile, fileIndex, fileNames.Length, fileName, gameProcessed: 0, gameCount: 0);
                parser = new PgnParser(false);
                callback?.Invoke(cookie, ParsingPhase.ReadingFile, fileIndex, fileNames.Length, fileName, gameProcessed: 0, gameCount: 0);
                if (parser.InitFromFile(fileName)) {
                    retVal = parser.ParseAllPgnMoveList(movesList, callback, cookie, out int skipCount, out int truncatedCount, out errTxt);
                    if (retVal) {
                        totalSkipped   += skipCount;
                        totalTruncated += truncatedCount;
                    }
                } else {
                    errTxt = "Error loading file";
                }
            }
            if (errTxt == null && m_isJobCancelled) {
                errTxt = "Cancelled by the user";
            }
            retVal = (errTxt == null);
            return retVal;
        }

        /// <summary>
        /// Call to cancel the parsing job
        /// </summary>
        public static void CancelParsingJob() => m_isJobCancelled = true;

        /// <summary>
        /// true if job has been cancelled
        /// </summary>
        public static bool IsJobCancelled => m_isJobCancelled;

        /// <summary>
        /// Apply a SAN move to the board
        /// </summary>
        /// <param name="pgnGame"> PGN game</param>
        /// <param name="sanTxt">  SAN move</param>
        /// <param name="move">    Converted move</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        public bool ApplySanMoveToBoard(PgnGame pgnGame, string? sanTxt, out MoveExt move) {
            bool retVal;
            int  truncatedCount = 0;

            move = new MoveExt(ChessBoard.PieceType.None,
                               startPos: 0,
                               endPos:   0,
                               Move.MoveType.Normal,
                               comment: "",
                               permutationCount: -1,
                               searchDepth: -1,
                               cacheHit: 0,
                               nagCode: 0);
            if (!string.IsNullOrEmpty(sanTxt)) {
                try {
                    CnvSanMoveToPosMove(pgnGame, 
                                        m_chessBoard.CurrentPlayer,
                                        sanTxt,
                                        out short pos,
                                        ref truncatedCount,
                                        ref move);
                    retVal = (truncatedCount == 0);
                } catch(PgnParserException) {
                    retVal = false;
                }
            } else {
                retVal = false;
            }
            return retVal;
        }
    } // Class PgnParser
} // Namespace
