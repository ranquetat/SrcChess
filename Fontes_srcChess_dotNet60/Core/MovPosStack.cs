using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace SrcChess2.Core {
    /// <summary>
    /// Maintains the list of moves which has been done on a board. The undo moves are kept up to when a new move is done.
    /// </summary>
    public class MovePosStack : IXmlSerializable {
        /// <summary>List of move position</summary>
        private readonly List<MoveExt> m_movePosList;
        /// <summary>Position of the current move in the list</summary>
        private int                    m_posInList;

        /// <summary>
        /// Class constructor
        /// </summary>
        public MovePosStack() {
            m_movePosList = new List<MoveExt>(512);
            m_posInList   = -1;
        }

        /// <summary>
        /// Class constructor (copy constructor)
        /// </summary>
        private MovePosStack(MovePosStack movePosList) {
            m_movePosList = new List<MoveExt>(movePosList.m_movePosList);
            m_posInList   = movePosList.m_posInList;
        }

        /// <summary>
        /// Clone the stack
        /// </summary>
        /// <returns>
        /// Move list
        /// </returns>
        public MovePosStack Clone() => new(this);

        /// <summary>
        /// Save to the specified binary writer
        /// </summary>
        /// <param name="writer"> Binary writer</param>
        public void SaveToWriter(System.IO.BinaryWriter writer) {
            writer.Write(m_movePosList.Count);
            writer.Write(m_posInList);
            foreach (MoveExt move in m_movePosList) {
                writer.Write((byte)move.Move.OriginalPiece);
                writer.Write(move.Move.StartPos);
                writer.Write(move.Move.EndPos);
                writer.Write((byte)move.Move.Type);
            }
        }

        /// <summary>
        /// Load from reader
        /// </summary>
        /// <param name="reader"> Binary Reader</param>
        public void LoadFromReader(System.IO.BinaryReader reader) {
            int  moveCount;
            Move move;
            
            m_movePosList.Clear();
            moveCount   = reader.ReadInt32();
            m_posInList = reader.ReadInt32();
            for (int i = 0; i < moveCount; i++) {
                move.OriginalPiece = (ChessBoard.PieceType)reader.ReadByte();
                move.StartPos      = reader.ReadByte();
                move.EndPos        = reader.ReadByte();
                move.Type          = (Move.MoveType)reader.ReadByte();
                m_movePosList.Add(new MoveExt(move));
            }
        }

        /// <summary>
        /// Returns the XML schema if any
        /// </summary>
        /// <returns>
        /// null
        /// </returns>
        public System.Xml.Schema.XmlSchema? GetSchema() => null;

        /// <summary>
        /// Deserialize from XML
        /// </summary>
        /// <param name="reader"> XML reader</param>
        public void ReadXml(XmlReader reader) {
            Move move;
            bool isEmpty;

            m_movePosList.Clear();
            if (reader.MoveToContent() != XmlNodeType.Element || reader.LocalName != "MoveList") {
                throw new SerializationException("Unknown format");
            } else {
                isEmpty     = reader.IsEmptyElement;
                m_posInList = int.Parse(reader.GetAttribute("PositionInList") ?? "-1");
                if (isEmpty) {
                    reader.Read();
                } else {
                    if (reader.ReadToDescendant("Move")) {
                        while (reader.IsStartElement()) {
                            move = new Move {
                                OriginalPiece = (ChessBoard.PieceType)Enum.Parse(typeof(ChessBoard.SerPieceType), reader.GetAttribute("OriginalPiece") ?? "0"),
                                StartPos      = (byte)int.Parse(reader.GetAttribute("StartingPosition") ?? "0", CultureInfo.InvariantCulture),
                                EndPos        = (byte)int.Parse(reader.GetAttribute("EndingPosition") ?? "0",   CultureInfo.InvariantCulture),
                                Type          = (Move.MoveType)Enum.Parse(typeof(Move.MoveType), reader.GetAttribute("MoveType") ?? "0")
                            };
                            m_movePosList.Add(new MoveExt(move));
                            reader.ReadStartElement("Move");
                        }
                    }
                    reader.ReadEndElement();
                }
            }
        }

        /// <summary>
        /// Serialize the move list to an XML writer
        /// </summary>
        /// <param name="writer">   XML writer</param>
        public void WriteXml(XmlWriter writer) {
            writer.WriteStartElement("MoveList");
            writer.WriteAttributeString("PositionInList", m_posInList.ToString());
            foreach (MoveExt move in m_movePosList) {
                writer.WriteStartElement("Move");
                writer.WriteAttributeString("OriginalPiece",    ((ChessBoard.SerPieceType)move.Move.OriginalPiece).ToString());
                writer.WriteAttributeString("StartingPosition", ((int)move.Move.StartPos).ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("EndingPosition",   ((int)move.Move.EndPos).ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("MoveType",         move.Move.Type.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// Count
        /// </summary>
        public int Count => m_movePosList.Count;

        /// <summary>
        /// Indexer
        /// </summary>
        public MoveExt this[int index] => m_movePosList[index];

        /// <summary>
        /// Get the list of moves
        /// </summary>
        public List<MoveExt> List => m_movePosList;

        /// <summary>
        /// Add a move to the stack. All redo move are discarded
        /// </summary>
        /// <param name="move"> New move</param>
        public void AddMove(MoveExt move) {
            int count;
            int pos;
            
            count = Count;
            pos   = m_posInList + 1;
            while (count != pos) {
                m_movePosList.RemoveAt(--count);
            }
            m_movePosList.Add(move);
            m_posInList = pos;
        }

        /// <summary>
        /// Current move (last done move)
        /// </summary>
        public MoveExt CurrentMove => this[m_posInList];

        /// <summary>
        /// Next move in the redo list
        /// </summary>
        public MoveExt NextMove => this[m_posInList+1];

        /// <summary>
        /// Move to next move
        /// </summary>
        public void MoveToNext() {
            int maxPos;
            
            maxPos = Count - 1;
            if (m_posInList < maxPos) {
                m_posInList++;
            }
        }

        /// <summary>
        /// Move to previous move
        /// </summary>
        public void MoveToPrevious() {
            if (m_posInList > -1) {
                m_posInList--;
            }
        }

        /// <summary>
        /// Current move index
        /// </summary>
        public int PositionInList => m_posInList;

        /// <summary>
        /// Removes all move in the list
        /// </summary>
        public void Clear() {
            m_movePosList.Clear();
            m_posInList = -1;
        }
    } // Class MovePosStack
} // Namespace
