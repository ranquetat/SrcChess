using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SrcChess2.PgnParsing;

namespace SrcChess2.Core {
    /// <summary>
    /// Handle the book opening.
    /// </summary>
    public class Book {
    
        /// <summary>Entry in the book entries</summary>
        private struct BookEntry {        
            /// <summary>Position of this entry (Start + (End * 256))</summary>
            public short Pos;
            /// <summary>How many move for this entry at the index</summary>
            public short Size;
            /// <summary>Index in the table for the entry</summary>
            public int   Index;
            /// <summary>How many child book entries this one has</summary>
            public int   Weight;
        };

        /// <summary>Comparer use to sort array of short</summary>
        private class CompareShortArray : IComparer<short[]> {

            /// <summary>
            /// Comparer of Array of short
            /// </summary>
            /// <param name="x"> First move list</param>
            /// <param name="y"> Second move list</param>
            /// <returns>
            /// -1 if g1 less than g2, 1  if g1 greater than g2, 0 if g1 = g2
            /// </returns>
            public int Compare(short[]? x, short[]? y) {
                int retVal = 0;
                int i;
                int minSize;

                if (x != y) {
                    if (x == null) {
                        retVal = -1;
                    } else if (y == null) {
                        retVal = 1;
                    } else {
                        minSize = x.Length;
                        if (y.Length < minSize) {
                            minSize = y.Length;
                        }
                        i = 0;
                        while (i < minSize && retVal == 0) {
                            if (x[i] < y[i]) {
                                retVal--;
                            } else if (x[i] > y[i]) {
                                retVal++;
                            } else {
                                i++;
                            }
                        }
                        if (retVal == 0) {
                            if (x.Length < y.Length) {
                                retVal--;
                            } else if (x.Length > y.Length) {
                                retVal++;
                            }
                        }
                    }
                }
                return retVal;
            }
        } // Class CompareShortArray
        
        /// <summary>List of book entries</summary>
        private BookEntry[] m_bookEntries;

        /// <summary>
        /// Class constructor
        /// </summary>
        public Book() {
            m_bookEntries           = new BookEntry[1];
            m_bookEntries[0].Size   = 0;
            m_bookEntries[0].Pos    = 0;
            m_bookEntries[0].Index  = 1;
            m_bookEntries[0].Weight = 0;
        }

        /// <summary>
        /// Compute the number of child for each child moves
        /// </summary>
        /// <param name="parentMove">   Parent move</param>
        /// <returns>
        /// Nb of child
        /// </returns>
        private int ComputeWeight(int parentMove) {
            int retVal;
            int start;
            int end;
            
            start  = m_bookEntries[parentMove].Index;
            retVal = m_bookEntries[parentMove].Size;
            end    = start + retVal;
            for (int i = start; i < end; i++) {
                retVal += ComputeWeight(i);
            }
            m_bookEntries[parentMove].Weight += retVal;
            return retVal;
        }            

        /// <summary>
        /// Compute the number of child for each child moves
        /// </summary>
        private void ComputeWeight() => ComputeWeight(0);

        /// <summary>
        /// Read the book from a binary file
        /// </summary>
        private bool ReadBookFromReader(BinaryReader reader) {
            bool   retVal = false;
            string signature;
            int    size;
            
            signature = reader.ReadString();
            if (signature == "BOOK090") {
                size          = reader.ReadInt32();
                m_bookEntries = new BookEntry[size];
                for (int i = 0; i < size; i++) {
                    m_bookEntries[i].Pos   = reader.ReadInt16();
                    m_bookEntries[i].Size  = reader.ReadInt16();
                    m_bookEntries[i].Index = reader.ReadInt32();
                }
                retVal = true;
            }
            ComputeWeight();
            return retVal;
        }

        /// <summary>
        /// Read the book from a binary file
        /// </summary>
        /// <param name="fileName"> File Name</param>
        public bool ReadBookFromFile(string fileName) {
            bool         retVal = false;
            FileStream   fileStream;
            BinaryReader reader;
            
            if (File.Exists(fileName)) {
                using(fileStream = File.OpenRead(fileName)) {
                    reader = new BinaryReader(fileStream);
                    retVal = ReadBookFromReader(reader);
                }
            }
            return retVal;
        }

        /// <summary>
        /// Read the book from the specified resource
        /// </summary>
        /// <param name="assem">   Assembly or null for the current one</param>
        /// <param name="resName"> Resource Name</param>
        public bool ReadBookFromResource(Assembly? assem, string resName) {
            bool         retVal = false;
            BinaryReader reader;
            Stream       stream;

            stream  = (assem ?? GetType().Assembly).GetManifestResourceStream(resName) ?? throw new InvalidOperationException("Unable to find the opening book in resources");
            using(stream) {
                reader  = new BinaryReader(stream);
                retVal  = ReadBookFromReader(reader);
            }
            return retVal;
        }

        /// <summary>
        /// Read the book from the specified resource
        /// </summary>
        /// <param name="resName">  Resource Name</param>
        public bool ReadBookFromResource(string resName) => ReadBookFromResource(assem: null, resName);

        /// <summary>
        /// Save the book to a binary file
        /// </summary>
        /// <param name="fileName"> File name</param>
        public void SaveBookToFile(string fileName) {
            FileStream   fileStream;
            BinaryWriter writer;
            string       signature = "BOOK090";
            int          size;
            
            using(fileStream = File.Create(fileName)) {
                writer = new BinaryWriter(fileStream);
                writer.Write(signature);
                writer.Write(size = m_bookEntries.Length);
                for (int i = 0; i < size; i++) {
                   writer.Write(m_bookEntries[i].Pos);
                   writer.Write(m_bookEntries[i].Size);
                   writer.Write(m_bookEntries[i].Index);
                }
            }
        }

        /// <summary>
        /// Find a move from the book
        /// </summary>
        /// <param name="previousMoveList"> List of previous moves</param>
        /// <param name="rnd">              Random to use to pickup a move from a list. Can be null</param>
        /// <returns>
        /// Move in the form of StartPos + (EndPos * 256) or -1 if none found
        /// </returns>
        public short FindMoveInBook(MoveExt[] previousMoveList, Random? rnd) {
            short   retVal;
            bool    isFound = true;
            int     moveCount;
            int     moveIndex;
            MoveExt move;
            int[]   rndArr;
            int     startIndex;
            int     index;
            int     size;
            int     biggestRnd;
            short   pos;
            
            size       = m_bookEntries[0].Size;
            startIndex = m_bookEntries[0].Index;
            moveCount  = previousMoveList.Length;
            moveIndex  = 0;
            while (moveIndex < moveCount && isFound) {
                move    = previousMoveList[moveIndex];
                pos     = (short)(move.Move.StartPos + (move.Move.EndPos << 8));
                isFound = false;
                index   = 0;
                while (index < size && !isFound) {
                    if (m_bookEntries[startIndex + index].Pos == pos) {
                        isFound = true;
                    } else {
                        index++;
                    }
                }
                if (isFound) {
                    size       = m_bookEntries[startIndex + index].Size;
                    startIndex = m_bookEntries[startIndex + index].Index;
                    isFound    = (size != 0);
                    moveIndex++;
                }
            } 
            if (isFound && size != 0) {
                rndArr = new int[size];
                for (int i = 0; i < size; i++) {
                    rndArr[i] = (rnd == null) ? m_bookEntries[startIndex + i].Weight + 2 : rnd.Next(m_bookEntries[startIndex + i].Weight + 2);
                }
                index      = 0;
                biggestRnd = -1;
                for (int i = 0; i < size; i++) {
                    if (rndArr[i] > biggestRnd) {
                        biggestRnd = rndArr[i];
                        index      = i;
                    }
                }
                retVal = m_bookEntries[startIndex + index].Pos;
            } else {
                retVal = -1;
            }
            return retVal;
        }

        /// <summary>
        /// Compare the begining of two lists
        /// </summary>
        /// <param name="firsts">   First list</param>
        /// <param name="seconds">  Second list</param>
        /// <param name="maxDepth"> Maximum depth to compare</param>
        /// <returns>
        /// true if begining is equal
        /// </returns>
        private static bool CompareList(short[] firsts, short[] seconds, int maxDepth) {
            bool retVal = true;
            int  i;
            
            i = 0;
            while (i < maxDepth && retVal) {
                if (firsts[i] != seconds[i]) {
                    retVal = false;
                }
                i++;
            }
            return retVal;
        }

        /// <summary>
        /// Compare a key with a move list
        /// </summary>
        /// <param name="moves">    Move list</param>
        /// <param name="keyList">  Key to compare</param>
        /// <returns>
        /// true if equal
        /// </returns>
        private static bool CompareKey(short[] moves, List<short> keyList) {
            bool retVal;
            int  i;
            
            retVal = true;
            i      = 0;
            while (i < keyList.Count && retVal) {
                if (keyList[i] != moves[i]) {
                    retVal = false;
                }
                i++;
            }
            return retVal;
        }

        /// <summary>
        /// Create entries in the book
        /// </summary>
        /// <param name="moveList">      Array of move list</param>
        /// <param name="bookEntryList"> Book entry to be filled</param>
        /// <param name="keyList">       Current key</param>
        /// <param name="posInList">     Current position in the list</param>
        /// <param name="depth">         Current depth.</param>
        /// <param name="callback">      Callback to call to show progress</param>
        /// <param name="cookie">        Cookie for callback</param>
        /// <returns>
        /// Nb of entries created
        /// </returns>
        private int CreateEntries(List<short[]>                  moveList,
                                  List<BookEntry>                bookEntryList,
                                  List<short>                    keyList,
                                  out int                        posInList,
                                  int                            depth,
                                  PgnParser.DelProgressCallBack? callback,
                                  object?                        cookie) {
            int         retVal;
            int         keySize;
            short       oldValue;
            List<short> valueList;
            BookEntry   entry;
            
            keySize   = keyList.Count;
            oldValue  = -1;
            valueList = new List<short>(256);
            foreach (short[] moves in moveList) {
                if (CompareKey(moves, keyList)) {
                    if (moves[keySize] != oldValue) {
                        oldValue = moves[keySize];
                        valueList.Add(oldValue);
                    }
                }
            }
            retVal    = valueList.Count;
            posInList = bookEntryList.Count;
            for (int i = 0; i < retVal; i++) {
                entry.Pos    = (short)valueList[i];
                entry.Size   = 0;
                entry.Index  = 0;
                entry.Weight = 0;
                bookEntryList.Add(entry);
            }
            if (depth != 0) {
                for (int i = 0; i < retVal; i++) {
                    callback?.Invoke(cookie, PgnParser.ParsingPhase.CreatingBook, fileIndex: 0, fileCount: 0, fileName: null, i, retVal);
                    keyList.Add(valueList[i]);
                    entry       = bookEntryList[posInList+i];
                    entry.Index = posInList;
                    entry.Size  = (short)CreateEntries(moveList,
                                                       bookEntryList,
                                                       keyList,
                                                       out entry.Index,
                                                       depth - 1,
                                                       callback: null,
                                                       cookie: null);
                    bookEntryList[posInList+i]  = entry;
                    keyList.RemoveAt(keyList.Count - 1);
                }
                callback?.Invoke(cookie, PgnParser.ParsingPhase.CreatingBook, fileIndex: 0, fileCount: 0, fileName: null, retVal, retVal);
            }
            return retVal;
        }

        /// <summary>
        /// Create the book entries from a series of move list
        /// </summary>
        /// <param name="moveList"> List of moves</param>
        /// <param name="maxDepth"> Maximum depth of the moves</param>
        /// <param name="callback"> Callback to call to show progress</param>
        /// <param name="cookie">   Cookie for callback</param>
        /// <returns>
        /// Nb of entries created
        /// </returns>
        private BookEntry[] CreateBookList(List<short[]> moveList, int maxDepth, PgnParser.DelProgressCallBack? callback, object? cookie) {
            List<BookEntry> bookEntryList;
            List<short>     keyList;
            BookEntry       entry;
            
            keyList       = new List<short>(maxDepth);
            bookEntryList = new List<BookEntry>(moveList.Count * 10);
            entry.Pos     = -1;
            entry.Index   = 1;
            entry.Size    = 0;
            entry.Weight  = 0;
            bookEntryList.Add(entry);
            entry.Size       = (short)CreateEntries(moveList, bookEntryList, keyList, out entry.Index, maxDepth - 1, callback, cookie);
            bookEntryList[0] = entry;
            return bookEntryList.ToArray();
        }          

        /// <summary>
        /// Create the book entries from a series of move list
        /// </summary>
        /// <param name="MovesList">    List of PGN games</param>
        /// <param name="minMoveCount"> Minimum number of moves a move list must have to be consider</param>
        /// <param name="maxDepth">     Maximum depth of the moves.</param>
        /// <param name="callback">     Callback to call to show progress</param>
        /// <param name="cookie">       Cookie for callback</param>
        /// <returns>
        /// Nb of entries created
        /// </returns>
        public int CreateBookList(List<short[]> MovesList, int minMoveCount, int maxDepth, PgnParser.DelProgressCallBack? callback, object? cookie) {
            short[]?      lastMoveList = null;
            List<short[]> uniqueMovesList;

            uniqueMovesList = new List<short[]>(MovesList.Count);
            MovesList.Sort(new CompareShortArray());
            foreach (short[] moveList in MovesList) {
                if (moveList.Length >= minMoveCount) {
                    if (lastMoveList == null || !CompareList(moveList, lastMoveList, maxDepth)) {
                        uniqueMovesList.Add(moveList);
                        lastMoveList = moveList;
                    }
                }
            }
            m_bookEntries = CreateBookList(uniqueMovesList, maxDepth, callback, cookie);
            ComputeWeight();
            return m_bookEntries.Length;
        }
    } // Class Book
} // Namespace
