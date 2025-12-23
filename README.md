# <img src="images/ai-foundry.png" height="32" alt="Azure AI Foundry" /> Azure AI Foundry Workflows - Payment Service Demo


A declarative workflow demo using Microsoft Foundry agents for processing service payments.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) installed and logged in
- Access to an Azure AI Foundry project

## Setup

### 1. Login to Azure

```bash
az login
```

### 2. Configure the application

Edit `src/appsettings.json` with your Azure AI Foundry project details:

```json
{
  "Foundry": {
    "Endpoint": "https://your-project.services.ai.azure.com/api/projects/proj-default",
    "Model": "gpt-4o-mini"
  }
}
```

### 3. Restore dependencies

```bash
cd src
dotnet restore
```

## Running the Workflow

```bash
cd src
dotnet run
```

Or with a specific input message:

```bash
dotnet run -- "Quiero pagar un servicio"
```

## Project Structure

```
├── src/
│   ├── Program.cs              # Main entry point, agent definitions
│   ├── PaymentPlugin.cs        # Mock payment tools (ListFavoriteServices, GetBalance, etc.)
│   ├── appsettings.json        # Configuration (Foundry endpoint, model)
│   └── ServicesPayment.csproj  # Project file
├── shared/
│   ├── Directory.Build.props   # MSBuild shared code injection
│   ├── Foundry/Agents/         # Agent factory utilities
│   └── Workflows/Execution/    # Workflow runner and factory
└── workflows/
    └── ServicesPayment.yaml    # Declarative workflow definition
```

## Workflow Overview

![Payment Workflow](images/workflow.png)

The payment workflow includes 4 agents:

| Agent | Purpose |
|-------|---------|
| `ServiceSelectionAgent` | Helps user select a service to pay |
| `GetBalanceAgent` | Retrieves account balance |
| `BalanceAndConfirmationAgent` | Confirms payment amount with user |
| `PayServiceAgent` | Executes the payment |

### Flow

1. User selects a service from their favorites
2. System retrieves account balance
3. System retrieves latest bill amount
4. User confirms payment 
4. Payment is executed
5. Receipt is displayed

## Sample Conversations

### ✅ Successful Payment (Sufficient Balance)

```
🧑 User: mis servicios favoritos

🤖 ServiceSelectionAgent: Estos son tus servicios favoritos:
   • Luz del Sur (Electricidad) — id SVC001
   • Sedapal (Agua) — id SVC002
   • Netflix (Streaming) — id SVC004
   ¿Cuál deseas pagar? Puedes responder con el nombre o con el id.

🧑 User: netflix

🤖 ServiceSelectionAgent: ¿Confirmas que deseas pagar Netflix?

🧑 User: si

🤖 ServiceSelectionAgent: Confirmado: procederé a pagar Netflix (id SVC004).

🤖 LatestBillAndConfirmationAgent: El monto a pagar por Netflix es 44.90 S/. 
   Tu saldo disponible es 1000.5 S/. ¿Confirmas el pago?

🧑 User: si

🤖 LatestBillAndConfirmationAgent: Pago confirmado. Se iniciará el pago de 
   Netflix por 44.90 S/. El sistema verificará fondos y procesará la transacción.

✅ Pago confirmado — ReceiptId: RCP-B5C2DB72
   Pago de 44.90 S/. a Netflix realizado exitosamente. 
   Fecha: 12/23/2025 7:20 PM
   ¿Deseas realizar otro pago?
```

### ❌ Insufficient Balance

```
🧑 User: quiero pagar uno de mis servicios registrados

🤖 ServiceSelectionAgent: Estos son tus servicios registrados:
   • Luz del Sur (Electricidad) — ID: SVC001
   • Sedapal (Agua) — ID: SVC002
   • Netflix (Streaming) — ID: SVC004
   ¿Cuál deseas pagar? Indica el nombre o el ID del servicio.

🧑 User: si, seria netflix

🤖 ServiceSelectionAgent: Has seleccionado Netflix. ¿Confirmas que deseas pagar Netflix?

🧑 User: si!

🤖 ServiceSelectionAgent: Confirmación recibida: procederé a iniciar el pago 
   de Netflix (ID: SVC004).

🤖 LatestBillAndConfirmationAgent: El monto a pagar por Netflix es 44.90 S/. 
   Tu saldo disponible es 20.5 S/. ¿Confirmas el pago?

🧑 User: si

🤖 LatestBillAndConfirmationAgent: Pago confirmado. Se iniciará el pago de 
   44.90 S/. por Netflix (ID: SVC004). El sistema verificará fondos y 
   completará la transacción.

❌ Fondos insuficientes: El monto a pagar (44.9 S/.) 
   es mayor que tu saldo disponible (20.5 S/.).
   No se realizó el pago. ¿Deseas pagar otro servicio?
```

## Troubleshooting

### Slow startup

The first run may take 7-10 seconds due to Azure authentication cold start. Subsequent runs are faster.

### Azure CLI credential errors

Make sure you're logged in:

```bash
az login
az account show
```

### Missing agents

Agents are created automatically on startup. If you see errors about missing agents, check your Foundry endpoint configuration.