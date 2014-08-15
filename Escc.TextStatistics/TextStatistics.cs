using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Exceptionless;


namespace Escc.TextStatistics
{
    public class TextStatistics
    {


        public int SyllableCount(string text)
        {

            // Should be non non-alpha characters
            var matched = Regex.Matches(text, "[^a-zA-Z]");
            foreach (Match match in matched)
                text = text.Replace(match.Value, "");


            // Prepare text - make lower case
            text = text.ToLower(CultureInfo.CurrentCulture);


            // Specific common exceptions that don't follow the rule set below are handled individually
            // Dictionary of problem words (with text as key, syllable count as value)
            var problemWords = new Dictionary<string, int> { { "simile", 3 }, { "forever", 3 }, { "shoreline", 2 } };

            // Return if we've hit one of those...
            if (problemWords.ContainsKey(text)) return problemWords[text];

            // These syllables would be counted as two but should be one
            var subSyllables = new Dictionary<string, int>
                {
                    {"cial", 1},
                    {"tia", 1},
                    {"cius", 1},
                    {"cious", 1},
                    {"giu", 1},
                    {"ion", 1},
                    {"iou", 1},
                    {"sia$", 1},
                    {"[^aeiuoyt]{2,}ed$", 1},
                    {".ely$", 1},
                    {"[cg]h?e[rsd]?$", 1},
                    {"rved?$", 1},
                    {"[aeiouy][dt]es?$", 1},
                    {"[aeiouy][^aeiouydt]e[rsd]?$", 1},
                    {"[aeiouy]rse$", 1}
                };


            // These syllables would be counted as one but should be two
            var addSyllables = new Dictionary<string, int>
                {
                    {"ia", 2},
                    {"riet", 2},
                    {"dien", 2},
                    {"iu", 2},
                    {"io", 2},
                    {"ii", 2},
                    {"[aeiouym]bl$", 2},
                    {"[aeiou]{3}", 2},
                    {"^mc", 2},
                    {"ism$", 2},
                    {@"([^aeiouy])\1l$", 2},
                    {"[^l]lien", 2},
                    {"^coa[dglx].", 2},
                    {"[^gq]ua[^auieo]", 2},
                    {"dnt$", 2},
                    {"uity$", 2},
                    {"ie(r|st)$", 2}
                };


            // Single syllable prefixes and suffixes
            var prefixSuffix = new List<string>
                {
                    "^un",
                    "^fore",
                    "ly$",
                    "less$",
                    "ful$",
                    "ers?$",
                    "ings?$"
                };

            int prefixSuffixCount = 0;
            // Remove prefixes and suffixes and count how many were taken
            foreach (string pattern in prefixSuffix)
            {
                var matches = Regex.Matches(text, pattern);
                foreach (Match match in matches)
                {
                    text = text.Replace(match.Value, "");
                    prefixSuffixCount++;

                }
            }


            // Removed non-text characters from text
            text = Regex.Replace(text, "[^a-z]", "", RegexOptions.IgnoreCase);
            string[] wordParts = Regex.Split(text, "[^aeiouy]+");
            int wordPartCount = 0;
            foreach (string wordPart in wordParts)
            {
                if (wordPart != "") wordPartCount++;
            }


            // Get preliminary syllable count...
            int syllableCount = wordPartCount + prefixSuffixCount;


            int result = syllableCount;
            foreach (KeyValuePair<string, int> pattern in subSyllables)
            {
                Regex matchPrefix = new Regex(pattern.Key);
                if (matchPrefix.IsMatch(text)) result = result - 1;
            }
            int count = 0;
            foreach (KeyValuePair<string, int> pattern in addSyllables)
            {
                Regex matchPrefix = new Regex(pattern.Key);
                if (matchPrefix.IsMatch(text)) count++;
            }
            syllableCount = result + count;


            return (syllableCount == 0) ? 1 : syllableCount;
        }

        //       
        public double AverageSyllablesPerWord(string text)
        {
            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;
            var syllableCount = 0;
            var wordCount = WordCount(text);

            if (text != null)
            {
                var words = Regex.Split(text, @"\s+");
                int sum = 0;
                foreach (string word in words)
                    sum += SyllableCount(word);
                syllableCount += sum;
            }

            return (syllableCount) / ((float)wordCount);
        }

        public int WordCount(string text)
        {
            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;
            if (text != null)
            {
                var words = Regex.Split(text, @"\s+", RegexOptions.IgnoreCase);

                return words.Length;
            }
            return 0;
        }

        public string CleanText(string text)
        {
            // all these tags should be preceeded by a full stop.
            var fullStopTags = new List<string> { "li", "p", "h1", "h2", "h3", "h4", "h5", "h6", "dd" };

            // BUG: We should only put in full stop if punctuation doesn't already exist.
            foreach (string tag in fullStopTags)
                text = text.Replace("</" + tag + ">", ".");

            var replacement1Patterns = new Dictionary<string, string>
                {
                    {"<[^>]+>", ""}, // Strip tags
                    {"[\",:;()-]", " "}, // Replace commas, hypens
                    {@"[\.!?]", "."} // Unify terminators
                    
                };

            foreach (var replacementPattern in replacement1Patterns)
            {
                foreach (Match match in Regex.Matches(text, replacementPattern.Key, RegexOptions.IgnoreCase))
                {
                    text = text.Replace(match.Value, replacementPattern.Value);
                }
            }

            //Add final terminator, just in case it's missing
            text = text.Trim() + ".";

            var replacement2Patterns = new Dictionary<string, string>
                {
                    {@"[ ]*(\n|\r\n|\r)[ ]*", " "}, // Replace new lines with spaces
                    {@"([\.])[\. ]+", "."}, // Check for duplicated terminators
                    {@"[ ]*([\.])", ". "}, // Pad sentence terminators
                    
                };

            foreach (var replacementPattern in replacement2Patterns)
            {
                foreach (Match match in Regex.Matches(text, replacementPattern.Key, RegexOptions.IgnoreCase))
                {
                    text = text.Replace(match.Value, replacementPattern.Value);
                }
            }

            text = text.Trim();


            var replacement3Patterns = new Dictionary<string, string>
                {
                    {"[0-9]+"," "}, //Remove text comprised of only numbers BUG what about dates?
                    {@"[ ]+", " "} // Remove multiple spaces
                    
                    
                };

            foreach (var replacementPattern in replacement3Patterns)
            {
                foreach (Match match in Regex.Matches(text, replacementPattern.Key, RegexOptions.IgnoreCase))
                {
                    text = text.Replace(match.Value, replacementPattern.Value);
                }
            }


            return text.Trim();
        }

        public int WordsWithThreeSyllables(string text, bool countProperNouns = true)
        {
            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;
            int longWordCount = 0;
            int wordCount = WordCount(text);


            try
            {

                if (text != null)
                {
                    var words = Regex.Split(text, @"\s+");
                    for (int i = 0; i < wordCount; i++)
                    {
                        if (SyllableCount(words[i]) > 2)
                        {
                            if (countProperNouns)
                            {
                                longWordCount++;
                            }
                            else
                            {
                                {
                                    string firstLetter = text.Substring(
                                        text.IndexOf(words[i], StringComparison.Ordinal), 1);

                                    if (!HasUpperCase(firstLetter))
                                    {
                                        // First letter is lower case, count it
                                        longWordCount++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException outEx)
            {

                new Exception("Error thrown when computing words with 3 syllables for the text - " + text, outEx).ToExceptionless().Submit();

            }
            catch (Exception ex)
            {
                new Exception("Unhandled exception thrown when computing words with 3 syllables for the text - " + text, ex).ToExceptionless().Submit();
            }

            return (longWordCount);
        }

        bool HasUpperCase(string str)
        {
            bool any = false;
            foreach (char c in str)
            {
                if (char.IsUpper(c))
                    any = true;
                break;
            }
            return !string.IsNullOrEmpty(str) && any;
        }


        public double PercentageWordsWithThreeSyllables(string text, bool countProperNouns = true)
        {
            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;
            int wordCount = WordCount(text);
            int longWordCount = WordsWithThreeSyllables(text, countProperNouns);
            float percentage = ((longWordCount / (float)wordCount) * 100);
            return (Math.Round(percentage, 1));
        }


        public int LetterCount(string text)
        {
            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;

            if (text != null)
            {
                var matches = Regex.Matches(text, "[^A-Za-z]+", RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                    text = text.Replace(match.Value, "");
            }
            if (text != null) return text.Length;
            return 0;
        }

        public int SentenceCount(string text)
        {
            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;
            if (text != null)
            {
                var matches = Regex.Matches(text, @"[^\.!?]", RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                    text = text.Replace(match.Value, "");
            }

            if (text != null) return Math.Max(1, text.Length);
            return 0;
        }

        public double AverageWordsPerSentence(string text)
        {
            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;
            int sentenceCount = SentenceCount(text);
            int wordCount = WordCount(text);

            return (wordCount / (float)sentenceCount);
        }

        public double FleschKincaidReadingEase(string text)
        {

            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;
            return Math.Round((206.835 - (1.015 * AverageWordsPerSentence(text)) - (84.6 * AverageSyllablesPerWord(text))), 1);

        }

        public double FleschKincaidGradeLevel(string text)
        {
            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;

            return Math.Round(((0.39 * AverageWordsPerSentence(text)) + (11.8 * AverageSyllablesPerWord(text)) - 15.59), 1);

        }

        public double GunningFogScore(string text)
        {
            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;
            return Math.Round(((AverageWordsPerSentence(text) + PercentageWordsWithThreeSyllables(text, false)) * 0.4), 1);
        }

        public double ColemanLiauIndex(string text)
        {
            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;
            return Math.Round(((5.89 * LetterCount(text) / WordCount(text))) - (0.3 * (SentenceCount(text) / (float)WordCount(text))) - 15.8, 1);
        }

        public double SmogIndex(string text)
        {
            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;
            return Math.Round(1.043 * Math.Sqrt((WordsWithThreeSyllables(text) * (30 / SentenceCount(text))) + 3.1291), 1);

        }

        public double AutomatedReadabilityIndex(string text)
        {
            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;
            return Math.Round(((4.71 * (LetterCount(text) / (float)WordCount(text))) + (0.5 * (WordCount(text) / (float)SentenceCount(text))) - 21.43), 1);
        }


        public double AverageGradeLevel(string text)
        {
            text = !string.IsNullOrEmpty(text) ? CleanText(text) : text;

            var fleshGradelevel = FleschKincaidGradeLevel(text);
            var gunningFogScore = GunningFogScore(text);
            var smogIndex = SmogIndex(text);
            var colemanIndex = ColemanLiauIndex(text);
            var automatedReadabilityIndex = AutomatedReadabilityIndex(text);

            return Math.Round(((fleshGradelevel + gunningFogScore + smogIndex + colemanIndex +
                     automatedReadabilityIndex) / 5), 1);


        }

        public string TranslateReadingEase(double readingEase)
        {
            string explanation;

            if (readingEase >= 0.0 & readingEase <= 30.0)
            {
                explanation = "Best understood by university graduates";
            }
            else if (readingEase >= 30.1 & readingEase <= 50.0)
            {
                explanation = "Best understood by university undergraduates";
            }
            else if (readingEase >= 50.1 & readingEase <= 59.9)
            {
                explanation = "Best understood by A'Level students";
            }
            else if (readingEase >= 60.0 & readingEase <= 70.0)
            {
                explanation = "Easily understood by an average 13 to 15 year old students";
            }
            else if (readingEase >= 70.1 & readingEase <= 89.9)
            {
                explanation = "Easily understood by an average 12 year old student";
            }
            else if (readingEase >= 90.0 & readingEase <= 100.0)
            {
                explanation = "Easily understood by an average 11 year old student";
            }
            else
            {
                explanation = "SOMETHING WENT WRONG WITH THE SCORING";
            }

            return explanation;
        }

    }
}