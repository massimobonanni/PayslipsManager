# Copilot Instructions for PayslipsManager

## Goal
Build a **.NET 10** solution named **PayslipsManager** that demonstrates how to use **Azure Storage Account / Blob Storage** in a realistic business scenario.

The application allows employees to:
- sign in with their corporate Microsoft Entra account,
- see the list of their monthly payslips,
- open or download only their own payslip PDFs,
- never access payslips belonging to other employees.

The solution must be:
- clean and easy to understand,
- suitable for conference demos and training,
- compatible with **Azure Developer CLI (azd)** deployment,
- focused on **Blob Storage**, **Entra authentication**, **access tiers**, and **storage events**.

Do **not** create test projects.

---

## Architecture and project structure
Use **Clean Architecture** style and create this solution structure:

```text
PayslipsManager/
├─ .github/
│  └─ copilot-instructions.md
├─ azure.yaml
├─ infra/
│  ├─ main.bicep
│  └─ ...additional bicep modules if needed
├─ src/
│  ├─ PayslipsManager.Web/
│  ├─ PayslipsManager.Application/
│  ├─ PayslipsManager.Domain/
│  ├─ PayslipsManager.Infrastructure/
│  └─ PayslipsManager.Functions/
└─ README.md