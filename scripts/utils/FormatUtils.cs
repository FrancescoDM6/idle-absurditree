// using System;
// using IdleAbsurditree.Scripts.Core.Data;

// namespace IdleAbsurditree.Scripts.Utils
// {
//     /// <summary>
//     /// Utility class for formatting game values
//     /// </summary>
//     public static class FormatUtils
//     {
//         /// <summary>
//         /// Format a number with appropriate suffix (K, M, B, T)
//         /// </summary>
//         public static string FormatNumber(double number)
//         {
//             if (number < GameData.Formatting.ThousandThreshold) 
//                 return number.ToString("F0");
//             if (number < GameData.Formatting.MillionThreshold) 
//                 return $"{number / GameData.Formatting.ThousandThreshold:F2}{GameData.Formatting.ThousandSuffix}";
//             if (number < GameData.Formatting.BillionThreshold) 
//                 return $"{number / GameData.Formatting.MillionThreshold:F2}{GameData.Formatting.MillionSuffix}";
//             if (number < GameData.Formatting.TrillionThreshold) 
//                 return $"{number / GameData.Formatting.BillionThreshold:F2}{GameData.Formatting.BillionSuffix}";
//             return $"{number / GameData.Formatting.TrillionThreshold:F2}{GameData.Formatting.TrillionSuffix}";
//         }
        
//         /// <summary>
//         /// Format currency with the game's currency name
//         /// </summary>
//         public static string FormatCurrency(double amount)
//         {
//             return $"{FormatNumber(amount)} {GameData.Currency.Name}";
//         }
        
//         /// <summary>
//         /// Format production rate with appropriate suffix
//         /// </summary>
//         public static string FormatProduction(double rate)
//         {
//             return $"{FormatNumber(rate)} {GameData.Currency.PerSecondSuffix}";
//         }
        
//         /// <summary>
//         /// Format time in a human-readable way
//         /// </summary>
//         public static string FormatTime(double seconds)
//         {
//             if (seconds < 60)
//                 return $"{seconds:F0}s";
            
//             if (seconds < 3600)
//             {
//                 int minutes = (int)(seconds / 60);
//                 int secs = (int)(seconds % 60);
//                 return $"{minutes}m {secs}s";
//             }
            
//             if (seconds < 86400)
//             {
//                 int hours = (int)(seconds / 3600);
//                 int minutes = (int)((seconds % 3600) / 60);
//                 return $"{hours}h {minutes}m";
//             }
            
//             int days = (int)(seconds / 86400);
//             int hours = (int)((seconds % 86400) / 3600);
//             return $"{days}d {hours}h";
//         }
        
//         /// <summary>
//         /// Format a percentage value
//         /// </summary>
//         public static string FormatPercentage(double value, int decimals = 0)
//         {
//             return $"{value * 100:F{decimals}}%";
//         }
//     }
// }