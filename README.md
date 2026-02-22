# NIOP Partner Number Endpoints - Consumer (PCAW)

This is the **consumer-side** contract testing project for the PCAW system interacting with the NIOP Beat Inventory API.

## Project Structure

```
NIOP_PARTNUMBERENDPOINTS_CONSUMER/
├── .github/
│   └── workflows/
│       └── consumer-contract-tests.yml    # GitHub Actions pipeline
├── pacts/                                  # Generated pact files (output)
├── src/
│   └── Consumer.PCAW.ContractTests/
│       ├── Client/                         # HTTP client for the provider API
│       ├── Constants/                      # Pact configuration constants
│       ├── Models/                         # Request/Response models
│       └── PcawUpdateDeviceTests.cs        # Pact consumer tests
├── NIOP.Consumer.sln                       # Solution file
└── README.md
```

## Running Consumer Tests

```bash
dotnet test src/Consumer.PCAW.ContractTests/Consumer.PCAW.ContractTests.csproj
```

This generates pact files under the `pacts/` directory.

## Relationship with Provider

- **Provider Repository:** `NIOP_PartNumberEndpoints`
- **Provider API:** NIOP Beat Inventory API (`NIOP-Beat-Inventory-Api`)
- **Consumer Name:** `PCAW-Consumer`

The consumer and provider are completely independent projects with no shared code.
Pact files generated here are published to the Pact Broker and verified by the provider.
