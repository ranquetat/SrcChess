using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Resources;

namespace SrcChess2 {
    /// <summary>
    /// Piece Set included in the assembly
    /// </summary>
    public class PieceSetStandard : PieceSet {
        /// <summary>Base Path of the resource</summary>
        private readonly string  m_basePath;

        /// <summary>
        /// Class Ctor
        /// </summary>
        /// <param name="name">     Piece set Name</param>
        /// <param name="basePath"> Base path in the assembly for this piece set</param>
        /// 
        private PieceSetStandard(string name, string basePath) : base(name) => m_basePath   = basePath;

        /// <summary>
        /// Gets the pieces name as defined in the assembly
        /// </summary>
        /// <param name="piece">   Piece</param>
        /// <returns>
        /// Piece name
        /// </returns>
        protected static string NameFromChessPiece(ChessPiece piece)
            => piece switch {
                ChessPiece.Black_Pawn   => "black pawn",
                ChessPiece.Black_Rook   => "black rook",
                ChessPiece.Black_Bishop => "black bishop",
                ChessPiece.Black_Knight => "black knight",
                ChessPiece.Black_Queen  => "black queen",
                ChessPiece.Black_King   => "black king",
                ChessPiece.White_Pawn   => "white pawn",
                ChessPiece.White_Rook   => "white rook",
                ChessPiece.White_Bishop => "white bishop",
                ChessPiece.White_Knight => "white knight",
                ChessPiece.White_Queen  => "white queen",
                ChessPiece.White_King   => "white king",
                _                       => ""
            };

        /// <summary>
        /// Load the specified piece from BAML
        /// </summary>
        /// <param name="piece">    Piece</param>
        protected override UserControl LoadPiece(ChessPiece piece) {
            UserControl retVal;
            Uri         uri;
            string      uriName;

            uriName = "piecesets/" + m_basePath + "/" + NameFromChessPiece(piece) + ".xaml";
            uri     = new Uri(uriName, UriKind.Relative);
            retVal  = (UserControl)App.LoadComponent(uri);
            return retVal;
        }

        /// <summary>
        /// Load piece sets from resource
        /// </summary>
        /// <returns>
        /// List of pieces
        /// </returns>
        public static SortedList<string, PieceSet> LoadPieceSetFromResource() {
            SortedList<string, PieceSet> retVal;
            Assembly                     asm;
            string                       resName;
            string?                      keyName;
            string                       pieceSetName;
            string[]                     parts;
            Stream?                      streamResource;
            ResourceReader               resReader;
            PieceSet                     pieceSet;
            
            retVal         = new SortedList<string,PieceSet>(64);
            asm            = typeof(App).Assembly;
            resName        = asm.GetName().Name + ".g.resources";
            streamResource = asm.GetManifestResourceStream(resName) ?? throw new InvalidOperationException("Unable to access the resource stream");
            try {
                resReader      = new ResourceReader(streamResource);
                streamResource = null;
                using (resReader) {
                    foreach (DictionaryEntry dictEntry in resReader.Cast<DictionaryEntry>()) {
                        keyName = dictEntry.Key as string;
                        if (keyName != null) {
                            keyName = Uri.UnescapeDataString(keyName);
                            keyName = keyName.ToLower();
                            if (keyName.StartsWith("piecesets/") && keyName.EndsWith(".baml")) {
                                parts = keyName.Split('/');
                                if (parts.Length == 3) {
                                    pieceSetName = parts[1];
                                    if (!retVal.ContainsKey(pieceSetName)) {
                                        pieceSet = new PieceSetStandard(pieceSetName, pieceSetName);
                                        retVal.Add(pieceSetName, pieceSet);
                                    }
                                }
                            }
                        }
                    }
                }
            } finally {
                if (streamResource != null) {
                    streamResource.Dispose();
                }
            }
            return retVal;
        }
    } // Class PieceSetStandard
} // Namespace
