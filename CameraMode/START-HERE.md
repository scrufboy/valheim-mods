# Valheim Photo / Free Camera — ready to install

Selected mod: **Mekhusha – Free Camera 1.2.1**

This is a client-side BepInEx mod. Your Viking stays visibly parked in the world while the camera detaches and flies. It does not require `devcommands` and other players do not need to install it.

## Fastest install (r2modman)

1. Open the Valheim profile you want to use.
2. Choose **Online**, search for **Free Camera** by **Mekhusha**, and install version **1.2.1**.
3. Launch **Start modded**.

Thunderstore dependency string: `Mekhusha-Free_Camera-1.2.1`

## Manual install

Copy `FreeCam.dll` from the included `package` folder into:

`BepInEx/plugins/FreeCamera/FreeCam.dll`

BepInExPack Valheim 5.4.2333 or newer is required.

## Photo-mode controls

- **Shift+P** — enter or leave free camera
- **WASD** — fly
- **E / Q** — rise / descend
- Hold **right mouse button** and move the mouse — look around
- **Mouse wheel** — change field of view
- **Up / Down arrow** — faster / slower movement
- **Z / C** — camera tilt; double-tap either to level the camera
- **O** — hide/show the full HUD
- **P** — hide/show the camera help panels
- **F11** — save a clean PNG screenshot

Screenshots are created automatically in:

`C:\Users\scruf\AppData\LocalLow\IronGate\Valheim\screenshots`

The folder appears after the first capture. OBS can record at the same time.

## Important behavior

- Your character remains where camera mode began, so the body can appear in shots.
- The mod freezes character control; it does **not** make the body invulnerable. Enter camera mode somewhere safe if hostile creatures are nearby.
- This is ideal for local/single-player and local multiplayer. Test on a dedicated server before relying on it for a shoot.
- Config is generated after first launch at `BepInEx/config/com.valheimtools.freecam.cfg`.

Source page: https://thunderstore.io/c/valheim/p/Mekhusha/Free_Camera/

