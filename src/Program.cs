// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable OPENAI001 // Experimental OpenAI types

using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;
using Shared.Foundry;
using Shared.Workflows;

namespace Demo.Workflows.Declarative.ServicesPayment;

/// <summary>
/// This workflow demonstrates using multiple agents to provide automated
/// troubleshooting steps to resolve common issues with escalation options.
/// </summary>
/// <remarks>
/// See the README.md file in the parent folder (../README.md) for detailed
/// information about the configuration required to run this sample.
/// </remarks>
internal sealed class Program
{
    public static async Task Main(string[] args)
    {
        // Initialize configuration
        IConfiguration configuration = Application.InitializeConfig();
        Uri foundryEndpoint = new(configuration.GetValue(Application.Settings.FoundryEndpoint));

        // Create the ticketing plugin (mock functionality)
        PaymentPlugin plugin = new();

        // Ensure sample agents exist in Foundry.
        await CreateAgentsAsync(foundryEndpoint, configuration, plugin);

        // Get input from command line or console
        string workflowInput = Application.GetInput(args);

        // Create the workflow factory.  This class demonstrates how to initialize a
        // declarative workflow from a YAML file. Once the workflow is created, it
        // can be executed just like any regular workflow.
        WorkflowFactory workflowFactory =
            new("ServicesPayment.yaml", foundryEndpoint)
            {
                Functions =
                [
                    AIFunctionFactory.Create(plugin.ListFavoriteServices),
                    AIFunctionFactory.Create(plugin.GetBalance),
                    AIFunctionFactory.Create(plugin.PayService),
                    AIFunctionFactory.Create(plugin.GetLatestBill)
                ]
            };

        // Execute the workflow:  The WorkflowRunner demonstrates how to execute
        // a workflow, handle the workflow events, and providing external input.
        // This also includes the ability to checkpoint workflow state and how to
        // resume execution.
        WorkflowRunner runner = new();
        await runner.ExecuteAsync(workflowFactory.CreateWorkflow, workflowInput);
    }

    private static async Task CreateAgentsAsync(Uri foundryEndpoint, IConfiguration configuration, PaymentPlugin plugin)
    {
        AIProjectClient aiProjectClient = new(foundryEndpoint, new AzureCliCredential());

        await aiProjectClient.CreateAgentAsync(
            agentName: "ServiceSelectionAgent",
            agentDefinition: DefineServiceSelectionAgent(configuration, plugin),
            agentDescription: "Helps user select a service to pay");

        await aiProjectClient.CreateAgentAsync(
            agentName: "GetBalanceAgent",
            agentDefinition: DefineGetBalanceAgent(configuration, plugin),
            agentDescription: "Agent that retrieves account balance");

        await aiProjectClient.CreateAgentAsync(
            agentName: "BalanceAndConfirmationAgent",
            agentDefinition: DefineBalanceAndConfirmationAgent(configuration, plugin),
            agentDescription: "Confirms payment amount with user");

        await aiProjectClient.CreateAgentAsync(
            agentName: "PayServiceAgent",
            agentDefinition: DefinePayServiceAgent(configuration, plugin),
            agentDescription: "Agent that processes service payments");

        await aiProjectClient.CreateAgentAsync(
            agentName: "PaymentAgent",
            agentDefinition: DefinePaymentAgent(configuration),
            agentDescription: "eceipt	Shows receipt to user");
    }

    private static PromptAgentDefinition DefineServiceSelectionAgent(IConfiguration configuration, PaymentPlugin plugin) =>
        new(configuration.GetValue(Application.Settings.FoundryModel))
        {
            Instructions =
                """
                Eres un asistente de pagos.

                Objetivo:
                - Ayudar al usuario a seleccionar un servicio favorito para pagar.

                Comportamiento:
                - Pregunta cuál es el servicio que desea pagar.
                - Si tienes acceso a la herramienta ListFavoriteServices, úsala para mostrar opciones (solo si aporta valor).
                - Si el usuario menciona un servicio ambiguo, haz una pregunta de aclaración.
                - No inventes ServiceId. Si no puedes determinarlo, sigue preguntando.
                - Cuando tengas suficiente información para una selección única, marca IsComplete=true.

                Reglas de salida:
                - Devuelve SIEMPRE un JSON válido que cumpla el esquema.
                - Mientras falte información para elegir un servicio, usa IsComplete=false y deja ServiceId/ServiceName vacíos.

                CustomerId: {{CustomerId}}
                """,
            Tools =
            {
                AIFunctionFactory.Create(plugin.ListFavoriteServices).AsOpenAIResponseTool()
            },
            StructuredInputs =
            {
                ["CustomerId"] = new StructuredInputDefinition
                {
                    IsRequired = true,
                    Description = "Customer identifier if available."
                }
            },
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "ServiceSelectionResult",
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "additionalProperties": false,
                          "properties": {
                            "IsComplete": {
                              "type": "boolean",
                              "description": "True when a unique service has been selected."
                            },
                            "ServiceId": {
                              "type": "string",
                              "description": "Identifier of the selected service."
                            },
                            "ServiceName": {
                              "type": "string",
                              "description": "Human-friendly name of the selected service."
                            },
                            "UserMessage": {
                              "type": "string",
                              "description": "Message to the user (question or confirmation)."
                            }
                          },
                          "required": ["IsComplete", "ServiceId", "ServiceName", "UserMessage"]
                        }
                        """),
                    jsonSchemaFormatDescription: null,
                    jsonSchemaIsStrict: true)
            }
        };


    private static PromptAgentDefinition DefineGetBalanceAgent(IConfiguration configuration, PaymentPlugin plugin) =>
        new(configuration.GetValue(Application.Settings.FoundryModel))
        {
            Instructions =
                """
                Eres un agente que obtiene el saldo de una cuenta bancaria.

                Tarea:
                - Llama a GetBalance(AccountId) exactamente una vez para obtener el saldo.
                - Devuelve el resultado (Balance, Currency) en la salida estructurada.

                Reglas:
                - No hagas preguntas al usuario.
                - No inventes datos: si la herramienta falla, indica ErrorMessage y deja Balance/Currency en valores seguros.

                El AccountId es: {{AccountId}}
                """,
            Tools =
            {
                AIFunctionFactory.Create(plugin.GetBalance).AsOpenAIResponseTool()
            },
            StructuredInputs =
            {
                ["AccountId"] = new StructuredInputDefinition
                {
                    IsRequired = true,
                    Description = "Account identifier."
                }
            },
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "BalanceResult",
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "additionalProperties": false,
                          "properties": {
                            "Balance": {
                              "type": "number",
                              "description": "Available balance."
                            },
                            "Currency": {
                              "type": "string",
                              "description": "Currency code (e.g., S/.)"
                            },
                            "ErrorMessage": {
                              "type": "string",
                              "description": "Present when the tool call failed."
                            }
                          },
                          "required": ["Balance", "Currency", "ErrorMessage"]
                        }
                        """),
                    jsonSchemaFormatDescription: null,
                    jsonSchemaIsStrict: true)
            }
        };

    private static PromptAgentDefinition DefineBalanceAndConfirmationAgent(IConfiguration configuration, PaymentPlugin plugin) =>
        new(configuration.GetValue(Application.Settings.FoundryModel))
        {
            Instructions =
                """
                Eres un asistente de pagos. Debes pedir confirmación antes de realizar el pago.

                Contexto (ya proporcionado):
                - CustomerId: {{CustomerId}}
                - Servicio: {{ServiceName}} ({{ServiceId}})
                - Saldo disponible: {{Balance}} {{Currency}}

                Qué debes hacer:
                - Llama a la herramienta GetLatestBill(CustomerId, ServiceId) para obtener el monto pendiente del servicio.
                - Indica el saldo disponible al usuario.
                - Asegúrate de que el usuario confirme el pago.
                - Si el usuario dice que NO desea pagar/cancelar: Confirmed=false, IsComplete=true.
                - Si el usuario desea pagar pero **NO** fue posible confirmar el monto: pide el monto y mantén IsComplete=false.
                - Si el usuario confirma: IsComplete=true, Confirmed=true, Amount=... (número).

                Importante:
                - NO ejecutes el pago aquí (este agente solo confirma).
                - No valides reglas duras (monto > 0 o fondos suficientes) como decisión final; el workflow lo valida.
                  Aun así, si el monto es claramente inválido o mayor que el saldo, puedes advertir al usuario y pedir otro monto,
                  manteniendo IsComplete=false para que el loop continúe.

                Reglas de salida:
                - Devuelve SIEMPRE un JSON válido que cumpla el esquema.
                - Siempre incluye ServiceId (eco del input).
                - Amount debe ser numérico (usa 0 si aún no se ha proporcionado un monto).
                """,
            Tools = {
                AIFunctionFactory.Create(plugin.GetLatestBill).AsOpenAIResponseTool()
            },
            StructuredInputs =
            {
                ["CustomerId"] = new StructuredInputDefinition
                {
                    IsRequired = true,
                    Description = "Customer identifier."
                },
                ["ServiceId"] = new StructuredInputDefinition
                {
                    IsRequired = true,
                    Description = "Selected service id."
                },
                ["ServiceName"] = new StructuredInputDefinition
                {
                    IsRequired = true,
                    Description = "Selected service name."
                },
                ["Balance"] = new StructuredInputDefinition
                {
                    IsRequired = true,
                    Description = "Available balance."
                },
                ["Currency"] = new StructuredInputDefinition
                {
                    IsRequired = false,
                    DefaultValue = BinaryData.FromString(@"""S/."""),
                    Description = "Currency code."
                }
            },
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "PaymentConfirmationResult",
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "additionalProperties": false,
                          "properties": {
                            "IsComplete": {
                              "type": "boolean",
                              "description": "True when confirmation flow is finished (confirmed or cancelled)."
                            },
                            "Confirmed": {
                              "type": "boolean",
                              "description": "True if the user confirmed the payment."
                            },
                            "Amount": {
                              "type": "number",
                              "description": "Payment amount; 0 if not yet provided."
                            },
                            "ServiceId": {
                              "type": "string",
                              "description": "Echo of the selected service id."
                            },
                            "UserMessage": {
                              "type": "string",
                              "description": "Message to the user (prompt/confirmation)."
                            }
                          },
                          "required": ["IsComplete", "Confirmed", "Amount", "ServiceId", "UserMessage"]
                        }
                        """),
                    jsonSchemaFormatDescription: null,
                    jsonSchemaIsStrict: true)
            }
    };


    private static PromptAgentDefinition DefinePayServiceAgent(IConfiguration configuration, PaymentPlugin plugin) =>
        new(configuration.GetValue(Application.Settings.FoundryModel))
        {
            Instructions =
                """
                Eres un agente de ejecución de pagos.

                Tarea:
                - Llama a PayService(AccountId, ServiceId, Amount).
                - Devuelve ReceiptId y ReceiptDetails exactamente como los entregue la herramienta.

                Reglas:
                - No converses con el usuario.
                - No inventes recibos: si falla, devuelve ErrorMessage y deja ReceiptId/ReceiptDetails vacíos.

                Datos:
                - AccountId: {{AccountId}}
                - ServiceId: {{ServiceId}}
                - Amount: {{Amount}}
                """,
            Tools =
            {
                AIFunctionFactory.Create(plugin.PayService).AsOpenAIResponseTool()
            },
            StructuredInputs =
            {
                ["AccountId"] = new StructuredInputDefinition
                {
                    IsRequired = true,
                    Description = "Account identifier."
                },
                ["ServiceId"] = new StructuredInputDefinition
                {
                    IsRequired = true,
                    Description = "Service identifier."
                },
                ["Amount"] = new StructuredInputDefinition
                {
                    IsRequired = true,
                    Description = "Amount to pay."
                }
            },
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "PaymentExecutionResult",
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "additionalProperties": false,
                          "properties": {
                            "ReceiptId": {
                              "type": "string",
                              "description": "Receipt identifier."
                            },
                            "ReceiptDetails": {
                              "type": "string",
                              "description": "Receipt details."
                            },
                            "ErrorMessage": {
                              "type": "string",
                              "description": "Present when the tool call failed."
                            }
                          },
                          "required": ["ReceiptId", "ReceiptDetails", "ErrorMessage"]
                        }
                        """),
                    jsonSchemaFormatDescription: null,
                    jsonSchemaIsStrict: true)
            }
    };

    private static PromptAgentDefinition DefinePaymentAgent(IConfiguration configuration) =>
        new(configuration.GetValue(Application.Settings.FoundryModel))
        {
            Instructions =
                """
                Eres un asistente que presenta el comprobante de pago al usuario.

                Tarea:
                - Redacta un mensaje claro y corto confirmando el pago.
                - Incluye el ReceiptId.
                - Resume los ReceiptDetails (sin exponer datos sensibles si existieran).
                - Ofrece ayuda para realizar otro pago.

                Datos:
                - ReceiptId: {{ReceiptId}}
                - ReceiptDetails: {{ReceiptDetails}}

                Reglas de salida:
                - Devuelve SIEMPRE un JSON válido con un campo UserMessage listo para enviarse.
                """,
            StructuredInputs =
            {
                ["ReceiptId"] = new StructuredInputDefinition
                {
                    IsRequired = true,
                    Description = "Receipt identifier."
                },
                ["ReceiptDetails"] = new StructuredInputDefinition
                {
                    IsRequired = true,
                    Description = "Receipt details."
                }
            },
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "ReceiptMessage",
                    BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "additionalProperties": false,
                          "properties": {
                            "UserMessage": {
                              "type": "string",
                              "description": "Natural language receipt/confirmation message to send."
                            }
                          },
                          "required": ["UserMessage"]
                        }
                        """),
                    jsonSchemaFormatDescription: null,
                    jsonSchemaIsStrict: true)
            }
    };
}