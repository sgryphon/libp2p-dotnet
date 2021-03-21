# Libp2p.Net

Libp2p implementation for Dotnet Core.

## Project status

This library is still in alpha development. 

It only has the most very basic address parsing, and only enough to get a single transport working for a raw connection.

## Getting started

### Prerequisites

* Dotnet Core 3.1 LTS

### Compile and run unit tests

```pwsh
dotnet test
```

### Run basic example

In one terminal, run the HelloLibp2p example in listen mode:

```pwsh
dotnet run --project examples/HelloLibp2p -- listen
```

Open a second terminal, and run the example in connect mode:

```pwsh
dotnet run --project examples/HelloLibp2p -- connect
```

