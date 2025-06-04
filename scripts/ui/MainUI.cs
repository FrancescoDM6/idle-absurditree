using Godot;
using System;
using System.Collections.Generic;
using IdleAbsurditree.Scripts.Core;
using IdleAbsurditree.Scripts.Gameplay;

namespace IdleAbsurditree.Scripts.UI
{
    public partial class MainUI : Control
    {
        // UI References
        private Label _lifetimeNutrientsLabel;
        private Label _availableNutrientsLabel;
        private Label _nutrientsPerSecondLabel;
        private Button _clickButton;
        private VBoxContainer _generatorContainer;

        // Generators
        private List<Generator> _generators = new();

        // Click values
        private double _baseClickValue = 1.0;
        private double _clickMultiplier = 1.0;

        public override void _Ready()
        {
            // Add this UI to a group so generators can find it
            AddToGroup("MainUI");

            // Get UI references
            _lifetimeNutrientsLabel = GetNode<Label>("%LifetimeNutrientsLabel");
            _availableNutrientsLabel = GetNode<Label>("%AvailableNutrientsLabel");
            _nutrientsPerSecondLabel = GetNode<Label>("%NutrientsPerSecondLabel");
            _clickButton = GetNode<Button>("%ClickButton");
            _generatorContainer = GetNode<VBoxContainer>("%GeneratorContainer");

            // Connect signals
            _clickButton.Pressed += OnClickButtonPressed;

            // Subscribe to game events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NutrientsChanged += OnNutrientsChanged;
            }

            // Initialize generators
            InitializeGenerators();

            // Initial UI update
            //UpdateUI();
        }

        private void InitializeGenerators()
        {
            // Clear any existing generators in the UI
            foreach (Node child in _generatorContainer.GetChildren())
            {
                child.QueueFree();
            }
            _generators.Clear();

            if (GameManager.Instance != null)
            {
                // Create generators based on the count initialized in GameData
                // The actual baseCost and baseProduction should probably come from a separate data definition
                // For this example, we'll hardcode them matching GameManager.RecalculateTotalBaseProduction
                var generatorDefs = new (string name, double baseCost, double baseProduction)[]
                {
                    ("Root Sprout", 10, 1.0), // Matches index 0 in GameManager.RecalculateTotalBaseProduction
                    ("Leaf Cluster", 100, 5.0) // Matches index 1 in GameManager.RecalculateTotalBaseProduction
                };

                // Create generator UI elements
                for (int i = 0; i < generatorDefs.Length; i++)
                {
                    var def = generatorDefs[i];
                    var generator = new Generator();
                    generator.Initialize(def.name, def.baseCost, def.baseProduction);
                    _generatorContainer.AddChild(generator);
                    _generators.Add(generator);
                }

             
                var counts = GameManager.Instance.GameData.GeneratorCounts;
                for (int i = 0; i < _generators.Count; i++)
                {
                    int savedCount = (i < counts.Count) ? counts[i] : 0;
                    _generators[i].LoadSaveData(
                        new Godot.Collections.Dictionary { ["count"] = savedCount }
                    );
                }
                
            }

            UpdateInitialGeneratorPrices(); // Ensure prices are correct after initialization
        }

        //not ui focused
        // public void UpdateGeneratorProduction()
        // {
        //     double totalProduction = 0.0;
        //     for (int i = 0; i < _generators.Count; i++)
        //     {
        //         var gen = _generators[i];
        //         totalProduction += gen.CurrentProduction;

        //         // Also sync the count back into GameData.GeneratorCounts
        //         // so SaveGame() already has the correct array.
        //         GameManager.Instance.SetGeneratorCount(i, gen.Count);
        //     }

        //     // Inform GameManager of the up-to-date auto production value
        //     GameManager.Instance.GameData.AutoProductionPerSecond = totalProduction;
        // }

        // private void LoadGeneratorData()
        // {
        //     if (GameManager.Instance == null) return;

        //     var generatorData = GameManager.Instance.GetPendingGeneratorData();
        //     if (generatorData == null) return;

        //     for (int i = 0; i < generatorData.Count && i < _generators.Count; i++)
        //     {
        //         var data = generatorData[i].AsGodotDictionary();
        //         _generators[i].LoadSaveData(data);
        //     }
        // }

        public void ResetGenerators()
        {
            InitializeGenerators();
        }

        private void OnClickButtonPressed()
        {
            var clickValue = _baseClickValue * _clickMultiplier;
            GameManager.Instance?.AddNutrients(clickValue, "manual_click");

            // Add visual feedback
            CreateFloatingText($"+{FormatNumber(clickValue)}", _clickButton.GlobalPosition);
        }

        private void OnNutrientsChanged(double available, double lifetime, double perSecondDisplay)
        {
            UpdateUI(available, lifetime, perSecondDisplay);
        }

        private void UpdateUI(double available, double lifetime, double perSecondDisplay)
        {
            _lifetimeNutrientsLabel.Text = $"Lifetime Nutrients: {FormatNumber(lifetime)}";
            _availableNutrientsLabel.Text = $"Available Nutrients: {FormatNumber(available)}";
            _nutrientsPerSecondLabel.Text = $"Nutrients per Second: {FormatNumber(perSecondDisplay)}"; // Use the display rate
        }

        private void UpdateInitialGeneratorPrices()
        {
            foreach (var generator in _generators)
            {
                generator.CallDeferred("UpdateUI"); // Call deferred to ensure it runs after UI is ready
            }
        }

        //
        //utils (probably replace with functionalitiers from utils folder later)
        //

        private void CreateFloatingText(string text, Vector2 position)
        {
            var floatingLabel = new Label();
            floatingLabel.Text = text;
            floatingLabel.Position = position + new Vector2(-50, -30); // Offset from button
            floatingLabel.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());

            // Style the floating text
            floatingLabel.AddThemeFontSizeOverride("font_size", 20);
            floatingLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.8f, 0.4f)); // Green

            GetTree().Root.AddChild(floatingLabel);

            // Animate the floating text
            var tween = GetTree().CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(floatingLabel, "position", position + Vector2.Up * 80 + new Vector2(-50, -30), 1.5f);
            tween.TweenProperty(floatingLabel, "modulate:a", 0.0f, 1.5f);
            tween.Chain().TweenCallback(Callable.From(() => floatingLabel.QueueFree()));
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
            // Unsubscribe from events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NutrientsChanged -= OnNutrientsChanged;
            }
        }
    }
}