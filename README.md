# Local AI

Local AI use cases prototype:

1. Live Chat.
2. Extract invoice information.
3. Semantic search.

## Requirements

- [Docker](https://www.docker.com)
- [.NET SDK 8.0](https://dotnet.microsoft.com)

## Start infrastructure

Start [Ollama](https://ollama.com):

```bash
docker compose up -d
```

Install model:

```bash
docker exec -it ollama ollama pull SpeakLeash/bielik-11b-v3.0-instruct:Q4_K_M
```

## Live Chat

Start Live Chat console application:

```bash
cd live-chat
dotnet run
```

## Invoices

Start invoices API http://localhost:5120:

```bash
cd api
dotnet run
```
