using System;
using System.Collections.Generic;
using System.IO;
using SrcChess2.Core;
using SrcChess2.PgnParsing;

namespace SrcChess2 {
    /// <summary>
    /// Override chess control to add information to the saved board
    /// </summary>
    internal class LocalChessBoardControl : ChessBoardControl {
        /// <summary>Parent Window</summary>
        public  MainWindow? ParentBoardWindow { get; set; }

        /// <summary>
        /// Class constructor
        /// </summary>
        public LocalChessBoardControl() : base() {}

        /// <summary>
        /// Load the game board
        /// </summary>
        /// <param name="reader">   Binary reader</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        public override bool LoadGame(BinaryReader reader) {
            bool                       retVal;
            string                     version;
            MainWindow.MainPlayingMode playingMode;
                
            version = reader.ReadString();
            if (version == "SRCCHESS095") {
                retVal = base.LoadGame(reader);
                if (retVal) {
                    playingMode                    = (MainWindow.MainPlayingMode)reader.ReadInt32();
                    ParentBoardWindow!.PlayingMode = playingMode;
                } else {
                    retVal = false;
                }
            } else {
                retVal = false;
            }
            return retVal;
        }

        /// <summary>
        /// Save the game board
        /// </summary>
        /// <param name="writer">   Binary writer</param>
        public override void SaveGame(BinaryWriter writer) {
            writer.Write("SRCCHESS095");
            base.SaveGame(writer);
            writer.Write((int)ParentBoardWindow!.PlayingMode);
        }

        /// <summary>
        /// Create a new game using the specified list of moves
        /// </summary>
        /// <param name="startingChessBoard"> Starting board or null if standard board</param>
        /// <param name="moveList">           List of moves</param>
        /// <param name="nextMoveColor">      Color starting to play</param>
        /// <param name="whitePlayerName">    Name of the player playing white pieces</param>
        /// <param name="blackPlayerName">    Name of the player playing black pieces</param>
        /// <param name="whitePlayerType">    Type of player playing white pieces</param>
        /// <param name="blackPlayerType">    Type of player playing black pieces</param>
        /// <param name="whitePlayerTime">    Timer for white</param>
        /// <param name="blackPlayerTime">    Timer for black</param>
        public override void CreateGameFromMove(ChessBoard?            startingChessBoard,
                                                List<MoveExt>          moveList,
                                                ChessBoard.PlayerColor nextMoveColor,
                                                string                 whitePlayerName,
                                                string                 blackPlayerName,
                                                PgnPlayerType          whitePlayerType,
                                                PgnPlayerType          blackPlayerType,
                                                TimeSpan               whitePlayerTime,
                                                TimeSpan               blackPlayerTime) {
            base.CreateGameFromMove(startingChessBoard,
                                    moveList,
                                    nextMoveColor,
                                    whitePlayerName,
                                    blackPlayerName,
                                    whitePlayerType,
                                    blackPlayerType,
                                    whitePlayerTime,
                                    blackPlayerTime);
            if (whitePlayerType == PgnPlayerType.Program) {
                if (blackPlayerType == PgnPlayerType.Program) {
                    ParentBoardWindow!.PlayingMode = MainWindow.MainPlayingMode.ComputerPlayBoth;
                } else {
                    ParentBoardWindow!.PlayingMode = MainWindow.MainPlayingMode.ComputerPlayWhite;
                }
            } else if (blackPlayerType == PgnPlayerType.Program) {
                ParentBoardWindow!.PlayingMode = MainWindow.MainPlayingMode.ComputerPlayBlack;
            } else {
                ParentBoardWindow!.PlayingMode = MainWindow.MainPlayingMode.PlayerAgainstPlayer;
            }
            MainWindow.SetCmdState();
        }
    }
}
