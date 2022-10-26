#this command must be executed on Exchange Manager Shell
#deploy.ps1 {deploy|undeploy}

Function EnableAgent()
{
   write-host "Enabling agent..."
   enable-transportagent -Identity $RouteAgentName
   $enableAgentResult = $?
   if($enableAgentResult)
   {
      write-host "Enable agent Success."
   }
   else
   {
     write-host "Enable agent Failed."
   }

   return $enableAgentResult
}

Function DisableAgent()
{
   write-host "Disabling agent..."
   disable-transportagent -Identity $RouteAgentName -Confirm:$false
   $disableAgentResult = $?
   if($disableAgentResult)
   {
      write-host "Disable agent Success."
   }
   else
   {
     write-host "Disable agent Failed."
   }

   return $disableAgentResult
}


Function RegisterAgent()
{
  write-host "Registering agent..."
  install-transportagent -Name $RouteAgentName -AssemblyPath  $scriptDir/bin/RouteAgent.dll -TransportAgentFactory RouteAgent.MyRoutingAgentFactory
  $registerAgentResult = $?
  if($registerAgentResult)
  {
     write-host "Register agent success."
  }
  else
  {
     write-host "Register agent failed."
  }
  return $registerAgentResult
}

Function UnregisterAgent()
{
  write-host "unregistering agent..."
  uninstall-transportagent $RouteAgentName -Confirm:$false
  $unregisterAgentResult = $?
  if($unregisterAgentResult)
  {
     write-host "Unregister agent success."
  }
  else
  {
     write-host "Unregister agent failed."
  }
  return $unregisterAgentResult
}

Function StopExchangeService()
{
   write-host "Enter Stop MSExchangeTransport..."
   net stop MSExchangeTransport
   $stopExResult = $?
   if($stopExResult)
   {
      write-host "Stop MsExchangeTransport Agent Success.";
   }
   else
   {
      write-host "Stop MsExchangeTransport Agent Failed.";
   }
   return $stopExResult
}

Function StartExchangeService()
{
   write-host "Start MSExchangeTransport..."
   net start MSExchangeTransport
   $startExResult = $?
   if($startExResult)
   {
      write-host "Start MsExchangeTransport Agent Success."
   }
   else
   {
      write-host "Start MsExchangeTransport Agent Failed."
   }
   return $startExResult
}

Function RestartExchangeService()
{
   write-host "Restart Exchange Service..."
   Restart-Service msexchangetransport
}

Function DeployAgent()
{
       $RegAgentResult = RegisterAgent
       if($RegAgentResult)
       {
          EnableAgent
          RestartExchangeService
       }
}

Function UndeployAgent()
{
       $DisableAgent = DisableAgent
       if($DisableAgent)
       {
          UnregisterAgent
          RestartExchangeService
       }
}

$RouteAgentName = "ExchangePEP Route Agent"

#get script directory
$scriptPath = $MyInvocation.MyCommand.Definition
$scriptDir = Split-Path -Parent $scriptPath

if($args[0] -ieq "deploy")
{
  DeployAgent
}
elseif($args[0] -ieq "undeploy")
{
  UndeployAgent
}
else
{
  write-host "invalid argument, usage:"
  write-host "deploy.ps1 {deploy|undeploy}"
}
