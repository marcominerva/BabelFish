using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabelFish.Models
{
    /// <summary>
    /// Output Result Model that contains the Recognition and Translation result.
    /// </summary>
    public class RecognitionResult
    {
        /// <summary>
        /// Recognition Result
        /// </summary>
        public string Recognition { get; set; }

        /// <summary>
        /// Translation Result
        /// </summary>
        public string Translation { get; set; }

        /// <summary>
        /// Status Message to be displayed in case of error
        /// </summary>
        public string Status { get; set; }

        public bool HasError => !string.IsNullOrWhiteSpace(Status);
    }
}
