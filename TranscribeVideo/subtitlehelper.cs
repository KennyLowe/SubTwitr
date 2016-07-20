using System;
using System.Text.RegularExpressions;

namespace SubtitleConverter
{
    public static class SubtitleHelper
    {
        /// <summary>
        ///     Convert WebVTT subtitles to Srt subtitles
        /// </summary>
        /// <param name="webvttContent">WebVTT string</param>
        /// <returns>SRT result</returns>
        public static string ConvertWebvttToSrt(string webvttContent)
        {
            if (webvttContent == null)
                throw new ArgumentNullException(nameof(webvttContent));

            var srtResult = webvttContent;

            var srtPartLineNumber = 0;

            srtResult = Regex.Replace(srtResult, @"(WEBVTT\s+)", "");

            srtResult = Regex.Replace(srtResult, @"(NOTE recognizability [0-9].[0-9][0-9][0-9]?\s+)", "");

            srtResult = Regex.Replace(srtResult, @"(\d{2}:\d{2}:\d{2})\.(\d{3}\s+)-->(\s+\d{2}:\d{2}:\d{2})\.(\d{3}\s*)",
                match =>
                {
                    srtPartLineNumber++;
                    return srtPartLineNumber.ToString() + Environment.NewLine +
                           Regex.Replace(match.Value,
                               @"(\d{2}:\d{2}:\d{2})\.(\d{3}\s+)-->(\s+\d{2}:\d{2}:\d{2})\.(\d{3}\s*)", "$1,$2-->$3,$4");
                    // Writes '00:00:19.620' instead of '00:00:19,620'
                }); // Writes Srt section numbers for each section

            return srtResult;
        }
    }
}