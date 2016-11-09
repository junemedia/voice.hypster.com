using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace hypster_voice.Code
{
    public class Recognition
    {
        /// <summary>
        /// Gets or sets a unique string that identifies this particular transaction.
        /// </summary>
        public string ResponseId { get; set; }

        /// <summary>
        /// Gets or sets NBest Complex structure that holds the results of the transcription. Supports multiple transcriptions.
        /// </summary>
        public List<NBest> NBest { get; set; }

        /// <summary>
        /// Gets or sets the Status of the transcription.
        /// </summary>
        public string Status { get; set; }

        ///<summary>
        /// Gets or sets the Info
        /// </summary>
        public SpeechInfo Info { get; set; }
    }
}