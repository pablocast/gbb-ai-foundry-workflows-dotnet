# <svg height="32" viewBox="0 0 32 32" width="32" xmlns="http://www.w3.org/2000/svg" role="presentation"><path clip-rule="evenodd" d="M20.4052 2C20.3713 2.04989 20.3403 2.10356 20.3119 2.15906C20.1753 2.42519 20.0629 2.80022 19.9685 3.2499C19.7794 4.15205 19.6545 5.3972 19.5714 6.7798C19.405 9.54716 19.405 12.8938 19.405 15.213V24.4338L19.4049 24.4698C19.3854 27.5153 16.8918 29.9806 13.8112 29.9999L13.7749 30H3.57642C3.18062 30 2.9073 29.6141 3.04346 29.2496C4.56004 25.1917 6.6982 19.4832 8.50404 14.6901C9.40697 12.2934 10.2268 10.1257 10.8442 8.50763C11.4636 6.88453 11.876 5.82419 11.9665 5.63239C12.2132 5.10978 12.6147 4.1951 13.1873 3.40856C13.7637 2.61659 14.4808 2.00001 15.3445 2H20.4052ZM29.2769 10.1842C29.4966 10.1842 29.6747 10.3603 29.6747 10.5775V17.6706L29.6745 17.7148C29.6504 19.5836 28.1106 21.0913 26.2147 21.0913H21.668C21.6778 21.0796 21.6872 21.0676 21.6966 21.0552C21.8605 20.8367 21.9531 20.526 21.9587 20.134L21.9589 20.0958V14.0817C21.9589 11.9291 23.7238 10.1842 25.9011 10.1842H29.2769ZM21.2532 2.14424C21.5631 2.14425 21.8986 2.38926 22.2468 2.88783C22.5881 3.37681 22.9111 4.06635 23.2065 4.85721C23.7783 6.3875 24.2354 8.26487 24.5265 9.71512C22.6354 10.2861 21.2595 12.0248 21.2595 14.0817V20.0782L21.2594 20.0921C21.2575 20.2355 21.2263 20.4039 21.1685 20.5329C21.1042 20.6758 21.0375 20.7121 20.9938 20.7121C20.9575 20.7121 20.8869 20.6826 20.7852 20.5652C20.6894 20.4549 20.5915 20.2961 20.4975 20.1117C20.3151 19.7539 20.1614 19.3273 20.0739 19.0482V15.213C20.0739 8.68733 20.3039 5.39271 20.5834 3.73209C20.7239 2.89797 20.8739 2.49601 20.9998 2.30459C21.0605 2.21243 21.1101 2.17748 21.1426 2.16241C21.1755 2.14714 21.207 2.14424 21.2532 2.14424Z" fill="#0078D4" fill-rule="evenodd"></path></svg> Azure AI Foundry Workflows - Payment Service Demo


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

## Sample Conversation

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