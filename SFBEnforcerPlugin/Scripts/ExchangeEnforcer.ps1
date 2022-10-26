$CurrentDirectory =  Split-Path -parent $MyInvocation.MyCommand.Definition
$EEServiceName = "MSExchangeTransport"
# $ServiceName = "ComplianceEnforcerService"
$TargetEEPath = "C:\Program Files\NextLabs\Exchange Enforcer"
$TargetPCPath = "C:\Program Files\NextLabs\Policy Controller"

function CopyDll {
   $dllArray = @("SFBEnforcerPlugin.dll","TDFFileAnalyser.dll","Newtonsoft.Json.dll", "CSBase.Common.dll", "CSBase.Diagnose.dll")
   $configArray = @("SFBEnforcerPluginConfig.xml")
   $DllPath = "$CurrentDirectory\Dll"
   $ConfigPath = "$CurrentDirectory\Config"
   New-Item  "$TargetEEPath\bin\tmp" -ItemType "Directory" -Force
   for ( $i= 0; $i -lt $dllArray.Count; $i++) {
    Copy-Item  "$TargetEEPath\bin\$dllArray[$i]" "$TargetEEPath\bin\tmp"
   }
   for ($i = 0; $i -lt $configArray.Count; $i++) {
    Copy-Item "$TargetEEPath\config\$configArray[$i]" "$TargetEEPath\bin\tmp"
   }
   Stop-Service  $EEServiceName
   Copy-Item "$DllPath\*" "$TargetEEPath\bin"
   Copy-Item "$ConfigPath\*" "$TargetEEPath\config"
   remove-item "$TargetEEPath\bin\tmp" -Recurse
   Start-Service $EEServiceName
}
function CopyJar {

   $JarPath = "$CurrentDirectory\Jar\*.jar"
   $ConfigPath = "$CurrentDirectory\Jar\*.properties"

   Copy-Item $JarPath "$TargetPCPath\jservice\jar"
   Copy-Item $ConfigPath "$TargetPCPath\jservice\config"
#  Restart-Service $ServiceName
}
function UpdateConfig ($pluginName,$pluginArchitecture,$pluginToken,$pluginVersion){
    $PluginXmlPath = "$TargetEEPath\config\plugin.xml"
    $xmldata = [xml](Get-Content $PluginXmlPath)
    $pluginsNode =  $xmldata.SelectSingleNode("pluginsConfig/plugins")
    $pluginNodes = $pluginsNode.SelectNodes("plugin")
    $needReplaceNode = $null
    foreach($node in $pluginNodes){
        if($node.name -eq $pluginName){
            $needReplaceNode = $node
            break
        }
    }
    $pluginElement = $xmldata.CreateElement("plugin")
    $pluginElement.SetAttribute("name",$pluginName);
    $pluginElement.SetAttribute("processorArchitecture",$pluginArchitecture);
    $pluginElement.SetAttribute("publicKeyToken",$pluginToken);
    $pluginElement.SetAttribute("culture","neutral");
    $pluginElement.SetAttribute("version",$pluginVersion);
    if($null -eq $needReplaceNode){
        $pluginsNode.AppendChild($pluginElement)
    }else{
        $pluginsNode.ReplaceChild($pluginElement, $needReplaceNode)
    }
    $xmldata.Save($PluginXmlPath)
}
function Main(){
    UpdateConfig "SFBEnforcerPlugin" "MSIL" "e1bc6d8a1b0503fd" "1.0.0.0"
    CopyJar
    CopyDll
}

Main