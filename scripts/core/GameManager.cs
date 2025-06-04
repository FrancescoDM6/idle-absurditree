using Godot;
using IdleAbsurditree.Scripts.Gameplay;
using IdleAbsurditree.Scripts.Data;
using System;
using System.Collections.Generic;

namespace IdleAbsurditree.Scripts.Core
{
    [GlobalClass]
    public partial class GameManager : Node
    {

        //
        // Singleton
        //
        private static GameManager _instance;
        public static GameManager Instance => _instance;

        public override void _EnterTree()
        {
            // one instance
            if (_instance == null)
            {
                _instance = this;
                // prevent this from being freed on scene changes
                SetProcess(true);
            }
            else
            {
                // if second GameManager added, remove it
                QueueFree();
                return;
            }
        }

        // game data state
        private GameData _data;
        public GameData GameData => _data; 


        // Nutrient gain tracking for per-second calculation
        private Queue<(double timestamp, double amount)> _recentGains = new();
        private const double GAIN_TRACKING_WINDOW = 5.0; // Track gains over 5 seconds

        // Time tracking
        private double _autosaveTimer = 0;
        private const double AUTO_SAVE_INTERVAL = 30.0; // Auto-save every 30 seconds

        //
        // Events
        //
        public delegate void NutrientsChangedEventHandler(double newAvailable, double newLifetime, double newPerSecond);
        public event NutrientsChangedEventHandler NutrientsChanged;

        private void NotifyNutrientsChanged()
        {
            NutrientsChanged?.Invoke(
                _data.AvailableNutrients,
                _data.LifetimeNutrients,
                _data.NutrientsPerSecond
            );
        }      

        private const string SAVE_PATH = "user://savegame.tres"; 

        // private const string DEFAULT_DATA_PATH = "res://data/GameData.tres";
        // (Optional—only if you exported a .tres with default values and placed it under res://data/ )

        private const int GENERATOR_TYPE_COUNT = 2;

        public override void _Ready()
        {
            _instance = this;
            LoadGame();
            Logger.Info("Game Manager initialized. Version: {0}", GameVersion.VERSION);

            ProcessOfflineGains();

            // Trigger initial UI update
            NotifyNutrientsChanged();
        }

        public override void _Process(double delta)
        {
            // pasive nutrients (from generators for now)
            if (_data.AutoProductionPerSecond > 0)
            {
                AddNutrients(_data.AutoProductionPerSecond * delta, "generators");
            }
            
            // _data.AutoProductionPerSecond = CalculateGeneratorProduction();

            // Update nutrient per second calculation
            UpdateNutrientsPerSecondCalculation();

            // Handle auto-save
            _autosaveTimer += delta;
            if (_autosaveTimer >= AUTO_SAVE_INTERVAL)
            {
                SaveGame();
                Logger.LogAutosave();
                _autosaveTimer = 0;
            }
        }

        //legacy?
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

        // legacy?
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
            if (Math.Abs(newPerSecond - _data.NutrientsPerSecond) > 0.01)
            {
                _data.NutrientsPerSecond = newPerSecond;
                NotifyNutrientsChanged();
            }
        }

        public void AddNutrients(double amount, string source = "unknown")
        {
            if (amount <= 0) return;

            _data.AvailableNutrients += amount;
            _data.LifetimeNutrients += amount;

            // Track this gain for per-second calculation
            var currentTime = Time.GetUnixTimeFromSystem();
            _recentGains.Enqueue((currentTime, amount));

            Logger.LogNutrientGain(amount, source);
            NotifyNutrientsChanged();

            //save game after certain thresholds (probably not here, but maybe based on this sort of thing)
        }

        public bool SpendNutrients(double amount, string purpose = "unknown")
        {
            if (amount <= 0) return false;

            if (_data.AvailableNutrients >= amount)
            {
                _data.AvailableNutrients -= amount;
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

        public int GetGeneratorCount(int index)
        {
            if (index < 0 || index >= _data.GeneratorCounts.Count)
                return 0;
            return _data.GeneratorCounts[index];
        }

        public void SetGeneratorCount(int index, int newCount)
        {
            if (index < 0 || index >= _data.GeneratorCounts.Count)
                return;

            _data.GeneratorCounts[index] = newCount;
            UpdateProductionRate();
            NotifyNutrientsChanged();
        }

        public void IncrementGeneratorCount(int index)
        {
            if (index < 0 || index >= _data.GeneratorCounts.Count)
                return;

            _data.GeneratorCounts[index]++;
            // Optionally: recalc total production rate here, 
            // e.g. _data.NutrientsPerSecond = RecalculateProduction();
            UpdateProductionRate();
            NotifyNutrientsChanged();
        }

        public void SaveGame()
        {

            // Save generator data
            // var generatorData = new Godot.Collections.Array();
            var mainUI = GetTree().GetFirstNodeInGroup("MainUI");
            if (mainUI != null)
            {
                var generators = mainUI.GetNode<VBoxContainer>("%GeneratorContainer").GetChildren();
                // foreach (var child in generators)
                // {
                //     if (child is Generator generator)
                //     {
                //         generatorData.Add(generator.GetSaveData());
                //     }
                // }
                _data.InitializeGeneratorCounts(generators.Count);
                
                for (int i = 0; i < generators.Count; i++)
                {
                    if (generators[i] is Generator generator)
                    {
                        _data.GeneratorCounts[i] = generator.Count;
                    }
                }
            }
            // saveGame["generators"] = generatorData;
            _data.LastSaveTime = Time.GetUnixTimeFromSystem();

            Error error = ResourceSaver.Save(_data, SAVE_PATH);
            if (error == Error.Ok)
            {
                GD.Print($"Game saved to {SAVE_PATH}");
            }
            else
            {
                GD.PrintErr($"Failed to save game: {error}");
            }
        }

        public void ResetGame()
        {
            // Reset all game state to initial values in GameData
            _data.AvailableNutrients = 0;
            _data.LifetimeNutrients = 0;
            _data.NutrientsPerSecond = 0;
            _data.LastSaveTime = 0;

            _data.GeneratorCounts.Clear(); // Clear current counts
            _data.InitializeGeneratorCounts(GENERATOR_TYPE_COUNT); // Re-initialize for a fresh game

            // Also clear _recentGains if it were still being used, but it's removed now.
            // _recentGains.Clear();

            // Delete save file
            if (FileAccess.FileExists(SAVE_PATH))
            {
                OS.MoveToTrash(ProjectSettings.GlobalizePath(SAVE_PATH));
            }

            // Reset any generators by having MainUI re-initialize them
            var mainUI = GetTree().GetFirstNodeInGroup("MainUI");
            if (mainUI != null && mainUI.HasMethod("ResetGenerators"))
            {
                mainUI.Call("ResetGenerators");
            }

            // Save the reset state
            NotifyNutrientsChanged();
            SaveGame();

            Logger.Info("Game has been reset to initial state");
        }

        public void LoadGame()
        {
            if (ResourceLoader.Exists(SAVE_PATH))
            {
                var loadedData = GD.Load<GameData>(SAVE_PATH);
                if (loadedData != null)
                {
                    _data = loadedData;
                    Logger.LogGameLoad();
                    GD.Print("Game loaded successfully");
                }
                else
                {
                    // Fallback if loading fails for some reason
                    GD.PrintErr($"Failed to load GameData from {SAVE_PATH}. Creating new data.");
                    _data = new GameData();
                    _data.InitializeGeneratorCounts(GENERATOR_TYPE_COUNT); // Initialize for new game
                }
            }
            else
            {
                GD.Print("No save file found, starting fresh");
                _data = new GameData();
                _data.InitializeGeneratorCounts(GENERATOR_TYPE_COUNT); // Initialize for new game
            }
        }

        private void ProcessOfflineGains()
        {
            // Calculate offline progress
            var savedTime = _data.LastSaveTime;
            var currentTime = Time.GetUnixTimeFromSystem();
            var offlineTime = currentTime - savedTime;

            if (offlineTime > 0 && _data.AutoProductionPerSecond > 0)
            {
                // cap offline time (e.g. 24 hours)
                var cappedOfflineTime = Math.Min(offlineTime, 24 * 60 * 60);
                var offlineEarnings = _data.NutrientsPerSecond * cappedOfflineTime;

                if (offlineEarnings > 0)
                {
                    AddNutrients(offlineEarnings, "offline");
                    var timeString = FormatTime(cappedOfflineTime);
                    Logger.LogOfflineProgress(offlineEarnings, cappedOfflineTime);
                    Logger.Info(
                        "Welcome back! You earned {0} nutrients while away for {1}.",
                        FormatNumber(offlineEarnings), timeString
                    );
                    SaveGame();
                }
            }

            _data.LastSaveTime = currentTime;
        }

        // Calculates total production based on generators
        private double RecalculateTotalProduction()
        {
            // e.g. if generator type 0 produces 0.1/sec and type 1 produces 1.0/sec:
            // double production = GetGeneratorCount(0) * 0.1 + GetGeneratorCount(1) * 1.0;
            // You could store those base‐values in constants or pull from a small table.
            // For now, just return whatever the old code did.

            double total = 0.0;
            // Example: assume two generator types with base production 0.1 and 1.0:
            // You might want to make these base production values configurable (e.g., in a separate data resource)
            // For now, hardcode them based on the generator order in MainUI
            // if (_data.GeneratorCounts.Count > 0)
            // {
            //     total += GetGeneratorCount(0) * 1.0; // Assuming Root Sprout from MainUI
            // }
            // if (_data.GeneratorCounts.Count > 1)
            // {
            //     total += GetGeneratorCount(1) * 5.0; // Assuming Leaf Cluster from MainUI
            // }
            return total;
        }

        /// Call this whenever a generator is bought or sold to keep NutrientsPerSecond up to date.
        public void UpdateProductionRate()
        {
            _data.AutoProductionPerSecond = RecalculateTotalProduction();
            NotifyNutrientsChanged();
        }

        //
        // Dev methods ONLY
        //

        // Clear value of lifetime nutrients
        public void ClearLifetimeNutrients()
        {
            _data.LifetimeNutrients = 0;
            Logger.LogDevCommand("ClearLifetimeNutrients");
            NotifyNutrientsChanged();
        }

        // Clear value of available nutrients
        public void ClearAvailableNutrients()
        {
            _data.AvailableNutrients = 0;
            Logger.LogDevCommand("ClearAvailableNutrients");
            NotifyNutrientsChanged();
        }

        // Clear value of all nutrients
        public void ClearAllNutrients()
        {
            _data.AvailableNutrients = 0;
            _data.LifetimeNutrients = 0;
            Logger.LogDevCommand("ClearAllNutrients");
            NotifyNutrientsChanged();
        }

        // Set value of lifetime nutrients
        public void SetLifetimeNutrients(double amount)
        {
            _data.LifetimeNutrients = Math.Max(0, amount);
            Logger.LogDevCommand("SetLifetimeNutrients", amount.ToString());
            NotifyNutrientsChanged();
        }

        // Set value of available nutrients
        public void SetAvailableNutrients(double amount)
        {
            _data.AvailableNutrients = Math.Max(0, amount);
            Logger.LogDevCommand("SetAvailableNutrients", amount.ToString());
            NotifyNutrientsChanged();
        }

        // Set value of all nutrients
        public void SetNutrients(double amount)
        {
            _data.AvailableNutrients = Math.Max(0, amount);
            _data.LifetimeNutrients = Math.Max(0, amount);
            Logger.LogDevCommand("SetNutrients", amount.ToString());
            NotifyNutrientsChanged();
        }


        //
        // util methods (replace with functions in utils folder?)
        //

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