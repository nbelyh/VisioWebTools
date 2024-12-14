using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VisioWebTools
{
    /// <summary>
    /// Service for generating random readable strings with specific constraints.
    /// </summary>
    public class RandomStringService
    {
        private readonly Random _random = new();
        private readonly Dictionary<string, string> _wordCache = [];

        public static bool ShouldBeIgnored(string input)
        {
            return string.IsNullOrWhiteSpace(input) || Regex.IsMatch(input, @"^[\s\d\n\r\.]*$");
        }

        public string GenerateReadableRandomString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var result = string.Join('\n', input.Split('\n').Select(GenerateReadableRandomLine));
            return result;
        }

        /// <summary>
        /// Generates a pseudo-readable random string that matches the length and number of spaces of the input string.
        /// The spaces are placed at random positions, and the character casing matches the original string.
        /// </summary>
        /// <param name="input">The original string to base the random string on.</param>
        /// <returns>A pseudo-readable random string with the same length and number of spaces as the input.</returns>
        public string GenerateReadableRandomLine(string input)
        {
            if (ShouldBeIgnored(input))
                return input;
                
            if (_wordCache.TryGetValue(input, out string cachedValue))
                return cachedValue;

            int spaceCount = 0;
            int totalLength = input.Length;

            // Step 1: Count spaces
            foreach (char c in input)
            {
                if (c == ' ')
                    spaceCount++;
            }

            int nonSpaceCount = totalLength - spaceCount;
            if (nonSpaceCount <= 0)
                return input; // Edge case: string contains only spaces

            // Step 2: Determine the number of words
            int wordCount = spaceCount + 1;

            // Step 3: Distribute characters into words
            List<int> wordLengths = DistributeCharactersIntoWords(nonSpaceCount, wordCount);

            // Step 4: Generate pseudo-words
            List<string> words = new List<string>();
            foreach (int length in wordLengths)
            {
                string word = GeneratePseudoWord(length);
                words.Add(word);
            }

            // Step 5: Concatenate words without spaces
            StringBuilder sb = new StringBuilder();
            foreach (string word in words)
            {
                sb.Append(word);
            }

            string withoutSpaces = sb.ToString();

            // Step 6: Insert spaces at random positions
            char[] chars = withoutSpaces.ToCharArray();
            List<int> spacePositions = GetRandomUniquePositions(nonSpaceCount, spaceCount);

            // Create a list to manipulate characters easily
            List<char> finalChars = new List<char>(chars);

            // Insert spaces into the list at the specified positions
            spacePositions.Sort(); // Sort to maintain correct indexing
            int offset = 0;
            foreach (int pos in spacePositions)
            {
                // Ensure space is not inserted at the very start or end
                if (pos + offset >= 0 && pos + offset < finalChars.Count)
                {
                    finalChars.Insert(pos + offset, ' ');
                    offset++;
                }
            }

            // Step 7: Apply original casing to the new characters
            ApplyOriginalCasing(input, finalChars);

            var result = new string(finalChars.ToArray());
            _wordCache[input] = result;
            return result;
        }

        /// <summary>
        /// Distributes the total number of characters into a specified number of words,
        /// ensuring each word has at least one character.
        /// </summary>
        /// <param name="totalChars">Total number of non-space characters to distribute.</param>
        /// <param name="wordCount">Number of words to distribute characters into.</param>
        /// <returns>A list containing the length of each word.</returns>
        private List<int> DistributeCharactersIntoWords(int totalChars, int wordCount)
        {
            List<int> lengths = new List<int>();

            // Initialize each word with at least one character
            for (int i = 0; i < wordCount; i++)
            {
                lengths.Add(1);
            }

            int remaining = totalChars - wordCount;

            // Randomly distribute the remaining characters
            while (remaining > 0)
            {
                int index = _random.Next(wordCount);
                lengths[index]++;
                remaining--;
            }

            return lengths;
        }

        /// <summary>
        /// Generates a pseudo-readable word of a given length by alternating consonants and vowels.
        /// </summary>
        /// <param name="length">The length of the word to generate.</param>
        /// <returns>A pseudo-readable word.</returns>
        private string GeneratePseudoWord(int length)
        {
            const string vowels = "aeiou";
            const string consonants = "bcdfghjklmnpqrstvwxyz";

            StringBuilder word = new StringBuilder();

            bool startWithVowel = _random.Next(2) == 0;

            for (int i = 0; i < length; i++)
            {
                if ((i % 2 == 0 && startWithVowel) || (i % 2 != 0 && !startWithVowel))
                {
                    // Add vowel
                    word.Append(vowels[_random.Next(vowels.Length)]);
                }
                else
                {
                    // Add consonant
                    word.Append(consonants[_random.Next(consonants.Length)]);
                }
            }

            // Ensure single-letter words are vowels for better readability
            if (length == 1)
            {
                word.Clear();
                word.Append(vowels[_random.Next(vowels.Length)]);
            }

            return word.ToString();
        }

        /// <summary>
        /// Gets a list of unique random positions to insert spaces.
        /// Ensures that spaces are not inserted at the start of the string.
        /// </summary>
        /// <param name="maxPosition">The maximum position (exclusive) where a space can be inserted.</param>
        /// <param name="spaceCount">The number of spaces to insert.</param>
        /// <returns>A list of unique positions where spaces should be inserted.</returns>
        private List<int> GetRandomUniquePositions(int maxPosition, int spaceCount)
        {
            HashSet<int> positions = new HashSet<int>();

            while (positions.Count < spaceCount)
            {
                // Avoid inserting space at index 0 to prevent leading spaces
                int pos = _random.Next(1, maxPosition);
                positions.Add(pos);
            }

            return new List<int>(positions);
        }

        /// <summary>
        /// Applies the casing from the original input string to the final character list.
        /// Non-space characters in the final list will match the casing pattern of the original.
        /// </summary>
        /// <param name="original">The original input string.</param>
        /// <param name="finalChars">The final list of characters with spaces inserted.</param>
        private void ApplyOriginalCasing(string original, List<char> finalChars)
        {
            List<bool> isUpperCase = new List<bool>();

            // Extract casing information from the original string's non-space characters
            foreach (char c in original)
            {
                if (c != ' ')
                    isUpperCase.Add(char.IsUpper(c));
            }

            int nonSpaceIndex = 0;

            // Apply casing to the final characters
            for (int i = 0; i < finalChars.Count; i++)
            {
                if (finalChars[i] != ' ')
                {
                    if (nonSpaceIndex < isUpperCase.Count)
                    {
                        if (isUpperCase[nonSpaceIndex])
                            finalChars[i] = char.ToUpper(finalChars[i]);
                        else
                            finalChars[i] = char.ToLower(finalChars[i]);

                        nonSpaceIndex++;
                    }
                    else
                    {
                        // Default to lowercase if original casing is exhausted
                        finalChars[i] = char.ToLower(finalChars[i]);
                    }
                }
            }
        }
    }
}