using Godot;
using System;
using IdleAbsurditree.Scripts.Core;

namespace IdleAbsurditree.Scripts.UI
{
    public partial class MainUI : Control
    {
        // UI References
        private Label _lifetimeCurrencyLabel;
        private Label _availableCurrencyLabel;
        private Button _clickButton;
        private VBoxContainer _generatorContainer;

        // Click values
        private double _baseClickValue = 1.0;
        private double _clickMultiplier = 1.0;

        public override void _Ready()
        {
            // Get UI references
            _lifetimeCurrencyLabel = GetNode<Label>("%LifeCurrencyLabel");
            _availableCurrencyLabel = GetNode<Label>("%AvailableCurrencyLabel");
            _clickButton = GetNode<Button>("%ClickButton");
            _generatorContainer = GetNode<VBoxContainer>("%GeneratorContainer");

            // Connect signals
            _clickButton.Pressed += OnClickButtonPressed;
            
            // Subscribe to game events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CurrencyChanged += OnCurrencyChanged;
            }

            // Initial UI update
            UpdateUI();
        }

        private void OnClickButtonPressed()
        {
            var clickValue = _baseClickValue * _clickMultiplier;
            GameManager.Instance?.AddCurrency(clickValue);
            
            // Add a little visual feedback
            CreateFloatingText($"+{clickValue:F0}", _clickButton.GlobalPosition);
        }

        private void OnCurrencyChanged(double newAmount, double change)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (GameManager.Instance == null) return;

            _lifetimeCurrencyLabel.Text = $"Absurdity: {FormatNumber(GameManager.Instance.Currency)}";
            _availableCurrencyLabel.Text = $"per second: {FormatNumber(GameManager.Instance.CurrencyPerSecond)}";
        }

        private void CreateFloatingText(string text, Vector2 position)
        {
            var floatingLabel = new Label();
            floatingLabel.Text = text;
            floatingLabel.Position = position;
            floatingLabel.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
            
            // Style the floating text
            floatingLabel.AddThemeFontSizeOverride("font_size", 24);
            floatingLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0)); // Yellow
            
            GetTree().Root.AddChild(floatingLabel);

            // Animate the floating text
            var tween = GetTree().CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(floatingLabel, "position", position + Vector2.Up * 50, 1.0f);
            tween.TweenProperty(floatingLabel, "modulate:a", 0.0f, 1.0f);
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
                GameManager.Instance.CurrencyChanged -= OnCurrencyChanged;
            }
        }
    }
}