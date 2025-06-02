using Godot;
using System;
using IdleAbsurditree.Scripts.Core;
using IdleAbsurditree.Scripts.UI;

namespace IdleAbsurditree.Scripts.Gameplay
{
    public partial class Generator : Control
    {
        // Generator properties
        public string GeneratorName { get; private set; }
        public double BaseCost { get; private set; }
        public double BaseProduction { get; private set; }
        public int Count { get; private set; } = 0;

        // Cost scaling
        private const double COST_MULTIPLIER = 1.15;

        // UI References
        private Label _nameLabel;
        private Label _countLabel;
        private Label _productionLabel;
        private Label _costLabel;
        private Button _buyButton;

        public double CurrentCost => BaseCost * Math.Pow(COST_MULTIPLIER, Count);
        public double CurrentProduction => BaseProduction * Count;

        public void Initialize(string name, double baseCost, double baseProduction)
        {
            GeneratorName = name;
            BaseCost = baseCost;
            BaseProduction = baseProduction;

            CreateUI();
            UpdateUI();
        }

        private void CreateUI()
        {
            // Create the generator UI panel
            var panel = new PanelContainer();
            AddChild(panel);

            var hbox = new HBoxContainer();
            panel.AddChild(hbox);

            // Left side - Info
            var leftVBox = new VBoxContainer();
            leftVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            hbox.AddChild(leftVBox);

            _nameLabel = new Label();
            _nameLabel.Text = GeneratorName;
            _nameLabel.AddThemeFontSizeOverride("font_size", 16);
            leftVBox.AddChild(_nameLabel);

            _countLabel = new Label();
            leftVBox.AddChild(_countLabel);

            _productionLabel = new Label();
            leftVBox.AddChild(_productionLabel);

            // Right side - Purchase
            var rightVBox = new VBoxContainer();
            hbox.AddChild(rightVBox);

            _costLabel = new Label();
            _costLabel.HorizontalAlignment = HorizontalAlignment.Center;
            rightVBox.AddChild(_costLabel);

            _buyButton = new Button();
            _buyButton.Text = "Buy";
            _buyButton.CustomMinimumSize = new Vector2(80, 40);
            _buyButton.Pressed += OnBuyPressed;
            rightVBox.AddChild(_buyButton);
        }

        private void OnBuyPressed()
        {
            var cost = CurrentCost;
            if (GameManager.Instance != null && GameManager.Instance.SpendNutrients(cost, $"generator_{GeneratorName}"))
            {
                Count++;
                Logger.LogGeneratorPurchase(GeneratorName, Count, cost);
                UpdateUI();

                // Notify the UI to recalculate total production
                var mainUI = GetTree().GetFirstNodeInGroup("MainUI") as MainUI;
                mainUI?.CallDeferred("UpdateGeneratorProduction");
            }
        }

        private void UpdateGameProduction()
        {
            // This would be better handled by a GeneratorManager, but for now we'll calculate total here
            // In a real implementation, you'd want a centralized system to manage all generators
            if (GameManager.Instance != null)
            {
                // For this simple demo, we'll just set it directly
                // In practice, you'd sum all generators' production
                GameManager.Instance.UpdateNutrientsPerSecond(CurrentProduction);
            }
        }

        private void UpdateUI()
        {
            if (_countLabel != null)
                _countLabel.Text = $"Owned: {Count}";

            if (_productionLabel != null)
                _productionLabel.Text = $"Production: {FormatNumber(CurrentProduction)}/sec";

            if (_costLabel != null)
                _costLabel.Text = $"Cost: {FormatNumber(CurrentCost)}";

            if (_buyButton != null)
            {
                var canAfford = GameManager.Instance?.AvailableNutrients >= CurrentCost;
                _buyButton.Disabled = !canAfford;
            }
        }

        public override void _Process(double delta)
        {
            // Update UI every frame to reflect current affordability
            if (_buyButton != null && GameManager.Instance != null)
            {
                var canAfford = GameManager.Instance.AvailableNutrients >= CurrentCost;
                _buyButton.Disabled = !canAfford;
            }
        }

        private string FormatNumber(double number)
        {
            if (number < 1000) return number.ToString("F0");
            if (number < 1_000_000) return $"{number / 1000:F2}K";
            if (number < 1_000_000_000) return $"{number / 1_000_000:F2}M";
            if (number < 1_000_000_000_000) return $"{number / 1_000_000_000:F2}B";
            return $"{number / 1_000_000_000_000:F2}T";
        }

        // Save/Load methods for persistence
        public Godot.Collections.Dictionary GetSaveData()
        {
            return new Godot.Collections.Dictionary
            {
                ["name"] = GeneratorName,
                ["baseCost"] = BaseCost,
                ["baseProduction"] = BaseProduction,
                ["count"] = Count
            };
        }

        public void LoadSaveData(Godot.Collections.Dictionary data)
        {
            Count = data["count"].AsInt32();
            UpdateUI();
        }
    }
}