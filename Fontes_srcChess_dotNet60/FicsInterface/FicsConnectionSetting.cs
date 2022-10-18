namespace SrcChess2.FicsInterface {

    /// <summary>
    /// FICS Connection setting
    /// </summary>
    public class FicsConnectionSetting {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="hostName"> Host name</param>
        /// <param name="hostPort"> Host port</param>
        /// <param name="userName"> User name</param>
        public FicsConnectionSetting(string hostName, int hostPort, string userName) {
            HostPort = hostPort;
            HostName = hostName;
            UserName = userName;
        }

        /// <summary>
        /// FICS Server Host Name
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// FICS Server Host port
        /// </summary>
        public int HostPort { get; set; }

        /// <summary>
        /// true for anonymous, false for rated
        /// </summary>
        public bool Anonymous { get; set; }

        /// <summary>
        /// User name
        /// </summary>
        public string UserName { get; set; }
    }
}
