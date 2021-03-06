using System;
using Microsoft.Xna.Framework.Input;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.SkipIntro.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace Pathoschild.Stardew.SkipIntro
{
    /// <summary>The mod entry point.</summary>
    internal class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>The mod configuration.</summary>
        private ModConfig Config;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides methods for interacting with the mod directory, such as read/writing a config file or custom JSON files.</param>
        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<ModConfig>();

            MenuEvents.MenuChanged += this.MenuEvents_MenuChanged;
        }


        /*********
        ** Private methods
        *********/
        /****
        ** Event handlers
        ****/
        /// <summary>The method called when the player returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void MenuEvents_MenuChanged(object sender, EventArgsClickableMenuChanged e)
        {
            if (e.NewMenu is TitleMenu)
                GameEvents.UpdateTick += this.GameEvents_UpdateTick;
        }

        /// <summary>Receives an update tick.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            try
            {
                // get open title screen
                if (!(Game1.activeClickableMenu is TitleMenu menu))
                {
                    GameEvents.UpdateTick -= this.GameEvents_UpdateTick;
                    return;
                }

                // skip intro
                if (this.TrySkipIntro(menu))
                    GameEvents.UpdateTick -= this.GameEvents_UpdateTick;
            }
            catch (Exception ex)
            {
                this.Monitor.InterceptError(ex, "skipping the intro");
                GameEvents.UpdateTick -= this.GameEvents_UpdateTick;
            }
        }

        /****
        ** Methods
        ****/
        /// <summary>Skip the intro if the game is ready.</summary>
        /// <param name="menu">The title menu whose intro to skip.</param>
        /// <returns>Returns whether the intro was skipped successfully.</returns>
        private bool TrySkipIntro(TitleMenu menu)
        {
            // wait until the game is ready
            if (Game1.currentGameTime == null)
                return false;

            // skip to title screen
            menu.receiveKeyPress(Keys.Escape);
            menu.update(Game1.currentGameTime);

            // skip to other screen
            switch (this.Config.SkipTo)
            {
                case Screen.Title:
                    // skip button transition
                    while (this.Helper.Reflection.GetField<int>(menu, "buttonsToShow").GetValue() < TitleMenu.numberOfButtons)
                        menu.update(Game1.currentGameTime);
                    break;

                case Screen.Load:
                    // skip to load screen
                    menu.performButtonAction("Load");
                    while (TitleMenu.subMenu == null)
                        menu.update(Game1.currentGameTime);
                    break;

                case Screen.JoinCoop:
                case Screen.HostCoop:
                    // skip to co-op screen
                    menu.performButtonAction("Co-op");
                    while (TitleMenu.subMenu == null)
                        menu.update(Game1.currentGameTime);

                    // skip to host tab
                    if (this.Config.SkipTo == Screen.HostCoop && TitleMenu.subMenu is CoopMenu submenu)
                    {
                        ClickableComponent hostTab = submenu.hostTab;
                        submenu.receiveLeftClick(hostTab.bounds.X, hostTab.bounds.Y, playSound: false);
                    }
                    break;
            }

            return true;
        }
    }
}
