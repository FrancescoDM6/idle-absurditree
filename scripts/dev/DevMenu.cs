using Godot;
using System;
using IdleAbsurditree.Scripts.Core;
using IdleAbsurditree.Scripts.Data;

namespace IdleAbsurditree.Scripts.Dev
{
    public partial class DevMenu : Control
    {
        private bool _isVisible = false;
        private VBoxContainer _menuContainer;

        // Input fields
        private LineEdit _nutrientAmountInput;
        private LineEdit _productionRateInput;

        // Buttons
        private Button _addNutrientsButton;
        private Button _setNutrientsButton;
        private Button _clearNutrientsButton;
        private Button _setProductionButton;
        private Button _saveGameButton;
        private Button _loadGameButton;
        private Button _resetGameButton;

        private OptionButton _nutrientTargetDropdown;

        private enum NutrientTarget
        {
            Available = 0,
            Lifetime = 1,
            Both = 2
        }

        public override void _Ready()
        {
            CreateDevMenu();
            SetVisible(false);
        }

        public override void _Input(InputEvent @event)
        {
            // Toggle dev menu with F12 or Ctrl+D
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                if (keyEvent.Keycode == Key.F12 ||
                    (keyEvent.Keycode == Key.D && keyEvent.CtrlPressed))
                {
                    ToggleDevMenu();
                }
            }
        }

        private void CreateDevMenu()
        {
            // Create semi-transparent background
            var background = new ColorRect();
            background.Color = new Color(0, 0, 0, 0.7f);
            background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            AddChild(background);

            // Create centered container
            var centerContainer = new CenterContainer();
            centerContainer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            AddChild(centerContainer);

            // Create main panel
            var panel = new PanelContainer();
            panel.CustomMinimumSize = new Vector2(400, 500);
            centerContainer.AddChild(panel);

            // Create main container
            _menuContainer = new VBoxContainer();
            _menuContainer.AddThemeConstantOverride("separation", 10);
            panel.AddChild(_menuContainer);

            // Add title
            var title = new Label();
            title.Text = "Developer Menu";
            title.AddThemeFontSizeOverride("font_size", 24);
            title.HorizontalAlignment = HorizontalAlignment.Center;
            _menuContainer.AddChild(title);

            var separator = new HSeparator();
            _menuContainer.AddChild(separator);

            // Create nutrient manipulation section
            CreateNutrientSection();

            // Create game control section
            CreateGameControlSection();

            // Add close button
            var closeButton = new Button();
            closeButton.Text = "Close (F12)";
            closeButton.Pressed += () => ToggleDevMenu();
            _menuContainer.AddChild(closeButton);
        }

        private void CreateNutrientSection()
        {
            var nutrientLabel = new Label();
            nutrientLabel.Text = "Nutrient Manipulation";
            nutrientLabel.AddThemeFontSizeOverride("font_size", 18);
            _menuContainer.AddChild(nutrientLabel);

            // HBox for "target" dropdown + amount
            var targetAndInputContainer = new HBoxContainer();
            _menuContainer.AddChild(targetAndInputContainer);

            // 1) OptionButton for which pool to affect
            _nutrientTargetDropdown = new OptionButton();
            _nutrientTargetDropdown.AddItem("Available", (int)NutrientTarget.Available);
            _nutrientTargetDropdown.AddItem("Lifetime", (int)NutrientTarget.Lifetime);
            _nutrientTargetDropdown.AddItem("Both", (int)NutrientTarget.Both);
            _nutrientTargetDropdown.Select((int)NutrientTarget.Both); // default to "Both"
            targetAndInputContainer.AddChild(_nutrientTargetDropdown);

            // 2) Label + LineEdit for amount
            var inputLabel = new Label();
            inputLabel.Text = "Amount:";
            inputLabel.CustomMinimumSize = new Vector2(80, 0);
            targetAndInputContainer.AddChild(inputLabel);

            _nutrientAmountInput = new LineEdit();
            _nutrientAmountInput.PlaceholderText = "Enter amount";
            _nutrientAmountInput.Text = "1000";
            targetAndInputContainer.AddChild(_nutrientAmountInput);

            // Buttons: Add / Set / Clear
            var buttonContainer = new HBoxContainer();
            _menuContainer.AddChild(buttonContainer);

            _addNutrientsButton = new Button();
            _addNutrientsButton.Text = "Add";
            _addNutrientsButton.Pressed += OnAddNutrients;
            buttonContainer.AddChild(_addNutrientsButton);

            _setNutrientsButton = new Button();
            _setNutrientsButton.Text = "Set";
            _setNutrientsButton.Pressed += OnSetNutrients;
            buttonContainer.AddChild(_setNutrientsButton);

            _clearNutrientsButton = new Button();
            _clearNutrientsButton.Text = "Clear";
            _clearNutrientsButton.Pressed += OnClearNutrients;
            buttonContainer.AddChild(_clearNutrientsButton);

            // Production rate section
            var productionContainer = new HBoxContainer();
            _menuContainer.AddChild(productionContainer);

            var productionLabel = new Label();
            productionLabel.Text = "Rate/sec:";
            productionLabel.CustomMinimumSize = new Vector2(80, 0);
            productionContainer.AddChild(productionLabel);

            _productionRateInput = new LineEdit();
            _productionRateInput.PlaceholderText = "Production rate";
            _productionRateInput.Text = "10";
            productionContainer.AddChild(_productionRateInput);

            _setProductionButton = new Button();
            _setProductionButton.Text = "Set Production";
            _setProductionButton.Pressed += OnSetProduction;
            productionContainer.AddChild(_setProductionButton);

            _menuContainer.AddChild(new HSeparator());
        }

        private void CreateGameControlSection()
        {
            var gameLabel = new Label();
            gameLabel.Text = "Game Controls";
            gameLabel.AddThemeFontSizeOverride("font_size", 18);
            _menuContainer.AddChild(gameLabel);

            var gameButtonContainer = new VBoxContainer();
            _menuContainer.AddChild(gameButtonContainer);

            _saveGameButton = new Button();
            _saveGameButton.Text = "Force Save";
            _saveGameButton.Pressed += OnForceSave;
            gameButtonContainer.AddChild(_saveGameButton);

            _loadGameButton = new Button();
            _loadGameButton.Text = "Force Load";
            _loadGameButton.Pressed += OnForceLoad;
            gameButtonContainer.AddChild(_loadGameButton);

            _resetGameButton = new Button();
            _resetGameButton.Text = "Reset Game (Danger!)";
            _resetGameButton.Pressed += OnResetGame;
            gameButtonContainer.AddChild(_resetGameButton);

            // Add current game state display
            var stateLabel = new Label();
            stateLabel.Text = "Current State:";
            stateLabel.AddThemeFontSizeOverride("font_size", 14);
            _menuContainer.AddChild(stateLabel);

            var stateInfo = new RichTextLabel();
            stateInfo.CustomMinimumSize = new Vector2(0, 80);
            stateInfo.FitContent = true;
            _menuContainer.AddChild(stateInfo);

            // Update state info periodically
            var timer = new Timer();
            timer.WaitTime = 0.5f;
            timer.Timeout += () => UpdateStateInfo(stateInfo);
            timer.Autostart = true;
            AddChild(timer);
        }

        private void UpdateStateInfo(RichTextLabel stateInfo)
        {
            if (GameManager.Instance == null)
            {
                stateInfo.Text = "GameManager not available";
                return;
            }

            var info = $"Available: {FormatNumber(GameManager.Instance.GameData.AvailableNutrients)}\n";
            info += $"Lifetime: {FormatNumber(GameManager.Instance.GameData.LifetimeNutrients)}\n";
            info += $"Per Second: {FormatNumber(GameManager.Instance.GameData.NutrientsPerSecond)}";

            stateInfo.Text = info;
        }

        private void ToggleDevMenu()
        {
            _isVisible = !_isVisible;
            SetVisible(_isVisible);

            if (_isVisible)
            {
                Logger.LogDevCommand("DevMenu opened");
                //show current per second production
                _productionRateInput.Text = GameManager.Instance.GameData.AutoProductionPerSecond.ToString("F0");
            }
        }

        private void OnAddNutrients()
        {
            if (!double.TryParse(_nutrientAmountInput.Text, out double amount))
            {
                GD.PrintErr("Invalid amount entered");
                return;
            }

            var target = (NutrientTarget)_nutrientTargetDropdown.Selected;
            var gameManager = GameManager.Instance;

            if (gameManager == null)
            {
                GD.PrintErr("GameManager not available");
                return;
            }

            switch (target)
            {
                case NutrientTarget.Available:
                    gameManager.AddNutrients(amount, "dev_add_available");
                    Logger.LogDevCommand("AddNutrients", $"target: Available, amount: {amount}");
                    break;
                
                case NutrientTarget.Lifetime:
                    // Add to lifetime only by manipulating it directly
                    var newLifetime = gameManager.GameData.LifetimeNutrients + amount;
                    gameManager.SetLifetimeNutrients(newLifetime);
                    Logger.LogDevCommand("AddNutrients", $"target: Lifetime, amount: {amount}");
                    break;
                
                case NutrientTarget.Both:
                    // Add to available (which also adds to lifetime)
                    gameManager.AddNutrients(amount, "dev_add_both");
                    Logger.LogDevCommand("AddNutrients", $"target: Both, amount: {amount}");
                    break;
            }
        }
        
        private void OnSetNutrients()
        {
            if (!double.TryParse(_nutrientAmountInput.Text, out double amount))
            {
                GD.PrintErr("Invalid amount entered");
                return;
            }

            var target = (NutrientTarget)_nutrientTargetDropdown.Selected;
            var gameManager = GameManager.Instance;

            if (gameManager == null)
            {
                GD.PrintErr("GameManager not available");
                return;
            }

            switch (target)
            {
                case NutrientTarget.Available:
                    gameManager.SetAvailableNutrients(amount);
                    Logger.LogDevCommand("SetNutrients", $"target: Available, amount: {amount}");
                    break;

                case NutrientTarget.Lifetime:
                    gameManager.SetLifetimeNutrients(amount);
                    Logger.LogDevCommand("SetNutrients", $"target: Lifetime, amount: {amount}");
                    break;

                case NutrientTarget.Both:
                    gameManager.SetNutrients(amount);
                    Logger.LogDevCommand("SetNutrients", $"target: Both, amount: {amount}");
                    break;
            }
        }

        private void OnClearNutrients()
        {
            var target = (NutrientTarget)_nutrientTargetDropdown.Selected;
            var gameManager = GameManager.Instance;

            if (gameManager == null)
            {
                GD.PrintErr("GameManager not available");
                return;
            }

            switch (target)
            {
                case NutrientTarget.Available:
                    gameManager.ClearAvailableNutrients();
                    Logger.LogDevCommand("ClearNutrients", "target: Available");
                    break;
                
                case NutrientTarget.Lifetime:
                    gameManager.ClearLifetimeNutrients();
                    Logger.LogDevCommand("ClearNutrients", "target: Lifetime");
                    break;
                
                case NutrientTarget.Both:
                    gameManager.ClearAllNutrients();
                    Logger.LogDevCommand("ClearNutrients", "target: Both");
                    break;
            }
        }

        private void OnSetProduction()
        {
            if (double.TryParse(_productionRateInput.Text, out double rate))
            {
                // Set the BaseNutrientsPerSecond in GameData
                GameManager.Instance.GameData.AutoProductionPerSecond = rate;
                GameManager.Instance.UpdateProductionRate(); // Trigger update for game logic and UI
                Logger.LogDevCommand("SetBaseProduction", $"rate: {rate}");
            }
            else
            {
                GD.PrintErr("Invalid production rate entered.");
            }
        }

        private void OnForceSave()
        {
            GameManager.Instance?.SaveGame();
            Logger.LogDevCommand("ForceSave");
        }

        private void OnForceLoad()
        {
            GameManager.Instance?.LoadGame();
            Logger.LogDevCommand("ForceLoad");
        }

        private void OnResetGame()
        {
            var confirmDialog = new AcceptDialog();
            confirmDialog.DialogText = "Are you sure you want to reset the game? This will delete all progress!";
            confirmDialog.Title = "Confirm Reset";

            confirmDialog.Confirmed += () =>
            {
                GameManager.Instance?.ResetGame();
                Logger.LogDevCommand("ResetGame");
            };

            GetTree().Root.AddChild(confirmDialog);
            confirmDialog.PopupCentered();
        }

        private string FormatNumber(double number)
        {
            if (number < 1000) return number.ToString("F2");
            if (number < 1_000_000) return $"{number / 1000:F2}K";
            if (number < 1_000_000_000) return $"{number / 1_000_000:F2}M";
            if (number < 1_000_000_000_000) return $"{number / 1_000_000_000:F2}B";
            return $"{number / 1_000_000_000_000:F2}T";
        }
    }
}