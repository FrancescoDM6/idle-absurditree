using Godot;
using System;
using System.Collections.Generic;

namespace IdleAbsurditree.Scripts.Core
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public static class Logger
    {
        private static readonly Dictionary<LogLevel, string> LogColors = new()
        {
            [LogLevel.Debug] = "gray",
            [LogLevel.Info] = "white",
            [LogLevel.Warning] = "yellow",
            [LogLevel.Error] = "red"
        };

        private static readonly Dictionary<LogLevel, string> LogPrefixes = new()
        {
            [LogLevel.Debug] = "[DEBUG]",
            [LogLevel.Info] = "[INFO]",
            [LogLevel.Warning] = "[WARN]",
            [LogLevel.Error] = "[ERROR]"
        };

        public static void Debug(string message, params object[] args)
        {
            Log(LogLevel.Debug, message, args);
        }

        public static void Info(string message, params object[] args)
        {
            Log(LogLevel.Info, message, args);
        }

        public static void Warning(string message, params object[] args)
        {
            Log(LogLevel.Warning, message, args);
        }

        public static void Error(string message, params object[] args)
        {
            Log(LogLevel.Error, message, args);
        }

        private static void Log(LogLevel level, string message, params object[] args)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
            var logEntry = $"{timestamp} {LogPrefixes[level]} {formattedMessage}";

            // Output to Godot console with appropriate level
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    GD.Print(logEntry);
                    break;
                case LogLevel.Warning:
                    GD.PrintErr($"⚠️ {logEntry}");
                    break;
                case LogLevel.Error:
                    GD.PrintErr($"❌ {logEntry}");
                    break;
            }
        }

        // Game-specific logging methods
        public static void LogNutrientGain(double amount, string source)
        {
            Debug("Nutrients gained: {0:F2} from {1}", amount, source);
        }

        public static void LogNutrientSpend(double amount, string purpose)
        {
            Debug("Nutrients spent: {0:F2} for {1}", amount, purpose);
        }

        public static void LogGeneratorPurchase(string generatorName, int count, double cost)
        {
            Info("Generator purchased: {0} (count: {1}, cost: {2:F0})", generatorName, count, cost);
        }

        public static void LogAutosave()
        {
            Info("Game auto-saved successfully");
        }

        public static void LogGameLoad()
        {
            Info("Game loaded from save file");
        }

        public static void LogOfflineProgress(double nutrients, double timeAway)
        {
            Info("Offline progress: {0:F2} nutrients earned in {1:F0} seconds", nutrients, timeAway);
        }

        public static void LogDevCommand(string command, string details = "")
        {
            Warning("Dev command executed: {0} {1}", command, details);
        }
    }
}