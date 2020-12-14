datablock AudioProfile(ES_Engine_Loop)
{
	filename = "./exampleEngineLoop.wav";
	//keep this description if you are making your own audioprofile (IMPORTANT)
	description = AudioEngineLooping3d;
	uiName = "Engine Loop";

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


JeepVehicle.ES_GearCount = 7;
JeepVehicle.ES_PitchShiftDelay = 0.6; // whatever unit of time this is
JeepVehicle.ES_gearShiftDelay = 0.3; // SECONDS
JeepVehicle.ES_GearSpeeds = "0 7 14 23 30 37 46";
JeepVehicle.ES_gearPitches = "0 2 0.1 2 0.2 2 0.3 2 0.4 2 0.5 2 0.7 1.8";
