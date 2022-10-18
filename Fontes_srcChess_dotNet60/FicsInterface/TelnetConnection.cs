using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SrcChess2.FicsInterface {

    /// <summary>
    /// Telnet Verb
    /// </summary>
    public enum Verbs {
        /// <summary>Ask if option is available</summary>
        WILL = 251,
        /// <summary>Refuse the option</summary>
        WONT = 252,
        /// <summary>Please do it</summary>
        DO   = 253,
        /// <summary>Please don't</summary>
        DONT = 254,
        /// <summary>IAC command</summary>
        IAC  = 255
    }

    /// <summary>
    /// TELNET options
    /// </summary>
    public enum Options {
        /// <summary>SGA option</summary>
        SGA = 3
    }

    /// <summary>
    /// minimalistic telnet implementation
    /// conceived by Tom Janssens on 2007/06/06  for codeproject
    /// </summary>
    public class TelnetConnection : IDisposable {
        /// <summary>Called when a new text has been received</summary>
        public event EventHandler?      NewTextReceived;
        /// <summary>Called when a new line has been received</summary>
        public event EventHandler?      NewLineReceived;
        /// <summary>TCP/IP socket</summary>
        private TcpClient?              m_tcpSocket;
        /// <summary>Network stream</summary>
        private NetworkStream?          m_stream;
        /// <summary>Receiving buffer</summary>
        private byte[]?                 m_buf;
        /// <summary>Up to one unprocessed byte</summary>
        private byte?                   m_lastByte = null;
        /// <summary>String builder containing the received character</summary>
        private readonly StringBuilder? m_inputStrb;
        /// <summary>true if object is listening</summary>
        private bool                    m_isListening;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="isDebugTrace"> true to send send text and received text to the debugger output</param>
        public TelnetConnection(bool isDebugTrace) {
            m_tcpSocket = null;
            m_inputStrb = null;
            m_inputStrb = new StringBuilder(65536);
            DebugTrace  = isDebugTrace;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public TelnetConnection() : this(isDebugTrace: false) {}

        /// <summary>
        /// Disposing the object
        /// </summary>
        /// <param name="isDisposing"> true for dispose, false for finallizing</param>
        protected virtual void Dispose(bool isDisposing) {
            m_isListening = false;
            if (m_stream != null) {
                m_stream.Dispose();
                m_stream = null;
            }
            if (m_tcpSocket != null) {
                m_tcpSocket.Close();
                m_tcpSocket = null;
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
        /// Connect to the port
        /// </summary>
        /// <param name="hostName"> Host name</param>
        /// <param name="port">     Port number</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        public bool Connect(string hostName, int port) {
            bool   retVal;
            Action action;

            if (m_tcpSocket != null) {
                throw new MethodAccessException("Already connected");
            }
            try {
                m_tcpSocket = new TcpClient(hostName, port);
                m_stream    = m_tcpSocket.GetStream();
                m_buf       = new byte[m_tcpSocket.ReceiveBufferSize + 1];
                retVal      = true;
            } catch(Exception) {
                retVal = false;
            }
            if (retVal) {
                action = ProcessInput;
                Task.Factory.StartNew(action);
            }
            return retVal;
        }

        /// <summary>
        /// true to send debugging information to the debugging output
        /// </summary>
        public bool DebugTrace { get; set; }

        /// <summary>
        /// Trigger the NewTextReceived event
        /// </summary>
        /// <param name="e">    Event argument</param>
        protected virtual void OnNewTextReceived(EventArgs e) => NewTextReceived?.Invoke(this, e);

        /// <summary>
        /// Trigger the NewLineReceived event
        /// </summary>
        /// <param name="e">    Event argument</param>
        protected virtual void OnNewLineReceived(EventArgs e) => NewLineReceived?.Invoke(this, e);

        /// <summary>
        /// Send a text to telnet host
        /// </summary>
        /// <param name="textCmd"> Command</param>
        public void Send(string textCmd) {
            byte[]  buf;

            if (m_tcpSocket != null && m_tcpSocket.Connected) {
                lock(m_stream!) {
                    buf = ASCIIEncoding.ASCII.GetBytes(textCmd.Replace("\0xFF","\0xFF\0xFF"));
                    m_stream.Write(buf, offset: 0, buf.Length);
                    if (DebugTrace) {
                        System.Diagnostics.Debug.Write(textCmd);
                    }
                }
            }
        }

        /// <summary>
        /// Send a line to telnet host
        /// </summary>
        /// <param name="strCmd">   Command</param>
        public void SendLine(string strCmd) => Send(strCmd + "\n\r");

        /// <summary>
        /// Parse the received buffer
        /// </summary>
        private void ParseTelnet(int byteCount) {
            int  pos;
            Byte inp;
            Byte inputVerb;
            Byte inputOption;

            pos = 0;
            while (pos < byteCount) {
                inp = m_buf![pos++];
                switch ((Verbs)inp) {
                case Verbs.IAC:
                    if (pos < byteCount) {
                        // interpret as command
                        inputVerb = m_buf[pos++];
                        switch ((Verbs)inputVerb) {
                        case Verbs.IAC: 
                            //literal IAC = 255 escaped, so append char 255 to string
                            m_inputStrb!.Append(Convert.ToChar(inputVerb));
                            if (DebugTrace) {
                                System.Diagnostics.Debug.Write(Convert.ToChar(inputVerb));
                            }
                            break;
                        case Verbs.DO: 
                        case Verbs.DONT:
                        case Verbs.WILL:
                        case Verbs.WONT:
                            // reply to all commands with "WONT", unless it is SGA (suppress go ahead)
                            inputOption = m_buf[pos++];
                            if (inputOption == (int)Options.SGA) {
                                m_stream!.WriteByte((Verbs)inputVerb == Verbs.DO ? (byte)Verbs.WILL: (byte)Verbs.DO);
                            } else {
                                m_stream!.WriteByte((Verbs)inputVerb == Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT);
                            }
                            m_stream.WriteByte(inputOption);
                            break;
                        default:
                            break;
                        }
                    } else {
                        m_lastByte = inp;
                    }
                    break;
                default:
                    if (inp == '\r') {
                        inp = (byte)'\n';
                    } else if (inp == '\n') {
                        inp = (byte)'\r';
                    }
                    m_inputStrb!.Append(Convert.ToChar(inp));
                    if (DebugTrace) {
                        System.Diagnostics.Debug.Write(Convert.ToChar(inp));
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Read the received data
        /// </summary>
        private void ReadInput() {
            int readSize;
            int offset;

            offset = 0;
            if (m_lastByte != null) {
                m_buf![offset++] = m_lastByte.Value;
                m_lastByte       = null;
            }
            readSize = m_stream!.Read(m_buf!, offset, m_buf!.Length) + offset;
            if (readSize != 0) {
                ParseTelnet(readSize);
            }
        }

        /// <summary>
        /// Process the input
        /// </summary>
        private void ProcessInput() {            
            bool isTextReceived;
            bool isLineReceived;

            m_isListening = true;
            try {
                while (m_isListening && m_tcpSocket != null && m_tcpSocket.Connected && m_stream != null) {
                    lock(m_stream) {
                        while (m_stream.DataAvailable) {
                            ReadInput();
                        }
                        isTextReceived = m_inputStrb!.Length != 0;
                        isLineReceived = isTextReceived && m_inputStrb.ToString().IndexOf('\n') != -1;
                    }
                    if (m_isListening) {
                        if (isTextReceived) {
                            OnNewTextReceived(EventArgs.Empty);
                        }
                        if (isLineReceived) {
                            OnNewLineReceived(EventArgs.Empty);
                        }
                        System.Threading.Thread.Sleep(10);
                    }
                }
            } catch(Exception) {}
            m_isListening = false;
        }


        /// <summary>
        /// Returns true if still listening
        /// </summary>
        public bool IsListening => m_isListening;

        /// <summary>
        /// Read text already read
        /// </summary>
        /// <returns>
        /// Read text
        /// </returns>
        public string GetAllReadText() {
            string retVal;

            if (m_stream == null) {
                throw new InvalidOperationException("Connection not openned");
            }
            lock(m_stream!) {
                retVal = m_inputStrb!.ToString();
                m_inputStrb.Clear();
            }
            return retVal;
        }

        /// <summary>
        /// Returns the next already read line
        /// </summary>
        /// <returns>
        /// Next read line or null if not read yet
        /// </returns>
        public string? GetNextReadLine() {
            string? retVal;
            int     index;

            if (m_stream == null) {
                throw new InvalidOperationException("Connection not opened");
            }
            lock(m_stream) {
                retVal = m_inputStrb!.ToString();
                index  = retVal.IndexOf('\n');
                if (index == -1) {
                    retVal = null;
                } else {
                    retVal = retVal[..index].Replace("\r", "");
                    m_inputStrb.Remove(0, index + 1);
                }
            }
            return retVal;
        }

        /// <summary>
        /// Flush received buffer
        /// </summary>
        public void FlushInput() {
            if (m_stream != null) {
                lock(m_stream) {
                    m_inputStrb!.Clear();
                }
            } else {
                m_inputStrb?.Clear();
            }
        }
    }
}
