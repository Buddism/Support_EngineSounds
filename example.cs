datablock AudioProfile(ES_Engine_Loop)
{
	filename = "./Engine_Drive.ogg";
	description = AudioEngineLooping3d;
	preload = false;
	uiName = "Engine Loop";
};


JeepVehicle.ES_SoundDB = nameToID(ES_Engine_Loop);
JeepVehicle.ES_StartPitch = 0.5;
JeepVehicle.ES_VelocityScalar = 30; // TU
// engine sound pitch = startpitch + ( vectorlen ( vehicle.velocity ) * velocityScalar )
JeepVehicle.ES_Enabled = true;
