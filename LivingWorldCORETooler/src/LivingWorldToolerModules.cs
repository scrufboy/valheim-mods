using System;

namespace Scrufboy.LivingWorld.Tooler
{
    // This catalog is the interface contract between the stable Tooler shell and
    // its independently maintained responsibilities. Page order and labels are
    // deliberate: modules may repair their own page without rewriting the shell.
    internal static class LivingWorldToolerModules
    {
        internal static ToolerPage[] Create(
            Action companions,
            Action raids,
            Action player,
            Action spawn,
            Action world,
            Action build,
            Action admin)
        {
            return new[]
            {
                new ToolerPage("Companions", ToolerDuty.Companions, companions),
                new ToolerPage("Raids", ToolerDuty.RaidTesting, raids),
                new ToolerPage("Player", ToolerDuty.PlayerTesting, player),
                new ToolerPage("Spawn", ToolerDuty.CatalogAndSpawn, spawn),
                new ToolerPage("World", ToolerDuty.WorldControl, world),
                new ToolerPage("Build", ToolerDuty.BuildAndTerrain, build),
                new ToolerPage("Admin", ToolerDuty.Administration, admin)
            };
        }
    }

    internal enum ToolerDuty
    {
        Companions,
        RaidTesting,
        PlayerTesting,
        CatalogAndSpawn,
        WorldControl,
        BuildAndTerrain,
        Administration
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
