# Boss rig source tools

The editable source is `SourceAssets/BossRig/M4Boss.blend`. Completed test projects, logs and preview renders were removed on 2026-11-04.

To regenerate the procedural rig from the original FBX, run these scripts in Blender in order: `inspect.py`, `preview.py`, `build_rig.py`. The first script recreates the temporary `Preview/BossRig` directory; the second generates the normalized mesh consumed by the third. `build_rig.py` overwrites the rig source and exported FBX, so preserve any manually edited Blender source before regenerating.
