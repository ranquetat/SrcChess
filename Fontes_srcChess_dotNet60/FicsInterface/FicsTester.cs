using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Globalization;

namespace SrcChess2.FicsInterface {
        
    /// <summary>
    /// Test FICS interface
    /// </summary>
    public class FicsTester {

        /// <summary>
        /// Testing version of the game interface
        /// </summary>
        private class GameIntfTest : FicsGameIntf {
            /// <summary>Stream use to log error and message</summary>
            private readonly System.IO.StreamWriter m_streamLog;

            /// <summary>
            /// Ctor
            /// </summary>
            /// <param name="game">              Game</param>
            /// <param name="chessBoardControl"> Chess board control if any</param>
            /// <param name="streamLog">         Stream where to send the log information</param>
            /// <param name="eventWaitHandle">   Use to inform background tester the game is terminated</param>
            /// <param name="moveTimeOut">       Move timeout in second</param>
            /// <param name="moveTimeOutAction"> Action to call if move timeout</param>
            public GameIntfTest(FicsGame               game,
                                ChessBoardControl      chessBoardControl,
                                System.IO.StreamWriter streamLog,
                                EventWaitHandle        eventWaitHandle,
                                int                    moveTimeOut,
                                Action<FicsGameIntf>   moveTimeOutAction) : base(game, chessBoardControl, moveTimeOut, moveTimeOutAction, gameFinishedAction: null) {
                m_streamLog     = streamLog;
                EventWaitHandle = eventWaitHandle;
            }

            /// <summary>
            /// Use to inform background runner the game is terminating
            /// </summary>
            public EventWaitHandle EventWaitHandle { get; private set; }

            /// <summary>
            /// Send an error message to the log file
            /// </summary>
            /// <param name="errTxt"> Error message</param>
            public override void ShowError(string errTxt) {
                lock(m_streamLog) {
                    m_streamLog.WriteLine($"{DateTime.Now:HH:mm:ss}: *** Error -  GameId: {Game.GameId.ToString(CultureInfo.InvariantCulture)} {errTxt}");
                    m_streamLog.Flush();
                }
            }

            /// <summary>
            /// Send a message to the log file
            /// </summary>
            /// <param name="msg"> Message</param>
            protected override void ShowMessage(string msg) {
                lock(m_streamLog) {
                    m_streamLog.WriteLine($"{DateTime.Now:HH:mm:ss}: *** Info -  GameId: {Game.GameId.ToString(CultureInfo.InvariantCulture)} {msg}");
                    m_streamLog.Flush();
                }
            }

            /// <summary>
            /// Set the termination code and the error if any
            /// </summary>
            /// <param name="terminationCode">    Termination code</param>
            /// <param name="terminationComment"> Termination comment</param>
            /// <param name="errTxt">             Error if any</param>
            public override void SetTermination(TerminationCode terminationCode, string? terminationComment, string? errTxt) {
                base.SetTermination(terminationCode, terminationComment, errTxt);
                if (EventWaitHandle != null) {
                    EventWaitHandle.Set();
                }
            }
        }

        /// <summary>
        /// Write a message to a log and to the debugger output
        /// </summary>
        /// <param name="writer"> Writer</param>
        /// <param name="msg">    Message</param>
        private static void LogWrite(System.IO.StreamWriter writer, string msg) {
            writer.WriteLine(msg);
            System.Diagnostics.Debug.WriteLine(msg);
        }

        /// <summary>
        /// Start a background game
        /// </summary>
        /// <param name="conn">          Connection to FICS server</param>
        /// <param name="chessBoardCtl"> Chess board control</param>
        private static void BackgroundGame(FicsConnection conn, ChessBoardControl chessBoardCtl) {
            List<FicsGame>  gameList;
            FicsGame?       game;
            EventWaitHandle eventWaitHandle;
            GameIntfTest    gameIntf;
            bool            isGameFound;
            int             lastGameId = -1;

            eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            isGameFound     = false;
            System.IO.FileStream stream = System.IO.File.Open("c:\\tmp\\chesslog.txt", System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write, System.IO.FileShare.Read);
            using (stream) {
                stream.Seek(0, System.IO.SeekOrigin.End);
                System.IO.StreamWriter writer = new(stream, Encoding.UTF8);
                using (writer) {
                    writer.WriteLine();
                    writer.WriteLine($"Starting new session at {DateTime.Now:HH:mm:ss}");
                    writer.WriteLine("-----------------------------------------");
                    writer.WriteLine();
                    do {
                        gameList = conn.GetGameList(true, 10);
                        game     = gameList.FirstOrDefault(x => (x.GameId   !=  lastGameId &&
                                                                 !x.IsPrivate              &&
                                                                 x.GameType == FicsGame.FicsGameType.Lightning || x.GameType == FicsGame.FicsGameType.Blitz) &&
                                                                 x.PlayerTimeInMin < 3 && x.IncTimeInSec < 5);
                        if (game != null) {
                            isGameFound = true;
                            lastGameId  = game.GameId;
                            LogWrite(writer, $"{DateTime.Now:HH:mm:ss}: Found game: {game.GameId.ToString(CultureInfo.InvariantCulture)}");
                            writer.Flush();
                            gameIntf = new GameIntfTest(game, chessBoardCtl, writer, eventWaitHandle,moveTimeOut: 30, conn.GetTimeOutAction());
                            eventWaitHandle.Reset();
                            if (conn.ObserveGame(gameIntf, 10, out string? errTxt)) {
                                eventWaitHandle.WaitOne();
                                lock(writer) {
                                    writer.WriteLine("PGN Game");
                                    writer.WriteLine("----------------------");
                                    writer.WriteLine(gameIntf.GetPGNGame());
                                    writer.WriteLine("----------------------");
                                }
                                if (gameIntf.TerminationCode == TerminationCode.TerminatedWithErr) {
                                    lock(writer) {
                                        LogWrite(writer, $"{DateTime.Now:HH:mm:ss}: Game {gameIntf.Game.GameId.ToString(CultureInfo.InvariantCulture)} terminated with error - {gameIntf.TerminationError}");
                                    }
                                } else {
                                    lock(writer) {
                                       LogWrite(writer, $"{DateTime.Now:HH:mm:ss}: Game finished - {gameIntf.TerminationCode}");
                                    }
                                }
                                lock(writer) {
                                    writer.Flush();
                                }
                                isGameFound = false;
                            } else {
                                lock(writer) {
                                    LogWrite(writer, $"Games failed to start - {errTxt ?? "???"}");
                                    writer.Flush();
                                }
                                Thread.Sleep(5000);
                            }
                        } else {
                            lock(writer) {
                                LogWrite(writer, "No games found - trying again in 5 sec.");
                                writer.Flush();
                            }
                            Thread.Sleep(5000);
                        }
                    } while (!isGameFound);
                    writer.WriteLine($"Session end at {DateTime.Now:HH:mm:ss}");
                }
            }
        }

        /// <summary>
        /// Start a background game
        /// </summary>
        /// <param name="conn">          Connection with FICS server</param>
        /// <param name="chessBoardCtl"> Chess board control use to display the games</param>
        public static void StartBackgroundGame(FicsConnection conn, ChessBoardControl chessBoardCtl) {
            void action() { BackgroundGame(conn, chessBoardCtl); }

            Task.Factory.StartNew(action);
        }
    }
}
