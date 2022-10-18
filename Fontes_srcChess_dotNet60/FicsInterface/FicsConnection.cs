using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SrcChess2.FicsInterface {
    /// <summary>
    /// Interface with FICS (Free Chess Interface Server)
    /// </summary>
    /// <remarks>
    /// Implements playing game with a human through FICS
    /// Implements chat?
    /// </remarks>
    public class FicsConnection : IDisposable {

        /// <summary>Current command executing</summary>
        private enum CmdExecuting {
            /// <summary>No command executing</summary>
            None,
            /// <summary>Before login to the server</summary>
            PreLogin,
            /// <summary>Login to the server</summary>
            Login,
            /// <summary>Getting a game move list</summary>
            MoveList,
            /// <summary>Getting the list of games</summary>
            GameList,
            /// <summary>Get the date from the server</summary>
            Date,
            /// <summary>List of variables values</summary>
            VariableList
        }

        #region Inner Class
        /// <summary>
        /// State of the listening automaton
        /// </summary>
        private class AutomatonState : IDisposable {
            /// <summary>List of active games</summary>
            private readonly Dictionary<int,FicsGameIntf> m_gameIntfDict;
            /// <summary>Command being executed</summary>
            public CmdExecuting                           CmdExecuting { get; private set; }
            /// <summary>Execution phase. 0 for awaiting first part</summary>
            public int                                    Phase { get; set; }
            /// <summary>true if listening to single move of at least one game</summary>
            public bool                                   SingleMoveListening { get; private set; }
            /// <summary>Time at which the command started to be processed</summary>
            public DateTime                               TimeStarted;
            /// <summary>Game for which the command is being executed if any</summary>
            public FicsGameIntf?                          CurrentGameIntf { get; private set; }
            /// <summary>Last command error if any</summary>
            public string?                                LastCmdError { get; private set; }
            /// <summary>List of games from the last 'games' command</summary>
            public List<FicsGame>                         GameList { get; private set; }
            /// <summary>Server date list</summary>
            public List<string>                           ServerDateList { get; private set; }
            /// <summary>List of variable settings</summary>
            public Dictionary<string,string>              VariableList { get; private set; }
            /// <summary>Signal use to indicate when a command finished executing</summary>
            public System.Threading.EventWaitHandle?      CmdSignal;
            /// <summary>User name</summary>
            public string?                                UserName { get; set; }
            /// <summary>Password</summary>
            public string?                                Password { get; set; }
            /// <summary>Text received in the login process</summary>
            public StringBuilder                          LoginText { get; set; }

            /// <summary>
            /// Ctor
            /// </summary>
            public AutomatonState() {
                CmdExecuting        = CmdExecuting.PreLogin;
                Phase               = 0;
                SingleMoveListening = false;
                TimeStarted         = new DateTime(0);
                CurrentGameIntf     = null;
                m_gameIntfDict      = new Dictionary<int, FicsGameIntf>(16);
                GameList            = new List<FicsGame>(512);
                ServerDateList      = new List<string>(6);
                VariableList        = new Dictionary<string, string>(128, StringComparer.OrdinalIgnoreCase);
                LastCmdError        = null;
                CmdSignal           = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset);
                LoginText           = new StringBuilder(2048);
            }

            /// <summary>
            /// Disposing the object
            /// </summary>
            public void Dispose() {
                if (CmdSignal != null) {
                    CmdSignal.Close();
                    CmdSignal = null;
                }
            }

            /// <summary>
            /// Add a game interface
            /// </summary>
            /// <param name="gameIntf"> Game interface</param>
            /// <returns>
            /// true if succeed, false if game is already defined
            /// </returns>
            public bool AddGameIntf(FicsGameIntf gameIntf) {
                bool retVal;
                int  gameId;

                lock (m_gameIntfDict) {
                    gameId = gameIntf.Game.GameId;
                    if (m_gameIntfDict.ContainsKey(gameId)) {
                        retVal = false;
                    } else {
                        m_gameIntfDict.Add(gameId, gameIntf);
                        retVal              = true;
                        SingleMoveListening = true;
                    }
                }
                return retVal;
            }

            /// <summary>
            /// Remove a game interface
            /// </summary>
            /// <param name="gameId">   Game interface id</param>
            /// <returns>
            /// true if succeed, false if game is not found
            /// </returns>
            private bool RemoveGameIntfInt(int gameId) {
                bool retVal;

                if (m_gameIntfDict.ContainsKey(gameId)) {
                    m_gameIntfDict.Remove(gameId);
                    SingleMoveListening = (m_gameIntfDict.Count != 0);
                    retVal              = true;
                } else {
                    retVal              = false;
                }
                return retVal;
            }

            /// <summary>
            /// Remove a game interface
            /// </summary>
            /// <param name="gameId">   Game id</param>
            /// <returns>
            /// true if succeed, false if game is not found
            /// </returns>
            public bool RemoveGameIntf(int gameId) {
                bool retVal;

                lock(m_gameIntfDict) {
                    retVal = RemoveGameIntfInt(gameId);
                }
                return retVal;
            }

            /// <summary>
            /// Terminate a game
            /// </summary>
            /// <param name="gameIntf">           Game interface</param>
            /// <param name="terminationCode">    Termination code</param>
            /// <param name="terminationComment"> Termination comment</param>
            /// <param name="errTxt">             Error if any</param>
            public void TerminateGame(FicsGameIntf gameIntf, TerminationCode terminationCode, string terminationComment, string? errTxt) {
                lock(m_gameIntfDict) {
                    if (RemoveGameIntfInt(gameIntf.Game.GameId)) {
                        gameIntf.SetTermination(terminationCode, terminationComment, errTxt);
                    }
                }
            }

            /// <summary>
            /// Find a game using its id
            /// </summary>
            /// <param name="gameId"> Game id</param>
            /// <returns>
            /// Game or null if not found
            /// </returns>
            public FicsGameIntf? FindGameIntf(int gameId) {
                FicsGameIntf? retVal;

                lock(m_gameIntfDict) {
                    m_gameIntfDict.TryGetValue(gameId, out retVal);
                }
                return retVal;
            }

            /// <summary>
            /// Find a game using its attached chess board control
            /// </summary>
            /// <param name="chessBoardControl"> Chess board control</param>
            /// <returns>
            /// Game or null if not found
            /// </returns>
            public FicsGameIntf? FindGameIntf(ChessBoardControl chessBoardControl) {
                FicsGameIntf? retVal = null;

                lock(m_gameIntfDict) {
                    retVal = m_gameIntfDict.Values.FirstOrDefault(x => x.ChessBoardCtl == chessBoardControl);
                }
                return retVal;
            }

            /// <summary>
            /// Gets the number of observed games
            /// </summary>
            /// <returns>
            /// Game count
            /// </returns>
            public int GameCount() {
                int retVal;

                lock(m_gameIntfDict) {
                    retVal = m_gameIntfDict.Count;
                }
                return retVal;
            }

            /// <summary>
            /// Set the current command or reset it to none
            /// </summary>
            /// <param name="cmd">      Command</param>
            /// <param name="gameIntf"> Associated game interface if any</param>
            public void SetCommand(CmdExecuting cmd, FicsGameIntf? gameIntf) {
                if (cmd == CmdExecuting.None) {
                    throw new ArgumentException("Use ResetCommand to set a command to none");
                }
                lock(this) {
                    if (CmdExecuting != CmdExecuting.None && CmdExecuting != CmdExecuting.PreLogin) {
                        throw new MethodAccessException("Cannot execute a command while another is executing");
                    }
                    CmdExecuting    = cmd;
                    TimeStarted     = DateTime.Now;
                    CurrentGameIntf = gameIntf;
                    Phase           = 0;
                    LastCmdError    = null;
                    CmdSignal!.Reset();
                }
            }

            /// <summary>
            /// Reset the current command to none
            /// </summary>
            /// <param name="errTxt"> Error message</param>
            public void ResetCommand(string? errTxt) {
                lock(this) {
                    CmdExecuting    = CmdExecuting.None;
                    TimeStarted     = new DateTime(0);
                    CurrentGameIntf = null;
                    LastCmdError    = errTxt;
                    Phase           = 0;
                    CmdSignal!.Set();
                }
            }

            /// <summary>
            /// Reset the current command to none
            /// </summary>
            public void ResetCommand() => ResetCommand(null);

        } // Class AutomatonState
        #endregion

        /// <summary>TELNET Connection with the server</summary>
        private TelnetConnection?          m_connection;
        /// <summary>State of the listening routine</summary>
        private AutomatonState?            m_state;
        /// <summary>Original parameter values</summary>
        private Dictionary<string,string>  m_variableDict;
        /// <summary>Set of setting which has been changed</summary>
        private readonly HashSet<string>   m_changedSettingsSet;
        /// <summary>Window where to send some error message</summary>
        private readonly ChessBoardControl m_mainCtl;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="mainCtl">    Main chess board control</param>
        /// <param name="hostname">   Host name</param>
        /// <param name="port">       Port number</param>
        /// <param name="debugTrace"> true to send trace to the debugging output</param>
        public FicsConnection(ChessBoardControl mainCtl, string hostname, int port, bool debugTrace) {
            m_state                       = new AutomatonState();
            m_variableDict                = new Dictionary<string, string>(512, StringComparer.OrdinalIgnoreCase);
            m_changedSettingsSet          = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            m_mainCtl                     = mainCtl;
            m_connection                  = new TelnetConnection(debugTrace);
            m_connection.NewTextReceived += Connection_NewTextReceived;
            m_connection.Connect(hostname, port);
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="ctlMain">              Main chess board control</param>
        /// <param name="connectionSetting">    Connection setting</param>
        public FicsConnection(ChessBoardControl ctlMain, FicsConnectionSetting connectionSetting) : this(ctlMain,
                                                                                                         connectionSetting.HostName,
                                                                                                         connectionSetting.HostPort,
                                                                                                         debugTrace: false) => ConnectionSetting = connectionSetting;

        /// <summary>
        /// Debugging trace
        /// </summary>
        public bool DebugTrace {
            get => (m_connection != null) && m_connection.DebugTrace;
            set {
                if (m_connection != null) {
                    m_connection.DebugTrace = value;
                }
            }
        }

        /// <summary>
        /// Disposing the object
        /// </summary>
        /// <param name="isDisposing"> true for dispose, false for finallizing</param>
        protected virtual void Dispose(bool isDisposing) {
            if (m_connection != null) {
                try {
                    RestoreOldSetting();
                    m_connection.SendLine("quit");
                } catch(Exception) {
                }
                m_connection.Dispose();
                m_connection = null;
            }
            if (m_state != null) {
                m_state.Dispose();
                m_state = null;
            }
        }

        /// <summary>
        /// Dispose the connection to the FICS server
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Connection setting
        /// </summary>
        public FicsConnectionSetting? ConnectionSetting { get;  }

        /// <summary>
        /// Gets the number of games which are observed
        /// </summary>
        /// <returns>
        /// Observed games count
        /// </returns>
        public int GetObservedGameCount() => m_state?.GameCount() ?? 0;

        /// <summary>
        /// Original settings
        /// </summary>
        public Dictionary<string,string> OriginalSettings => m_variableDict;

        /// <summary>
        /// Change a server setting
        /// </summary>
        /// <param name="settingName"> Name of the setting</param>
        /// <param name="value">       Value of the setting</param>
        /// <param name="addToSet">    true to add to the list of change setting</param>
        private void SetSetting(string settingName, string value, bool addToSet) {
            if (!addToSet || m_variableDict.ContainsKey(settingName)) {
                m_connection!.SendLine($"set {settingName} {value}");
                if (addToSet                                    &&
                    !m_changedSettingsSet.Contains(settingName) &&
                    m_variableDict[settingName] != value) {
                    m_changedSettingsSet.Add(settingName);
                }
#if DEBUG
            } else {
                throw new ArgumentException("Oops.. setting not found");
#endif
            }
        }

        /// <summary>
        /// Change a server setting (iVariable)
        /// </summary>
        /// <param name="settingName"> Name of the setting</param>
        /// <param name="value">       Value of the setting</param>
        private void SetISetting(string settingName, string value)
            => m_connection!.SendLine($"iset {settingName} {value}");

        /// <summary>
        /// Restore the old settings
        /// </summary>
        public void RestoreOldSetting() {
            string  oldValue;

            if (m_connection == null) {
                throw new InvalidOperationException("Connection not initialized");
            }
            m_connection.FlushInput();
            foreach (string strSetting in m_changedSettingsSet) {
                oldValue = m_variableDict[strSetting];
                SetSetting(strSetting, oldValue, addToSet: false);
            }
            m_connection.FlushInput();
        }

        /// <summary>
        /// Set a quiet mode
        /// </summary>
        private void SetQuietModeInt() {
            m_connection!.FlushInput();
            SetISetting("defprompt", "1");      // Force using standard prompt
            //SetISetting("gameinfo", "1");     // Add game info when starting the observe
            SetISetting("ms", "1");             // Player's time contains millisecond
            SetISetting("startpos", "1");       // Add a board setting at the beginning of a move list if not a standard starting position
            //SetISetting("pendinfo", "1");     // Add information on pending offer 
            SetSetting("interface", "SrcChess", addToSet: false);
            SetSetting("ptime", "0", addToSet: true);
            SetISetting("lock", "1");           // No more internal setting before logging out
            SetSetting("shout", "0", addToSet: true);
            SetSetting("cshout", "0", addToSet: true);
            SetSetting("kibitz", "0", addToSet: true);
            SetSetting("pin", "0", addToSet: true);
            SetSetting("tell", "0", addToSet: true);
            SetSetting("ctell", "0", addToSet: true);
            SetSetting("gin", "0", addToSet: true);
            SetSetting("seek", "0", addToSet: true);
            SetSetting("showownseek", "0", addToSet: true);
            m_connection.FlushInput();
        }

        /// <summary>
        /// Set a quiet mode
        /// </summary>
        public void SetQuietMode() {
            if (m_connection == null) {
                throw new InvalidOperationException("Connection not initialized");
            }
            m_connection.FlushInput();
            SetQuietModeInt();
            SetSetting("Style", "12", addToSet: true);
            m_connection.FlushInput();
        }

        /// <summary>
        /// Login to the session
        /// </summary>
        /// <param name="password"> User password</param>
        /// <param name="timeOut">  Timeout in seconds</param>
        /// <param name="errTxt">   Returned error if any</param>
        /// <returns>
        /// true if succeed, false if failed (bad password)
        /// </returns>
        public bool Login(string      password,
                          int         timeOut,
                          out string? errTxt) {
            bool retVal;
            
            if (m_connection == null) {
                throw new InvalidOperationException("Connection not created");
            }
            if (ConnectionSetting == null) {
                throw new InvalidOperationException("Connection setting not created");
            }
            errTxt            = null;
            m_state!.UserName = ConnectionSetting.UserName;
            m_state!.Password = password;
            m_state!.SetCommand(CmdExecuting.Login, gameIntf: null);
            if (m_state.CmdSignal!.WaitOne(timeOut * 1000)) {
                if (m_state.LastCmdError == null) {
                    m_connection.NewLineReceived += Connection_NewLineReceived;
                    if (GetVariableList(timeOut * 1000) > 20) { // At least 20 variables are defined
                        SetQuietMode();
                        retVal = true;
                    } else {
                        errTxt = "Unable to access user variables";
                        retVal = false;
                    }
                } else {
                    errTxt = m_state.LastCmdError;
                    m_state.ResetCommand();
                    retVal = false;
                }
                if (!retVal) {
                    errTxt = $"Error login to the chess server - {errTxt}";
                }
            } else {
                retVal           = false;
                errTxt           = "Login timeout";
                m_state.UserName = null;
                m_state.Password = null;
            }
            return retVal;
        }

        /// <summary>
        /// Start observing a game using a predefined game interface
        /// </summary>
        /// <param name="gameIntf"> Game to observe</param>
        /// <param name="timeOut">  Command timeout in second</param>
        /// <param name="errTxt">   Error if any</param>
        /// <returns>
        /// true if succeed, false if game is already defined
        /// </returns>
        public bool ObserveGame(FicsGameIntf gameIntf, int timeOut, out string? errTxt) {
            bool retVal;
            int  gameId;

            if (m_connection == null) {
                throw new InvalidOperationException("Connection not created");
            }
            if (gameIntf.Game.IsPrivate) {
                throw new ArgumentException("Cannot listen to private game");
            }
            retVal = m_state!.AddGameIntf(gameIntf);
            if (retVal) {
                gameId = gameIntf.Game.GameId;
                m_state.SetCommand(CmdExecuting.MoveList, gameIntf);
                m_connection.SendLine($"observe {gameId.ToString(CultureInfo.InvariantCulture)}");
                m_connection.SendLine("moves " + gameId.ToString(CultureInfo.InvariantCulture));
                if (m_state!.CmdSignal!.WaitOne(timeOut * 1000)) {
                    errTxt = m_state.LastCmdError;
                    retVal = errTxt == null;
                } else {
                    m_state.ResetCommand();
                    errTxt = "Timeout";
                }
            } else {
                errTxt = "Already defined";
            }
            return retVal;
        }

        /// <summary>
        /// Gets the timeout action
        /// </summary>
        /// <returns>
        /// Timeout action
        /// </returns>
        public Action<FicsGameIntf> GetTimeOutAction() => (x) => { m_state!.TerminateGame(x, TerminationCode.TerminatedWithErr, terminationComment: "", "Move timeout"); };

        /// <summary>
        /// Start to observe a game
        /// </summary>
        /// <param name="game">               Game to observe</param>
        /// <param name="chessBoardControl">  Chess board control to associate with the game</param>
        /// <param name="timeOut">            Command timeout in second</param>
        /// <param name="moveTimeOut">        Command timeout in second</param>
        /// <param name="gameFinishedAction"> Action to call when game is finished or null if none</param>
        /// <param name="errTxt">             Error if any</param>
        /// <returns>
        /// true if succeed, false if game is already defined
        /// </returns>
        public bool ObserveGame(FicsGame game, ChessBoardControl chessBoardControl, int timeOut, int? moveTimeOut, Action<FicsGameIntf,TerminationCode,string> gameFinishedAction, out string? errTxt) {
            bool                  retVal;
            FicsGameIntf          gameIntf;
            Action<FicsGameIntf>? moveTimeOutAction;

            moveTimeOutAction = moveTimeOut == 0 ? null : GetTimeOutAction();
            gameIntf          = new FicsGameIntf(game, chessBoardControl, moveTimeOut, moveTimeOutAction, gameFinishedAction);
#if DEBUG
            timeOut           = 3600;
#endif
            retVal            = ObserveGame(gameIntf, timeOut, out errTxt);
            return retVal;
        }

        /// <summary>
        /// Terminate the game observation for the specified chess board control
        /// </summary>
        /// <param name="chessBoardControl"> Chess board control</param>
        /// <returns>
        /// true if found, false if not
        /// </returns>
        public bool TerminateObservation(ChessBoardControl chessBoardControl) {
            bool          retVal;
            FicsGameIntf? gameIntf;

            if (m_connection == null) {
                throw new InvalidOperationException("Connection not created");
            }
            gameIntf = m_state!.FindGameIntf(chessBoardControl);
            if (gameIntf == null) {
                retVal = false;
            } else {
                retVal = true;
                m_state.TerminateGame(gameIntf, TerminationCode.TerminatedWithErr, terminationComment: "", "Stop by user");
            }
            return retVal;
        }

        /// <summary>
        /// Find the list of games
        /// </summary>
        /// <param name="refresh"> True to refresh the list</param>
        /// <param name="timeOut"> Command timeout in second</param>
        /// <returns>
        /// List of game
        /// </returns>
        public List<FicsGame> GetGameList(bool refresh, int timeOut) {
            List<FicsGame> retVal;

            if (m_connection == null) {
                throw new InvalidOperationException("Connection not created");
            }
#if DEBUG
            timeOut = 3600;
#endif
            retVal = m_state!.GameList;
            if (refresh) {
                m_state.GameList.Clear();
                m_state.SetCommand(CmdExecuting.GameList, gameIntf: null);
                m_connection.SendLine("games");
                if (!m_state.CmdSignal!.WaitOne(timeOut * 1000)) {
                    m_state.ResetCommand();
                }
            }
            return retVal;
        }

        /// <summary>
        /// Gets the variable setting
        /// </summary>
        /// <param name="timeOut"> Command timeout in second</param>
        /// <returns>
        /// Setting count
        /// </returns>
        public int GetVariableList(int timeOut) {
            if (m_connection == null) {
                throw new InvalidOperationException("Connection not created");
            }
            m_variableDict.Clear();
            m_state!.SetCommand(CmdExecuting.VariableList, gameIntf: null);
            m_connection.SendLine("variables");
            if (m_state.CmdSignal!.WaitOne(timeOut * 1000)) {
                m_variableDict = new Dictionary<string, string>(m_state.VariableList, StringComparer.OrdinalIgnoreCase);
            } else {
                m_state.ResetCommand();
            }
            return m_variableDict.Count;
        }

        /// <summary>
        /// Gets the date from the server
        /// </summary>
        /// <param name="timeOut">  Time out in seconds</param>
        /// <returns>
        /// List of date or null if timeout
        /// </returns>
        public List<string>? GetServerDate(int timeOut) {
            List<string>? retVal;

            if (m_connection == null) {
                throw new InvalidOperationException("Connection not created");
            }
            m_state!.SetCommand(CmdExecuting.Date, gameIntf: null);
            m_connection.SendLine("date");
            if (m_state.CmdSignal!.WaitOne(timeOut * 1000)) {
                retVal = m_state.ServerDateList;
            } else {
                retVal = null;
                m_state.ResetCommand();
            }
            return retVal;
        }

        /// <summary>
        /// Process the line if it's a MoveList header
        /// </summary>
        /// <param name="line"> Line</param>
        /// <returns>
        /// true if it's a move list header, false if not
        /// </returns>
        private void ProcessMoveListHeader(string line) {
            string moveListStartingWith;

            moveListStartingWith = $"Movelist for game {m_state!.CurrentGameIntf!.Game.GameId.ToString(CultureInfo.InvariantCulture)}";
            if (line.StartsWith(moveListStartingWith)) {
                m_state.Phase++;
            }
        }

        /// <summary>
        /// Skip the move list header
        /// </summary>
        /// <param name="line"> Line</param>
        /// <returns>
        /// true if found the last line of the header, false if not
        /// </returns>
        private void SkipMoveListHeader(string line) {
            if (line.StartsWith("---- ")) {
                m_state!.Phase++;
            }
        }

        /// <summary>
        /// Process a move list line
        /// </summary>
        /// <param name="line"> Line</param>
        private void ProcessMoveListLine(string line) {
            bool?        result;
            FicsGameIntf gameIntf;

            gameIntf = m_state!.CurrentGameIntf!;
            result   = gameIntf!.ParseInitialMove(line, out string? errTxt);
            if (result == null) {
                m_state.ResetCommand();
                if (!gameIntf.CreateInitialBoard(out errTxt)) {
                    m_state.TerminateGame(gameIntf, TerminationCode.TerminatedWithErr, terminationComment: "", errTxt);
                }
            } else if (result == false) {
                m_state.TerminateGame(gameIntf, TerminationCode.Terminated, "Error", null);
                m_state.ResetCommand(errTxt);
            }
        }

        /// <summary>
        /// Process a move line
        /// </summary>
        /// <param name="line"> Line to analyze</param>
        /// <returns>
        /// true if a move line has been found, false if not
        /// </returns>
        private bool ProcessSingleMove(string line) {
            bool             retVal;
            Style12MoveLine? moveLine;
            FicsGameIntf?    gameIntf;

            moveLine = Style12MoveLine.ParseLine(line, out int gameId, out TerminationCode terminationCode, out string terminationComment, out string? errTxt);
            if (errTxt != null) {
                m_mainCtl.Dispatcher.Invoke((Action) (() => { m_mainCtl.ShowError($"Error decoding a move - {errTxt}\r\n({line})"); }));
                retVal = true;
            } else if (moveLine != null || terminationCode != TerminationCode.None) {
                retVal   = true;
                gameIntf = m_state!.FindGameIntf(gameId);
                if (gameIntf != null) {
                    if (terminationCode == TerminationCode.None) {
                        if (!gameIntf.PlayMove(moveLine!)) {
                            m_state.TerminateGame(gameIntf, TerminationCode.TerminatedWithErr, terminationComment: "", errTxt);
                        }
                    } else {
                        m_state.TerminateGame(gameIntf, terminationCode, terminationComment, errTxt: null);
                    }
                }
            } else {
                retVal = false;
            }
            return retVal;
        }

        /// <summary>
        /// Process first game list line
        /// </summary>
        /// <param name="line"> Received line</param>
        private void ProcessFirstGameListLine(string line) {
            FicsGame? game;

            if (!string.IsNullOrWhiteSpace(line)) {
                if (FicsGame.IsLastGameLine(line)) {
                    m_state!.ResetCommand();
                } else {
                    game = FicsGame.ParseGameLine(line, out bool isSupported);
                    if (game != null) {
                        if (isSupported) {
                            m_state!.GameList.Add(game);
                        }
                        m_state!.Phase++;
                    }
                }
            }
        }

        /// <summary>
        /// Process first game list line
        /// </summary>
        /// <param name="line">  Received line</param>
        private void ProcessGameListLine(string line) {
            FicsGame? game;

            game = FicsGame.ParseGameLine(line, out bool isSupported);
            if (game != null) {
                if (isSupported) {
                    m_state!.GameList.Add(game);
                }
            } else {
                m_state!.ResetCommand();
            }
        }

        /// <summary>
        /// Identifies the variable part
        /// </summary>
        /// <param name="line"> Line</param>
        private void ProcessVariableListHeader(string line) {
            const string startingWith = "Variable settings of ";

            if (line.Contains(startingWith)) {
                m_state!.VariableList.Clear();
                m_state.Phase++;
            }
        }

        /// <summary>
        /// Process variable list lines
        /// </summary>
        /// <param name="line"> Line</param>
        private void GettingVariableListValue(string line) {
            string[] vars;
            string[] settings;
            string   settingName;
            string   settingValue;

            line = line.Trim();
            if (!string.IsNullOrEmpty(line)) {
                if (line.StartsWith("Formula:")) {
                    m_state!.ResetCommand();
                } else if (line.Contains('=')) {
                    vars = line.Split(' ');
                    foreach (string strVar in vars) {
                        if (!string.IsNullOrEmpty(strVar) && strVar.Contains('=')) {
                            settings = strVar.Split('=');
                            if (settings.Length == 2) {
                                settingName  = settings[0];
                                settingValue = settings[1];
                                if (!m_state!.VariableList.ContainsKey(settingName)) {
                                    m_state.VariableList.Add(settingName, settingValue);
                                }
                            }
                        }
                    }
                } else { // Guest doesn't receive the Formula setting
                    m_state!.ResetCommand();
                }
            }
        }

        /// <summary>
        /// Process an input line. Use to process command
        /// </summary>
        /// <param name="line"> Received line</param>
        private void ProcessLine(string line) {
            int  phase;
            bool singleMoveListening;

            singleMoveListening = m_state!.SingleMoveListening;
            phase               = m_state.Phase;
            if (!singleMoveListening || phase != 0 || !ProcessSingleMove(line)) {
                switch(m_state.CmdExecuting) {
                case CmdExecuting.None:
                    break;
                case CmdExecuting.MoveList:
                    switch(phase) {
                    case 0:
                        ProcessMoveListHeader(line);
                        break;
                    case 1:
                        SkipMoveListHeader(line);
                        break;
                    case 2:
                        ProcessMoveListLine(line);
                        break;
                    default:
                        throw new NotImplementedException();
                    }
                    break;
                case CmdExecuting.GameList:
                    switch(phase) {
                    case 0:
                        ProcessFirstGameListLine(line);
                        break;
                    case 1:
                        ProcessGameListLine(line);
                        break;
                    default:
                        throw new NotImplementedException();
                    }
                    break;
                case CmdExecuting.VariableList:
                    switch(phase) {
                    case 0:
                        ProcessVariableListHeader(line);
                        break;
                    case 1:
                        GettingVariableListValue(line);
                        break;
                    default:
                        throw new NotImplementedException();
                    }
                    break;
                case CmdExecuting.Date:
                    switch(phase) {
                    case 0:
                        if (line.StartsWith("Local time ") || line.StartsWith("fics% Local time")) {
                            m_state.ServerDateList.Clear();
                            m_state.ServerDateList.Add(line);
                            m_state.Phase++;
                        }
                        break;
                    case 1:
                        if (line.StartsWith("Server time ")) {
                            m_state.ServerDateList.Add(line);
                        } else if (line.StartsWith("GMT ")) {
                            m_state.ServerDateList.Add(line);
                            m_state.ResetCommand();
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                    }
                    break;
                default:
                    throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// Process input text. Use to process Login
        /// </summary>
        private void ProcessLoginText() {
            string  recText;

            if (m_state!.CmdExecuting == CmdExecuting.Login) {
                recText = m_state.LoginText.ToString();
                switch(m_state.Phase) {
                case 0: // Set the user name
                    if (recText.EndsWith("login: ")) {
                        m_connection!.SendLine(m_state.UserName!);
                        m_state.Phase++;
                        m_state.LoginText.Clear();
                    }
                    break;
                case 1: // Set the password
                    if (string.Compare(m_state.UserName, "Guest", true) == 0) {
                        m_connection!.SendLine("");  // Accept guest
                        m_state.ResetCommand();
                    } else {
                        if (recText.Contains("Sorry, names can only")) {
                            m_state.ResetCommand("Invalid character in login name");
                        } else if (recText.Contains("is not a registred name.")) {
                            m_state.ResetCommand("Unknown login name");
                        } else if (!recText.EndsWith("password: ")) {
                            m_state.ResetCommand("Unknown error at login");
                        } else {
                            m_connection!.SendLine(m_state.Password ?? "");
                            m_state.Phase++;
                        }
                    }
                    m_state.Password = null;
                    m_state.LoginText.Clear();
                    break;
                case 2:
                    if (recText.Contains("**** Starting FICS session as")) {
                        m_state.ResetCommand();
                    } else if (recText.Contains("**** Invalid password! ****")) {
                        m_state.ResetCommand("Invalid password");
                    } else {
                        m_state.ResetCommand("Unknown error with password");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Called when a new line has been received
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void Connection_NewLineReceived(object? sender, EventArgs e) {
            string? line;

            do {
                line = m_connection!.GetNextReadLine();
                if (!string.IsNullOrEmpty(line)) {
                    ProcessLine(line);
                }
            } while (!string.IsNullOrEmpty(line));
        }

        /// <summary>
        /// Called when new text has been received
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void Connection_NewTextReceived(object? sender, EventArgs e) {
            string  text;

            switch(m_state!.CmdExecuting) {
            case CmdExecuting.PreLogin:
            case CmdExecuting.Login:
                do {
                    text = m_connection!.GetAllReadText();
                    if (!string.IsNullOrEmpty(text)) {
                        m_state.LoginText.Append(text);
                    }
                } while (!string.IsNullOrEmpty(text));
                ProcessLoginText();
                break;
            }
        }
    }
}
