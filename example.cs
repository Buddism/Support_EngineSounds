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
