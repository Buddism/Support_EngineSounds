registerOutputEvent("fxDTSBrick", "setEngineState", "list Start 0 Stop 1 Start-No-Audio 5 Stop-No-Audio 6 On-Driver 2 Always-On 3 Always-Off 4");
function fxDTSBrick::setEngineState(%this, %state)
{
	if (%this.getDataBlock ().specialBrickType !$= "VehicleSpawn")
		return;

	if(%state >= 2 && %state <= 4)
		%this.ES_EngineState = %state;

	%vehicle = %this.vehicle;
	if(!isObject(%vehicle) || !%vehicle.getDatablock().ES_Enabled)
		return;
	
	if(getSimTime() - %this.ES_lastEngineStateTime < 1000) //dont trust the players enough to be able to spam this
		return;

	switch(%state)
	{
		case 0: // Start
			%didAction = %vehicle.ES_EngineStart();

		case 1: // Stop
			%didAction = %vehicle.ES_EngineStop();

		case 5: // Start-No-Audio
			%didAction = %vehicle.ES_EngineStart(true); //skip startup noise

		case 6: // Stop-No-Audio
			%didAction = %vehicle.ES_EngineStop(true); //skip shutoff noise

		case 2: // On-Driver
			if(isObject(%vehicle.getControllingClient())) //there is a driver, start the engine
				%didAction = %vehicle.ES_EngineStart();
			else
				%didAction = %vehicle.ES_EngineStop();

		case 3: // Always-On
			%didAction = %vehicle.ES_EngineStart();
		
		case 4: //Always-Off
			%didAction = %vehicle.ES_EngineStop();
	}

	if(%didAction)
		%this.ES_lastEngineStateTime = getSimTime();
		
	if(%state >= 2 && %state <= 4)
		%vehicle.ES_EngineState = %state;
}