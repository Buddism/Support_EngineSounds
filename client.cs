function reloadCES()
{
	exec("./client.cs");
}

//if(!isObject(OptAudioVolumeEngine))
	exec("./EngineSounds_Pane.gui");

if(!isObject(ES_MonitorSet))
	new simSet(ES_MonitorSet);

//this simset is for pitch updates on moving vehicles
if(!isObject(ES_ActiveSet))
	new simSet(ES_ActiveSet);

$ES::Version = "2.0.1";

$EngineAudioType = 8;

if($ES::DebugLevel $= "") //is it undefined?
	$ES::DebugLevel = 0;

if($Pref::ES::AllowVolumeAdjustment $= "")
	$Pref::ES::AllowVolumeAdjustment = true;

if($Pref::ES::DopplerEffect $= "")
	$Pref::ES::DopplerEffect = false;
	

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


//%type can be "Global"
//or a clients object GhostIdx (%ghostID = %client.getGhostID(%vehicle))
function clientCmdES_setVolume(%type, %volumeLevel)
{
	if(!$Pref::ES::AllowVolumeAdjustment)
		return 0;

	%clampedVolumeLevel = mClampF(%volumeLevel, 0.0, 1.0);
	if(%type $= "Global")
	{
		serverConnection.ES_GlobalVolumeLevel = %clampedVolumeLevel;

		return 1;
	} else {
		%vehicle = serverConnection.resolveGhostID(%type | 0);
		if(!isObject(%vehicle) || ! ( %vehicle.getType() & $TypeMasks::VehicleObjectType ) )
			return 0;

		if(ES_ActiveSet.isMember(%vehicle))
			return 0;

		%vehicle.ES_VolumeLevel = %clampedVolumeLevel;
		return 1;
	}
}

function clientCmdES_Handshake(%serverVersion)
{
	commandToServer('ES_Handshake', $ES::Version);
	if($Pref::ES::EnableHandshakeMessage)
	{
		if($ES::Version !$= %serverVersion)
			newChatHud_addLine("\c6This server is running \c3EngineSounds\c6 version \c3"@ %serverVersion @"\c6, you are using version\c3 "@ $ES::Version);
		else newChatHud_addLine("\c6This server supports \c3EngineSounds");
	}
	
	//initialize this value
	serverConnection.ES_GlobalVolumeLevel = 1;

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
function clientCmdES_closestVehicle(%audioHandle, %closestVehicleGID, %StartValues, %scalars, %maxPitch, %gearPitchDelay, %gearCount, %gearSpeeds, %gearPitches, %gearShiftTime, %gearShiftAnims, %audioDescription, %gearVolumeLevels)
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

	ES_RegisterActiveVehicle(%audioHandle, %closestVehicle, %StartValues, %scalars, %maxPitch, %gearPitchDelay, %gearCount, %gearSpeeds, %gearPitches, %gearShiftTime, %gearShiftAnims, %gearVolumeLevels);
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

function ES_RegisterActiveVehicle(%audioHandle, %vehicle, %StartValues, %scalars, %maxPitch, %gearPitchDelay, %gearCount, %gearSpeeds, %gearPitches, %gearShiftTime, %gearShiftAnims, %gearVolumeLevels)
{
	%con = nameToID(serverConnection);
	if(%con.ES_hasBoundHandle[%audioHandle])
		return;

	if(!isObject(%vehicle) || ! ( %vehicle.getType() & $TypeMasks::VehicleObjectType ) )
		return;

	%con.ES_hasBoundHandle[%audioHandle] = true;
	%vehicle.ES_VolumeLevel = 1;

	%vehicle.ES_AudioHandle = %audioHandle;

	%vehicle.ES_StartValues = %StartValues;

	%vehicle.ES_StartPitch  = getWord(%StartValues, 0);
	%vehicle.ES_StartVolume = getWord(%StartValues, 1);

	%vehicle.ES_VelocityScalar = getWord(%scalars, 0);
	%vehicle.ES_VolumeScalar   = getWord(%scalars, 1);

	%vehicle.ES_SupportsVolume = getWordCount(%gearVolumeLevels) + %vehicle.ES_VolumeScalar + %vehicle.ES_StartVolume > 0;

	%gearCount = mClamp(%gearCount, 0, 24);
	if(%gearCount != getWordCount(%gearSpeeds) || %gearCount != (getWordCount(%gearPitches) / 2))
	{
		ES_Debug(1, "invalid gear data from server (mismatch) gearCount: %1, gearSpeedsCount: %2, gearPitchesCount: %3 (total: %4)", %gearCount, getWordCount(%gearSpeeds), getWordCount(%gearPitches) / 2, getWordCount(%gearPitches));
		%gearCount = 0;
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

			%vehicle.ES_GearVolumeStart[%i] = getWord(%gearVolumeLevels, %gearPitchIndex + 0);
			%vehicle.ES_GearVolumePeak [%i] = getWord(%gearVolumeLevels, %gearPitchIndex + 1);

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
		alListener3f("AL_VELOCITY", "0 0 0");
		cancel($EngineSound_Schedule);
		return;
	}
	
	if(isObject(%ctrl = %con.getControlObject()))
	{
		if($Pref::ES::DopplerEffect)
			alListener3f("AL_VELOCITY", vectorScale(%ctrl.getVelocity(), 1.0));

		%myMount = %ctrl.getObjectMount();
	}


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
			//%vel = %vehicle.getVelocity();
			//%fvec = %vehicle.getForwardVector();

			//velocity relative to the facing vector
			//%velocityLength = mAbs(getWord(%fvec, 0) * getWord(%vel, 0) + getWord(%fvec, 1) * getWord(%vel, 1) + getWord(%fvec, 2) * getWord(%vel, 2));
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

				if(%gear + 1 >= %vehicle.ES_GearCount) //its on its last gear - not sure why i made it like this
					%nextGearSpeed = %vehicle.getDataBlock().maxWheelSpeed;
				else
					%nextGearSpeed = %vehicle.ES_GearSpeed[getMin(%gear + 1, %vehicle.ES_GearCount - 1)];

				%currentGearSpeed = %vehicle.ES_GearSpeed[%gear];
				%fractOnGear = (%velocityLength - %currentGearSpeed) / (%nextGearSpeed - %currentGearSpeed);

				%gearPitch  = ES_mLerp( %vehicle.ES_GearPitchStart[%gear],  %vehicle.ES_GearPitchPeak[%gear], %fractOnGear);
				%gearVolume = ES_mLerp(%vehicle.ES_GearVolumeStart[%gear], %vehicle.ES_GearVolumePeak[%gear], %fractOnGear);

				%pitch = %vehicle.ES_StartPitch + %gearPitch;
				%volume = %vehicle.ES_startVolume + %gearVolume;
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

			if($Pref::ES::DopplerEffect)
				alxSource3f(%handle, "AL_VELOCITY", vectorScale(%vehicle.getVelocity(), 1.0)); //this requires a modified openAl32.dll (or 1.0) for doppler effect support

			alxSourcef(%handle, "AL_PITCH", %clampedPitch);

			
			%clampedVolume = mClampF((%vehicle.ES_SupportsVolume ? %volume : 1.0), 0.0, 1.0);
			//for some reason i need to add alxGetChannelVolume() even though i thought it was handled by the engine
			%finalVolume = mClampF(%clampedVolume * %vehicle.ES_VolumeLevel * %con.ES_GlobalVolumeLevel * alxGetChannelVolume($EngineAudioType), 0.0, 1.0);
			
			//	AL_GAIN & AL_GAIN_LINEAR have an odd problem so we need to use AL_CONE_OUTER_GAIN
			if(%vehicle == %myMount && $firstPerson)
				%finalVolume *= $Pref::ES::FirstPersonVolume;
			
			alxSourcef(%handle, "AL_CONE_OUTER_GAIN", %finalVolume);

			if(%vehicle.ES_originalReferenceDistance $= "")
				%vehicle.ES_originalReferenceDistance = alxGetSourceF(%handle, "AL_REFERENCE_DISTANCE");
			
			//weird crapola to REMOVE audio distance scaling
			if(firstWord(alGetString("AL_VERSION")) $= "1.1") //== 1.1 is buggy (THIS is not needed in old openAL)
				alxSourcef(%handle, "AL_REFERENCE_DISTANCE", ((%vehicle == %myMount) ? 0 : %vehicle.ES_originalReferenceDistance));


			if($ES::DebugLevel >= 1 && (%vehicle == %myMount || %vehicle.debug))
			{
				//round and trim the extra 0s (ex: 1.10 => 1.1)
				%velocityLength = 0 + mFloatLength(%velocityLength, 2);
				%fractOnGear 	= 0 + mFloatLength(%fractOnGear, 2);
				%clampedPitch 	= 0 + mFloatLength(%clampedPitch, 2);
				%clampedVolume	= 0 + mFloatLength(%clampedVolume, 2);
				%finalVolume	= 0 + mFloatLength(%finalVolume, 2);
				%volume 		= 0 + mFloatLength(%volume, 2);
					
				if(%vehicle.ES_GearCount > 1)
				{
					if(%vehicle.ES_SupportsVolume)
					{
						%volumeString = ES_filterString("<just:left>volume: %1<tab:400>\tgearVolumes: %2->%3" NL
														"<just:left>Clamped Volume: %4<tab:400>\tFinal Volume: %5",
															%volume,
															%vehicle.ES_GearVolumeStart[%gear],
															%vehicle.ES_GearVolumePeak[%gear],
															%clampedVolume,
															%finalVolume
														);
					} else {
						%volumeString = ES_filterString("supportsVolume: false<tab:400>\tFinal Volume: %1", %finalVolume);
					}
					
					//this debug line is more readable now
					%string = ES_filterString("<just:left>pitch: %1<tab:260,450>\tgear:%2/%3\tvelocity: %4"			NL
											 "<just:left>progress into gear: %5<just:right>gearPitches: %6->%7"		NL
											 "<just:left>gearSpeeds(L,C,N):%8,%9,%10<tab:400>\taudioHandleID: %11"	NL
											 "%12",
												%clampedPitch,
												%gear,
												%vehicle.ES_gearCount - 1,
												%velocityLength,
												%fractOnGear,
												%vehicle.ES_GearPitchStart[%gear],
												%vehicle.ES_GearPitchPeak[%gear],
												%vehicle.ES_GearSpeed[%gear-1], %vehicle.ES_GearSpeed[%gear], %nextGearSpeed,
												%handle,
												%volumeString
											);

					clientcmdbottomprint(%string, 1, 1);
				} else {
					if(%vehicle.ES_SupportsVolume)
					{
						%volumeString = ES_filterString("<just:left>volume: %1<tab:260,450>\tstartVolume: %2\tvolumeScalar: %3" NL
														"<just:center>Clamped Volume: %4",
															%volume,
															%vehicle.ES_StartVolume,
															%vehicle.ES_VolumeScalar,
															%clampedVolume,
															%finalVolume
														);
					} else {
						%volumeString = "supportsVolume: false";
					}

					%string = ES_filterString("<just:left>pitch: %1<just:right>velocity: %2"			NL
											 "<just:left>startPitch: %3<just:right>velocityscalar: %4"	NL
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