# Optional embedding providers

The repository's live-verified default is **Ollama + `nomic-embed-text`**, which returns 768-dimensional vectors in the verified environment.

`EmbeddingProviderFactory` also contains compile/construction-verified examples for OpenAI and Azure OpenAI. They are intentionally not selectable through `appsettings.json` in the default application because the database schema is fixed to `vector(768)`.

If you switch embedding models/providers, verify the model's output dimension first, change the vector column dimension, recreate/migrate the database, and regenerate all stored embeddings. Do not mix embeddings from incompatible models or dimensions in the same vector column.

Cloud-provider live requests are outside this sample's default verification because they require user-owned credentials and may incur cost.