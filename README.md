# 🧠 SynthesisAIAgents — Orchestrator Prototype

![.NET](https://img.shields.io/badge/.NET-8.0-blueviolet)
![Build](https://img.shields.io/badge/build-passing-brightgreen)
![Coverage](https://img.shields.io/badge/coverage-97%25-green)

## 🚀 Overview
**SynthesisAIAgents** is a prototype AI **agent orchestration layer** built for the “Multi-Agent Task Solver” backend challenge.  
It enables enterprises to integrate AI as part of a **hybrid workforce**, running agents in isolation, chaining results, and exposing a simple API for orchestration and monitoring.

---

## 🧩 Core Features
- **Agent orchestration** with concurrency, retries, and timeouts  
- **Pluggable Tools** — HTTP fetcher, AWS Comprehend (Sentiment Analysis, Key Phrases)  
- **REST API** to submit graphs and poll execution status  
- **Structured logging** and in-memory persistence for prototyping  

---

## ⚙️ Components
| Type | Implementations |
|------|-----------------|
| **Agents** | `FetchHttpAgent`, `KeyPhrasesAgent`, `AwsSentimentAnalysisAgent` |
| **Tools** | `HttpFetcherTool`, `AwsKeyPhrasesTool`, `AwsSentimentAnalysisTool` |
| **API** | `/api/orchestrator` — submit, status, cancel |

---

## 🧠 Example Graph (fetch → keyphrases)
```json
{
  "graph": {
    "runId": "run-fetch-kp-001",
    "name": "fetch-then-keyphrases",
    "agents": [
      {
        "id": "fetch1",
        "type": "fetch",
        "parameters": { "tool": "http_fetcher", "url": "https://httpbin.org/json" },
        "next": ["kp1"]
      },
      {
        "id": "kp1",
        "type": "keyphrases",
        "parameters": {},
        "next": []
      }
    ]
  }
}
```

---

## 🌐 API Endpoints
| Method | Endpoint | Description |
|--------|-----------|-------------|
| **POST** | `/api/orchestrator/submit` | Submit a graph for execution |
| **GET** | `/api/orchestrator/status/{runId}` | Retrieve run status and results |
| **POST** | `/api/orchestrator/cancel/{runId}` | Cancel a running job |

---

## 📂 Folder Structure
synthesis-ai-agents/
├── src/
│   └── SynthesisAIAgents.Api/
│       ├── Agents/
│       ├── Controllers/
│       ├── DTOs/
│       ├── Models/
│       ├── Properties/
│       ├── Services/
│       ├── Tools/
│       ├── Utilities/
│       ├── appsettings.Development.json
│       ├── appsettings.json
│       ├── Program.cs
│       ├── SynthesisAIAgents.Api.csproj
│       ├── SynthesisAIAgents.http
│       └── SynthesisAIAgents.slnx
├── tests/
│   └── SynthesisAIAgents.Tests/
│       ├── Agents/
│       ├── Controllers/
│       ├── Services/
│       ├── Tools/
│       ├── Utilities/
│       └── SynthesisAIAgents.Tests.csproj
├── README.md
└── .gitignore

- src folder contains the API implementation and configuration.
- tests folder contains unit tests organized by folder. (move here before running test coverage commands)

---

## 🧰 Run Locally
### Requirements
- .NET 8 SDK  
- AWS credentials (optional, for Comprehend tools)  

### Setup
```bash
git clone https://github.com/FPDPanda/synthesis-ai-agents.git
cd SynthesisAIAgents
export AWS_REGION=us-east-1
export AWS_ACCESS_KEY_ID=...
export AWS_SECRET_ACCESS_KEY=...
dotnet run --project src/SynthesisAIAgents.Api
```
API runs at: **http://localhost:5258**

You can test endpoints using `SynthesisAIAgents.http` (VS Code REST Client) or `curl`.

---

## 🧪 Testing
- **Framework:** xUnit  
- **Mocking:** Moq  
- **Coverage:** coverlet + ReportGenerator  

Step 1. Install report-generator tool (if not already installed):
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Step 2. Navigate to the test folder (/tests) and run dotnet test with coverage (excluding Program.cs):
```bash
dotnet test .\SynthesisAIAgents.Tests\SynthesisAIAgents.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=./coverage/coverage.cobertura.xml /p:CoverletOutputFormat=cobertura /p:ExcludeByFile="**/Program.cs"```

Step 3. Generate report with report generator:
```bash
reportgenerator "-reports:**/coverage.cobertura.xml" "-targetdir:coveragereport" -reporttypes:Html
```

Step 3. Navigate to the generated report directory and open `index.html` in your browser to view the coverage report.

---

## 🔭 Next Steps that were not implemented due to time constraints
- Persistent store (PostgresSQL or other database)  
- Authentication and Authotization with JWT 
- Security improvements such as rate limiting  
- Observability (Logs, OpenTelemetry)  
- Streaming for LLM outputs (SSE / WebSockets)  
- Frontend visualizer for orchestration graphs
- End to end tests
- Kubernetes deployment scripts
- Higher unit test coverage (from 97% to 100%)
