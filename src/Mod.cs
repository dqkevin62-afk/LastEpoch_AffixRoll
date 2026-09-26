using MelonLoader;
using LastEpoch_AffixRoll.Core;
using LastEpoch_AffixRoll.Features;

namespace LastEpoch_AffixRoll;

public class Mod : MelonMod
{
	public override void OnInitializeMelon()
	{
		Cfg.Init();
		Tooltip.Resolve();
		GroundLabel.Resolve();
		Log.Msg("[AffixRoll] 已加载。Tooltip: " + Tooltip.HookName + " | Ground: " + GroundLabel.HookName);
	}

	public override void OnSceneWasLoaded(int buildIndex, string sceneName)
	{
		Tooltip.Resolve();
		GroundLabel.Resolve();
	}

	public override void OnUpdate()
	{
		if (Tooltip.HookName == "none")
		{
			Tooltip.Resolve();
		}
		if (GroundLabel.HookName == "none")
		{
			GroundLabel.Resolve();
		}
	}
}
