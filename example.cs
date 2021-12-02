datablock AudioProfile(ES_Engine_Loop)
{
    filename = "./exampleEngineLoop.wav";
    //keep this description if you are making your own audioprofile (IMPORTANT)
    description = AudioEngineLooping3d; //only use this audioprofile in engine loops
    uiName = ""; //dont suggest using a uiname since the way audio is detected

    preload = true;
};

//JeepVehicle in this example has a high maxWheelSpeed
//these variables can be declared inside of the vehicle datablocks {}; (remove prefix of JeepVehicle.)

//is the vehicle engine sound enabled
JeepVehicle.ES_Enabled = true;
//vehicle engine sound Datablock (DB) 
JeepVehicle.ES_SoundDB = ES_Engine_Loop;
//vehicle start/idle pitch (standard is 1)
JeepVehicle.ES_StartPitch = 1;

//vehicle velocity scalar (useless in gear mode)
JeepVehicle.ES_VelocityScalar = 50; // TU

// engine sound pitch = startpitch + ( vectorlen ( vehicle.velocity ) * velocityScalar )


//gear count, will not work if you do not get the count right for ES_gearSpeeds and ES_gearPitches
JeepVehicle.ES_GearCount = 7;

// in seconds how long it takes to go from REV of last gear to REV Of new gear
JeepVehicle.ES_GearPitchDelay = 0.1;

//can only shift gears every 0.3 seconds
JeepVehicle.ES_gearShiftDelay = 0.3; // SECONDS

//gear speeds in TU - Scale to the maxWheelSpeed of the Datablock (1:2 in terms of studs)
JeepVehicle.ES_GearSpeeds = "0 7 14 23 30 40 60";

//gear0: 0  TU 0 BPS
//gear1: 7  TU 14 BPS
//gear2: 14 TU 28 BPS
//gear3: 23 TU 43 BPS
//gear5: 40 TU 80 BPS
//gear6: 60 TU 120 BPS

//gear pitches, PITCH_START PITCH_PEAK. 
//The gear pitches are shifted from PITCH_START into PITCH_PEAK by the percentage into the next gear (curGear + 1)
//The PITCH_PEAK values should be lower or equal to ES_maxPitch.
JeepVehicle.ES_gearPitches = "0 1.5 0 1.7 0 1.8 0 1.9 0 2 0 2.2 0 2.3";

//gear0 START: ES_StartPitch + 0
//gear0 PEAK:  ES_StartPitch + 1.5

//gear1 START: ES_StartPitch + 0
//gear1 PEAK:  ES_StartPitch + 1.7

//gear2 START: ES_StartPitch + 0
//gear2 PEAK:  ES_StartPitch + 1.8

//gear3 START: ES_StartPitch + 0
//gear3 PEAK:  ES_StartPitch + 1.9

//gear4 START: ES_StartPitch + 0
//gear4 PEAK:  ES_StartPitch + 2



//  gear0 START VELOCITY: 0 TU
//  gear0 START: ES_StartPitch + 0

//  ====BASIC ALGORITHM====
//  currentGear 0:
//  currentGearSpeed = 0
//  nextGearSpeed    = 7
//  progressIntoNextGear = currentVelocity - currentGearSpeed) / (nextGearSpeed - currentGearSpeed);

//  scaled_pitch: ( ES_GearPitchStart[currentGear] shifted to ES_GearPitchPeak[currentGear] ) by progressIntoNextGear
//    actual_pitch: ES_StartPitch + scaled_pitch
//   if( actual_pitch IS_MORE_THAN ES_maxPitch ) THEN actual_pitch = ES_maxPitch;
//   if( actual_pitch IS_LESS_THAN   0.001     ) THEN actual_pitch = 0.001;


//  =====EXAMPLE=====
//   in use at 3.5 TU /sec in gear 0
//  currentGear 0:
//  currentGearSpeed = 0
//  nextGearSpeed    = 7

//  currentVelocity  = 3.5
//  progressIntoNextGear = currentVelocity - currentGearSpeed) / (nextGearSpeed - currentGearSpeed);
//  progressIntoNextGear = currentVelocity[3.5] - currentGearSpeed[0.0]) / (nextGearSpeed[7.0] - currentGearSpeed[0.0]);
//  progressIntoNextGear = (3.5 - 0.0) / (7.0 - 0.0) = 0.5
//  progressIntoNextGear = 0.5

//  scaled_pitch: ( ES_GearPitchStart[currentGear] shifted to ES_GearPitchPeak[currentGear] ) by progressIntoNextGear
//  using ES_GearPitchStart[0] shifted to ES_GearPitchPeak[1.5] by progressIntoNextGear[0.5]

//  ====THE END PITCH====
//   real pitch at 3.5 TU /sec [7 BPS] = ES_StartPitch + 0.75 = 1.75


//gear0 PEAK VELOCITY: 7 TU
//gear0 PEAK:  ES_StartPitch + 1.5



//max pitch the vehicle can ever make
JeepVehicle.ES_maxPitch = 2.7;

//Engine Shut-Off Sound
JeepVehicle.ES_EngineStopSound = DeathCrySound;

//Engine Start-On Sound
JeepVehicle.ES_EngineStartSound = DeathCrySound;
//Engine Sound delay (amount of time it takes for the EngineStartSound to play)
JeepVehicle.ES_EngineStartDelay = 480; //you can caculate the time by using a command (>ONLY< ON THE CLIENT): echo(alxGetWaveLen("PATH/FILE.wav"));

JeepVehicle.ES_AudioSlot = 3;


//if you specify an animation that doesnt exist this will break the steering animation (very jank system)
JeepVehicle.ES_GearShiftAnim = "gearShiftAnim0 gearShiftAnim1 gearShiftAnim2 gearShiftAnim3 gearShiftAnim4 gearShiftAnim5 gearShiftAnim6";
//OR just this for every gear
JeepVehicle.ES_GearShiftAnim = "gearShiftAnimAllGears";

JeepVehicle.engineTorque = 2000;
JeepVehicle.maxWheelSpeed = 50;

//NEW AUDIO STUFF

//INITIAL VOLUME LEVEL
JeepVehicle.ES_StartVolume = 0.2;

//VOLUME SCALAR
JeepVehicle.ES_VolumeScalar = 0.2;

//OR (still effected by ES_StartVolume) MAXES AT 1
JeepVehicle.ES_GearVolumeLevels = "0.4 1 0.9 1 0.9 1 0.9 1 0.9 1 0.9 1 0.9 1";


//volume algorithm

//this is a linear line
//	PROGRESS_TO_NEXT_GEAR is 1 at the peak of the gear, and 0 at the start
//  VOLUME = START_VOLUME + PROGRESS_TO_NEXT_GEAR * VOLUME_SCALAR
//	VOLUME is maxed at 1.0

//less horrible values
//JeepVehicle.ES_gearPitches = "0.7 1.4 0.9 1.4 0.9 1.4 0.9 1.4 0.9 1.4 0.9 1.4 0.9 1.4";
//JeepVehicle.ES_maxPitch = 1.7;
//JeepVehicle.ES_StartPitch = 0;