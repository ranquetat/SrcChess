namespace GenericSearchEngine {
    /// <summary>
    /// Position information. Positive value for player 1, negative value for player 2.
    /// All these informations are computed before the last move to improve performance.
    /// </summary>
    public struct AttackPosInfo {
        /// <summary>
        /// Class Ctor
        /// </summary>
        /// <param name="piecesAttacked">   Number of pieces attacking this position</param>
        /// <param name="piecesDefending">  Number of pieces defending this position</param>
        public AttackPosInfo(int piecesAttacked, int piecesDefending) {
            PiecesAttacked  = piecesAttacked;
            PiecesDefending = piecesDefending;
        }

        /// <summary>
        /// Number of pieces being attacked by player's pieces
        /// </summary>
        public int PiecesAttacked;

        /// <summary>
        /// Number of pieces defending player's pieces
        /// </summary>
        public int PiecesDefending;

        /// <summary>
        /// null attack position
        /// </summary>
        public static AttackPosInfo NullAttackPosInfo { get; } = new AttackPosInfo(0, 0);
    }
}
