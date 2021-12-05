function reloadSES()
{
	exec("./server.cs");
}

$ES::Version = "2.0.1";
$EngineAudioType = 8;

exec("./events.cs");

datablock AudioDescription(AudioEngineLooping3d : AudioMusicLooping3d)
{
	//if you modify volume make sure coneOutsideVolume is the same
	volume = 1;
	coneOutsideVolume = 1;

	isLooping = 1;
	is3D = 1;

	//mod looks for the .248
	ReferenceDistance = 20.248;
	maxDistance = 150.248;

	type = $EngineAudioType;

	//these values are important
	coneInsideAngle = 0;
	coneOutsideAngle = 0;
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
	//dont think i need to sent it anymore but just incase for later
	%audioDescData = %vehDB.ES_SoundDB.getDescription();

	%scalars = %vehDB.ES_VelocityScalar SPC %vehDB.ES_VolumeScalar;
	%startValues = %vehDB.ES_StartPitch SPC %vehDB.ES_StartVolume;
	commandToClient(%client, 'ES_closestVehicle', 
													%audioHandle,
													%ghostID,
													%startValues,
													%scalars,
													%vehDB.ES_maxPitch,
													%vehDB.ES_GearPitchDelay,
													%vehDB.ES_gearCount,
													%vehDB.ES_gearSpeeds,
													%vehDB.ES_gearPitches,
													%vehDB.ES_gearShiftTime,
													%vehDB.ES_GearShiftAnims,
													%audioDescData,
													%vehDB.ES_GearVolumeLevels
					);
}

function serverCMDES_handshake(%client, %version)
{
	if(%version $= "")
	{
		%client.chatMessage("\c6This server is running \c3EngineSounds\c6 version \c3"@ $ES::Version @"\c6, your version of Support_EngineSounds will \c0not \c6work!");
		return;
	}

	%client.hasEngineSounds = true;
	if(!isObject(%client.ES_AudioSet))
	{
		%client.ES_AudioSet = new simSet();
		%client.add(%client.ES_AudioSet); // auto delete the clients simset on disconnect
	}
}
function Vehicle::ES_EngineStop(%this, %skipStopNoise)
{
	if(!%this.ES_Playing && !isEventPending(%this.ES_EngineStartSchedule))
		return false;

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

	%this.stopAudio(%this.getDatablock().ES_AudioSlot);
	if(!%skipStopNoise && isObject(%this.getDataBlock().ES_EngineStopSound))
		%this.playAudio(%this.getDatablock().ES_AudioSlot, %this.getDataBlock().ES_EngineStopSound);

	cancel(%this.ES_EngineStartSchedule);
	%this.ES_Playing = false;

	return true;
}
function Vehicle::ES_EngineStart(%this, %skipStartNoise)
{
	if(%this.ES_Playing || isEventPending(%this.ES_EngineStartSchedule))
		return false;

	cancel(%this.ES_EngineStartSchedule);

	%startSound = %this.getDataBlock().ES_EngineStartSound;
	%startDelay = %this.getDataBlock().ES_EngineStartDelay;
	if(!%skipStartNoise && isObject(%startSound) && %startDelay > 0)
	{
		%this.playAudio(%this.getDatablock().ES_AudioSlot, %startSound);
		%this.ES_EngineStartSchedule = %this.schedule(%startDelay, ES_EngineStart_Actual);
	} else {
		%this.ES_EngineStart_Actual();
	}

	return true;
}
function Vehicle::ES_EngineStart_Actual(%this)
{
	%this.playAudio(%this.getDatablock().ES_AudioSlot, %this.getDataBlock().ES_SoundDB);
	%this.ES_Playing = true;

	%count = ClientGroup.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%client = ClientGroup.getObject(%i);
		if(%client.hasEngineSounds)
			commandToClient(%client, 'ES_RemarkVehicle', %client.getGhostID(%this));
	}
}

function Vehicle::ES_CheckNoDriver(%this)
{
	if(%this.getControllingClient() == 0)
		%this.ES_EngineStop();
}

//this function is delayed by 1 frame
function Vehicle::ES_Init(%this)
{
	%spawnBrick = %this.spawnBrick;
	if(!isObject(%spawnBrick))
		return;

	%this.ES_EngineState = %spawnBrick.ES_EngineState;
	if(%this.ES_EngineState == 3) //ALWAYS-ON
	{
		//delay if they spam respawn it
		%time = 1000 - getMin(getSimTime() - %this.spawnBrick.ES_lastVehicleSpawnTime, 1000);
		%this.schedule(%time, ES_EngineStart);

		%this.spawnBrick.ES_lastVehicleSpawnTime = getSimTime();
	}
}

package ES_Server_Package
{
	//this func is buggy or some addon breaks it
	function Armor::onUnMount (%this, %obj, %vehicle, %node)
	{
		if(isObject(%vehicle) && %vehicle.getDataBlock().ES_Enabled && %vehicle.ES_EngineState != 3) //State 3 is ALWAYS-ON
			%vehicle.schedule(32, ES_CheckNoDriver); //delay this to avoid any issues

		return parent::onUnMount (%this, %obj, %vehicle, %node);
	}

	function GameConnection::AutoAdminCheck(%client)
	{
		//simple handshake system
		commandToClient(%client, 'ES_Handshake', $ES::Version, AudioEngineLooping3d.coneInsideAngle, AudioEngineLooping3d.coneOutsideAngle);
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
		if(%this.ES_AudioSlot $= "")
			%this.ES_AudioSlot = 1;

		%vehicle.ES_Playing = false;
		
		%vehicle.schedule(0, ES_Init);

		return %ret; //probably not important
	}
	function Armor::onMount (%this, %obj, %vehicle, %node)
	{
		if(%vehicle.getDataBlock().ES_Enabled && %vehicle.ES_EngineState != 4 && %node == 0 && %vehicle.getControllingClient() == 0)
			%vehicle.ES_EngineStart();

		return parent::onMount(%this, %obj, %vehicle, %node);
	}
};
activatePackage(ES_Server_Package);
