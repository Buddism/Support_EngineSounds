$EngineAudioType = 8;

for(%i = 0; %i < OptAudioPane.getCount(); %i++)
{
	%gui = OptAudioPane.getObject(%i);
	if(%gui.getCount() == 0 && %gui.extent $= "610 57") //gonna steal this for this mod
		%grabbedGui = %gui;
}

new GuiSliderCtrl(OptAudioVolumeEngine) {
	profile = "BlockButtonProfile";
	horizSizing = "right";
	vertSizing = "bottom";
	position = "108 10";
	extent = "305 34";
	minExtent = "8 8";
	enabled = "1";
	visible = "1";
	clipToParent = "1";
	variable = "value";
	altCommand = "OptAudioUpdateChannelVolume($EngineAudioType, OptAudioVolumeEngine.value);";
	range = "0.000000 1.000000";
	ticks = "10";
	value = "0.8";
	snap = "0";
};
new GuiTextCtrl(GUI_EngineSounds_VolumeSlider) {
	profile = "BlockButtonProfile";
	horizSizing = "right";
	vertSizing = "bottom";
	position = "0 10";
	extent = "120 18";
	minExtent = "8 8";
	enabled = "1";
	visible = "1";
	clipToParent = "1";
	text = "Engine Volume";
	maxLength = "255";
};
new GuiCheckBoxCtrl (GUI_EngineSounds_VolumeSetting)
{
	profile = "GuiCheckBoxProfile";
	horizSizing = "right";
	vertSizing = "bottom";
	position = "421 5";
	extent = "250 30";
	minExtent = "8 2";
	enabled = 1;
	visible = 1;
	clipToParent = 1;
	variable = "$Pref::ES::AllowVolumeAdjustment";
	text = "Server can change engine volume";
	groupNum = -1;
	buttonType = "ToggleButton";
};

%grabbedGui.add(OptAudioVolumeEngine);
%grabbedGui.add(GUI_EngineSounds_VolumeSlider);
%grabbedGui.add(GUI_EngineSounds_VolumeSetting);