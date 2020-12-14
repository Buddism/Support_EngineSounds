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

function clientCmdES_closestVehicle(%audioHandle, %closestVehicleGID, %startPitch, %scalar)
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

    ES_RegisterActiveVehicle(%audioHandle, %closestVehicle, %startPitch, %scalar);
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

function ES_RegisterActiveVehicle(%audioHandle, %vehicle, %startPitch, %scalar)
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

    ES_ActiveSet.add(%vehicle);

    //newchathud_addline(RegistedActiveVehicle SPC %vehicle SPC %vehicle.getDataBlock().shapefile);

    if(!isEventPending($EngineSound_Schedule))
        ES_Client_Loop();
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
        }
    }

    $EngineSound_Schedule = schedule(1, %set, ES_Client_Loop);
}
