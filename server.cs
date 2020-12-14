function reloadES()
{
    exec("./server.cs");
}

datablock AudioDescription(AudioEngineLooping3d : AudioMusicLooping3d)
{
    volume = 0.9781; // (this is the only important var if you make a new audio description)
	isLooping = 1;
	is3D = 1;
	ReferenceDistance = 10;
	maxDistance = 50;
	type = $SimAudioType;
};

function serverCmdES_newAudioHandle(%client, %audioHandle)
{
    if(!%client.hasEngineSounds)
        return;

    if(!isObject(%ctrl = %client.getControlObject()))
        return;

    initContainerRadiusSearch(%ctrl.getTransform(), 50, $TypeMasks::VehicleObjectType);
    while( isObject(%foundVehicle = containerSearchNext()) )
    {
        if(!%foundVehicle.getDataBlock().ES_Enabled)
            continue;

        if(%client.ES_AudioSet.isMember(%foundVehicle)) //vehicle has already been handled
            continue;

        commandToClient(%client, 'ES_closestVehicle', %audioHandle, %client.getGhostID(%foundVehicle), %foundVehicle.getDataBlock().ES_StartPitch, %foundVehicle.getDataBlock().ES_VelocityScalar);
        %client.ES_AudioSet.add(%foundVehicle);
        break;
    }
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

package ES_Server_Package
{
    function GameConnection::AutoAdminCheck(%client)
    {
        //simple handshake system
        commandToClient(%client, 'ES_Handshake');
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

        %vehicle.playAudio(0, %this.ES_SoundDB);
        return %ret; //probably not important
    }
};
activatePackage(ES_Server_Package);
