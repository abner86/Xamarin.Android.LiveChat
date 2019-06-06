##-----------------------------------------------------------------------
## <copyright file="ApplyVersionToAssemblies.ps1">(c) Microsoft Corporation. 
## This source is subject to the Microsoft Permissive License. 
## See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. 
## All other rights reserved.</copyright>
##-----------------------------------------------------------------------
# Look for a 0.0.0.0 pattern in the build number. 
# If found use it to version the assemblies.
#
# For example, if the 'Build number format' build process parameter 
# $(BuildDefinitionName)_$(Year:yyyy).$(Month).$(DayOfMonth)$(Rev:.r)
# then your build numbers come out like this:
# "Build HelloWorld_2013.07.19.1"
# This script would then apply version 2013.07.19.1 to your assemblies.

# Enable -Verbose option
[CmdletBinding()]
Param(
    [string]$suffix="",
    [ValidateSet("1.0","2.0")][string] $semVer = "2.0"
)

# functions used to version SQL Server Database Projects
# 
function Get-XmlNode([ xml ]$XmlDocument, [string]$NodePath, [string]$NamespaceURI = "", [string]$NodeSeparatorCharacter = '.') 
{     
    # If a Namespace URI was not given, use the Xml document's default namespace.     
    if ([string]::IsNullOrEmpty($NamespaceURI)) 
    { 
        $NamespaceURI = $XmlDocument.DocumentElement.NamespaceURI 
    }              
    # In order for SelectSingleNode() to actually work, we need to use the fully qualified node path along with an Xml Namespace Manager, so set them up.     
    $xmlNsManager = New-Object System.Xml.XmlNamespaceManager($XmlDocument.NameTable)     
    $xmlNsManager.AddNamespace("ns", $NamespaceURI)     
    $fullyQualifiedNodePath = "/ns:$($NodePath.Replace($($NodeSeparatorCharacter), '/ns:'))"          
    # Try and get the node, then return it. Returns $null if the node was not found.     
    $node = $XmlDocument.SelectSingleNode($fullyQualifiedNodePath, $xmlNsManager)     
    return $node
}

function Get-XmlElementsTextValue([ xml ]$XmlDocument, [string]$ElementPath, [string]$NamespaceURI = "", [string]$NodeSeparatorCharacter = '.') 
{     
    # Try and get the node.      
    $node = Get-XmlNode -XmlDocument $XmlDocument -NodePath $ElementPath -NamespaceURI $NamespaceURI -NodeSeparatorCharacter $NodeSeparatorCharacter          
    # If the node already exists, return its value, otherwise return null.     
    if ($node) 
    { 
        return $node.InnerText 
    } 
    else 
    { 
        return $null 
    } 
}   

function Set-XmlElementsTextValue([ xml ]$XmlDocument, [string]$ElementPath, [string]$TextValue, [string]$NamespaceURI = "", [string]$NodeSeparatorCharacter = '.') 
{     
    # Try and get the node.      
    $node = Get-XmlNode -XmlDocument $XmlDocument -NodePath $ElementPath -NamespaceURI $NamespaceURI -NodeSeparatorCharacter $NodeSeparatorCharacter          
    # If the node already exists, update its value.     
    if ($node)     
    {          
        $node.InnerText = $TextValue    
    }     
    # Else the node doesn't exist yet, so create it with the given value.     
    else     
    {         
        # Create the new element with the given value.         
        $elementName = $ElementPath.SubString($ElementPath.LastIndexOf($NodeSeparatorCharacter) + 1)         
        $element = $XmlDocument.CreateElement($elementName, $XmlDocument.DocumentElement.NamespaceURI)               
        $textNode = $XmlDocument.CreateTextNode($TextValue)         
        $element.AppendChild($textNode) > $null                  
        # Try and get the parent node.         
        $parentNodePath = $ElementPath.SubString(0, $ElementPath.LastIndexOf($NodeSeparatorCharacter))         
        $parentNode = Get-XmlNode -XmlDocument $XmlDocument -NodePath $parentNodePath -NamespaceURI $NamespaceURI -NodeSeparatorCharacter $NodeSeparatorCharacter                  
        if ($parentNode)         
        {             
            $parentNode.AppendChild($element) > $null        
        }         
        else         
        {             
            throw "$parentNodePath does not exist in the xml."        
        }     
    }
} 

# Regular expression pattern to find the version in the build number 
# and then apply it to the assemblies
$VersionRegex = "\d+\.\d+\.\d+\.\d+"
$artifactsDirectory = ""

# If this script is not running on a build server, remind user to 
# set environment variables so that this script can be debugged
if(-not ($Env:BUILD_SOURCESDIRECTORY -and $Env:BUILD_BUILDNUMBER) -and -not ($Env:AGENT_RELEASEDIRECTORY -and $Env:BUILD_BUILDNUMBER))
{
    Write-Error "You must set the following environment variables"
    Write-Error "to test this script interactively."
    Write-Host '$Env:BUILD_SOURCESDIRECTORY - For example, enter something like:'
    Write-Host '$Env:BUILD_SOURCESDIRECTORY = "C:\code\FabrikamTFVC\HelloWorld"'
    Write-Host '$Env:BUILD_BUILDNUMBER - For example, enter something like:'
    Write-Host '$Env:BUILD_BUILDNUMBER = "Build HelloWorld_0000.00.00.0"'
    exit 1
}

if (-not $Env:AGENT_RELEASEDIRECTORY) 
{

    # Make sure path to source code directory is available
    if (-not $Env:BUILD_SOURCESDIRECTORY)
    {
        Write-Error ("BUILD_SOURCESDIRECTORY environment variable is missing.")
        exit 1
    }
    elseif (-not (Test-Path $Env:BUILD_SOURCESDIRECTORY))
    {
        Write-Error "BUILD_SOURCESDIRECTORY does not exist: $Env:BUILD_SOURCESDIRECTORY"
        exit 1
    }
    Write-Verbose "BUILD_SOURCESDIRECTORY: $Env:BUILD_SOURCESDIRECTORY"
    $artifactsDirectory = $Env:BUILD_SOURCESDIRECTORY
}
else 
{
    # Make sure path to source code directory is available
    if (-not $Env:AGENT_RELEASEDIRECTORY)
    {
        Write-Error ("AGENT_RELEASEDIRECTORY environment variable is missing.")
        exit 1
    }
    elseif (-not (Test-Path $Env:AGENT_RELEASEDIRECTORY))
    {
        Write-Error "AGENT_RELEASEDIRECTORY does not exist: $Env:AGENT_RELEASEDIRECTORY"
        exit 1
    }
    Write-Verbose "AGENT_RELEASEDIRECTORY: $Env:AGENT_RELEASEDIRECTORY"
    $artifactsDirectory = $Env:AGENT_RELEASEDIRECTORY
}

# Make sure there is a build number
if (-not $Env:BUILD_BUILDNUMBER)
{
    Write-Error ("BUILD_BUILDNUMBER environment variable is missing.")
    exit 1
}
Write-Verbose "BUILD_BUILDNUMBER: $Env:BUILD_BUILDNUMBER"

# Get and validate the version data
$VersionData = [regex]::matches($Env:BUILD_BUILDNUMBER,$VersionRegex)
switch($VersionData.Count)
{
   0        
      { 
         Write-Error "Could not find version number data in BUILD_BUILDNUMBER."
         exit 1
      }
   1 {}
   default 
      { 
         Write-Warning "Found more than instance of version data in BUILD_BUILDNUMBER." 
         Write-Warning "Will assume first instance is version."
      }
}
$NewVersion = $VersionData[0]
Write-Verbose "Version: $NewVersion"

# Apply the version to the assembly property files
$files = gci $artifactsDirectory -recurse -include "*Properties*","My Project" | 
    ?{ $_.PSIsContainer } | 
    foreach { gci -Path $_.FullName -Recurse -include AssemblyInfo.* }
if($files)
{
    Write-Verbose "Will apply $NewVersion to $($files.count) files."

    foreach ($file in $files) {
        $filecontent = Get-Content($file)
        attrib $file -r
        $filecontent -replace $VersionRegex, $NewVersion | Out-File $file
        Write-Verbose "$file.FullName - version applied"
    }
}
else
{
    Write-Warning "Found no *.AssemblyInfo files."
}

# Put the version/description in the DacVersion/DacDescription elements in the .sqlproj (SSDT) project files
$files = gci $artifactsDirectory -recurse | 
    ?{ $_.Extension -eq ".sqlproj" } | 
    foreach { gci -Path $_.FullName -Recurse -include *.sqlproj }
if($files)
{
    Write-Verbose "Will apply $NewVersion to $($files.count) files."
    
    foreach ($file in $files) {	
        [xml]$fileContent = Get-Content($file)            
        attrib $file -r
        # Read in the file contents, update the version element's value, and save the file. 
        Set-XmlElementsTextValue -XmlDocument $fileContent -ElementPath "Project.PropertyGroup.DacVersion" -TextValue $NewVersion
        Set-XmlElementsTextValue -XmlDocument $fileContent -ElementPath "Project.PropertyGroup.DacDescription" -TextValue $Env:BUILD_BUILDNUMBER
        $fileContent.Save($file)            
        Write-Verbose "$file.FullName - version applied"		
    }
}
else
{
    Write-Warning "Found no *.sqlproj files."
}

# check if user wants to use semantic versioning, by including a $suffix parameter.  We also check if we are using the SemVer 1.0 or 2.0 format
$semanticVersion = ""
if ($suffix -ne "")
{
  $vData = [regex]::matches($NewVersion, "\d+")
  $majorVersion = $vData[0].Value
  $minorVersion = $vData[1].Value
  $buildVersion = $vData[2].Value
  $revision = $vData[3].Value
  
  if ($semVer -eq "1.0")
  {
    $semanticVersion = $majorVersion +"." + $minorVersion + "." + $buildVersion + "-" + $suffix + $revision 
  }
  else 
  {
    $semanticVersion = $majorVersion +"." + $minorVersion + "." + $buildVersion + "-" + $suffix + "." + $revision
  }
  write-warning "Semantic Version: $semanticVersion"
}
else
{
  $semanticVersion = $newVersion  
}

# Put the version/description in the Version elements in the .nuspec (NuGet) files
$files = gci $artifactsDirectory -recurse | 
    ?{ $_.Extension -eq ".nuspec" } | 
    foreach { gci -Path $_.FullName -Recurse -include *.nuspec }
if($files)
{
    Write-Verbose "Will apply $semanticVersion to $($files.count) files."
    
    foreach ($file in $files) 
    {	
        [xml]$fileContent = Get-Content($file)            
        attrib $file -r
        # Read in the file contents, update the version element's value, and save the file. 
        Set-XmlElementsTextValue -XmlDocument $fileContent -ElementPath "package.metadata.version" -TextValue $semanticVersion
        $fileContent.Save($file)            
        Write-Verbose "$file.FullName - version applied"		
    }
    Write-Host "##vso[task.setvariable variable=semanticVersion]$semanticVersion"
}
else
{
    Write-Warning "Found no *.nuspec files."
}