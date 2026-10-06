# Architecture

```text
McpClientDemo
    |
    | stdio
    v
FirstMcpServer
    |
    +-- get_service_health
    |      -> ServiceHealthStore
    |
    +-- get_recent_deployment
    |      -> DeploymentStore
    |
    +-- get_runbook
           -> RunbookStore
                |
                v
        deterministic JSON data
```

The MCP server owns trusted tool registration and deterministic local data. The client can discover and invoke only the tools exposed by the server.

The sample deliberately uses stdio so protocol behavior can be demonstrated without networking, authentication, hosting infrastructure, or an LLM.
