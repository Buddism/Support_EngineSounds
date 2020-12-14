function clientCmdES_Handshake()
{
    commandToServer('ES_Handshake');
}
function ClientCMDES_MarkVehicle(%vehicleGhostID)
{
    %vehicle = serverConnection.resolveGhostID(%vehicleGhostID);
    if(!isObject(%vehicle) || ! ( %vehicle.getType() & $TypeMasks::VehicleObjectType ) )
        return;

    if(!isObject(ES_MonitorSet))
        new simSet(ES_MonitorSet);

    if(ES_MonitorSet.isMember(%vehicle))
        return;

    %lastHandle = alxplay(AdminSound); // get the most recent audio handle ID (hacky)
    alxStop(%lastHandle); //stop it

    //mark its handler because handlers arent active until the player goes near them, but they keep using an older handle id?
    %vehicle.ES_HandlePosition = %lastHandle;
    ES_MonitorSet.add(%vehicle);


    %sorter = new GuiTextListCtrl();

    for(%i = 0; %i < ES_MonitorSet.getCount(); %i++)
        %sorter.addRow(%i, ES_MonitorSet.getObject(%i));

	%sorter.sortNumerical(0, false); //oldest car (lowest ID) is last in the list

    for(%i = 0; %i < ES_MonitorSet.getCount(); %i++)
        %dat = %dat SPC %sorter.getRowText(%i);

    ES_MonitorSet.dataList = trim(%dat);
    //newchathud_addline(getWordCount(trim(%dat)));
    //newchathud_addline(strreplace(trim(%dat), " ", ", "));
    %sorter.delete();

    if(!isEventPending($ES_MonitorSchedule))
        ES_Client_MonitorHandles();

    //newchathud_addline($COUNT++);
}

function clientCmdES_ConfirmHandle(%audioHandle, %ghostIndex, %startPitch, %scalar)
{
    if(!$ES_checkHandle[%audioHandle])
        return;

    $ES_checkHandle[%audioHandle] = false;
    %vehicle = serverConnection.resolveGhostID(%ghostIndex);

    if(!isObject(%vehicle) || ! ( %vehicle.getType() & $TypeMasks::VehicleObjectType ) )
        return;

    %vehicle.ES_AudioHandle = %audioHandle;

    %vehicle.ES_StartPitch = %startPitch;
    %vehicle.ES_VelocityScalar = %scalar;

    //this simset is for pitch updates on moving vehicles
    if(!isObject(ES_ActiveSet))
        new simSet(ES_ActiveSet);

    ES_ActiveSet.add(%vehicle);

    if(!isEventPending($EngineSound_Schedule))
        ES_Client_Loop();
}

function ES_Client_MonitorHandles()
{
    %set = nameTOID("ES_MonitorSet");
    %dat = %set.dataList;
    %c = getWordCount(%dat);
    for(%k = %c - 1; %k >= 0; %k--)
    {
        %obj = getWord(%dat, %k);
        //echo(%k SPC %obj);
        if(!isObject(%obj))
        {
            %dat = removeWord(%dat, %k);
            continue;
        }
        %handleIndex = %obj.ES_HandlePosition;
        for(%i = %handleIndex - 16; %i <= %handleIndex + 16; %i++)
        {
            if(alxIsPlaying(%i))
            {
                %lastHandle = %i;
                %lastHandleTime = getSimTime();
                if(alxGetSourceI(%i, "AL_LOOPING") && alxGetSourceF(%i, "AL_GAIN") $= 0.85 && !$ES_AudioHandle[%i])
                {
                    //handshake is probably overkill but here we are
                    commandToServer('ES_checkVehicle', %i, serverConnection.getGhostID(%obj));
                    //newchathud_addline(newHandleHook);

                    %set.remove(%obj);
                    %dat = removeWord(%dat, %k);

                    $ES_checkHandle[%i] = true;
                    $ES_AudioHandle[%i] = true;

                    //break out of this loop
                    break;
                }
            }
        }
    }
    %set.dataList = %dat;
    if(%set.getCount() == 0) //we are not looking for anything anymore
        return;

    $ES_MonitorSchedule = schedule(1, 0, ES_Client_MonitorHandles);
}

function ES_Client_Loop()
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
            %pitch = %vehicle.ES_StartPitch + vectorLen(%vehicle.getVelocity()) / %vehicle.ES_VelocityScalar;
            %pitch = mClampF(%pitch, -100.0, 100.0);
            alxSourcef(%handle, "AL_PITCH", %pitch);
        } else {
            %set.remove(%vehicle);
        }
    }

    $EngineSound_Schedule = schedule(1, %set, ES_Client_Loop);
}
