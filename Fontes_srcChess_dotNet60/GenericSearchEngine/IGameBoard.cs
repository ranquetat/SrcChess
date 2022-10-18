using SrcChess2.Core;
using System;
using System.Collections.Generic;

namespace GenericSearchEngine {

    /// <summary>
    /// Game board interface needed by the search engines
    /// </summary>
    public interface IGameBoard<TMove> where TMove : struct {

        /// <summary>
        /// Translation table
        /// </summary>
        TransTable? GetTransTable(SearchEngineSetting searchEngineSetting);

        /// <summary>
        /// Gets the value of the zobrist key for the current board
        /// </summary>
        long ZobristKey { get; }

        /// <summary>
        /// Clone the specified board
        /// </summary>
        /// <returns>
        /// Clone of the board
        /// </returns>
        IGameBoard<TMove> Clone();

        /// <summary>
        /// Do a move
        /// </summary>
        /// <param name="move"> Do the specified move</param>
        /// <returns>
        /// Return true if move has been done, false if draw
        /// </returns>
        bool DoMoveNoLog(TMove move);

        /// <summary>
        /// Look if the specified player is in check
        /// </summary>
        /// <param name="playerId"> Player id</param>
        /// /// <returns>
        /// Game result
        /// </returns>
        bool IsCheck(int playerId);


        /// <summary>
        /// Undo the specified move
        /// </summary>
        /// <param name="move"> Move to be undone</param>
        void UndoMoveNoLog(TMove move);

        /// <summary>
        /// Gets the list of moves for the specified player
        /// </summary>
        /// <param name="playerId">         Player id</param>
        /// <param name="attackPosInfo">    Returned number of attacking/defending position</param>
        /// <returns>
        /// Move list
        /// </returns>
        List<TMove> GetMoves(int playerId, out AttackPosInfo attackPosInfo);

        /// <summary>
        /// Creates an empty move
        /// </summary>
        /// <returns>
        /// Empty move
        /// </returns>
        TMove CreateEmptyMove();

        /// <summary>
        /// Compute the board extra info if any
        /// </summary>
        /// <returns>
        /// Extra info
        /// </returns>
        public int ComputeBoardExtraInfo();

        /// <summary>
        /// Determines if there is enough pieces to win
        /// </summary>
        /// <returns>
        /// true if ok, false if not enough piece for winning
        /// </returns>
        bool IsEnoughPieceForWinning();

        /// <summary>
        /// Evaluate the board
        /// </summary>
        /// <param name="searchEngineSetting">  Search engine setting</param>
        /// <param name="playerId">             Player id (0 or 1)</param>
        /// <param name="moveCountDelta">       Player 1 count - Player 2 count</param>
        /// <param name="player1AttackPosInfo"> Information about pieces attack for player 1</param>
        /// <param name="player2AttackPosInfo"> Information about pieces attack for player 2</param>
        /// <returns>
        /// Points for the board
        /// </returns>
        int EvaluateBoard(SearchEngineSetting searchEngineSetting, int playerId, int moveCountDelta, AttackPosInfo player1AttackPosInfo, AttackPosInfo player2AttackPosInfo);

        /// <summary>
        /// true if points are a winning
        /// </summary>
        /// <param name="pts"></param>
        /// <returns>
        /// true if pts is a winning condition
        /// </returns>
        public bool IsWinningPts(int pts);

        /// <summary>
        /// Determine if the move is terminal. If yes, return the board evaluation
        /// </summary>
        /// <param name="moveListPlayerId">        Id of the player which did the last move</param>
        /// <param name="moveList">                List of moves for the specified player id</param>
        /// <param name="minMaxInfo">              MinMax info</param>
        /// <param name="isSearchHasBeenCanceled"> true if search has been canceled</param>
        /// <param name="pts">                     Points given by the evaluation if terminal</param>
        /// <returns>
        /// true if terminal, false if not
        /// </returns>
        bool IsMoveTerminal(int          moveListPlayerId,
                            List<TMove>  moveList,
                            MinMaxInfo   minMaxInfo,
                            bool         isSearchHasBeenCanceled,
#if MinMaxDebug                   
                            TMove        moveDone,
                            Stack<TMove> moveStack,
#endif                            
                            out int      pts);

        /// <summary>
        /// Invoke a move action for the specified move
        /// </summary>
        /// <typeparam name="TCookie">      Cookie type</typeparam>
        /// <param name="move">             Move</param>
        /// <param name="cookie">           Cookir</param>
        /// <param name="permCount">        Permutation count</param>
        /// <param name="cacheHit">         Cache hit</param>
        /// <param name="maxDepth">         Maximum depth</param>
        /// <param name="useDispatcher">    True to use a dispatcher to call the moving action function in the UI thread (when function is called on a background thread</param>
        /// <param name="foundMoveAction">  Action to call if a move has been found</param>
        void InvokeMoveAction<TCookie>(TMove move, TCookie cookie, int permCount, long cacheHit, int maxDepth, bool useDispatcher, Action<TCookie,object>? foundMoveAction);
    }
}
