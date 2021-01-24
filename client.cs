if(!isObject(ES_MonitorSet))
    new simSet(ES_MonitorSet);

//this simset is for pitch updates on moving vehicles
if(!isObject(ES_ActiveSet))
    new simSet(ES_ActiveSet);

$ES::Version = "1.0.0";

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

function clientCmdES_Handshake(%serverVersion, %coneInsideAngle, %coneOutsideAngle)
{
    commandToServer('ES_Handshake');
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
function clientCmdES_closestVehicle(%audioHandle, %closestVehicleGID, %startPitch, %scalar, %maxPitch, %gearPitchDelay, %gearCount, %gearSpeeds, %gearPitches, %gearShiftTime, %gearShiftAnims, %audioDescription)
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

	// if(%audioDescription.ES_AdjustedVolume == false)
	// {
	// 	%audioDescription.volume *= 3;
	// 	%audioDescription.ES_AdjustedVolume = true;
	// }

    ES_RegisterActiveVehicle(%audioHandle, %closestVehicle, %startPitch, %scalar, %maxPitch, %gearPitchDelay, %gearCount, %gearSpeeds, %gearPitches, %gearShiftTime, %gearShiftAnims);
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


//monitor the logged vehicles from 'ES_Client_LookForVehicles' for exposed audioHandles
function ES_Client_MonitorHandles()
{
    %con = nameToID(serverConnection);
	if(!isObject(%con))
	{
		cancel($ES_MonitorSchedule);
		return;
	}

    %CIA = $ES::InsideAngle;  //Cone Inner Angle
    %COA = $ES::OutsideAngle; //ConeOutterAngle
    %set = nameTOID("ES_MonitorSet");
    %c = %set.getCount();
    for(%k = %c - 1; %k >= 0; %k--)
    {
        %obj = %set.getObject(%k);
        %handleIndex = %obj.ES_HandlePosition;
        for(%i = %handleIndex - 16; %i <= %handleIndex + 16; %i++)
        {
            if(alxIsPlaying(%i) && alxGetSourceI(%i, "AL_LOOPING") && alxGetSourceI(%i, "AL_CONE_INNER_ANGLE") == %CIA && alxGetSourceI(%i, "AL_CONE_OUTER_ANGLE") == %COA && !%con.ES_AudioHandle[%i]) //'fingerprinting' of the audio handle
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

function ES_RegisterActiveVehicle(%audioHandle, %vehicle, %startPitch, %scalar, %maxPitch, %gearPitchDelay, %gearCount, %gearSpeeds, %gearPitches, %gearShiftTime, %gearShiftAnims)
{
    %con = nameToID(serverConnection);
    if(%con.ES_hasBoundHandle[%audioHandle])
        return;

    if(!isObject(%vehicle) || ! ( %vehicle.getType() & $TypeMasks::VehicleObjectType ) )
        return;

    %con.ES_hasBoundHandle[%audioHandle] = true;

    %vehicle.ES_AudioHandle = %audioHandle;

    %vehicle.ES_StartPitch = %startPitch;
    %vehicle.ES_VelocityScalar = %scalar;

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
		
    %vehicle.ES_maxPitch        = mClampF(%maxPitch, 0.0, 10.0);

    %vehicle.ES_lastPitch = %startPitch; //initialize this var

    ES_DEBUG(2, "startPitch(%1), scalar(%2), maxPitch(%3), gearPitchDelay(%4), gearCount(%5), gearShiftTime(%6), gearShiftAnims(%7)", %startPitch, %scalar, %maxPitch, %gearPitchDelay, %gearCount, %gearShiftTime, %gearShiftAnims);

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
    if(!isObject(%set) || %set.getCount() == 0)
        return;

	%con = serverConnection;
	if(!isObject(%con))
	{
		cancel($EngineSound_Schedule);
		return;
	}

    if($ES::DebugLevel >= 1 && isObject(%ctrl = %con.getControlObject()))
        %myMount = %ctrl.getObjectMount();

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
                if($Sim::Time - %vehicle.ES_lastGearShiftTime < %vehicle.ES_GearPitchDelay)
                {
                    %pitch = ES_mLerp(%vehicle.ES_lastPitch, %pitch, ($Sim::Time - %vehicle.ES_lastGearShiftTime) / %vehicle.ES_GearPitchDelay);
                } else
                    %vehicle.ES_lastPitch = %pitch;
            } else {
                %pitch = %vehicle.ES_StartPitch + %velocityLength / %vehicle.ES_VelocityScalar;
            }

            %newPitch = mClampF(%pitch, 0.001, %vehicle.ES_maxPitch);
            alxSourcef(%handle, "AL_PITCH", %newPitch);

			//alxSourcef(%handle, "AL_GAIN", 			1 / 3);
			//alxSourcef(%handle, "AL_GAIN_LINEAR", 	1 / 3);

            if($ES::DebugLevel >= 1 && %vehicle == %myMount)
            {
                if(%vehicle.ES_GearCount > 1)
                {
                    //this debug line is very long
                    clientcmdbottomprint("<just:left>pitch: " @ %newPitch @ "<just:center>gear:" SPC %gear @"/"@ %vehicle.ES_gearCount SPC "<just:right>velocity: "@ %velocityLength NL "<just:left>progress into gear: "@ %fractOnGear @ "<just:right>gearPitches: "@ %vehicle.ES_GearPitchStart[%gear] @"->"@ %vehicle.ES_GearPitchPeak[%gear] NL "<just:center>gearSpeeds(L,C,N):"@ %vehicle.ES_GearSpeed[%gear-1] @","@ %vehicle.ES_GearSpeed[%gear] @","@ %vehicle.ES_GearSpeed[%gear+1], 1, 1);
                } else
                    clientcmdbottomprint("<just:left>pitch: " @ %newPitch @ "<just:right>velocity: "@ %velocityLength NL "<just:left>startPitch: " @ %vehicle.ES_StartPitch @"<just:right>velocityscalar: "@ %vehicle.ES_VelocityScalar, 1, 1);
            }
        }
    }

    //weird bug with low delay schedules where the velocity randomly fluctuates
    $EngineSound_Schedule = schedule(32, %set, ES_Client_Loop, %lastLoopTime = $Sim::Time);
}
