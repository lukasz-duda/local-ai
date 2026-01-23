# Invoice Extractor

This project extracts key information from Polish PDF invoices using a local LLM.

First start [Ollama](https://ollama.com) and the [invoice API](http://localhost:5120/swagger):

```bash
docker compose --progress plain build
docker compose up --detach --remove-orphans
```

Then install the required language model:

```bash
docker exec -it ollama ollama pull SpeakLeash/bielik-11b-v3.0-instruct:Q4_K_M
```
