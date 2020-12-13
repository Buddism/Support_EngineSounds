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
        commandToClient(%client, 'ES_markVehicle', %client.getGhostID(%vehicle));
    }
}

function serverCmdES_checkVehicle(%client, %audioHandle, %ghostIndex)
{
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

package ES_Server_Package
{
    function WheeledVehicleData::onAdd (%this, %vehicle)
    {
        %ret = parent::onAdd (%this, %vehicle); //put wheels on dat bad boi

        if(!%this.ES_Enabled)
            return %ret;

        for(%i = 0; %i < clientGroup.getCount(); %i++)
        {
            %client = clientGroup.getObject(%i);
            if(!isObject(%client.ES_AudioSet))
            {
                %client.ES_AudioSet = new simSet();
                %client.add(%client.ES_AudioSet); // yes
            }

            %vehicle.scopeToClient(%client);
        }

        %vehicle.schedule(100, ES_ApplyData);
        ES_SimSet.add(%vehicle);

        return %ret; //probably not important
    }
};
activatePackage(ES_Server_Package);
