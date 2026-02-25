# NIOP Partner Number Endpoints — Consumer (PCAW)

The **consumer-side** contract testing project for the PCAW system interacting with the NIOP Beat Inventory API.
Generates Pact JSON files that the provider verifies and that are cross-checked against the provider's OpenAPI spec.

---

## Project Structure

```
NIOP_PARTNUMBERENDPOINTS_CONSUMER/
├── pacts/                                  # Generated pact files (output of running tests)
├── src/
│   └── Consumer.PCAW.ContractTests/
│       ├── Client/
│       │   └── NiopInventoryApiClient.cs   # HTTP client wrapper
│       ├── Constants/
│       │   └── PactConstants.cs            # Provider/consumer names, endpoint paths
│       ├── Models/
│       │   ├── UpdateDeviceInformationRequest.cs
│       │   └── UpdateDeviceInformationResponse.cs
│       ├── Publisher/
│       │   └── PactBrokerPublisher.cs      # Pact Broker publish helper
│       ├── PcawUpdateDeviceTests.cs        # Consumer contract tests (3 interactions)
│       └── PublishPactsToBrokerTests.cs    # Publishes generated pacts to broker
├── NIOP.Consumer.sln
└── README.md
```

---

## Quick Start

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Step 1: Run Consumer Contract Tests

```powershell
cd NIOP_PARTNUMBERENDPOINTS_CONSUMER
dotnet test src/Consumer.PCAW.ContractTests/Consumer.PCAW.ContractTests.csproj --configuration Release
```

This generates pact JSON files under `pacts/`.

### Step 2: Copy Pacts for Local Provider Validation (local dev only)

> **In CI this is not needed.** The provider pipeline automatically downloads pact files from the Pact Broker before running swagger and contract tests.

For local development only:

```powershell
New-Item -ItemType Directory -Force -Path ..\NIOP_PartNumberEndpoints\pacts
Copy-Item pacts\*.json ..\NIOP_PartNumberEndpoints\pacts\
```

### Step 3: Run Swagger Mock Validation (provider side)

```powershell
cd ..\NIOP_PartNumberEndpoints
.\validate-swagger-pacts.ps1
```

---

## Consumer Interactions Tested

| Test | Method | Path | Expected Status |
|---|---|---|---|
| Successfully updates device part number during patient workflow | `POST` | `/api/UpdateDeviceInformation` | `200 OK` |
| Receives error when serial number is empty | `POST` | `/api/UpdateDeviceInformation` | `400 Bad Request` |
| Receives error when username is missing | `POST` | `/api/UpdateDeviceInformation` | `400 Bad Request` |

---

## Relationship with Provider

| | |
|---|---|
| **Provider Repository** | `NIOP_PartNumberEndpoints` |
| **Provider API** | NIOP Beat Inventory API (`NIOP-Beat-Inventory-Api`) |
| **Consumer Name** | `PCAW-Consumer` |
| **Shared code** | None — completely independent projects |

Pact files generated here are:
1. Published to the **Pact Broker** for provider CI verification
2. Cross-validated against the provider's **OpenAPI spec** by `SwaggerMockValidatorTests` (pure C#, no Node.js)

