using UnityEngine;
using System.Text.RegularExpressions;


    /// <summary>
    /// Utility class for validating user inputs in the TicTacToe game
    /// </summary>
    public static class TicTacToeInputValidator
    {
        // Constants for validation
        private const int MIN_USERNAME_LENGTH = 1;
        private const int MAX_USERNAME_LENGTH = 20;
        private const string USERNAME_PATTERN = @"^[a-zA-Z0-9_\-\s]+$";
        
        /// <summary>
        /// Validates a username for multiplayer games
        /// </summary>
        /// <param name="username">The username to validate</param>
        /// <param name="errorMessage">Output error message if validation fails</param>
        /// <returns>True if username is valid, false otherwise</returns>
        public static bool ValidateUsername(string username, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            // Check for null or empty
            if (string.IsNullOrWhiteSpace(username))
            {
                errorMessage = "Username cannot be empty.";
                return false;
            }
            
            // Check length
            if (username.Length < MIN_USERNAME_LENGTH)
            {
                errorMessage = $"Username must be at least {MIN_USERNAME_LENGTH} characters long.";
                return false;
            }
            
            if (username.Length > MAX_USERNAME_LENGTH)
            {
                errorMessage = $"Username must be {MAX_USERNAME_LENGTH} characters or less.";
                return false;
            }
            
            // Check for valid characters
            if (!Regex.IsMatch(username, USERNAME_PATTERN))
            {
                errorMessage = "Username can only contain letters, numbers, spaces, hyphens, and underscores.";
                return false;
            }
            
            // Check for reserved names
            if (IsReservedName(username))
            {
                errorMessage = "This username is reserved and cannot be used.";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Sanitizes a username by removing invalid characters and trimming
        /// </summary>
        /// <param name="username">The username to sanitize</param>
        /// <returns>Sanitized username</returns>
        public static string SanitizeUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return string.Empty;
            
            // Remove invalid characters
            string sanitized = Regex.Replace(username, @"[^a-zA-Z0-9_\-\s]", "");
            
            // Trim whitespace
            sanitized = sanitized.Trim();
            
            // Limit length
            if (sanitized.Length > MAX_USERNAME_LENGTH)
            {
                sanitized = sanitized.Substring(0, MAX_USERNAME_LENGTH);
            }
            
            return sanitized;
        }
        
        /// <summary>
        /// Checks if a username is reserved
        /// </summary>
        /// <param name="username">The username to check</param>
        /// <returns>True if username is reserved</returns>
        private static bool IsReservedName(string username)
        {
            if (string.IsNullOrEmpty(username))
                return false;
            
            string lowerUsername = username.ToLowerInvariant();
            
            // List of reserved names
            string[] reservedNames = {
                "admin", "administrator", "moderator", "mod", "system", "server",
                "host", "master", "client", "player", "guest", "anonymous",
                "null", "undefined", "test", "demo", "example", "sample"
            };
            
            foreach (string reserved in reservedNames)
            {
                if (lowerUsername == reserved)
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Validates a cell index for the game board
        /// </summary>
        /// <param name="index">The cell index to validate</param>
        /// <returns>True if index is valid</returns>
        public static bool ValidateCellIndex(int index)
        {
            return index >= 0 && index < 9;
        }
        
        /// <summary>
        /// Validates board coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>True if coordinates are valid</returns>
        public static bool ValidateBoardCoordinates(int x, int y)
        {
            return x >= 0 && x < 3 && y >= 0 && y < 3;
        }
        
        /// <summary>
        /// Converts cell index to board coordinates
        /// </summary>
        /// <param name="index">Cell index (0-8)</param>
        /// <param name="x">Output X coordinate</param>
        /// <param name="y">Output Y coordinate</param>
        /// <returns>True if conversion successful</returns>
        public static bool IndexToCoordinates(int index, out int x, out int y)
        {
            x = 0;
            y = 0;
            
            if (!ValidateCellIndex(index))
                return false;
            
            x = index / 3;
            y = index % 3;
            return true;
        }
        
        /// <summary>
        /// Converts board coordinates to cell index
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="index">Output cell index</param>
        /// <returns>True if conversion successful</returns>
        public static bool CoordinatesToIndex(int x, int y, out int index)
        {
            index = 0;
            
            if (!ValidateBoardCoordinates(x, y))
                return false;
            
            index = x * 3 + y;
            return true;
        }
    }
