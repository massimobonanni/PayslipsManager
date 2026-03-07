# PayslipsManager

A .NET 10 application demonstrating **Azure Blob Storage** integration for secure employee payslip management with **Microsoft Entra ID** authentication.

## 🎯 Project Overview

PayslipsManager showcases how to build a secure document management system using:
- **Azure Blob Storage** for storing PDF payslips
- **Microsoft Entra ID** for corporate authentication
- **Clean Architecture** principles
- **Azure Functions** for event-driven processing
- **Blob access tiers** (Hot/Cool/Archive) for cost optimization
- **SAS tokens** for secure, time-limited access

## 🏗️ Solution Structure

```
PayslipsManager/
├─ src/
│  ├─ PayslipsManager.Domain/          # Domain models, enums, value objects
│  ├─ PayslipsManager.Application/     # DTOs, interfaces, application services
│  ├─ PayslipsManager.Infrastructure/  # Azure Blob Storage repository, DI setup
│  ├─ PayslipsManager.Web/             # ASP.NET Core MVC web application
│  └─ PayslipsManager.Functions/       # Azure Functions for blob event handling
├─ azure.yaml                           # Azure Developer CLI configuration
├─ infra/                               # Bicep infrastructure as code
└─ README.md
```

### Projects

| Project | Framework | Purpose |
|---------|-----------|---------|
| **Domain** | net10.0 | Core business entities and enums |
| **Application** | net10.0 | Business logic, DTOs, service interfaces |
| **Infrastructure** | net10.0 | Azure Blob Storage implementation |
| **Web** | net10.0 | ASP.NET Core MVC UI with Entra ID auth |
| **Functions** | net10.0 | Blob trigger for automated metadata tagging |

## 🔑 Key Features

### 1. Secure Authentication
- Microsoft Entra ID (Azure AD) integration
- Employee can only see their own payslips
- Role-based access control ready

### 2. Azure Blob Storage Integration
- Store payslips as PDF files
- Automatic metadata tagging
- Blob indexing with tags for efficient queries
- Support for Hot, Cool, and Archive access tiers

### 3. Azure Functions Event Processing
- Blob trigger on new payslip uploads
- Automatic tagging with employee information
- Metadata enrichment for searchability

### 4. Clean Architecture
- Domain-driven design
- Separation of concerns
- Dependency inversion principle
- Repository pattern

## 🚀 Getting Started

### Prerequisites

- **.NET 10 SDK** (preview)
- **Azure subscription**
- **Visual Studio 2022** or **VS Code** with C# extension
- **Azure CLI** and **Azure Developer CLI (azd)**

### Local Development Setup

1. **Clone the repository**
   ```powershell
   git clone https://github.com/massimobonanni/PayslipsManager.git
   cd PayslipsManager
   ```

2. **Configure Azure AD Application**
   - Go to Azure Portal → Microsoft Entra ID → App registrations
   - Create a new app registration with **single-tenant** configuration
   - Set **Supported account types** to "Accounts in this organizational directory only"
   - Note down: 
     - `TenantId` (found in Overview)
     - `Domain` (e.g., `contoso.onmicrosoft.com`)
     - `ClientId` (Application ID in Overview)
   - Create a client secret under "Certificates & secrets"
   - Configure redirect URIs under "Authentication": `https://localhost:7xxx/signin-oidc`

3. **Update appsettings.local.json** (Web project)
   
   Create or update `src/PayslipsManager.Web/appsettings.local.json` with your values:
   
   ```json
   {
     "AzureAd": {
       "Instance": "https://login.microsoftonline.com/",
       "Domain": "contoso.onmicrosoft.com",
       "TenantId": "12345678-1234-1234-1234-123456789abc",
       "ClientId": "87654321-4321-4321-4321-cba987654321",
       "ClientSecret": "your-client-secret-here"
     },
     "BlobStorage": {
       "AccountUrl": "https://yourstorageaccount.blob.core.windows.net",
       "ContainerName": "payslips",
       "UseManagedIdentity": false,
       "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=..."
     },
     "BypassAuthentication": false
   }
   ```
   
   **Note:** This file is excluded from source control (.gitignore) to protect your secrets.

4. **Create Storage Account and Container**
   ```powershell
   az storage account create --name <account-name> --resource-group <rg-name>
   az storage container create --name payslips --account-name <account-name>
   ```

5. **Build and Run**
   ```powershell
   dotnet build
   dotnet run --project src/PayslipsManager.Web
   ```

## 📦 Deployment to Azure

### Using Azure Developer CLI (azd)

1. **Initialize azd**
   ```powershell
   azd init
   ```

2. **Deploy to Azure**
   ```powershell
   azd up
   ```

This will:
- Provision Azure resources (Storage Account, App Service, Function App)
- Configure managed identity
- Deploy the application
- Set up RBAC permissions

## 🔐 Security Features

- **Authentication**: Microsoft Entra ID (Single-Tenant)
  - Only users from your organization can sign in
  - Configure `TenantId` with your specific tenant ID (not "common")
  - Set `Domain` to your tenant domain
- **Authorization**: User can only access their own payslips
- **Data isolation**: Employee email validation on every request
- **Secure downloads**: Time-limited SAS tokens
- **Managed Identity**: No credentials stored in code

### Single-Tenant vs Multi-Tenant

This application is configured for **single-tenant** use:
- ✅ **Single-Tenant** (Default): Only users from YOUR organization
  - `TenantId`: Your specific tenant GUID
  - `Domain`: Your tenant domain (e.g., `contoso.onmicrosoft.com`)
- ❌ **Multi-Tenant**: Users from ANY organization
  - `TenantId`: "common" or "organizations" (NOT recommended for this app)

**Why Single-Tenant?**
Payslip data is highly sensitive and should only be accessible to employees within your organization.

## 📊 Azure Blob Storage Concepts Demonstrated

1. **Blob Containers** - Organizing payslips
2. **Blob Metadata** - Custom properties for each document
3. **Blob Tags** - Index tags for efficient querying
4. **Access Tiers** - Hot, Cool, Archive for cost optimization
5. **SAS Tokens** - Secure, time-limited download URLs
6. **Blob Triggers** - Event-driven processing with Azure Functions
7. **Managed Identity** - Secure authentication without keys

## 📁 Sample Blob Naming Convention

Payslip blobs follow the pattern:
```
{employeeId}_{year}_{month}.pdf
Example: EMP001_2026_03.pdf
```

## 🏷️ Blob Tags Structure

Each payslip blob is tagged with:
- `EmployeeId` - Employee identifier
- `EmployeeEmail` - Corporate email
- `EmployeeName` - Display name
- `Year` - Payslip year
- `Month` - Payslip month (1-12)
- `ProcessedAt` - Processing timestamp

## 🛠️ Technology Stack

- **Framework**: .NET 10
- **Web**: ASP.NET Core MVC
- **Storage**: Azure Blob Storage SDK
- **Authentication**: Microsoft Identity Web
- **Functions**: Azure Functions (Isolated Worker)
- **Logging**: Microsoft.Extensions.Logging
- **DI**: Microsoft.Extensions.DependencyInjection

## 📚 Learning Resources

- [Azure Blob Storage Documentation](https://learn.microsoft.com/azure/storage/blobs/)
- [Microsoft Identity Platform](https://learn.microsoft.com/entra/identity-platform/)
- [Azure Functions](https://learn.microsoft.com/azure/azure-functions/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

## 🤝 Contributing

This is a demo application for training and conference presentations. Feel free to fork and customize for your own use cases.

## 📄 License

MIT License - See LICENSE file for details

## 👤 Author

**Massimo Bonanni**
- Blog: [Configure and Code](https://configureandcode.cloud)
- GitHub: [@massimobonanni](https://github.com/massimobonanni)

---

**Note**: This application uses .NET 10 (preview). For production, use the latest stable .NET version.
