if(!isObject(ES_MonitorSet))
	new simSet(ES_MonitorSet);

//this simset is for pitch updates on moving vehicles
if(!isObject(ES_ActiveSet))
	new simSet(ES_ActiveSet);

$ES::Version = "2.0.0";

$EngineAudioType = 9;

if($ES::DebugLevel $= "") //is it undefined?
	$ES::DebugLevel = 0;

//DEBUG LEVEL 1 is for bottomprint data in your current vehicle & gear error checking
//DEBUG LEVEL 2 is for data recieved when the vehicle is enabled
//DEbuG LEVEL 3 has gear shift up/down message (including animation data)
//DEBUG LEVEL 4+ is mainly for debug
function ES_Debug(%level, %message, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9)
{
	if(%level <= $ES::DebugLevel)
	{
		for(%i = 1; %i <= 9; %i++)
			%message = strreplace(%message, "%" @ %i, %a[%i]);

		newChatHud_addLine("ES_DEBUG: " @ %message);
	}
}

function ES_filterString(%str, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13, %a14, %a15, %a16, %a17, %a18, %a19)
{
	for(%i = 19; %i >= 1; %i--) //start with larger numbers
		%str = strreplace(%str, "%" @ %i, %a[%i]);

	return %str;
}

//stolen from TGE:

//---------------------------------------------------------------------------
// the following db<->linear conversion functions >come from Loki openAL linux driver<
// code, here more for completeness than anything else (all current audio code
// uses AL_GAIN_LINEAR)... in Audio:: so that looping updates and audio channel updates
// can convert gain types and to give the miles driver access
$ES::logTab[0] = 0.00; $ES::logTab[1] = 0.001; $ES::logTab[2] = 0.002; $ES::logTab[3] = 0.003; $ES::logTab[4] = 0.004; $ES::logTab[5] = 0.005; $ES::logTab[6] = 0.01; $ES::logTab[7] = 0.011;
$ES::logTab[8] = 0.012; $ES::logTab[9] = 0.013; $ES::logTab[10] = 0.014; $ES::logTab[11] = 0.015; $ES::logTab[12] = 0.016; $ES::logTab[13] = 0.02; $ES::logTab[14] = 0.021; $ES::logTab[15] = 0.022;
$ES::logTab[16] = 0.023; $ES::logTab[17] = 0.024; $ES::logTab[18] = 0.025; $ES::logTab[19] = 0.03; $ES::logTab[20] = 0.031; $ES::logTab[21] = 0.032; $ES::logTab[22] = 0.033; $ES::logTab[23] = 0.034;
$ES::logTab[24] = 0.04; $ES::logTab[25] = 0.041; $ES::logTab[26] = 0.042; $ES::logTab[27] = 0.043; $ES::logTab[28] = 0.044; $ES::logTab[29] = 0.05; $ES::logTab[30] = 0.051; $ES::logTab[31] = 0.052;
$ES::logTab[32] = 0.053; $ES::logTab[33] = 0.054; $ES::logTab[34] = 0.06; $ES::logTab[35] = 0.061; $ES::logTab[36] = 0.062; $ES::logTab[37] = 0.063; $ES::logTab[38] = 0.064; $ES::logTab[39] = 0.07;
$ES::logTab[40] = 0.071; $ES::logTab[41] = 0.072; $ES::logTab[42] = 0.073; $ES::logTab[43] = 0.08; $ES::logTab[44] = 0.081; $ES::logTab[45] = 0.082; $ES::logTab[46] = 0.083; $ES::logTab[47] = 0.084;
$ES::logTab[48] = 0.09; $ES::logTab[49] = 0.091; $ES::logTab[50] = 0.092; $ES::logTab[51] = 0.093; $ES::logTab[52] = 0.094; $ES::logTab[53] = 0.10; $ES::logTab[54] = 0.101; $ES::logTab[55] = 0.102;
$ES::logTab[56] = 0.103; $ES::logTab[57] = 0.11; $ES::logTab[58] = 0.111; $ES::logTab[59] = 0.112; $ES::logTab[60] = 0.113; $ES::logTab[61] = 0.12; $ES::logTab[62] = 0.121; $ES::logTab[63] = 0.122;
$ES::logTab[64] = 0.123; $ES::logTab[65] = 0.124; $ES::logTab[66] = 0.13; $ES::logTab[67] = 0.131; $ES::logTab[68] = 0.132; $ES::logTab[69] = 0.14; $ES::logTab[70] = 0.141; $ES::logTab[71] = 0.142;
$ES::logTab[72] = 0.143; $ES::logTab[73] = 0.15; $ES::logTab[74] = 0.151; $ES::logTab[75] = 0.152; $ES::logTab[76] = 0.16; $ES::logTab[77] = 0.161; $ES::logTab[78] = 0.162; $ES::logTab[79] = 0.17;
$ES::logTab[80] = 0.171; $ES::logTab[81] = 0.172; $ES::logTab[82] = 0.18; $ES::logTab[83] = 0.181; $ES::logTab[84] = 0.19; $ES::logTab[85] = 0.191; $ES::logTab[86] = 0.192; $ES::logTab[87] = 0.20;
$ES::logTab[88] = 0.201; $ES::logTab[89] = 0.21; $ES::logTab[90] = 0.211; $ES::logTab[91] = 0.22; $ES::logTab[92] = 0.221; $ES::logTab[93] = 0.23; $ES::logTab[94] = 0.231; $ES::logTab[95] = 0.24;
$ES::logTab[96] = 0.25; $ES::logTab[97] = 0.251; $ES::logTab[98] = 0.26; $ES::logTab[99] = 0.27; $ES::logTab[100] = 0.271; $ES::logTab[101] = 0.28; $ES::logTab[102] = 0.29; $ES::logTab[103] = 0.30;
$ES::logTab[104] = 0.301; $ES::logTab[105] = 0.31; $ES::logTab[106] = 0.32; $ES::logTab[107] = 0.33; $ES::logTab[108] = 0.34; $ES::logTab[109] = 0.35; $ES::logTab[110] = 0.36; $ES::logTab[111] = 0.37;
$ES::logTab[112] = 0.38; $ES::logTab[113] = 0.39; $ES::logTab[114] = 0.40; $ES::logTab[115] = 0.41; $ES::logTab[116] = 0.43; $ES::logTab[117] = 0.50; $ES::logTab[118] = 0.60; $ES::logTab[119] = 0.65;
$ES::logTab[120] = 0.70; $ES::logTab[121] = 0.75; $ES::logTab[122] = 0.80; $ES::logTab[123] = 0.85; $ES::logTab[124] = 0.90; $ES::logTab[125] = 0.95; $ES::logTab[126] = 0.97; $ES::logTab[127] = 0.99;
$ES::logCount = 127;


//another TGE-ish function
function ES_linearToDB(%value)
{
	//(logtab[(U32)(logmax * value)]);
	return $ES::logTab[(mClampF(%value, 0, 1) * $ES::logCount) | 0];
}


function clientCmdES_Handshake(%serverVersion, %coneInsideAngle, %coneOutsideAngle)
{
	commandToServer('ES_Handshake', $ES::Version);
	if($Pref::ES::EnableHandshakeMessage)
	{
		if($ES::Version !$= %serverVersion)
			newChatHud_addLine("\c6This server is running \c3EngineSounds\c6 version \c3"@ %serverVersion @"\c6, you are using version\c3 "@ $ES::Version);
		else newChatHud_addLine("\c6This server supports \c3EngineSounds");
	}

	$ES::InsideAngle = %coneInsideAngle;
	$ES::OutsideAngle = %coneOutsideAngle;

	if(!isEventPending($ES_ScanVehicleSchedule))
		ES_Client_LookForVehicles();
}
function clientCmdES_RemarkVehicle(%vehicleGID)
{
	%con = nameToID("serverConnection");
	if(!isObject(%con))
		return;

	%vehicle = %con.resolveGhostID(%vehicleGID);
	if(!isObject(%vehicle) || ! ( %vehicle.getType() & $TypeMasks::VehicleObjectType ) )
		return;

	if(ES_ActiveSet.isMember(%vehicle))
		return;

	%lastHandle = alxplay(AdminSound); // get the most recent audio handle ID (hacky)
	alxStop(%lastHandle); //stop it

	//mark its handler because handlers arent active until the player goes near them, but they keep using an older handle id?
	%vehicle.ES_HandlePosition = %lastHandle;

	if(!ES_MonitorSet.isMember(%vehicle))
		ES_MonitorSet.add(%vehicle);

	if(!isEventPending($ES_MonitorSchedule))
		ES_Client_MonitorHandles();
}
function clientCmdES_stopEngine(%vehicleGID)
{
	%con = nameToID("serverConnection");
	if(!isObject(%con))
		return;

	%vehicle = %con.resolveGhostID(%vehicleGID);
	if(!isObject(%vehicle) || ! ( %vehicle.getType() & $TypeMasks::VehicleObjectType ) )
		return;

	if(ES_ActiveSet.isMember(%vehicle))
	{
		ES_ActiveSet.remove(%vehicle);
		%vehicle.ES_AudioHandle = "";
		ES_Debug(19, "   clientCmdES_stopEngine ( %1 - %2) removed from activeset", %vehicleGID, %vehicle.getDataBlock().shapefile);
	} else
		ES_Debug(20, "   clientCmdES_stopEngine ( %1 - %2)", %vehicleGID, %vehicle.getDataBlock().shapefile);
}

//this is a lot of args
function clientCmdES_closestVehicle(%audioHandle, %closestVehicleGID, %StartValues, %scalars, %maxPitch, %gearPitchDelay, %gearCount, %gearSpeeds, %gearPitches, %gearShiftTime, %gearShiftAnims, %audioDescription)
{
	%con = nameToID(serverConnection);
	ES_Debug(10, "RECIEVE" SPC %closestVehicleGID);
	if(!%con.ES_allowCheck[%audioHandle])
		return;

	%closestVehicle = %con.resolveGhostID(%closestVehicleGID);
	ES_Debug(11, "   isObject:%1 / isAVehicle:%2 / audioHandleInUse:%3", !isObject(%closestVehicle), ( ! ( %closestVehicle.getType() & $TypeMasks::VehicleObjectType) ), %con.ES_hasBoundHandle[%audioHandle] + 0);

	if(!isObject(%closestVehicle) || ! ( %closestVehicle.getType() & $TypeMasks::VehicleObjectType)  || %closestVehicle.ES_AudioHandle != 0)
		return;

	if(!ES_MonitorSet.isMember(%closestVehicle))
		return;

	ES_MonitorSet.remove(%closestVehicle);
	%con.ES_allowCheck[%audioHandle] = false;
	ES_Debug(12, "   set up audio handle for %1", %closestVehicleGID); //kinda useless message because of the one in ES_RegisterActiveVehicle

	ES_RegisterActiveVehicle(%audioHandle, %closestVehicle, %StartValues, %scalars, %maxPitch, %gearPitchDelay, %gearCount, %gearSpeeds, %gearPitches, %gearShiftTime, %gearShiftAnims);
}

function ES_MarkVehicle(%vehicle)
{
	if(!isObject(%vehicle) || ! ( %vehicle.getType() & $TypeMasks::VehicleObjectType ) )
		return;

	if(ES_ActiveSet.isMember(%vehicle) || ES_MonitorSet.isMember(%vehicle))
		return;

	%lastHandle = alxplay(AdminSound); // get the most recent audio handle ID (hacky)
	alxStop(%lastHandle); //stop it, as it doesnt need to actually play

	//mark its handler because handlers arent active until the player goes near them, but they keep using an older handle id?
	%vehicle.ES_HandlePosition = %lastHandle;
	ES_MonitorSet.add(%vehicle);

	if(!isEventPending($ES_MonitorSchedule))
		ES_Client_MonitorHandles();
}

//this function scans the top of the serverconnection objects for new vehicles to be marked for when they expose the audioHandlerID to TS
function ES_Client_LookForVehicles()
{
	cancel($ES_ScanVehicleSchedule);

	%con = nameToID(serverConnection);
	if(!isObject(%con)) //serverConnection is doesn't exist anymore, cancel the scan if its still running
	{
		cancel($ES_ScanVehicleSchedule);
		return;
	}

	%c = %con.getCount();
	%scanCount = %con.getFinishedInitialGhost() ? 100 : 1000; //if they are ghosting do a larger scan
	%s = getMax(%c - %scanCount, 0);
	for(%i = %s; %i < %c; %i++)
	{
		%obj = %con.getObject(%i);
		if(%obj.getType() & $TypeMasks::VehicleObjectType && !%obj.ES_Marked)
		{
			%obj.ES_Marked = true;
			ES_MarkVehicle(%obj);
		}
	}
	$ES_ScanVehicleSchedule = schedule(1, 0, ES_Client_LookForVehicles);
}


function ES_checkFingerprintValue(%value)
{
	%fract = %value - (%value | 0);
	
	return mAbs(%fract - 0.248) < 0.001;
}



//monitor the logged vehicles from 'ES_Client_LookForVehicles' for exposed audioHandles
function ES_Client_MonitorHandles()
{
	%con = nameToID(serverConnection);
	if(!isObject(%con))
	{
		cancel($ES_MonitorSchedule);
		return;
	}

	%set = nameTOID("ES_MonitorSet");
	%c = %set.getCount();
	for(%k = %c - 1; %k >= 0; %k--)
	{
		%obj = %set.getObject(%k);
		%handleIndex = %obj.ES_HandlePosition;
		for(%i = %handleIndex - 16; %i <= %handleIndex + 16; %i++)
		{
			if(	alxIsPlaying(%i) && alxGetSourceI(%i, "AL_LOOPING") && !%con.ES_AudioHandle[%i] &&
				ES_checkFingerprintValue(alxGetSourceF(%i, "AL_REFERENCE_DISTANCE")) &&
				ES_checkFingerprintValue(alxGetSourceF(%i, "AL_MAX_DISTANCE")))
			{
				//things can go wrong with this handshake easily
				//handshake with the server for more accuracy if it is the correct vehicle
				commandToServer('ES_newAudioHandle', %i);
				//callback from the server is clientCmdES_closestVehicle, and it removes this vehicle from ES_MonitorSet if it succeeds

				//allow the server to tell the client to save these values
				%con.ES_allowCheck[%i] = true;
				%con.ES_AudioHandle[%i] = true;

				ES_Debug(9, newAudioHandle SPC %obj SPC %obj.getDataBlock().shapefile);

				//break out of this loop, because we are done with this marker
				break;
			}
		}
	}
	if(%set.getCount() == 0) //we are not looking for anything anymore
		return;

	$ES_MonitorSchedule = schedule(1, 0, ES_Client_MonitorHandles);
}

function ES_RegisterActiveVehicle(%audioHandle, %vehicle, %StartValues, %scalars, %maxPitch, %gearPitchDelay, %gearCount, %gearSpeeds, %gearPitches, %gearShiftTime, %gearShiftAnims)
{
	%con = nameToID(serverConnection);
	if(%con.ES_hasBoundHandle[%audioHandle])
		return;

	if(!isObject(%vehicle) || ! ( %vehicle.getType() & $TypeMasks::VehicleObjectType ) )
		return;

	%con.ES_hasBoundHandle[%audioHandle] = true;

	%vehicle.ES_AudioHandle = %audioHandle;

	%vehicle.ES_StartValues = %StartValues;

	%vehicle.ES_StartPitch  = getWord(%StartValues, 0);
	%vehicle.ES_StartVolume = getWord(%StartValues, 1);

	%vehicle.ES_VelocityScalar = getWord(%scalars, 0);
	%vehicle.ES_VolumeScalar   = getWord(%scalars, 1);

	%vehicle.ES_SupportsVolume = %vehicle.ES_VolumeScalar + %vehicle.ES_StartVolume > 0;

	%gearCount = mClamp(%gearCount, 0, 24);
	if(%gearCount != getWordCount(%gearSpeeds) || %gearCount != (getWordCount(%gearPitches) / 2))
	{
		%gearCount = 0;
		ES_Debug(1, "invalid gear data from server (mismatch) gearCount: %1, gearSpeedsCount: %2, gearPitchesCount: %3 (total: %4)", %gearCount, getWordCount(%gearSpeeds), getWordCount(%gearPitches) / 2, getWordCount(%gearPitches));
	}

	if(%gearCount > 1)
	{
		%vehicle.ES_GearCount = %gearCount;
		for(%i = 0; %i < %gearCount; %i++)
		{
			%vehicle.ES_GearSpeed[%i] = getWord(%gearSpeeds, %i);

			%gearPitchIndex = %i * 2;
			%vehicle.ES_GearPitchStart[%i] = getWord(%gearPitches, %gearPitchIndex + 0);
			%vehicle.ES_GearPitchPeak [%i] = getWord(%gearPitches, %gearPitchIndex + 1);

			%vehicle.ES_GearShiftAnim[%i] = getWord(%gearShiftAnims, getMin(%i, getWordCount(%gearShiftAnims) - 1));
		}
	}

	%vehicle.ES_GearPitchDelay  = mClampF(%gearPitchDelay, 0.0, 32.0 );
	%vehicle.ES_gearShiftTime   = mClampF(%gearShiftTime, 0.0, 10.0); //defaults to 0 (instant gear change)
	if(%maxPitch == 0)
		%maxPitch = 10;
		
	%vehicle.ES_maxPitch		= mClampF(%maxPitch, 0.0, 10.0);

	%vehicle.ES_lastPitch = %StartValues; //initialize this var

	ES_DEBUG(2, "StartValues(%1), scalars(%2), maxPitch(%3), gearPitchDelay(%4), gearCount(%5), gearShiftTime(%6), gearShiftAnims(%7)", %StartValues, %scalars, %maxPitch, %gearPitchDelay, %gearCount, %gearShiftTime, %gearShiftAnims);

	ES_ActiveSet.add(%vehicle);

	ES_Debug(9, "registed audio handle ["@ %audioHandle @"] for ["@ %vehicle @"]"@ %vehicle.getDataBlock().shapefile);
	if(!isEventPending($EngineSound_Schedule))
		ES_Client_Loop();
}
function ES_mLerp(%init, %end, %t)
{
	return %init + (%end - %init) * %t;
}
function ES_Client_Loop(%lastLoopTime)
{
	cancel($EngineSound_Schedule);
	%set = nameToID("ES_ActiveSet");
	if(!isObject(%set))
		return;

	%con = serverConnection;
	if(!isObject(%con))
	{
		cancel($EngineSound_Schedule);
		return;
	}

	if($ES::DebugLevel >= 1 && isObject(%ctrl = %con.getControlObject()))
		%myMount = %ctrl.getObjectMount();
	
	if(isObject(%ctrl = %con.getControlObject()))
		alListener3f("AL_VELOCITY", vectorScale(%ctrl.getVelocity(), 1.0));


	for(%i = %set.getCount() - 1; %I >= 0; %I--)
	{
		%vehicle = %set.getObject(%i);
		%handle = %vehicle.ES_AudioHandle;
		if( ! (%vehicle.getType() & $TypeMasks::VehicleObjectType) ) //wonderful type check
		{
			%set.remove(%vehicle);
			continue;
		}

		if(alxIsPlaying(%handle) && alxGetSourceI(%handleIndex, "AL_LOOPING")) // is the audio handle still playing?
		{
			%velocityLength = vectorLen(%vehicle.getVelocity());
			if(%vehicle.ES_GearCount > 1)
			{
				if($Sim::Time - %vehicle.ES_lastGearShiftTime > %vehicle.ES_gearShiftTime)
				{
					for(%k = %vehicle.ES_GearCount - 1; %k >= 0; %k--)
						if(%velocityLength >= %vehicle.ES_GearSpeed[%k])
						{
							%gear = %k;
							if(%gear != %vehicle.ES_lastGear)
							{
								%nextGearSpeed = %vehicle.ES_GearSpeed[getMin(%gear + 1, %vehicle.ES_GearCount - 1)];
								%vehicle.ES_ShiftedUp = %gear > %vehicle.ES_lastGear; //if the new gear is higher than the old gear

								%vehicle.ES_lastGearShiftTime = $Sim::Time;
								%vehicle.ES_lastGear = %gear;
								if(%vehicle.ES_GearShiftAnim[%k] !$= "") //this is fairly jank
								{
									%vehicle.playThread(0, %vehicle.ES_GearShiftAnim[%k]); //play the animation on the client side
									//if you specify an animation that doesnt exist this will break steering
									%vehicle.schedule(100, playThread, 1, "steering");

									ES_DEBUG(3, "GEAR SHIFT %3 [%1] PLAY ANIM:[%2]", %gear, %vehicle.ES_GearShiftAnim[%k], (%vehicle.ES_ShiftedUp ? "UP" : "DOWN"));
								} else
									ES_DEBUG(3, "GEAR SHIFT %2 [%1] Play ANIM:[UNDEFINED]", %gear, (%vehicle.ES_ShiftedUp ? "UP" : "DOWN"));
							}
							break;
						}
				} else {
					%gear = %vehicle.ES_lastGear;
				}

				if(%gear + 1 >= %vehicle.ES_GearCount) //its on its last gear
					%nextGearSpeed = %vehicle.getDataBlock().maxWheelSpeed;
				else
					%nextGearSpeed = %vehicle.ES_GearSpeed[getMin(%gear + 1, %vehicle.ES_GearCount - 1)];

				%currentGearSpeed = %vehicle.ES_GearSpeed[%gear];
				%fractOnGear = (%velocityLength - %currentGearSpeed) / (%nextGearSpeed - %currentGearSpeed);

				%gearPitch = ES_mLerp(%vehicle.ES_GearPitchStart[%gear], %vehicle.ES_GearPitchPeak[%gear], %fractOnGear);

				%pitch = %vehicle.ES_StartPitch + %gearPitch;
				%volume = %vehicle.ES_startVolume + %fractOnGear / %vehicle.ES_VolumeScalar;
				if($Sim::Time - %vehicle.ES_lastGearShiftTime < %vehicle.ES_GearPitchDelay)
				{
					%pitch = ES_mLerp(%vehicle.ES_lastPitch, %pitch, ($Sim::Time - %vehicle.ES_lastGearShiftTime) / %vehicle.ES_GearPitchDelay);
					%volume = ES_mLerp(%vehicle.ES_lastVolume, %volume, ($Sim::Time - %vehicle.ES_lastGearShiftTime) / %vehicle.ES_GearPitchDelay);
				} else {
					%vehicle.ES_lastPitch = %pitch;
					%vehicle.ES_lastVolume = %volume;
				}		
			} else {
				%pitch = %vehicle.ES_StartPitch + %velocityLength / %vehicle.ES_VelocityScalar;
				%volume = %vehicle.ES_startVolume + (%velocityLength / %vehicle.ES_VolumeScalar);
			}

			%clampedPitch = mClampF(%pitch, 0.001, %vehicle.ES_maxPitch);

			alxSource3f(%handle, "AL_VELOCITY", vectorScale(%vehicle.getVelocity(), 1.0)); //this requires a modified openAl32.dll for doppler effect support
			alxSourcef(%handle, "AL_PITCH", %clampedPitch);

			if(%vehicle.ES_SupportsVolume)
			{
				%clampedVolume = ES_linearToDB(%volume);
				//	AL_GAIN & AL_GAIN_LINEAR have an odd problem so we need to use AL_CONE_OUTER_GAIN
				alxSourcef(%handle, "AL_CONE_OUTER_GAIN", %clampedVolume);
			}

			if($ES::DebugLevel >= 1 && %vehicle == %myMount)
			{
				//round and trim the extra 0s (ex: 1.10 => 1.1)
				%velocityLength = 0 + mFloatLength(%velocityLength, 2);
				%fractOnGear 	= 0 + mFloatLength(%fractOnGear, 2);
				%clampedPitch 	= 0 + mFloatLength(%clampedPitch, 2);
				%clampedVolume	= 0 + mFloatLength(%clampedVolume, 2);
				%volume 		= 0 + mFloatLength(%volume, 2);

				if(%vehicle.ES_SupportsVolume)
				{
					%volumeString = ES_filterString("<just:left>volume: %1<tab:260,450>\tstartVolume: %2\tvolumeScalar: %3" NL
													"<just:center>DB_Volume: %4",
														%volume,
														%vehicle.ES_StartVolume,
														%vehicle.ES_VolumeScalar,
														%clampedVolume
													);
				} else {
					%volumeString = "supportsVolume: false";
				}
					
				if(%vehicle.ES_GearCount > 1)
				{
					//this debug line is more readable now
					%string = ES_filterString("<just:left>pitch: %1<tab:260,450>\tgear:%2/%3\tvelocity: %4" 	NL
											 "<just:left>progress into gear: %5<just:right>gearPitches: %6->%7"  	NL
											 "<just:left>gearSpeeds(L,C,N):%8,%9,%10<tab:400>\taudioHandleID: %11" 							NL
											 "%12",
												%clampedPitch,
												%gear,
												%vehicle.ES_gearCount,
												%velocityLength,
												%fractOnGear,
												%vehicle.ES_GearPitchStart[%gear],
												%vehicle.ES_GearPitchPeak[%gear],
												%vehicle.ES_GearSpeed[%gear-1], %vehicle.ES_GearSpeed[%gear], %vehicle.ES_GearSpeed[%gear+1],
												%handle,
												%volumeString
											);

					clientcmdbottomprint(%string, 1, 1);
				} else {
					%string = ES_filterString("<just:left>pitch: %1<just:right>velocity: %2" NL
											 "<just:left>startPitch: %3<just:right>velocityscalar: %4" NL
											 "audioHandleID: %5",
											 	%clampedPitch, %velocityLength, %vehicle.ES_StartPitch, %vehicle.ES_VelocityScalar, %handle);
					
					clientcmdbottomprint(%string, 1, 1);
				}
			}

			
		}
	}

	//weird bug with low delay schedules where the velocity randomly fluctuates
	$EngineSound_Schedule = schedule(32, %set, ES_Client_Loop, %lastLoopTime = $Sim::Time);
}