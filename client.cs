function clientCmdES_createAudio(%vehicleGhostID, %engineSound, %startPitch, %velocityScalar) //engineSound is unused
{
    $vehicle = %vehicle = serverConnection.resolveGhostID(%vehicleGhostID);

    if(!isObject(%vehicle) || !(%vehicle.getType() & $TypeMasks::VehicleObjectType))
        return;

    %newHandle = alxplay(AdminSound); // get the most recent audio handle ID (hacky)
    alxstop(%newHandle); //stop it

    %description = %engineSound.getDescription();
    %rDist = %description.ReferenceDistance;
	%mDist = %description.maxDistance;
    %AL_GAIN = 0.85; // turns into this somehow (.volume)

    for(%handleIndex = %newHandle; %handleIndex >= %newHandle - 32; %handleIndex--) //search a last 10 audio handles
    {
        if(alxIsPlaying(%handleIndex) && alxGetSourceI(%handleIndex, "AL_LOOPING")) //is the audio handle looping?
        {
            if($ES_AudioHandle[%handleIndex])
                continue;

            if(alxGetSourceF(%handleIndex, "AL_MAX_DISTANCE") == %mDist && alxGetSourceF(%handleIndex, "AL_GAIN") == %AL_GAIN && alxGetSourceF(%handleIndex, "AL_REFERENCE_DISTANCE") == %rDist) // compare the alx enum vars to the description for more accuracy
            {
                %gotHandle = true;
                alxSourceF(%handleIndex, "AL_GAIN", 1); // reset audio volume to 1
                break;
            }
        }
    }

    if(!%gotHandle)
        return;

    $ES_AudioHandle[%handleIndex] = %vehicle;
    %vehicle.ES_AudioHandle = %handleIndex;
    %startPitch = mClampF(%startPitch, -100.0, 100.0);

    %vehicle.ES_StartPitch = %startPitch;
    alxSourcef(%handle, "AL_PITCH", %startPitch);

    %vehicle.ES_VelocityScalar = %velocityScalar;
    if(!isObject(ES_Vehicle_SimSet))
        new simSet(ES_Vehicle_SimSet);

    ES_Vehicle_SimSet.add(%vehicle);

    if(!isEventPending($EngineSound_Schedule))
        ES_Client_Loop();
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
        if( ! (%vehicle.getType() & $TypeMasks::VehicleObjectType) ) //there might be a better way to do this
        {
            %set.remove(%vehicle);
            continue;
        }

        if(alxIsPlaying(%handle) && alxGetSourceI(%handleIndex, "AL_LOOPING")) // is the audio handle still playing?
        {
            %pitch = %vehicle.ES_StartPitch + vectorLen(%vehicle.getVelocity()) / %vehicle.ES_VelocityScalar;
            %pitch = mClampF(%pitch, -100.0, 100.0);
            alxSourcef(%handle, "AL_PITCH", %pitch);
            clientcmdBottomprint(%pitch, 1);
        } else {
            %set.remove(%vehicle);
        }
    }

    $EngineSound_Schedule = schedule(1, %set, ES_Client_Loop);
}
