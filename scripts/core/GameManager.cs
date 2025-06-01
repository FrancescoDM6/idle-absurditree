using Godot;
using System;

namespace IdleAbsurditree.Scripts.Core
{
    public partial class GameManager : Node
    {
        private static GameManager _instance;
        public static GameManager Instance => _instance;

        // Core game state
        private double _currency = 0;
        private double _currencyPerSecond = 0;
        private double _totalCurrencyEarned = 0;
        
        // Time tracking
        private double _timeSinceLastSave = 0;
        private const double AUTO_SAVE_INTERVAL = 30.0; // Auto-save every 30 seconds

        // Events
        public delegate void CurrencyChangedEventHandler(double newAmount, double change);
        public event CurrencyChangedEventHandler CurrencyChanged;

        public double Currency => _currency;
        public double CurrencyPerSecond => _currencyPerSecond;

        public override void _Ready()
        {
            _instance = this;
            LoadGame();
            GD.Print($"Game Manager initialized. Version: {GameVersion.VERSION}");
        }

        public override void _Process(double delta)
        {
            // Update currency based on per-second rate
            if (_currencyPerSecond > 0)
            {
                AddCurrency(_currencyPerSecond * delta);
            }

            // Handle auto-save
            _timeSinceLastSave += delta;
            if (_timeSinceLastSave >= AUTO_SAVE_INTERVAL)
            {
                SaveGame();
                _timeSinceLastSave = 0;
            }
        }

        public void AddCurrency(double amount)
        {
            var oldAmount = _currency;
            _currency += amount;
            _totalCurrencyEarned += Math.Max(0, amount);
            
            CurrencyChanged?.Invoke(_currency, amount);
        }

        public bool SpendCurrency(double amount)
        {
            if (_currency >= amount)
            {
                _currency -= amount;
                CurrencyChanged?.Invoke(_currency, -amount);
                return true;
            }
            return false;
        }

        public void UpdateCurrencyPerSecond(double amount)
        {
            _currencyPerSecond = amount;
        }

        public void SaveGame()
        {
            var saveGame = new Godot.Collections.Dictionary
            {
                ["version"] = GameVersion.VERSION,
                ["currency"] = _currency,
                ["currencyPerSecond"] = _currencyPerSecond,
                ["totalCurrencyEarned"] = _totalCurrencyEarned,
                ["timestamp"] = Time.GetUnixTimeFromSystem()
            };

            var saveFile = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Write);
            if (saveFile != null)
            {
                saveFile.StoreLine(Json.Stringify(saveGame));
                saveFile.Close();
                GD.Print("Game saved successfully");
            }
        }

        public void LoadGame()
        {
            if (!FileAccess.FileExists("user://savegame.save"))
            {
                GD.Print("No save file found, starting fresh");
                return;
            }

            var saveFile = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Read);
            if (saveFile != null)
            {
                var jsonString = saveFile.GetLine();
                saveFile.Close();

                var json = new Json();
                var parseResult = json.Parse(jsonString);
                if (parseResult == Error.Ok)
                {
                    var saveData = json.Data.AsGodotDictionary();
                    
                    // Load saved values
                    _currency = saveData["currency"].AsDouble();
                    _currencyPerSecond = saveData["currencyPerSecond"].AsDouble();
                    _totalCurrencyEarned = saveData["totalCurrencyEarned"].AsDouble();
                    
                    // Calculate offline progress
                    if (saveData.ContainsKey("timestamp"))
                    {
                        var savedTime = saveData["timestamp"].AsDouble();
                        var currentTime = Time.GetUnixTimeFromSystem();
                        var offlineTime = currentTime - savedTime;
                        
                        if (offlineTime > 0 && _currencyPerSecond > 0)
                        {
                            var offlineEarnings = _currencyPerSecond * offlineTime;
                            AddCurrency(offlineEarnings);
                            GD.Print($"Welcome back! You earned {offlineEarnings:F2} while away.");
                        }
                    }
                    
                    GD.Print("Game loaded successfully");
                }
            }
        }

        public override void _ExitTree()
        {
            SaveGame();
        }
    }
}