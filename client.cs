if(!isObject(ES_MonitorSet))
    new simSet(ES_MonitorSet);

//this simset is for pitch updates on moving vehicles
if(!isObject(ES_ActiveSet))
    new simSet(ES_ActiveSet);

function clientCmdES_Handshake()
{
    commandToServer('ES_Handshake');
    if(!isEventPending($ES_ScanVehicleSchedule))
        ES_Client_LookForVehicles();
}
function ES_MarkVehicle(%vehicle)
{
    if(!isObject(%vehicle) || ! ( %vehicle.getType() & $TypeMasks::VehicleObjectType ) )
        return;

    if(ES_ActiveSet.isMember(%vehicle) || ES_MonitorSet.isMember(%vehicle))
        return;

    %lastHandle = alxplay(AdminSound); // get the most recent audio handle ID (hacky)
    alxStop(%lastHandle); //stop it

    //mark its handler because handlers arent active until the player goes near them, but they keep using an older handle id?
    %vehicle.ES_HandlePosition = %lastHandle;
    ES_MonitorSet.add(%vehicle);

    //SORT THE VEHICLES
    %sorter = new GuiTextListCtrl();

    for(%i = 0; %i < ES_MonitorSet.getCount(); %i++)
        %sorter.addRow(%i, ES_MonitorSet.getObject(%i));

	%sorter.sortNumerical(0, false); //oldest car (lowest ID) is last in the list

    for(%i = 0; %i < ES_MonitorSet.getCount(); %i++)
        %dat = %dat SPC %sorter.getRowText(%i);

    ES_MonitorSet.dataList = trim(%dat);
    %sorter.delete();

    if(!isEventPending($ES_MonitorSchedule))
        ES_Client_MonitorHandles();
}

//this is a lot of args
function clientCmdES_closestVehicle(%audioHandle, %closestVehicleGID, %startPitch, %scalar, %maxPitch, %gearPitchDelay, %gearCount, %gearSpeeds, %gearPitches, %gearShiftTime, %gearShiftAnims)
{
    %con = nameToID(serverConnection);
    if(!%con.ES_allowCheck[%audioHandle])
        return;

    %closestVehicle = %con.resolveGhostID(%closestVehicleGID);
    if(!isObject(%closestVehicle) || ! ( %closestVehicle.getType() & $TypeMasks::VehicleObjectType)  || %closestVehicle.ES_AudioHandle != 0)
        return;

    if(!ES_MonitorSet.isMember(%closestVehicle))
        return;

    ES_MonitorSet.remove(%closestVehicle);
    %con.ES_allowCheck[%audioHandle] = false;

    ES_RegisterActiveVehicle(%audioHandle, %closestVehicle, %startPitch, %scalar, %maxPitch, %gearPitchDelay, %gearCount, %gearSpeeds, %gearPitches, %gearShiftTime, %gearShiftAnims);
}
function ES_Client_LookForVehicles()
{
    cancel($ES_ScanVehicleSchedule);

    %con = nameToID(serverConnection);
    if(!isObject(%con))
    {
        $ES_ScanVehicleSchedule = schedule(1, 0, ES_Client_LookForVehicles);
        return;
    }

    %c = %con.getCount();
    %s = getMax(%c - 1000, 0);
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
function ES_Client_MonitorHandles()
{
    %con = nameToID(serverConnection);

    %set = nameTOID("ES_MonitorSet");
    %dat = %set.dataList;
    %c = getWordCount(%dat);
    for(%k = %c - 1; %k >= 0; %k--)
    {
        %obj = getWord(%dat, %k);
        if(!isObject(%obj) || %obj.ES_AudioHandle != 0)
        {
            //newchathud_addline(removedat);
            %dat = removeWord(%dat, %k);
            continue;
        }
        %handleIndex = %obj.ES_HandlePosition;
        for(%i = %handleIndex - 16; %i <= %handleIndex + 16; %i++)
        {
            if(alxIsPlaying(%i) && alxGetSourceI(%i, "AL_LOOPING") && alxGetSourceF(%i, "AL_GAIN") $= 0.85 && !%con.ES_AudioHandle[%i])
            {
                //handshake is probably overkill but here we are
                commandToServer('ES_newAudioHandle', %i);
                %con.ES_allowCheck[%i] = true;
                %con.ES_AudioHandle[%i] = true;

                //newchathud_addline(newAudioHandle SPC %obj SPC %obj.getDataBlock().shapefile);
                //break out of this loop
                break;
            }
        }
    }
    %set.dataList = %dat;
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

    %gearCount = mClamp(%gearCount, 0, 10);
    if(%gearCount != getWordCount(%gearSpeeds) || %gearCount != (getWordCount(%gearPitches) / 2))
    {
        %gearCount = 0;
        newchathud_addline(invalid SPC getWordCount(%gearSpeeds) SPC getWordCount(%gearPitches) SPC %gearCount);
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

    %vehicle.ES_GearPitchDelay = mClampF(%gearPitchDelay   , 0.0, 32.0 );
    %vehicle.ES_gearShiftTime   = mClampF(%gearShiftTime, 0.0, 10.0); //defaults to 0 (instant gear change)
    %vehicle.ES_maxPitch = mClampF(%maxPitch, 0.0, 10.0);

    %vehicle.ES_lastPitch = %startPitch; //initialize this var

    ES_ActiveSet.add(%vehicle);

    //newchathud_addline("registed audio handle ["@ %audioHandle @"] for ["@ %vehicle @"]"@ %vehicle.getDataBlock().shapefile);
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
            if(%vehicle.ES_GearCount > 1)
            {
                %velocityLength = vectorLen(%vehicle.getVelocity());
                if($Sim::Time - %vehicle.ES_lastGearShiftTime > %vehicle.ES_gearShiftTime)
                {
                    for(%k = %vehicle.ES_GearCount - 1; %k >= 0; %k--)
                        if(%velocityLength > %vehicle.ES_GearSpeed[%k])
                        {
                            %gear = %k;
                            if(%gear != %vehicle.ES_lastGear)
                            {
                                %nextGearSpeed = %vehicle.ES_GearSpeed[getMin(%gear + 1, %vehicle.ES_GearCount - 1)];
                                %vehicle.ES_ShiftedUp = %gear > %vehicle.ES_lastGear; //if the new gear is higher than the old gear

                                %vehicle.ES_lastGearShiftTime = $Sim::Time;
                                %vehicle.ES_lastGear = %gear;
                                if(%vehicle.ES_GearShiftAnim[%k] !$= "")
                                {
                                    %vehicle.playThread(0, %vehicle.ES_GearShiftAnim[%k]); //play the animation on the client side
                                    %vehicle.schedule(32, playThread, 1, "steering"); // weird stuff
                                }
                            }
                            break;
                        }
                } else {
                    %gear = %vehicle.ES_lastGear;
                }

                %nextGearSpeed = %vehicle.ES_GearSpeed[getMin(%gear + 1, %vehicle.ES_GearCount - 1)];
                %fractOnGear = %velocityLength / %nextGearSpeed;

                %gearPitch = ES_mLerp(%vehicle.ES_GearPitchStart[%gear], %vehicle.ES_GearPitchPeak[%gear], %fractOnGear);

                %pitch = %vehicle.ES_StartPitch + %gearPitch;
                if($Sim::Time - %vehicle.ES_lastGearShiftTime < %vehicle.ES_GearPitchDelay)
                {
                    %pitch = ES_mLerp(%vehicle.ES_lastPitch, %pitch, ($Sim::Time - %vehicle.ES_lastGearShiftTime) / %vehicle.ES_GearPitchDelay);
                } else %vehicle.ES_lastPitch = %pitch;
            } else {
                %pitch = %vehicle.ES_StartPitch + vectorLen(%vehicle.getVelocity()) / %vehicle.ES_VelocityScalar;
            }

            %newPitch = mClampF(%pitch, 0.001, %vehicle.ES_maxPitch);
            alxSourcef(%handle, "AL_PITCH", %newPitch);
            //clientcmdbottomprint(%pitch NL %newPitch NL %gear NL ($Sim::Time - %vehicle.ES_lastGearShiftTime) SPC %vehicle.ES_GearPitchDelay, 1, 1);
        }
    }

    //weird bug with low delay schedules where the velocity can randomly go lower than it was last schedule even though we are accelerating
    $EngineSound_Schedule = schedule(32, %set, ES_Client_Loop, %lastLoopTime = $Sim::Time);
}
