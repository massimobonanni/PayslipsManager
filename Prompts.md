# GitHub Copilot prompt — split into simple tasks

## Task 1 — Create the solution structure

Create a **.NET 10** solution named **PayslipsManager** using **Clean Architecture** style.

Use these projects:

* `src/PayslipsManager.Web` → ASP.NET Core MVC web app
* `src/PayslipsManager.Application` → application services, DTOs, interfaces
* `src/PayslipsManager.Domain` → domain models, enums, value objects
* `src/PayslipsManager.Infrastructure` → Azure Blob Storage, Azure Identity, service implementations
* `src/PayslipsManager.Functions` → Azure Functions isolated worker for BlobCreated event handling

Add project references consistent with Clean Architecture:

* Web → Application + Infrastructure
* Infrastructure → Application + Domain
* Application → Domain
* Functions → Application + Infrastructure + Domain

Use:

* `Azure.Storage.Blobs`
* `Azure.Identity`
* `Microsoft.Identity.Web`
* `Microsoft.Identity.Web.UI`

Use dependency injection, logging, options pattern, and async APIs.

Do not create any test projects.

---

## Task 2 — Implement the domain and application layer

Create the domain model for a web application called **PayslipsManager**.

Business scenario:

* Employees sign in with their corporate Microsoft Entra account
* Each employee can see only their own payslips
* Payslips are monthly PDF files stored in Azure Blob Storage
* There is one blob container per employee

Create these domain models:

* `Employee`

  * `EmployeeId`
  * `DisplayName`
  * `EntraObjectId`
  * `EmploymentStatus`

* `PayslipDocument`

  * `BlobName`
  * `FileName`
  * `EmployeeId`
  * `PayslipDate`
  * `AccessTier`
  * `IsArchived`
  * `UploadedOn`
  * `ContentType`
  * `Tags`

* `EmploymentStatus`

  * `Active`
  * `Former`

* `PayslipValidationResult`

  * `IsValid`
  * `Errors`

Create application interfaces:

* `IEmployeeContextService`
* `IPayslipQueryService`
* `IPayslipDownloadService`
* `IPayslipStorageService`
* `IPayslipEventProcessor`

Create DTOs/view models for:

* payslip list page
* payslip details
* archive warning state
* current signed-in employee info

---

## Task 3 — Implement the Azure Blob Storage infrastructure

Implement the infrastructure layer using **Azure Blob Storage** and **Azure Identity**.

Requirements:

* Use `DefaultAzureCredential`
* Use managed identity in Azure
* Do not use storage account keys in code
* Do not use connection strings in production
* Access Azure Blob Storage through `BlobServiceClient`

Storage design:

* Use one **Standard GPv2** storage account
* Use **Blob Storage**
* Use one **container per employee**
* Each payslip is a **PDF block blob**
* Blob naming convention: `yyyy-MM.pdf`

Add support for blob metadata and blob index tags:

* `EmployeeId`
* `EmployeeDisplayName`
* `PayslipYear`
* `PayslipMonth`
* `EmploymentStatus`
* `DocumentType = Payslip`

Implement services to:

* resolve employee container name from identity
* list payslips in the employee container
* get blob details and tier
* upload a payslip
* download a payslip
* set and update tags
* validate blob names and content type
* detect archive status and expose it to the app

---

## Task 4 — Implement the ASP.NET Core MVC web app

Create the employee-facing web app using **ASP.NET Core MVC**.

Requirements:

* Use Microsoft Entra ID authentication with `Microsoft.Identity.Web`
* Anonymous users must not access the payslip pages
* The signed-in employee must be resolved from claims
* The application must map the user to the correct employee container

Pages:

* `Home`
* `My Payslips`
* `Payslip Details`
* `Access Denied`
* `Error`

Behavior:

* Show only the signed-in employee’s payslips
* Sort payslips newest first
* Show:

  * month/year
  * file name
  * access tier
  * uploaded date
  * archive status
* Allow open/download only for the current employee’s payslips
* Do not expose direct URLs that allow guessing another employee’s files
* Enforce authorization server-side, not only in the UI

Archived behavior:

* If a payslip is archived, show a friendly message such as “Restore required”
* Archived files must not be downloaded directly

Style:

* Keep the UI simple, clean, modern, and demo-friendly

---

## Task 5 — Implement the Azure Function for storage events

Create an **Azure Functions isolated worker** project for event-driven processing.

Scenario:
When a new payslip PDF is uploaded to Blob Storage, Blob Storage emits a **BlobCreated** event and the function processes it.

Requirements:

* Handle Blob Storage events delivered through Event Grid
* Process `BlobCreated`
* Validate that:

  * file extension is `.pdf`
  * blob name follows `yyyy-MM.pdf`
  * blob belongs to a valid employee container
* Apply/update blob tags
* Log success or validation failure
* Make processing idempotent
* Do not delete invalid files automatically

Add a service called `IPayslipEventProcessor` and implement it in Infrastructure or Functions support code.

The function should be demo-friendly and easy to follow.

---

## Task 6 — Add configuration, demo data, and README

Add configuration support for local development and Azure deployment.

Requirements:

* `appsettings.json`
* `appsettings.Development.json`
* local settings for Functions
* options classes for Azure Storage and authentication settings
* support local development with `DefaultAzureCredential`

Add demo-friendly sample data assumptions:

* at least 3 sample employees
* several sample payslips
* a mix of Hot, Cool, Cold, and Archive examples
* one former employee example

Create a README that explains:

* solution structure
* prerequisites
* local development
* Azure resources needed
* authentication setup
* storage setup
* how the event flow works
* how to run the web app and functions locally

Do not add a testing section.

---

## Task 7 — Make the solution AZD-compatible

Make the repository **Azure Developer CLI compatible**.

Use this repository structure:

* `azure.yaml` at the root
* `infra/` for Bicep files
* `src/` for application source code
* optional `.azure/` support during azd usage

Create an `azure.yaml` file that maps:

* the MVC web project to an **App Service** host
* the Azure Functions project to a **Function** host

Create an `infra` folder with Bicep files for Azure deployment.

At minimum include:

* `infra/main.bicep`
* modules or supporting Bicep files as needed

Provision these Azure resources:

* resource group
* App Service plan
* Web App
* Function App
* Storage Account Standard GPv2
* Application Insights
* Event Grid subscription for BlobCreated
* managed identities for Web App and Function App
* role assignments for Blob data access

The `azure.yaml` file must define services and project paths correctly so `azd up`, `azd provision`, and `azd deploy` can work with the solution. `azd` uses `azure.yaml` to map service folders to Azure hosts, while infrastructure is typically taken from `infra/main.bicep` by default. ([Microsoft Learn][1])

Use a structure similar to:

* `azure.yaml`
* `infra/main.bicep`
* `src/PayslipsManager.Web`
* `src/PayslipsManager.Functions`
* `src/PayslipsManager.Application`
* `src/PayslipsManager.Domain`
* `src/PayslipsManager.Infrastructure`

---

## Task 8 — Final cleanup and polish

Review the entire solution and improve it for readability and demo usage.

Requirements:

* keep code production-style but simple
* use clear names
* add comments only where helpful
* make the app easy to demo during a conference session
* keep architecture clean
* avoid unnecessary complexity
* keep the solution focused on Blob Storage, Entra auth, access tiers, and storage events

Do not add test projects or test tasks.

---

# Compact version to paste into Copilot first

Use this if you want a shorter “master prompt” before running the tasks one by one:

```text
Create a .NET 10 solution named PayslipsManager using Clean Architecture style. Use ASP.NET Core MVC for the employee web app, Azure Functions isolated worker for BlobCreated processing, Azure Blob Storage for payslip PDFs, Azure Identity with DefaultAzureCredential, and Microsoft Entra ID with Microsoft.Identity.Web for sign-in. Use one blob container per employee, store payslips as block blob PDFs named yyyy-MM.pdf, and expose only the signed-in employee’s payslips. Show blob access tier and archive state in the UI. Use Standard GPv2 storage assumptions. Add blob tags such as EmployeeId, Year, Month, EmploymentStatus, and DocumentType. Implement the solution in multiple projects under src: PayslipsManager.Web, Application, Domain, Infrastructure, and Functions. Do not create any test projects. Make the repo Azure Developer CLI compatible using a root azure.yaml file, an infra folder with Bicep files including infra/main.bicep, and service mappings for the web app on App Service and the Functions app on Function host so azd up, azd provision, and azd deploy can be used.
```

If you want, I can also turn this into a **ready-to-paste `.github/copilot-instructions.md`** version.

[1]: https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/quickstart-explore-templates "Explore and customize an Azure Developer CLI Template | Microsoft Learn"
