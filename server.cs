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
exec("./example.cs");

//holds engine sound vehicles to sync to clients
if(!isObject(ES_SimSet))
    new SimSet(ES_SimSet);

function Vehicle::ES_ApplyData(%vehicle)
{
    %data = %vehicle.getDataBlock();
    %soundDB = %data.ES_SoundDB;
    %vehicle.playAudio(0, %soundDB);

    %startPitch = %data.ES_StartPitch;
    %velocityScalar = %data.ES_VelocityScalar;

    %vTranform = %vehicle.getTransform();

    for(%i = 0; %i < clientGroup.getCount(); %i++)
    {
        %client = clientGroup.getObject(%i);
        if(%client.hasES)
            commandToClient(%client, 'ES_markVehicle', %client.getGhostID(%vehicle));
    }
}

function serverCmdES_checkVehicle(%client, %audioHandle, %ghostIndex)
{
    if(!%client.hasES)
        return;

    %actualVehicle = %client.resolveObjectFromGhostIndex(%ghostIndex); // nice func name here
    if(!isObject(%actualVehicle) || ! (%actualVehicle.getType() & $TypeMasks::VehicleObjectType))
        return;

    if(!isObject(%client.ES_AudioSet))
        %client.ES_AudioSet = new simSet();

    if(isObject(%ctrl = %client.getControlObject()) && !%client.ES_AudioSet.isMember(%actualVehicle))
    {
        initContainerRadiusSearch(%ctrl.getTransform(), 30, $TypeMasks::VehicleObjectType);
        while( isObject(%foundVehicle = containerSearchNext()) )
        {
            if(%client.ES_AudioSet.isMember(%foundVehicle)) //vehicle has already been handled
                continue;

            if(%foundVehicle == %actualVehicle)
            {
                commandToClient(%client, 'ES_ConfirmHandle', %audioHandle, %ghostIndex, %actualVehicle.getDataBlock().ES_StartPitch, %actualVehicle.getDataBlock().ES_VelocityScalar);
                %client.ES_AudioSet.add(%foundVehicle);
                break;
            }
        }
    }
}

function serverCMDES_handshake(%client)
{
    %client.hasES = true;
}

package ES_Server_Package
{
    function GameConnection::AutoAdminCheck(%client)
    {
        //simple handshake system
        commandToClient(%client, 'ES_Handshake');
        return parent::AutoAdminCheck(%client);
    }
    //dont know if ghosting is possible (leaving it in anyway)
    function GameConnection::onClientJoinGame(%this)
    {
        if(%client.hasES)
            for(%i = 0; %i < ES_SimSet.getCount(); %i++)
                commandToClient(%this, 'ES_markVehicle', %this.getGhostID(ES_SimSet.getObject(%i))); //tell the client about vehicles to mark

        return parent::onClientJoinGame(%this);
    }
    function WheeledVehicleData::onAdd (%this, %vehicle)
    {
        %ret = parent::onAdd (%this, %vehicle); //put wheels on dat bad boi

        if(!%this.ES_Enabled)
            return %ret;

        for(%i = 0; %i < clientGroup.getCount(); %i++)
        {
            %client = clientGroup.getObject(%i);
            if(%client.hasES && !isObject(%client.ES_AudioSet))
            {
                %client.ES_AudioSet = new simSet();
                %client.add(%client.ES_AudioSet); // auto delete the clients simset on disconnect
            }

            %vehicle.scopeToClient(%client);
        }

        %vehicle.schedule(100, ES_ApplyData);
        ES_SimSet.add(%vehicle);

        //clients should load this in ghosting objects loading stage
        %vehicle.setScopeAlways();
        return %ret; //probably not important
    }
};
activatePackage(ES_Server_Package);
