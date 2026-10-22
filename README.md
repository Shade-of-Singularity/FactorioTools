# FactorioTools
Tools for modding Factorio

## Usage
You will need to compile the apps, or use the ones from Releases.

After that, simply put it in the root (or one folder up) from where your info.json is located.

## Running
When you first run `FactorioTools.Compiler.exe` - it will create `deploy.toml` file near each found `info.json`, if it doesn't exist:
```TOML
output = "%APPDATA%\Factorio\mods"
```
(Note: right now, only `%APPDATA%` special symbol is supported)

On a second run, it will try to compile the mod using settings from `deploy.toml`.
Empty files and folders will be excluded, producing a clean .zip archive.

## Console usage
To deploy in bulk, run .exe and provide a list of locations to scout/compile:
```BATCH
"F:\Tools\FactorioTools\FactorioTools.Compiler.exe" "F:\MyMods\Pyanodons Addon" "F:\MyMods\Space Exploration Rebalance"
```
For Linux uses - you probably know how to run apps.

I don't tho - if you can - please suggest how to do it.
