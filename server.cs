function reloadES()
{
    exec("./server.cs");
}

datablock AudioDescription(AudioEngineLooping3d : AudioMusicLooping3d)
{
    //if you modify volume make sure coneOutsideVolume is the same
    volume = 1;
    coneOutsideVolume = 1;

	isLooping = 1;
	is3D = 1;

	ReferenceDistance = 20;
	maxDistance = 150;

    type = $SimAudioType;

    //'fingerprint' for detection since who would ever use these values for an audiodescription?
    coneInsideAngle = 133;
    coneOutsideAngle = 337;

};

function serverCmdES_newAudioHandle(%client, %audioHandle)
{
    //talk("newAudioHandleRequest:" SPC %audioHandle);

    if(!%client.hasEngineSounds)
        return;

    if(!isObject(%ctrl = %client.getControlObject()))
        return;

    initContainerRadiusSearch(%ctrl.getTransform(), 150, $TypeMasks::VehicleObjectType);
    while( isObject(%foundVehicle = containerSearchNext()) )
    {
        if(!%foundVehicle.getDataBlock().ES_Enabled) //vehicle does not have ES support
            continue;

        if(!%foundVehicle.ES_Playing) //engine is not playing
            continue;

        if(%client.ES_AudioSet.isMember(%foundVehicle)) //vehicle has already been handled
            continue;

        %client.ES_AppendReply(%audioHandle, %foundVehicle);
        %client.ES_AudioSet.add(%foundVehicle);
        break;
    }
}
function GameConnection::ES_AppendReply(%client, %audioHandle, %vehicle)
{
    if(!isObject(%vehicle))
        return;

    %ghostID = %client.getGhostID(%vehicle);
    if(%ghostID == -1) //sometimes the game doesnt get a ghost id immediately
    {
        //talk("BAD GHOST ID - WAITING");
        %client.ES_AppendReply = %client.schedule(100, ES_AppendReply, %audioHandle, %vehicle);
        return;
    }

    %vehDB = %vehicle.getDataBlock();
    commandToClient(%client, 'ES_closestVehicle', %audioHandle, %ghostID, %vehDB.ES_StartPitch, %vehDB.ES_VelocityScalar, %vehDB.ES_maxPitch, %vehDB.ES_GearPitchDelay, %vehDB.ES_gearCount, %vehDB.ES_gearSpeeds, %vehDB.ES_gearPitches, %vehDB.ES_gearShiftTime, %vehDB.ES_GearShiftAnims);

    //talk("sent: " @ %client.getGhostID(%vehicle));
}

function serverCMDES_handshake(%client)
{
    %client.hasEngineSounds = true;
    if(!isObject(%client.ES_AudioSet))
    {
        %client.ES_AudioSet = new simSet();
        %client.add(%client.ES_AudioSet); // auto delete the clients simset on disconnect
    }
}
function Vehicle::ES_EngineStop(%this)
{
    if(!%this.ES_Playing)
        return;

    %count = ClientGroup.getCount();
    for(%i = 0; %i < %count; %i++)
    {
        %client = ClientGroup.getObject(%i);
        if(%client.hasEngineSounds)
        {
            commandToClient(%client, 'ES_stopEngine', %client.getGhostID(%this));
            %client.ES_AudioSet.remove(%this);
        }
    }
    %this.stopAudio(1);
    if(isObject(%this.getDataBlock().ES_EngineStopSound))
        %this.playAudio(1, %this.getDataBlock().ES_EngineStopSound);

    %this.ES_Playing = false;
}
function Vehicle::ES_EngineStart(%this)
{
    if(%this.ES_Playing)
        return;

    %startSound = %this.getDataBlock().ES_EngineStartSound;
    %startDelay = %this.getDataBlock().ES_EngineStartDelay;
    if(isObject(%startSound) && %startDelay > 0)
    {
        %this.playAudio(1, %startSound);
        %this.schedule(%startDelay, ES_EngineStart_Actual);
    } else {
        %this.ES_EngineStart_Actual();
    }
}
function Vehicle::ES_EngineStart_Actual(%this)
{
    %this.playAudio(1, %this.getDataBlock().ES_SoundDB);
    %this.ES_Playing = true;

    %count = ClientGroup.getCount();
    for(%i = 0; %i < %count; %i++)
    {
        %client = ClientGroup.getObject(%i);
        if(%client.hasEngineSounds)
            commandToClient(%client, 'ES_RemarkVehicle', %client.getGhostID(%this));
    }
}

package ES_Server_Package
{
    function GameConnection::AutoAdminCheck(%client)
    {
        //simple handshake system
        commandToClient(%client, 'ES_Handshake', AudioEngineLooping3d.coneInsideAngle, AudioEngineLooping3d.coneOutsideAngle);
        return parent::AutoAdminCheck(%client);
    }

    function WheeledVehicleData::onAdd (%this, %vehicle)
    {
        %ret = parent::onAdd (%this, %vehicle); //put wheels on dat bad boi

        if(!%this.ES_Enabled)
            return %ret;

        for(%i = 0; %i < clientGroup.getCount(); %i++)
        {
            %client = clientGroup.getObject(%i);
            %vehicle.scopeToClient(%client);
        }

        //vehicle stereo plays in slot 0
        //%vehicle.playAudio(1, %this.ES_SoundDB);
        %vehicle.ES_Playing = false;
        return %ret; //probably not important
    }
    function Vehicle::onDriverLeave (%obj, %player)
    {
        if(%obj.getDataBlock().ES_Enabled)
            %obj.ES_EngineStop();

        return parent::onDriverLeave (%obj, %player);
    }
    function Armor::onMount (%this, %obj, %vehicle, %node)
    {
        if(%vehicle.getDataBlock().ES_Enabled && %node == 0 && %vehicle.getControllingClient() == 0)
            %vehicle.ES_EngineStart();

        return parent::onMount(%this, %obj, %vehicle, %node);
    }
};
activatePackage(ES_Server_Package);
