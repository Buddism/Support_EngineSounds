function ClientCMDES_MarkVehicle(%vehicleGhostID)
{
    %vehicle = serverConnection.resolveGhostID(%vehicleGhostID);
    if(!isObject(ES_Client_MonitorVehicles))
        new simSet(ES_Client_MonitorVehicles);

    %lastHandle = alxplay(AdminSound); // get the most recent audio handle ID (hacky)
    alxStop(%lastHandle); //stop it

    //mark its handler because handlers arent active until the player goes near them, but they keep using an older handle id?
    %vehicle.ES_HandlePosition = %lastHandle;
    ES_Client_MonitorVehicles.add(%vehicle);

    if(!isEventPending($ES_MonitorSchedule))
        ES_Client_MonitorHandles();
}
function ES_Client_MonitorHandles(%lastHandle, %lastHandleTime)
{
    if(%lastHandle == 0 || getSimTime() - %lastHandleTime > 1000)
    {
        %lastHandleTime = getSimTime();
        %lastHandle = alxplay(AdminSound); // get the most recent audio handle ID (hacky)
        alxStop(%lastHandle); //stop it
    }
    %set = nameTOID("ES_Client_MonitorVehicles");
    %c = %set.getCount();
    for(%k = 0; %k < %c; %k++)
    {
        %obj = %set.getObject(%k);
        %handleIndex = %obj.ES_HandlePosition;
        for(%i = %handleIndex - 4; %i <= %handleIndex + 4; %i++)
        {
            if(alxIsPlaying(%i))
            {
                %lastHandle = %i;
                %lastHandleTime = getSimTime();
                if(alxGetSourceI(%i, "AL_LOOPING") && alxGetSourceF(%i, "AL_GAIN") $= 0.85 && !$ES_AudioHandle[%i])
                {
                    //handshake is probably overkill but here we are
                    commandToServer('ES_checkVehicle', %i, serverConnection.getGhostID(%obj));
                    $ES_checkHandle[%i] = true;
                    $ES_AudioHandle[%i] = true;
                }
            }
        }
    }
    $ES_MonitorSchedule = schedule(1, 0, ES_Client_MonitorHandles, %lastHandle, %lastHandleTime);
}
function clientCmdES_ConfirmHandle(%audioHandle, %ghostIndex, %startPitch, %scalar)
{
    if(!$ES_checkHandle[%audioHandle])
        return;

    $ES_checkHandle[%audioHandle] = false;
    %vehicle = serverConnection.resolveGhostID(%ghostIndex);
    if(isObject(%vehicle))
    {
        %vehicle.ES_AudioHandle = %audioHandle;

        %vehicle.ES_StartPitch = %startPitch;
        %vehicle.ES_VelocityScalar = %scalar;

        //this simset is for pitch updates on moving vehicles
        if(!isObject(ES_Vehicle_SimSet))
            new simSet(ES_Vehicle_SimSet);

        ES_Vehicle_SimSet.add(%vehicle);

        if(!isEventPending($EngineSound_Schedule))
            ES_Client_Loop();
    }
}
function ES_Client_Loop()
{
    cancel($EngineSound_Schedule);
    %set = nameToID("ES_Vehicle_SimSet");
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
            %pitch = %vehicle.ES_StartPitch + vectorLen(%vehicle.getVelocity()) / %vehicle.ES_VelocityScalar;
            %pitch = mClampF(%pitch, -100.0, 100.0);
            alxSourcef(%handle, "AL_PITCH", %pitch);
        } else {
            %set.remove(%vehicle);
        }
    }

    $EngineSound_Schedule = schedule(1, %set, ES_Client_Loop);
}
