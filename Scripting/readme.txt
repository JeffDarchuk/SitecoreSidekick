Copy the SitecoreSidekick folder to your WindowsPowerShell module directory. 

It should reside in one of the paths found under $env:PSModulePath

Example:
C:\Users\[USER]\Documents\WindowsPowerShell\Modules\SitecoreSidekick

Usage:
Import-Module -Name SitecoreSidekick -Force -Verbose
$params = @{
    LocalUrl = "http://demo2"
    RemoteUrl = "http://demo"
    SharedSecret = "3a4e9aad-5d61-42ae-b262-de0e13f3b576"
    EventDisabler = $true
    BulkUpdate = $true
    PullParent = $true
}
#MAKE THE CONTENT FROM ONE SERVER MATCH THE OTHER EXACTLY
Copy-SKContent @params -RootId '{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}' -Children -Overwrite -RemoveLocalNotInRemote
#MOVE ALL CONTENT FROM ONE SERVER TO THE OTHER AND ALLOWING UNIQUE ITEMS AT THE CONSUMER
Copy-SKContent @params -RootId '{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}' -Children -Overwrite
#MOVE ONLY NEW CONTENT FROM ONE SERVER TO THE OTHER WITHOUT UPDATING MODIFIED
Copy-SKContent @params -RootId '{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}' -Children
#ONLY MOVE THE INDIVIDUAL ITEMS AND NOT CHILDREN
Copy-SKContent @params -RootId '{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}' -Overwrite