using UnityEngine;


    /// <summary>
    /// Configuration system for TicTacToe game settings
    /// </summary>
    public static class TicTacToeConfig
    {
        // Production settings
        public static class Production
        {
            // Network settings
            public const float CONNECTION_TIMEOUT = 30f;
            public const float ROOM_WAIT_TIME = 10f;
            public const float SEARCH_TIMEOUT = 15f;
            public const int MAX_RECONNECT_ATTEMPTS = 3;
            
            // Game settings
            public const float AI_MOVE_DELAY_MIN = 0.1f;
            public const float AI_MOVE_DELAY_MAX = 2.0f;
            public const float AI_MOVE_DELAY_DEFAULT = 0.5f;
            
            // UI settings
            public const float GAME_END_TIMER = 5f;
            public const float SCENE_TRANSITION_DELAY = 2f;
            
            // Error handling
            public const int MAX_ERROR_LOGS = 100;
            public const bool ENABLE_DEBUG_LOGGING = false;
        }
        
        // Development settings
        public static class Development
        {
            // Network settings
            public const float CONNECTION_TIMEOUT = 60f;
            public const float ROOM_WAIT_TIME = 5f;
            public const float SEARCH_TIMEOUT = 10f;
            public const int MAX_RECONNECT_ATTEMPTS = 5;
            
            // Game settings
            public const float AI_MOVE_DELAY_MIN = 0.1f;
            public const float AI_MOVE_DELAY_MAX = 3.0f;
            public const float AI_MOVE_DELAY_DEFAULT = 0.3f;
            
            // UI settings
            public const float GAME_END_TIMER = 3f;
            public const float SCENE_TRANSITION_DELAY = 1f;
            
            // Error handling
            public const int MAX_ERROR_LOGS = 200;
            public const bool ENABLE_DEBUG_LOGGING = true;
        }
        
        // Feature toggles
        public static class Features
        {
            // AI features
            public const bool ENABLE_AI_OPPONENT = true;
            public const bool ENABLE_AI_TOGGLE = true;
            public const bool ENABLE_AI_DIFFICULTY_SETTINGS = false; // Future feature
            
            // Multiplayer features
            public const bool ENABLE_MULTIPLAYER = true;
            public const bool ENABLE_AUTO_MATCHMAKING = true;
            public const bool ENABLE_FALLBACK_TO_AI = true;
            
            // UI features
            public const bool ENABLE_PLAYER_NAMES = true;
            public const bool ENABLE_GAME_TIMER = true;
            public const bool ENABLE_RESET_BUTTON = true;
            
            // Error handling features
            public const bool ENABLE_ERROR_LOGGING = true;
            public const bool ENABLE_ERROR_RECOVERY = true;
            public const bool ENABLE_CRASH_REPORTING = false; // Future feature
        }
        
        // Input validation settings
        public static class Validation
        {
            public const int MIN_USERNAME_LENGTH = 6;
            public const int MAX_USERNAME_LENGTH = 20;
            public const string USERNAME_PATTERN = @"^[a-zA-Z0-9_\-\s]+$";
            
            public const int BOARD_SIZE = 3;
            public const int MAX_CELL_INDEX = 8;
        }
        
        // Scene names
        public static class Scenes
        {
            public const string MENU_CONNECT = "menu";
            public const string TICTACTOE_AUTO = "ArrowduelAuto";
            public const string TICTACTOE_MULTIPLAYER = "GameScene";
        }
        
        // Player symbols
        public static class Symbols
        {
            public const string PLAYER_X = "X";
            public const string PLAYER_O = "O";
            public const string AI_PLAYER = "AI (O)";
        }
        
        // Network room settings
        public static class Network
        {
            public const int MAX_PLAYERS_PER_ROOM = 2;
            public const string GAME_TYPE = "Arrowduel";
            public const bool ENABLE_ROOM_VISIBILITY = true;
        }
        
        /// <summary>
        /// Gets the current configuration based on build type
        /// </summary>
        public static bool IsDevelopmentBuild()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
            #else
            return false;
            #endif
        }
        
        /// <summary>
        /// Gets connection timeout based on build type
        /// </summary>
        public static float GetConnectionTimeout()
        {
            return IsDevelopmentBuild() ? Development.CONNECTION_TIMEOUT : Production.CONNECTION_TIMEOUT;
        }
        
        /// <summary>
        /// Gets room wait time based on build type
        /// </summary>
        public static float GetRoomWaitTime()
        {
            return IsDevelopmentBuild() ? Development.ROOM_WAIT_TIME : Production.ROOM_WAIT_TIME;
        }
        
        /// <summary>
        /// Gets search timeout based on build type
        /// </summary>
        public static float GetSearchTimeout()
        {
            return IsDevelopmentBuild() ? Development.SEARCH_TIMEOUT : Production.SEARCH_TIMEOUT;
        }
        
        /// <summary>
        /// Gets AI move delay based on build type
        /// </summary>
        public static float GetAIMoveDelayDefault()
        {
            return IsDevelopmentBuild() ? Development.AI_MOVE_DELAY_DEFAULT : Production.AI_MOVE_DELAY_DEFAULT;
        }
        
        /// <summary>
        /// Gets game end timer based on build type
        /// </summary>
        public static float GetGameEndTimer()
        {
            return IsDevelopmentBuild() ? Development.GAME_END_TIMER : Production.GAME_END_TIMER;
        }
        
        /// <summary>
        /// Gets scene transition delay based on build type
        /// </summary>
        public static float GetSceneTransitionDelay()
        {
            return IsDevelopmentBuild() ? Development.SCENE_TRANSITION_DELAY : Production.SCENE_TRANSITION_DELAY;
        }
        
        /// <summary>
        /// Gets max error logs based on build type
        /// </summary>
        public static int GetMaxErrorLogs()
        {
            return IsDevelopmentBuild() ? Development.MAX_ERROR_LOGS : Production.MAX_ERROR_LOGS;
        }
        
        /// <summary>
        /// Checks if debug logging is enabled
        /// </summary>
        public static bool IsDebugLoggingEnabled()
        {
            return IsDevelopmentBuild() ? Development.ENABLE_DEBUG_LOGGING : Production.ENABLE_DEBUG_LOGGING;
        }
        
        /// <summary>
        /// Validates AI move delay
        /// </summary>
        public static float ValidateAIMoveDelay(float delay)
        {
            float minDelay = IsDevelopmentBuild() ? Development.AI_MOVE_DELAY_MIN : Production.AI_MOVE_DELAY_MIN;
            float maxDelay = IsDevelopmentBuild() ? Development.AI_MOVE_DELAY_MAX : Production.AI_MOVE_DELAY_MAX;
            
            return Mathf.Clamp(delay, minDelay, maxDelay);
        }
    }
