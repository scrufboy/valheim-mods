using System;

namespace Scrufboy.LivingWorld.Tooler
{
    // This catalog is the interface contract between the stable Tooler shell and
    // its independently maintained responsibilities. Page order and labels are
    // deliberate: modules may repair their own page without rewriting the shell.
    internal static class LivingWorldToolerModules
    {
        internal static ToolerPage[] Create(
            Action overview,
            Action lifecycle,
            Action community,
            Action combat,
            Action party,
            Action raids,
            Action player,
            Action itemsAndSpawn,
            Action world,
            Action siteSurvey,
            Action terrain,
            Action mapAndAdmin,
            Action danger,
            Action console)
        {
            return new[]
            {
                new ToolerPage("Overview", ToolerDuty.Overview, overview),
                new ToolerPage("Lifecycle", ToolerDuty.NpcLifecycle, lifecycle),
                new ToolerPage("Community", ToolerDuty.CompanionCommunity, community),
                new ToolerPage("Combat", ToolerDuty.CompanionCombat, combat),
                new ToolerPage("Party", ToolerDuty.PartyControl, party),
                new ToolerPage("Raids", ToolerDuty.RaidTesting, raids),
                new ToolerPage("Player", ToolerDuty.PlayerTesting, player),
                new ToolerPage("Items / Spawn", ToolerDuty.CatalogAndSpawn, itemsAndSpawn),
                new ToolerPage("World", ToolerDuty.WorldControl, world),
                new ToolerPage("Site Survey", ToolerDuty.SiteSurvey, siteSurvey),
                new ToolerPage("Terrain", ToolerDuty.TerrainControl, terrain),
                new ToolerPage("Map / Admin", ToolerDuty.MapAndAdministration, mapAndAdmin),
                new ToolerPage("Danger", ToolerDuty.ConfirmedDestructiveActions, danger),
                new ToolerPage("Console", ToolerDuty.CommandBridge, console)
            };
        }
    }

    internal enum ToolerDuty
    {
        Overview,
        NpcLifecycle,
        CompanionCommunity,
        CompanionCombat,
        PartyControl,
        RaidTesting,
        PlayerTesting,
        CatalogAndSpawn,
        WorldControl,
        SiteSurvey,
        TerrainControl,
        MapAndAdministration,
        ConfirmedDestructiveActions,
        CommandBridge
    }

    internal sealed class ToolerPage
    {
        internal readonly string Label;
        internal readonly ToolerDuty Duty;
        private readonly Action draw;

        internal ToolerPage(string label, ToolerDuty duty, Action draw)
        {
            if (String.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("A Tooler page must have a visible label.", "label");
            }
            if (draw == null)
            {
                throw new ArgumentNullException("draw");
            }

            Label = label;
            Duty = duty;
            this.draw = draw;
        }

        internal void Draw()
        {
            draw();
        }
    }
}

