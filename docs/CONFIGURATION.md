# Configuration Guide

## Azure AD Single-Tenant Configuration

This application is configured to **only allow users from your organization** to sign in.

### How to Find Your Tenant Information

#### Option 1: Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Microsoft Entra ID**
3. Click on **Overview** in the left sidebar
4. You'll see:
   - **Tenant ID**: A GUID like `12345678-1234-1234-1234-123456789abc`
   - **Primary domain**: Something like `contoso.onmicrosoft.com`

#### Option 2: Azure CLI

```powershell
# Login to Azure
az login

# Get tenant information
az account show --query "{TenantId:tenantId, Domain:user.name}" --output table
```

### Creating the App Registration

1. **Azure Portal** → **Microsoft Entra ID** → **App registrations** → **New registration**

2. **Configure the app:**
   - **Name**: `PayslipsManager` (or your preferred name)
   - **Supported account types**: Select **"Accounts in this organizational directory only (Single tenant)"**
   - **Redirect URI**: 
     - Platform: **Web**
     - URI: `https://localhost:7000/signin-oidc` (adjust port if needed)

3. **Create a client secret:**
   - Go to **Certificates & secrets**
   - Click **New client secret**
   - Add a description (e.g., "PayslipsManager Dev")
   - Select an expiry period
   - **Copy the secret value immediately** (you can't see it again!)

4. **Note down the values:**
   - **Application (client) ID** from the Overview page
   - **Directory (tenant) ID** from the Overview page
   - **Client secret value** from the previous step
   - **Primary domain** from the Entra ID Overview

### Update Configuration Files

#### Recommended: Use appsettings.local.json (Best Practice)

The repository includes an `appsettings.local.json` file that is **excluded from source control** (via .gitignore). This is the safest way to store your secrets locally.

**Location:** `src/PayslipsManager.Web/appsettings.local.json`

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "contoso.onmicrosoft.com",              // ← Your tenant domain
    "TenantId": "12345678-1234-1234-1234-123456789abc", // ← Your tenant ID
    "ClientId": "87654321-4321-4321-4321-cba987654321",  // ← Your app client ID
    "ClientSecret": "your-client-secret-here",            // ← Your client secret
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "BlobStorage": {
    "AccountUrl": "https://yourstorageaccount.blob.core.windows.net",
    "ContainerPrefix": "payslips",
    "UseManagedIdentity": false,
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=yourstorageaccount;AccountKey=..."
  },
  "BypassAuthentication": false
}
```

**✅ Benefits:**
- Automatically excluded from Git (protected from accidental commits)
- Overrides placeholder values in `appsettings.json`
- Safe to store real secrets locally
- Easy to share template with team

#### Alternative 1: Use User Secrets (Development Only)

If you prefer not to use local files:
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret-here" --project src/PayslipsManager.Web
dotnet user-secrets set "BlobStorage:ConnectionString" "your-connection-string" --project src/PayslipsManager.Web
```

#### Alternative 2: Update appsettings.Development.json (Not Recommended)

⚠️ **Warning:** This file IS tracked by Git. Only use for non-secret values.

If you want to test without Azure AD:
```json
{
  "BypassAuthentication": true  // ← Enables cookie-based auth for local testing
}
```

If you want to test with Azure AD in development:
```json
{
  "BypassAuthentication": false,
  "AzureAd": {
    "Domain": "contoso.onmicrosoft.com",
    "TenantId": "12345678-1234-1234-1234-123456789abc",
    "ClientId": "87654321-4321-4321-4321-cba987654321",
    "ClientSecret": "your-client-secret-here"
  }
}
```

### Testing the Configuration

1. **Start the application:**
   ```powershell
   dotnet run --project src/PayslipsManager.Web
   ```

2. **Navigate to the payslips page:**
   ```
   https://localhost:7000/Payslips
   ```

3. **You should be redirected to Microsoft login:**
   - Only users with `@contoso.onmicrosoft.com` (or your domain) can sign in
   - Users from other organizations will be rejected

### Troubleshooting

#### Error: AADSTS90013 "Invalid input received"
- **Cause**: Placeholder values still in configuration
- **Fix**: Replace `<YOUR_TENANT_ID>`, `<YOUR_CLIENT_ID>`, etc. with real values

#### Error: AADSTS50020 "User account from identity provider does not exist"
- **Cause**: User is trying to sign in from a different tenant
- **Fix**: This is expected! Only users from YOUR tenant can sign in

#### Error: AADSTS700016 "Application not found"
- **Cause**: `ClientId` is incorrect
- **Fix**: Verify the Application (client) ID in Azure Portal

#### Error: AADSTS7000215 "Invalid client secret"
- **Cause**: `ClientSecret` is incorrect or expired
- **Fix**: Generate a new client secret in Azure Portal

### Security Best Practices

1. **Never commit secrets**: Use Azure Key Vault or User Secrets for development
   ```powershell
   dotnet user-secrets set "AzureAd:ClientSecret" "your-secret-here" --project src/PayslipsManager.Web
   ```

2. **Use Managed Identity in Azure**: Set `UseManagedIdentity: true` for Blob Storage

3. **Rotate secrets regularly**: Set expiry on client secrets and rotate them

4. **Restrict redirect URIs**: Only add URIs you actually use

5. **Monitor sign-ins**: Use Azure AD sign-in logs to detect suspicious activity
