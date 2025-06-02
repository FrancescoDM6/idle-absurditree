using Godot;
using IdleAbsurditree.Scripts.Gameplay;
using System;
using System.Collections.Generic;

namespace IdleAbsurditree.Scripts.Core
{
    public partial class GameManager : Node
    {
        private static GameManager _instance;
        public static GameManager Instance => _instance;

        // Core game state - Updated for nutrients system
        private double _availableNutrients = 0;
        private double _lifetimeNutrients = 0;
        private double _nutrientsPerSecond = 0;
        
        // Nutrient gain tracking for per-second calculation
        private Queue<(double timestamp, double amount)> _recentGains = new();
        private const double GAIN_TRACKING_WINDOW = 5.0; // Track gains over 5 seconds
        
        // Time tracking
        private double _timeSinceLastSave = 0;
        private const double AUTO_SAVE_INTERVAL = 30.0; // Auto-save every 30 seconds

        // Events
        public delegate void NutrientsChangedEventHandler(double available, double lifetime, double perSecond);
        public event NutrientsChangedEventHandler NutrientsChanged;

        // Properties
        public double AvailableNutrients => _availableNutrients;
        public double LifetimeNutrients => _lifetimeNutrients;
        public double NutrientsPerSecond => _nutrientsPerSecond;

        public override void _Ready()
        {
            _instance = this;
            LoadGame();
            Logger.Info("Game Manager initialized. Version: {0}", GameVersion.VERSION);
            
            // Trigger initial UI update
            NotifyNutrientsChanged();
        }

        public override void _Process(double delta)
        {
            // Update nutrients based on per-second rate from generators only
            var generatorProduction = CalculateGeneratorProduction();
            if (generatorProduction > 0)
            {
                AddNutrients(generatorProduction * delta, "generators");
            }

            // Update nutrient per second calculation
            UpdateNutrientsPerSecondCalculation();

            // Handle auto-save
            _timeSinceLastSave += delta;
            if (_timeSinceLastSave >= AUTO_SAVE_INTERVAL)
            {
                SaveGame();
                Logger.LogAutosave();
                _timeSinceLastSave = 0;
            }
        }

        private double CalculateGeneratorProduction()
        {
            // Get production from generators through MainUI
            var mainUI = GetTree().GetFirstNodeInGroup("MainUI");
            if (mainUI != null && mainUI.HasMethod("GetGeneratorProduction"))
            {
                return mainUI.Call("GetGeneratorProduction").AsDouble();
            }
            return 0;
        }

        private void UpdateNutrientsPerSecondCalculation()
        {
            var currentTime = Time.GetUnixTimeFromSystem();
            
            // Clean old entries
            while (_recentGains.Count > 0 && 
                   currentTime - _recentGains.Peek().timestamp > GAIN_TRACKING_WINDOW)
            {
                _recentGains.Dequeue();
            }

            // Calculate average gain per second
            double totalGains = 0;
            foreach (var gain in _recentGains)
            {
                totalGains += gain.amount;
            }

            var newPerSecond = totalGains / GAIN_TRACKING_WINDOW;
            
            // Only update if there's a meaningful change
            if (Math.Abs(newPerSecond - _nutrientsPerSecond) > 0.01)
            {
                _nutrientsPerSecond = newPerSecond;
                NotifyNutrientsChanged();
            }
        }

        public void AddNutrients(double amount, string source = "unknown")
        {
            if (amount <= 0) return;
            
            _availableNutrients += amount;
            _lifetimeNutrients += amount;
            
            // Track this gain for per-second calculation
            var currentTime = Time.GetUnixTimeFromSystem();
            _recentGains.Enqueue((currentTime, amount));
            
            Logger.LogNutrientGain(amount, source);
            NotifyNutrientsChanged();
        }

        public bool SpendNutrients(double amount, string purpose = "unknown")
        {
            if (_availableNutrients >= amount)
            {
                _availableNutrients -= amount;
                Logger.LogNutrientSpend(amount, purpose);
                NotifyNutrientsChanged();
                return true;
            }
            return false;
        }

        public void UpdateNutrientsPerSecond(double amount)
        {
            // This method is kept for generator-specific production updates
            // The actual per-second display now uses the tracked gains
        }

        // Dev menu methods

        // Clear value of lifetime nutrients
        public void ClearLifetimeNutrients()
        {
            _lifetimeNutrients = 0;
            Logger.LogDevCommand("ClearLifetimeNutrients");
            NotifyNutrientsChanged();
        }

        // Clear value of available nutrients
        public void ClearAvailableNutrients()
        {
            _availableNutrients = 0;
            Logger.LogDevCommand("ClearAvailableNutrients");
            NotifyNutrientsChanged();
        }

        // Clear value of all nutrients
        public void ClearAllNutrients()
        {
            _availableNutrients = 0;
            _lifetimeNutrients = 0;
            Logger.LogDevCommand("ClearAllNutrients");
            NotifyNutrientsChanged();
        }

        // Set value of lifetime nutrients
        public void SetLifetimeNutrients(double amount)
        {
            _lifetimeNutrients = Math.Max(0, amount);
            Logger.LogDevCommand("SetLifetimeNutrients", amount.ToString());
            NotifyNutrientsChanged();
        }

        // Set value of available nutrients
        public void SetAvailableNutrients(double amount)
        {
            _availableNutrients = Math.Max(0, amount);
            Logger.LogDevCommand("SetAvailableNutrients", amount.ToString());
            NotifyNutrientsChanged();
        }

        // Set value of all nutrients
        public void SetNutrients(double amount)
        {
            _availableNutrients = Math.Max(0, amount);
            _lifetimeNutrients = Math.Max(0, amount);
            Logger.LogDevCommand("SetNutrients", amount.ToString());
            NotifyNutrientsChanged();
        }

        private void NotifyNutrientsChanged()
        {
            NutrientsChanged?.Invoke(_availableNutrients, _lifetimeNutrients, _nutrientsPerSecond);
        }

        public void SaveGame()
        {
            var saveGame = new Godot.Collections.Dictionary
            {
                ["version"] = GameVersion.VERSION,
                ["availableNutrients"] = _availableNutrients,
                ["lifetimeNutrients"] = _lifetimeNutrients,
                ["nutrientsPerSecond"] = _nutrientsPerSecond,
                ["timestamp"] = Time.GetUnixTimeFromSystem()
            };

            // Save generator data
            var generatorData = new Godot.Collections.Array();
            var mainUI = GetTree().GetFirstNodeInGroup("MainUI");
            if (mainUI != null)
            {
                var generators = mainUI.GetNode<VBoxContainer>("%GeneratorContainer").GetChildren();
                foreach (var child in generators)
                {
                    if (child is Generator generator)
                    {
                        generatorData.Add(generator.GetSaveData());
                    }
                }
            }
            saveGame["generators"] = generatorData;

            var saveFile = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Write);
            if (saveFile != null)
            {
                saveFile.StoreLine(Json.Stringify(saveGame));
                saveFile.Close();
                GD.Print("Game saved successfully");
            }
            else
            {
                GD.PrintErr("Failed to open save file for writing");
            }
        }
        
        public void ResetGame()
        {
            // Reset all game state to initial values
            _availableNutrients = 0;
            _lifetimeNutrients = 0;
            _nutrientsPerSecond = 0;
            _recentGains.Clear();
            _timeSinceLastSave = 0;

            // Delete save file
            if (FileAccess.FileExists("user://savegame.save"))
            {
                var file = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Write);
                if (file != null)
                {
                    file.Close();
                }
                OS.MoveToTrash(ProjectSettings.GlobalizePath("user://savegame.save"));
            }

            // Notify UI of changes
            NotifyNutrientsChanged();

            // Reset any generators
            var mainUI = GetTree().GetFirstNodeInGroup("MainUI");
            if (mainUI != null && mainUI.HasMethod("ResetGenerators"))
            {
                mainUI.Call("ResetGenerators");
            }

            Logger.Info("Game has been reset to initial state");
        }

        public void LoadGame()
        {
            if (!FileAccess.FileExists("user://savegame.save"))
            {
                GD.Print("No save file found, starting fresh");
                return;
            }

            var saveFile = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Read);
            if (saveFile == null)
            {
                GD.PrintErr("Failed to open save file for reading");
                return;
            }

            var jsonString = saveFile.GetLine();
            saveFile.Close();

            var json = new Json();
            var parseResult = json.Parse(jsonString);
            if (parseResult != Error.Ok)
            {
                GD.PrintErr("Failed to parse save file JSON");
                return;
            }

            var saveData = json.Data.AsGodotDictionary();

            // Load saved values with fallbacks
            _availableNutrients = saveData.GetValueOrDefault("availableNutrients", 0.0).AsDouble();
            _lifetimeNutrients = saveData.GetValueOrDefault("lifetimeNutrients", 0.0).AsDouble();
            _nutrientsPerSecond = saveData.GetValueOrDefault("nutrientsPerSecond", 0.0).AsDouble();

            // Calculate offline progress
            if (saveData.ContainsKey("timestamp"))
            {
                var savedTime = saveData["timestamp"].AsDouble();
                var currentTime = Time.GetUnixTimeFromSystem();
                var offlineTime = currentTime - savedTime;

                if (offlineTime > 0 && _nutrientsPerSecond > 0)
                {
                    // Cap offline time to prevent absurd gains (e.g., max 24 hours)
                    var cappedOfflineTime = Math.Min(offlineTime, 24 * 60 * 60);
                    var offlineEarnings = _nutrientsPerSecond * cappedOfflineTime;

                    if (offlineEarnings > 0)
                    {
                        AddNutrients(offlineEarnings, "offline");
                        var timeString = FormatTime(cappedOfflineTime);
                        Logger.LogOfflineProgress(offlineEarnings, cappedOfflineTime);
                        Logger.Info("Welcome back! You earned {0} nutrients while away for {1}.", FormatNumber(offlineEarnings), timeString);
                    }
                }
            }

            // Store generator data for loading after UI is ready
            if (saveData.ContainsKey("generators"))
            {
                _pendingGeneratorData = saveData["generators"].AsGodotArray();
            }

            Logger.LogGameLoad();
            GD.Print("Game loaded successfully");
        }

        // Add field to store generator data until UI is ready
        private Godot.Collections.Array _pendingGeneratorData;

        public Godot.Collections.Array GetPendingGeneratorData()
        {
            var data = _pendingGeneratorData;
            _pendingGeneratorData = null; // Clear after retrieval
            return data;
        }

        private string FormatTime(double seconds)
        {
            var hours = (int)(seconds / 3600);
            var minutes = (int)((seconds % 3600) / 60);
            
            if (hours > 0)
                return $"{hours}h {minutes}m";
            else if (minutes > 0)
                return $"{minutes}m";
            else
                return $"{(int)seconds}s";
        }

        private string FormatNumber(double number)
        {
            if (number < 1000) return number.ToString("F0");
            if (number < 1_000_000) return $"{number / 1000:F2}K";
            if (number < 1_000_000_000) return $"{number / 1_000_000:F2}M";
            if (number < 1_000_000_000_000) return $"{number / 1_000_000_000:F2}B";
            return $"{number / 1_000_000_000_000:F2}T";
        }

        public override void _ExitTree()
        {
            SaveGame();
        }
    }
}