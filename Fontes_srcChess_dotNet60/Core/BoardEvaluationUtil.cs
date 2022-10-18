using System;
using System.Collections.Generic;
using System.Linq;

namespace SrcChess2.Core {
    /// <summary>
    /// Utility class creating and holding all board evaluator functions
    /// </summary>
    public class BoardEvaluationUtil {

        /// <summary>
        /// Class constructor
        /// </summary>
        public BoardEvaluationUtil() { 
            Type[]  types;
            
            types = GetType().Assembly.GetTypes();
            foreach (Type type in types.Where(x => !x.IsInterface && x.GetInterface("IBoardEvaluation") != null)) {
                BoardEvaluators.Add(Activator.CreateInstance(type) as IBoardEvaluation ?? throw new InvalidOperationException($"Unable to instanciate {type}"));
            }
        }

        /// <summary>
        /// Returns the list of board evaluators
        /// </summary>
        public List<IBoardEvaluation> BoardEvaluators { get; } = new List<IBoardEvaluation>(32);

        /// <summary>
        /// Find a board evaluator using its name
        /// </summary>
        /// <param name="name"> Evaluation method name</param>
        /// <returns>
        /// Object
        /// </returns>
        public IBoardEvaluation? FindBoardEvaluator(string? name) => BoardEvaluators.FirstOrDefault(x => string.Compare(x.Name, name, ignoreCase: true) == 0);

    } // Class BoardEvaluationUtil
} // Namespace
