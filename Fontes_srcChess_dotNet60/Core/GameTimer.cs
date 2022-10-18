using System;

namespace SrcChess2.Core {
    /// <summary>
    /// Handle the timer for both player
    /// </summary>
    public class GameTimer {
        /// <summary>true if timer is tickling</summary>
        private bool      m_isEnabled;
        /// <summary>Time of last commit</summary>
        private DateTime  m_lastCommitTime;
        /// <summary>Commited time for the white</summary>
        private TimeSpan  m_whiteCommitedTime;
        /// <summary>Commited time for the black</summary>
        private TimeSpan  m_blackCommitedTime;
        /// <summary>Maximum time allowed for white player</summary>
        private TimeSpan? m_maxWhiteTime;
        /// <summary>Maximum time allowed for black player</summary>
        private TimeSpan? m_maxBlackTime;

        /// <summary>
        /// Class constructor
        /// </summary>
        public GameTimer() {
            m_isEnabled = false;
            Reset(ChessBoard.PlayerColor.White);
        }

        /// <summary>
        /// Commit the uncommited time to the current player
        /// </summary>
        private void Commit() {
            DateTime now;
            TimeSpan span;
            
            if (m_isEnabled) {
                now              = DateTime.Now;
                span             = now - m_lastCommitTime;
                m_lastCommitTime = now;
                if (PlayerColor == ChessBoard.PlayerColor.White) {
                    m_whiteCommitedTime += span;
                } else {
                    m_blackCommitedTime += span;
                }
            }
        }

        /// <summary>
        /// Enabled state of the timer
        /// </summary>
        public bool Enabled {
            get => m_isEnabled;
            set {
                if (value != m_isEnabled) {
                    if (value) {
                        m_lastCommitTime = DateTime.Now;
                    } else {
                        Commit();
                    }
                    m_isEnabled = value;
                }
            }
        }

        /// <summary>
        /// Reset the timer of both player
        /// </summary>
        /// <param name="playerColor"> Playing color</param>
        /// <param name="whiteTicks">  White Ticks</param>
        /// <param name="blackTicks">  Black Ticks</param>
        public void ResetTo(ChessBoard.PlayerColor playerColor, long whiteTicks, long blackTicks) {
            PlayerColor         = playerColor;
            m_whiteCommitedTime = new TimeSpan(whiteTicks);
            m_blackCommitedTime = new TimeSpan(blackTicks);
            m_lastCommitTime    = DateTime.Now;
        }

        /// <summary>
        /// Reset the timer of both player
        /// </summary>
        /// <param name="playerColor">  Playing color</param>
        public void Reset(ChessBoard.PlayerColor playerColor) => ResetTo(playerColor, 0, 0);

        /// <summary>
        /// Color of the player playing
        /// </summary>
        public ChessBoard.PlayerColor PlayerColor { get; set; }

        /// <summary>
        /// Time spent by the white player
        /// </summary>
        public TimeSpan WhitePlayTime {
            get {
                Commit();
                return m_whiteCommitedTime;
            }
        }

        /// <summary>
        /// Time spent by the white player
        /// </summary>
        public TimeSpan? MaxWhitePlayTime {
            get => m_maxWhiteTime;
            set => m_maxWhiteTime = value ?? throw new ArgumentNullException(nameof(MaxWhitePlayTime));
        }

        /// <summary>
        /// Time spent by the black player
        /// </summary>
        public TimeSpan? MaxBlackPlayTime {
            get => m_maxBlackTime;
            set => m_maxBlackTime = value ?? throw new ArgumentNullException(nameof(MaxBlackPlayTime));
        }

        /// <summary>
        /// Time spent by the black player
        /// </summary>
        public TimeSpan BlackPlayTime {
            get {
                Commit();
                return m_blackCommitedTime;
            }
        }

        /// <summary>
        /// Maximum time increment by move in second
        /// </summary>
        public int MoveIncInSec { get; set; }

        /// <summary>
        /// Time span to string
        /// </summary>
        public static string GetHumanElapse(TimeSpan timeSpan) {
            string retVal;
            int    index;
            
            retVal = timeSpan.ToString();
            index  = retVal.IndexOf(':');
            if (index != -1) {
                index = retVal.IndexOf('.', index);
                if (index != -1) {
                    retVal = retVal[..index];
                }
            }
            return retVal;
        }
    } // Class GameTimer
} // Namespace
