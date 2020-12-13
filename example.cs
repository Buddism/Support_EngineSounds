datablock AudioProfile(ES_Engine_Loop)
{
	filename = "./Aerospeed1.wav";
	description = AudioEngineLooping3d;
	uiName = "Engine Loop";

	preload = true;
};


JeepVehicle.ES_SoundDB = nameToID(ES_Engine_Loop);
JeepVehicle.ES_StartPitch = 1;
JeepVehicle.ES_VelocityScalar = 50; // TU
// engine sound pitch = startpitch + ( vectorlen ( vehicle.velocity ) * velocityScalar )
JeepVehicle.ES_Enabled = true;
