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
        private List<Generator> _generators = new List<Generator>();

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
            UpdateUI();
        }

        private void InitializeGenerators()
        {
            // Create a basic generator for demonstration
            var basicGenerator = new Generator();
            basicGenerator.Initialize("Root Sprout", 10, 1);
            _generatorContainer.AddChild(basicGenerator);
            _generators.Add(basicGenerator);

            // Add a second generator for more interesting gameplay
            var leafGenerator = new Generator();
            leafGenerator.Initialize("Leaf Cluster", 100, 5);
            _generatorContainer.AddChild(leafGenerator);
            _generators.Add(leafGenerator);

            // Load saved generator data if available
            LoadGeneratorData();

            // Update initial production
            UpdateGeneratorProduction();
        }

        private void LoadGeneratorData()
        {
            if (GameManager.Instance == null) return;

            var generatorData = GameManager.Instance.GetPendingGeneratorData();
            if (generatorData == null) return;

            for (int i = 0; i < generatorData.Count && i < _generators.Count; i++)
            {
                var data = generatorData[i].AsGodotDictionary();
                _generators[i].LoadSaveData(data);
            }
        }

        public void UpdateGeneratorProduction()
        {
            double totalProduction = 0;
            foreach (var generator in _generators)
            {
                totalProduction += generator.CurrentProduction;
            }

            // No longer need to update GameManager's nutrientsPerSecond
            // The per-second display now tracks all gains automatically
        }

        // Method for GameManager to get generator production
        public double GetGeneratorProduction()
        {
            double totalProduction = 0;
            foreach (var generator in _generators)
            {
                totalProduction += generator.CurrentProduction;
            }
            return totalProduction;
        }

        private void OnClickButtonPressed()
        {
            var clickValue = _baseClickValue * _clickMultiplier;
            GameManager.Instance?.AddNutrients(clickValue, "manual_click");

            // Add visual feedback
            CreateFloatingText($"+{FormatNumber(clickValue)}", _clickButton.GlobalPosition);
        }

        private void OnNutrientsChanged(double available, double lifetime, double perSecond)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (GameManager.Instance == null) return;

            _lifetimeNutrientsLabel.Text = $"Lifetime Nutrients: {FormatNumber(GameManager.Instance.LifetimeNutrients)}";
            _availableNutrientsLabel.Text = $"Available Nutrients: {FormatNumber(GameManager.Instance.AvailableNutrients)}";
            _nutrientsPerSecondLabel.Text = $"Nutrients per Second: {FormatNumber(GameManager.Instance.NutrientsPerSecond)}";
        }

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