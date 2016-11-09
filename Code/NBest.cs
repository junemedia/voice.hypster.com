using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace hypster_voice.Code
{
    public class NBest
    {
        /// <summary>
        /// Gets or sets the transcription of the audio.
        /// </summary>
        public string Hypothesis { get; set; }

        /// <summary>
        /// Gets or sets the language used to decode the Hypothesis.
        /// Represented using the two-letter ISO 639 language code, hyphen, two-letter ISO 3166 country code in lower case, e.g. �en-us�.
        /// </summary>
        public string LanguageId { get; set; }

        /// <summary>
        /// Gets or sets the confidence value of the Hypothesis, a value between 0.0 and 1.0 inclusive.
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Gets or sets a machine-readable string indicating an assessment of utterance/result quality and the recommended treatment of the Hypothesis.
        /// The assessment reflects a confidence region based on prior experience with similar results.
        /// accept - the hypothesis value has acceptable confidence
        /// confirm - the hypothesis should be independently confirmed due to lower confidence
        /// reject - the hypothesis should be rejected due to low confidence
        /// </summary>
        public string Grade { get; set; }

        /// <summary>
        /// Gets or sets a text string prepared according to the output domain of the application package.
        /// The string will generally be a formatted version of the hypothesis, but the words may have been altered through
        /// insertions/deletions/substitutions to make the result more readable or usable for the client.
        /// </summary>
        public string ResultText { get; set; }

        /// <summary>
        /// Gets or sets the words of the Hypothesis split into separate strings.
        /// May omit some of the words of the Hypothesis string, and can be empty. Never contains words not in hypothesis string.
        /// </summary>
        public List<string> Words { get; set; }

        /// <summary>
        /// Gets or sets the confidence scores for each of the strings in the words array. Each value ranges from 0.0 to 1.0 inclusive.
        /// </summary>
        public List<double> WordScores { get; set; }

        public Dictionary<string, string> NluHypothesis { get; set; }

    }
}