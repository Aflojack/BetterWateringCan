using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace BetterWateringCan{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod{
        
    /*********
    ** Properties
    *********/
    /// <summary>The mod configuration from the player.</summary>
    private ModConfig Config;
    /// <summary>The mod data from the player.</summary>
    private ModData Data;
    /// <summary>Bool value for data change detection.</summary>
    private bool dataChanged;
    
        /**********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper){
            this.Config = this.Helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        }

        /**********
        ** Private methods
        *********/
        /// <summary>Raised when the day started.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object? sender, DayStartedEventArgs e){
            if(this.Data is null)
                ModDataLoad();
        }
        
        /// <summary>Raised when the game launched. Function to build config with GenericModConfigMenu.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e){
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("configMenu.enabled.name"),
                tooltip: () => this.Helper.Translation.Get("configMenu.enabled.tooltip"),
                getValue: () => this.Config.ModEnabled,
                setValue: value => this.Config.ModEnabled = value
            );

            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("configMenu.selectionOpenKey.name"),
                tooltip: () => this.Helper.Translation.Get("configMenu.selectionOpenKey.tooltip"),
                getValue: () => this.Config.SelectionOpenKey,
                setValue: value => this.Config.SelectionOpenKey = value
            );
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e){
            if (!this.Config.ModEnabled)
                return;

            if (!Context.IsWorldReady)
                return;
            
            if (Game1.player.CurrentTool is not WateringCan)
                return;

            int currentUpgradeLevel=Game1.player.CurrentTool.UpgradeLevel;
            if(this.Data.SelectedOption>currentUpgradeLevel || this.Data.SelectedOption<0){
                this.Monitor.Log($"Illegal value for mode selection. Selected value is out of range or not valid with {currentUpgradeLevel} level watering can. Value was {this.Data.SelectedOption} now set to default.", LogLevel.Error);
                this.Data.SelectedOption=1;
                this.dataChanged=true;
            }

            if(dataChanged){
                ModDataWrite();
            }

            Game1.player.toolPower.Value=this.Data.SelectedOption;
        }

        /// <summary>Raised after the player pressed/released any buttons on the keyboard, mouse, or controller.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e){
            if (!this.Config.ModEnabled)
                return;
                
            if (!Context.IsWorldReady)
                return;

            if (Game1.player.IsBusyDoingSomething())
                return;

            if (Game1.player.CurrentTool is not WateringCan)
                return;

            SButtonState state = this.Helper.Input.GetState(this.Config.SelectionOpenKey);
            if (state==SButtonState.Released){
                SelectionOpen(Game1.player.CurrentTool.UpgradeLevel);
            }
        }

        /// <summary>Display a dialogue window to the player. Content depending on the level of the watering can.</summary>
        /// <param name="upgradeLevel">Currect watering can upgrade level.</param>
        private void SelectionOpen(int upgradeLevel){
            List<Response> choices = new List<Response>();
            string selectionText=this.Helper.Translation.Get("dialogbox.currentOption");
            for(int i=0;i<Math.Min(upgradeLevel+1,5);i++){
                string responseKey=$"{i}";
                string responseText=this.Helper.Translation.Get($"dialogbox.option{i}");
                choices.Add(new Response(responseKey,responseText+(this.Data.SelectedOption==i?$" --- {selectionText} ---":"")));
            }
            Game1.currentLocation.createQuestionDialogue(this.Helper.Translation.Get("dialogbox.question"), choices.ToArray(), new GameLocation.afterQuestionBehavior(DialogueSet));
        }

        /// <summary>Save the selected option after dialogbox closed.</summary>
        /// <param name="who">Actual farmer.</param>
        /// <param name="selectedOption">Selected option.</param>
        private void DialogueSet(Farmer who, string selectedOption){
            this.Data.SelectedOption=int.Parse(selectedOption);
            this.dataChanged=true;
        }

        /// <summary>Load currect player mod data json from data folder.</summary>
        private void ModDataLoad(){
            this.Data = this.Helper.Data.ReadJsonFile<ModData>($"data/{StardewModdingAPI.Constants.SaveFolderName}.json") ?? new ModData();
            this.dataChanged=false;
        }

        /// <summary>Write currect player mod data json to data folder.</summary>
        private void ModDataWrite(){
            this.Helper.Data.WriteJsonFile($"data/{StardewModdingAPI.Constants.SaveFolderName}.json", this.Data);
            this.dataChanged=false;
        }
    }
}