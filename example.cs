datablock AudioProfile(ES_Engine_Loop)
{
	filename = "./exampleEngineLoop.wav";
	//keep this description if you are making your own audioprofile (IMPORTANT)
	description = AudioEngineLooping3d; //only use this audioprofile in engine loops
	uiName = ""; //dont suggest using a uiname since the way audio is detected

	preload = true;
};


//is the vehicle engine sound enabled
JeepVehicle.ES_Enabled = true;
//vehicle engine sound DB
JeepVehicle.ES_SoundDB = nameToID(ES_Engine_Loop);
//vehicle start pitch (standard is 1)
JeepVehicle.ES_StartPitch = 1;
//vehicle velocity scalar
JeepVehicle.ES_VelocityScalar = 50; // TU
// engine sound pitch = startpitch + ( vectorlen ( vehicle.velocity ) * velocityScalar )


//gear count, will not work if you do not get the count right for ES_gearSpeeds and ES_gearPitches
JeepVehicle.ES_GearCount = 7;
// in seconds how long it takes to go from REV of last gear to REV Of new gear
JeepVehicle.ES_GearPitchDelay = 0.1;
//can only shift gears every 0.3 seconds
JeepVehicle.ES_gearShiftDelay = 0.3; // SECONDS
//gear speeds in TU (1:2 in terms of studs)
JeepVehicle.ES_GearSpeeds = "0 7 14 23 30 40 60";
//gear pitches, PITCH_START PITCH_PEAK
JeepVehicle.ES_gearPitches = "0 1.5 0 1.7 0 1.8 0 1.9 0 2 0 2.2 0 2.3";
//max pitch the vehicle can ever make
JeepVehicle.ES_maxPitch = 2.7;

//Engine Shut-Off Sound
JeepVehicle.ES_EngineStopSound = DeathCrySound;

//Engine Start-On Sound
JeepVehicle.ES_EngineStartSound = DeathCrySound;
//Engine Sound delay (amount of time it takes for the EngineStartSound to play)
JeepVehicle.ES_EngineStartDelay = 480; //you can caculate the time by using a command (>ONLY< ON THE CLIENT): echo(alxGetWaveLen("PATH/FILE.wav"));

JeepVehicle.ES_AudioSlot = 3;