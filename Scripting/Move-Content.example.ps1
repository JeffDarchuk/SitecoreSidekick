#EXAMPLE USEAGES OF THE MOVE-CONTENT POWERSHELL MODULE

$ScriptPath = Split-Path $MyInvocation.MyCommand.Path
#IMPORT MODULE
Import-Module $ScriptPath\Move-Content.psm1

#MAKE THE CONTENT FROM ONE SERVER MATCH THE OTHER EXACTLY
move-content -LocalUrl 'https://serverWhoWantsContent' -RemoteUrl 'https://serverWhoHasContent' -SharedSecret 'Secret key defined for content migrator on both local and remote sitecore sites' -RootIds '{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}' -Children -Overwrite -PullParent -EventDisabler -BulkUpdate -RemoveLocalNotInRemote

#MOVE ALL CONTENT FROM ONE SERVER TO THE OTHER AND ALLOWING UNIQUE ITEMS AT THE CONSUMER
move-content -LocalUrl 'https://serverWhoWantsContent' -RemoteUrl 'https://serverWhoHasContent' -SharedSecret 'Secret key defined for content migrator on both local and remote sitecore sites' -RootIds '{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}' -Children -Overwrite -PullParent -EventDisabler -BulkUpdate

#MOVE ONLY NEW CONTENT FROM ONE SERVER TO THE OTHER WITHOUT UPDATING MODIFIED
move-content -LocalUrl 'https://serverWhoWantsContent' -RemoteUrl 'https://serverWhoHasContent' -SharedSecret 'Secret key defined for content migrator on both local and remote sitecore sites' -RootIds '{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}' -Children -PullParent -EventDisabler -BulkUpdate

#ONLY MOVE THE INDIVIDUAL ITEMS AND NOT CHILDREN
move-content -LocalUrl 'https://serverWhoWantsContent' -RemoteUrl 'https://serverWhoHasContent' -SharedSecret 'Secret key defined for content migrator on both local and remote sitecore sites' -RootIds '{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}' -Overwrite -PullParent -EventDisabler -BulkUpdate