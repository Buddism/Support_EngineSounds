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
	maxDistance = 30;
	type = $SimAudioType;
};


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

        //delay is important
        schedule(%client.getPing() * 2, %client, commandToClient, %client, 'ES_createAudio', %client.getGhostID(%vehicle), %soundDB, %startPitch, %velocityScalar);
    }
}

function JeepVehicle::onAdd(%this, %vehicle)
{
    for(%i = 0; %i < clientGroup.getCount(); %i++)
    {
        %vehicle.scopeToClient(clientGroup.getObject(%i));
    }

    %vehicle.schedule(100, ES_ApplyData);
    parent::onAdd(%this, %vehicle);
}
