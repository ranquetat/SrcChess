using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SrcChess2.FicsInterface {
    /// <summary>
    /// Search Criteria
    /// </summary>
    public class SearchCriteria {
        /// <summary>Game played by this player or any player if empty</summary>
        public  string? PlayerName;
        /// <summary>Allow blitz game</summary>
        public  bool    BlitzGame;
        /// <summary>Allow lightning game</summary>
        public  bool    LightningGame;
        /// <summary>Allow untimed game</summary>
        public  bool    UntimedGame;
        /// <summary>Allow standard game</summary>
        public  bool    StandardGame;
        /// <summary>Allow only rated game or any game if false</summary>
        public  bool    IsRated;
        /// <summary>Minimum player rating or no minimum if null</summary>
        public  int?    MinRating;
        /// <summary>Minimum playing time per player or null if no minimum</summary>
        public  int?    MinTimePerPlayer;
        /// <summary>Maximum playing time per player or null for no maximum</summary>
        public  int?    MaxTimePerPlayer;
        /// <summary>Minimum increment time per move or null for no minimum</summary>
        public  int?    MinIncTimePerMove;
        /// <summary>Maximum increment time per move or null for no maximum</summary>
        public  int?    MaxIncTimePerMove;
        /// <summary>Maximum move count</summary>
        public  int     MaxMoveDone;
        /// <summary>Number of second between move before a timeout occurs. null for infinite</summary>
        public  int?    MoveTimeOut;

        /// <summary>
        /// Ctor
        /// </summary>
        public SearchCriteria() {}

        /// <summary>
        /// Copy ctor
        /// </summary>
        /// <param name="searchCriteria"></param>
        public SearchCriteria(SearchCriteria searchCriteria) {
            PlayerName        = searchCriteria.PlayerName;
            BlitzGame         = searchCriteria.BlitzGame;
            LightningGame     = searchCriteria.LightningGame;
            UntimedGame       = searchCriteria.UntimedGame;
            StandardGame      = searchCriteria.StandardGame;
            IsRated           = searchCriteria.IsRated;
            MinRating         = searchCriteria.MinRating;
            MinTimePerPlayer  = searchCriteria.MinTimePerPlayer;
            MaxTimePerPlayer  = searchCriteria.MaxTimePerPlayer;
            MinIncTimePerMove = searchCriteria.MinIncTimePerMove;
            MaxIncTimePerMove = searchCriteria.MaxIncTimePerMove;
            MaxMoveDone       = searchCriteria.MaxMoveDone;
            MoveTimeOut       = searchCriteria.MoveTimeOut;
        }

        /// <summary>
        /// Creates a default search criteria
        /// </summary>
        /// <returns>
        /// Default search criteria
        /// </returns>
        public static SearchCriteria CreateDefault()
            => new SearchCriteria {
                PlayerName        = "",
                BlitzGame         = true,
                LightningGame     = true,
                UntimedGame       = false,
                StandardGame      = false,
                IsRated           = true,
                MinRating         = 1000,
                MinTimePerPlayer  = 0,
                MaxTimePerPlayer  = 3,
                MinIncTimePerMove = 0,
                MaxIncTimePerMove = 4,
                MaxMoveDone       = 20,
                MoveTimeOut       = 30
            };

        /// <summary>
        /// Returns if input is valid
        /// </summary>
        /// <returns>
        /// true, false
        /// </returns>
        public bool IsValid() {
            bool retVal;

            if (!BlitzGame     && 
                !LightningGame &&
                !UntimedGame   &&
                !StandardGame) {
                retVal = false;
            } else if (IsRated && !MinRating.HasValue) {
                retVal = false;
            } else if (MinRating    < 0 ||
                        MinTimePerPlayer  < 0 ||
                        MaxTimePerPlayer  < 0 ||
                        MinIncTimePerMove < 0 ||
                        MaxIncTimePerMove < 0 ||
                        MaxMoveDone       < 0) {
                retVal = false;
            } else if (MinTimePerPlayer  > MaxTimePerPlayer ||
                       MinIncTimePerMove > MaxIncTimePerMove) {
                retVal = false;
            } else {
                retVal = true;
            }
            return retVal;
        }

        /// <summary>
        /// Convert a string to a nullable int value
        /// </summary>
        /// <param name="text"> Text value</param>
        /// <returns>
        /// int value or null if error
        /// </returns>
        public static int? CnvToNullableIntValue(string text) {
            int? retVal;

            text = text.Trim();
            if (string.IsNullOrEmpty(text)) {
                retVal = null;
            } else {
                if (int.TryParse(text, out int val)) {
                    retVal = val;
                } else {
                    retVal = -1;
                }
            }
            return retVal;
        }

        /// <summary>
        /// true if game meets the criteria
        /// </summary>
        /// <param name="game"> Game</param>
        /// <returns>
        /// true/false
        /// </returns>
        public bool IsGameMeetCriteria(FicsGame game) {
            bool retVal;

            retVal = (string.IsNullOrEmpty(PlayerName)                            || 
                      string.Compare(PlayerName, game.WhitePlayerName, true) == 0 ||
                      string.Compare(PlayerName, game.BlackPlayerName, true) == 0);
            if (retVal) {
                retVal = game.GameType switch {
                    FicsGame.FicsGameType.Blitz     => BlitzGame,
                    FicsGame.FicsGameType.Lightning => LightningGame,
                    FicsGame.FicsGameType.Untimed   => UntimedGame,
                    FicsGame.FicsGameType.Standard  => StandardGame,
                    _                               => false,
                };
            }
            if (retVal && IsRated) {
                retVal = game.WhiteRating >= MinRating && game.BlackRating >= MinRating;
            }
            if (retVal && MinTimePerPlayer.HasValue) {
                retVal = game.PlayerTimeInMin >= MinTimePerPlayer;
            }
            if (retVal && MaxTimePerPlayer.HasValue) {
                retVal = game.PlayerTimeInMin <= MaxTimePerPlayer;
            }
            if (retVal && MinIncTimePerMove.HasValue) {
                retVal = game.IncTimeInSec >= MinIncTimePerMove;
            }
            if (retVal && MaxIncTimePerMove.HasValue) {
                retVal = game.IncTimeInSec <= MaxIncTimePerMove;
            }
            if (retVal) {
                retVal = game.NextMoveCount <= MaxMoveDone;
            }
            return retVal;
        }
    } // Class SearchCriteria
} // Namespace
