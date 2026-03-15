@echo off
setlocal enabledelayedexpansion

if "%~2"=="" (
    echo Usage: %~nx0 ^<user-principal-name^> ^<storage-account-name^>
    echo   user-principal-name   : UPN of the user in the tenant ^(e.g. user@contoso.com^)
    echo   storage-account-name  : Name of the existing Azure Storage account
    exit /b 1
)

set "USER_UPN=%~1"
set "STORAGE_ACCOUNT=%~2"

:: Resolve the user's object ID from their UPN
echo Resolving user '%USER_UPN%'...
for /f "usebackq delims=" %%i in (`az ad user show --id "%USER_UPN%" --query id --output tsv`) do set "USER_OBJECT_ID=%%i"
if not defined USER_OBJECT_ID (
    echo Error: Could not find user '%USER_UPN%' in the tenant.
    exit /b 1
)
echo User object ID: %USER_OBJECT_ID%

:: Get the storage account resource ID
echo Looking up storage account '%STORAGE_ACCOUNT%'...
for /f "usebackq delims=" %%i in (`az storage account show --name "%STORAGE_ACCOUNT%" --query id --output tsv`) do set "STORAGE_RESOURCE_ID=%%i"
if not defined STORAGE_RESOURCE_ID (
    echo Error: Could not find storage account '%STORAGE_ACCOUNT%'.
    exit /b 1
)

:: Create the container named with the user's object ID
echo Creating container '%USER_OBJECT_ID%' in storage account '%STORAGE_ACCOUNT%'...
call az storage container create --name "%USER_OBJECT_ID%" --account-name "%STORAGE_ACCOUNT%" --auth-mode login
if %errorlevel% neq 0 (
    echo Error: Failed to create container '%USER_OBJECT_ID%'.
    exit /b 1
)

:: Build the container scope
set "CONTAINER_SCOPE=%STORAGE_RESOURCE_ID%/blobServices/default/containers/%USER_OBJECT_ID%"

:: Assign Storage Blob Data Reader role on the container
echo Assigning 'Storage Blob Data Reader' role to '%USER_UPN%' on container '%USER_OBJECT_ID%'...
call az role assignment create --assignee-object-id "%USER_OBJECT_ID%" --assignee-principal-type User --role "Storage Blob Data Reader" --scope "%CONTAINER_SCOPE%"
if %errorlevel% neq 0 (
    echo Error: Failed to assign role.
    exit /b 1
)

echo Done. User '%USER_UPN%' now has 'Storage Blob Data Reader' access on container '%USER_OBJECT_ID%'.
endlocal
