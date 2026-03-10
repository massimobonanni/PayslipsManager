# PayslipsManager

A **.NET 10** demo application showing how to use **Azure Blob Storage** in a realistic business scenario. Employees sign in with their corporate **Microsoft Entra ID** account, view their monthly payslips, and download PDF files — while the system enforces per-employee data isolation and demonstrates Hot, Cool, Cold, and Archive access tiers.

---

## Solution Structure

```text
PayslipsManager/
├─ .github/
│  └─ copilot-instructions.md        # Copilot coding rules
├─ docs/
│  └─ CONFIGURATION.md               # Detailed auth setup guide
├─ src/
│  ├─ PayslipsManager.Domain/        # Entities, enums, value objects
│  ├─ PayslipsManager.Application/   # DTOs, interfaces, application services
│  ├─ PayslipsManager.Infrastructure/# Azure Blob Storage repository, DI
│  ├─ PayslipsManager.Web/           # ASP.NET Core MVC front-end
│  └─ PayslipsManager.Functions/     # Azure Function – Event Grid trigger
├─ infra/                             # Bicep infrastructure-as-code
├─ azure.yaml                         # Azure Developer CLI manifest
└─ README.md
```

| Project | Target | Responsibility |
|---------|--------|----------------|
| **Domain** | net10.0 (class library) | `Employee`, `PayslipDocument`, enums (`BlobAccessTier`, `EmploymentStatus`, `PayslipStatus`), validation result |
| **Application** | net10.0 (class library) | Service interfaces (`IPayslipQueryService`, `IPayslipDownloadService`, `IPayslipEventProcessor`, `IPayslipStorageService`, `IEmployeeContextService`), DTOs, `PayslipService` |
| **Infrastructure** | net10.0 (class library) | `BlobPayslipRepository` (Azure Blob SDK), `PayslipEventProcessor`, `BlobStorageOptions`, `DependencyInjection` |
| **Web** | net10.0 (ASP.NET Core MVC) | Controllers, Razor views, `EmployeeContextService`, Entra ID / cookie auth |
| **Functions** | net10.0 (isolated worker) | `PayslipBlobCreatedFunction` — Event Grid trigger that validates new blobs and applies index tags |

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | **10.0** | `dotnet --version` |
| Azure CLI | latest | `az --version` |
| Azure Developer CLI | latest | `azd version` (optional, for `azd up`) |
| Azurite **or** Azure Storage Account | — | Local emulator or cloud storage |
| Azure Functions Core Tools | **v4** | Only needed to run the Functions project locally |

---

## Local Development

### 1. Clone and build

```powershell
git clone https://github.com/massimobonanni/PayslipsManager.git
cd PayslipsManager
dotnet build
```

### 2. Configuration files

The Web project loads settings in this order (last wins):

1. `appsettings.json` — shared defaults (checked in)
2. `appsettings.Development.json` — development overrides (checked in, sets `BypassAuthentication: true`)
3. `appsettings.local.json` — **your secrets** (git-ignored via `*.local.json`)

#### Web — `appsettings.json`

Contains empty placeholders for Entra ID and storage. In production, values come from App Service configuration or Key Vault references.

```jsonc
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "",
    "TenantId": "",
    "ClientId": "",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "BlobStorage": {
    "AccountUrl": "",
    "ContainerPrefix": "payslips",
    "UseManagedIdentity": true
  }
}
```

#### Web — `appsettings.Development.json`

Ships with `BypassAuthentication: true` so you can run locally without an Entra app registration. Uses the Azurite connection string by default.

```jsonc
{
  "BlobStorage": {
    "UseManagedIdentity": false,
    "ConnectionString": "UseDevelopmentStorage=true"
  },
  "BypassAuthentication": true
}
```

#### Web — `appsettings.local.json` (create manually, git-ignored)

To test with real Entra ID sign-in or a real Storage Account, create this file:

```jsonc
{
  "AzureAd": {
    "Domain": "contoso.onmicrosoft.com",
    "TenantId": "<YOUR_TENANT_ID>",
    "ClientId": "<YOUR_CLIENT_ID>",
    "ClientSecret": "<YOUR_CLIENT_SECRET>"
  },
  "BlobStorage": {
    "AccountUrl": "https://<YOUR_STORAGE_ACCOUNT>.blob.core.windows.net",
    "UseManagedIdentity": false,
    "ConnectionString": "<YOUR_STORAGE_CONNECTION_STRING>"
  },
  "BypassAuthentication": false
}
```

#### Functions — `local.settings.json`

```jsonc
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "BlobStorage:ConnectionString": "UseDevelopmentStorage=true",
    "BlobStorage:AccountUrl": "",
    "BlobStorage:ContainerPrefix": "payslips",
    "BlobStorage:UseManagedIdentity": "false"
  }
}
```

### 3. `DefaultAzureCredential` support

When `UseManagedIdentity` is `true`, the infrastructure layer creates the `BlobServiceClient` with `DefaultAzureCredential`. This works seamlessly in:

- **Local dev** — picks up your `az login` session or Visual Studio credentials.
- **Azure** — uses the system-assigned or user-assigned managed identity of the App Service / Function App.

When `UseManagedIdentity` is `false`, the `ConnectionString` value is used instead (suitable for Azurite or account-key access).

### 4. Options class — `BlobStorageOptions`

Located in `PayslipsManager.Infrastructure/Configuration/BlobStorageOptions.cs`. Bound to the `BlobStorage` configuration section.

| Property | Type | Default | Purpose |
|---|---|---|---|
| `AccountUrl` | `string` | `""` | Storage account blob endpoint URL |
| `ContainerPrefix` | `string` | `"payslips"` | Prefix for per-employee containers (`payslips-{employeeId}`) |
| `UseManagedIdentity` | `bool` | `true` | Use `DefaultAzureCredential` when true |
| `ConnectionString` | `string?` | `null` | Connection string for local / key-based access |

### 5. Run the Web app

```powershell
dotnet run --project src/PayslipsManager.Web
```

With `BypassAuthentication: true`, navigate to the dev-login page to sign in as a sample user:

```
https://localhost:<port>/Home/DevLogin?email=alice.rossi@contoso.com
```

### 6. Run the Azure Function locally

```powershell
cd src/PayslipsManager.Functions
func start
```

The function listens for **Event Grid** events (`Microsoft.Storage.BlobCreated`). To test locally, send a test event with the Azure CLI or Event Grid simulator.

---

## Azure Resources Needed

| Resource | SKU / Tier | Purpose |
|----------|-----------|---------|
| **Storage Account** | Standard LRS (GPv2) | Blob containers per employee, lifecycle management |
| **App Service** | B1 or higher | Hosts the MVC web app |
| **Function App** | Consumption or Flex Consumption | Runs the Event Grid trigger |
| **App Service Plan** | Shared between Web + Functions (optional) | Cost optimization for demo |
| **Microsoft Entra ID** | Any tenant | App registration for single-tenant auth |
| **Event Grid System Topic** | On the Storage Account | Routes `BlobCreated` events to the Function |
| **Application Insights** | (optional) | Telemetry for the Function app |

### Required RBAC role assignments

| Identity | Role | Scope |
|----------|------|-------|
| App Service managed identity | **Storage Blob Data Contributor** | Storage Account |
| Function App managed identity | **Storage Blob Data Contributor** | Storage Account |
| Developer (local dev with `az login`) | **Storage Blob Data Contributor** | Storage Account |

---

## Authentication Setup

1. Go to **Azure Portal** → **Microsoft Entra ID** → **App registrations** → **New registration**.
2. Set **Supported account types** to *Accounts in this organizational directory only* (single tenant).
3. Set **Redirect URI** to `https://localhost:<port>/signin-oidc` (Web platform).
4. Under **Certificates & secrets**, create a client secret.
5. Copy `TenantId`, `ClientId`, `Domain`, and the secret into `appsettings.local.json`.

For detailed steps and troubleshooting, see [docs/CONFIGURATION.md](docs/CONFIGURATION.md).

---

## Storage Setup

Each employee gets a dedicated blob container named `{ContainerPrefix}-{employeeId}` (e.g., `payslips-abc123`).

Payslip PDFs follow the naming convention **`yyyy-MM.pdf`** (e.g., `2026-03.pdf`).

Blob index tags applied by the Function:

| Tag | Example |
|-----|---------|
| `EmployeeId` | `abc123` |
| `PayslipYear` | `2026` |
| `PayslipMonth` | `03` |
| `DocumentType` | `Payslip` |

---

## Event Flow

```text
┌──────────────┐         ┌───────────────────┐         ┌──────────────────────┐
│  HR uploads  │  blob   │  Azure Storage    │  event  │  Event Grid System   │
│  a PDF to    │ ──────> │  (container       │ ──────> │  Topic               │
│  the employee│         │   payslips-<id>)  │         │  BlobCreated         │
│  container   │         └───────────────────┘         └──────────┬───────────┘
└──────────────┘                                                  │
                                                                  │ subscription
                                                                  ▼
                                                       ┌──────────────────────┐
                                                       │  PayslipBlobCreated  │
                                                       │  Function            │
                                                       │  (Event Grid Trigger)│
                                                       └──────────┬───────────┘
                                                                  │
                                          ┌───────────────────────┤
                                          │                       │
                                          ▼                       ▼
                                   Validate blob name      Apply blob index tags
                                   (yyyy-MM.pdf)           (EmployeeId, Year,
                                                            Month, DocumentType)
```

1. An HR operator (or automation) uploads a PDF named `yyyy-MM.pdf` into the employee's container.
2. Azure Storage raises a `Microsoft.Storage.BlobCreated` event.
3. The Event Grid System Topic routes the event to the `PayslipBlobCreatedFunction`.
4. The function validates the blob name, checks idempotency, and applies blob index tags.
5. When the employee signs into the Web app, `PayslipService` lists blobs in their container, reads tags, and shows the payslip list sorted by date.
6. The employee can view details or download the PDF. Archive-tier payslips show a warning that rehydration is needed.

---

## Demo Sample Data

The table below describes the sample employees and payslips you should create to demonstrate all features (access tiers, former employees, archive warnings).

### Sample Employees

| # | Display Name | Employee ID | Container | Status | Notes |
|---|-------------|-------------|-----------|--------|-------|
| 1 | Alice Rossi | `alice-rossi` | `payslips-alice-rossi` | **Active** | Primary demo user |
| 2 | Bob Fischer | `bob-fischer` | `payslips-bob-fischer` | **Active** | Second active employee |
| 3 | Carol Chen | `carol-chen` | `payslips-carol-chen` | **Active** | Shows Cool-tier payslips |
| 4 | Dave Müller | `dave-mueller` | `payslips-dave-mueller` | **Former** | Left the company — demonstrates archived/former scenario |

### Sample Payslips

Upload these files (any small PDF will do) into each employee's container, then let the Function apply tags, or set tags manually.

#### Alice Rossi (`payslips-alice-rossi`) — Active

| Blob Name | Access Tier | Tags (Year/Month) | Purpose |
|-----------|-------------|-------------------|---------|
| `2026-01.pdf` | **Hot** | 2026 / 01 | Recent payslip, immediately downloadable |
| `2026-02.pdf` | **Hot** | 2026 / 02 | Recent payslip |
| `2025-12.pdf` | **Cool** | 2025 / 12 | Older payslip moved to Cool tier |
| `2025-06.pdf` | **Cold** | 2025 / 06 | Even older payslip in Cold tier |
| `2024-12.pdf` | **Archive** | 2024 / 12 | Archived payslip — triggers rehydration warning in UI |

#### Bob Fischer (`payslips-bob-fischer`) — Active

| Blob Name | Access Tier | Tags (Year/Month) | Purpose |
|-----------|-------------|-------------------|---------|
| `2026-01.pdf` | **Hot** | 2026 / 01 | Recent payslip |
| `2026-02.pdf` | **Hot** | 2026 / 02 | Recent payslip |
| `2025-09.pdf` | **Cool** | 2025 / 09 | Demonstrates Cool tier |

#### Carol Chen (`payslips-carol-chen`) — Active

| Blob Name | Access Tier | Tags (Year/Month) | Purpose |
|-----------|-------------|-------------------|---------|
| `2026-01.pdf` | **Hot** | 2026 / 01 | Recent payslip |
| `2025-11.pdf` | **Cool** | 2025 / 11 | Cool tier |
| `2025-03.pdf` | **Cold** | 2025 / 03 | Cold tier |
| `2024-06.pdf` | **Archive** | 2024 / 06 | Archive tier — rehydration warning |

#### Dave Müller (`payslips-dave-mueller`) — Former Employee

| Blob Name | Access Tier | Tags (Year/Month) | Purpose |
|-----------|-------------|-------------------|---------|
| `2025-06.pdf` | **Archive** | 2025 / 06 | Last payslip before departure, archived |
| `2025-05.pdf` | **Archive** | 2025 / 05 | Archived |
| `2024-12.pdf` | **Archive** | 2024 / 12 | Archived — all payslips for former employees are in Archive |

### Creating demo data with Azure CLI

```powershell
# Variables
$storageAccount = "<YOUR_STORAGE_ACCOUNT>"
$rg = "<YOUR_RESOURCE_GROUP>"

# Create containers
az storage container create -n payslips-alice-rossi   --account-name $storageAccount
az storage container create -n payslips-bob-fischer    --account-name $storageAccount
az storage container create -n payslips-carol-chen     --account-name $storageAccount
az storage container create -n payslips-dave-mueller   --account-name $storageAccount

# Upload sample PDFs (use any small PDF file)
# Example for Alice Rossi:
az storage blob upload --account-name $storageAccount -c payslips-alice-rossi -n "2026-01.pdf" -f sample.pdf --tier Hot
az storage blob upload --account-name $storageAccount -c payslips-alice-rossi -n "2026-02.pdf" -f sample.pdf --tier Hot
az storage blob upload --account-name $storageAccount -c payslips-alice-rossi -n "2025-12.pdf" -f sample.pdf --tier Cool
az storage blob upload --account-name $storageAccount -c payslips-alice-rossi -n "2025-06.pdf" -f sample.pdf --tier Cold
az storage blob upload --account-name $storageAccount -c payslips-alice-rossi -n "2024-12.pdf" -f sample.pdf --tier Archive

# Set blob tags (the Function does this automatically via Event Grid,
# but you can also set them manually):
az storage blob tag set --account-name $storageAccount -c payslips-alice-rossi -n "2026-01.pdf" `
  --tags "EmployeeId=alice-rossi" "PayslipYear=2026" "PayslipMonth=01" "DocumentType=Payslip"
```

> **Tip:** If the Event Grid subscription is configured, uploading a blob triggers the Function automatically, and tags are applied without manual intervention.

---

## Azure Blob Storage Concepts Demonstrated

| Concept | Where |
|---------|-------|
| **Per-entity containers** | Each employee gets `payslips-{id}` |
| **Blob naming convention** | `yyyy-MM.pdf` enforced by validation |
| **Blob index tags** | Applied by the Function, queried by the app |
| **Access tiers (Hot / Cool / Cold / Archive)** | Demo data uses all four tiers |
| **Archive rehydration warning** | UI warns when payslip is in Archive tier |
| **SAS tokens** | Time-limited download URLs generated server-side |
| **Event Grid integration** | `BlobCreated` → Function → tag enrichment |
| **DefaultAzureCredential** | Managed identity in Azure, `az login` locally |
| **Lifecycle management** | Move older payslips to Cool → Cold → Archive |

## Technology Stack

| Technology | Purpose |
|-----------|---------|
| .NET 10 | Runtime & SDK |
| ASP.NET Core MVC | Employee-facing web app |
| Azure Blob Storage SDK | Payslip PDF storage |
| Microsoft Identity Web | Entra ID authentication |
| Azure Functions (Isolated Worker) | Event-driven blob processing |
| Azure Event Grid | BlobCreated event routing |
| Bicep | Infrastructure as Code |
| Azure Developer CLI (`azd`) | Deployment orchestration |

## Learning Resources

- [Azure Blob Storage Documentation](https://learn.microsoft.com/azure/storage/blobs/)
- [Microsoft Identity Platform](https://learn.microsoft.com/entra/identity-platform/)
- [Azure Functions](https://learn.microsoft.com/azure/azure-functions/)
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

## Contributing

This is a demo application for training and conference presentations. Feel free to fork and customize for your own use cases.

## License

MIT License — see [LICENSE](LICENSE) file for details.

## Author

**Massimo Bonanni**
- Blog: [Configure and Code](https://configureandcode.cloud)
- GitHub: [@massimobonanni](https://github.com/massimobonanni)
