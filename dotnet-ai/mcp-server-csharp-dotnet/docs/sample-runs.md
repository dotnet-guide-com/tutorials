# Sample Runs

Captured from the successful local MCP stdio verification on 2026-10-05.

`	ext
VERIFY|SERVER_DLL|PASS|<sample-root>\src\FirstMcpServer\bin\Release\net10.0\FirstMcpServer.dll
RUNTIME|TransportType|ModelContextProtocol.Client.StdioClientTransport
RUNTIME|ClientType|ModelContextProtocol.Client.McpClientImpl
RUNTIME|ToolCount|3
TOOL|get_recent_deployment|ModelContextProtocol.Client.McpClientTool|Returns deterministic synthetic evidence for the most recent known deployment of a service.
TOOL|get_runbook|ModelContextProtocol.Client.McpClientTool|Returns deterministic runbook guidance for a known service. Guidance is not incident evidence.
TOOL|get_service_health|ModelContextProtocol.Client.McpClientTool|Returns deterministic synthetic service-health evidence for a known service.
VERIFY|TOOL_DISCOVERY|PASS|get_recent_deployment,get_runbook,get_service_health
VERIFY|AIFUNCTION_ASSIGNABILITY|PASS|McpClientTool derives from AIFunction
VERIFY|SERVICE_HEALTH_CALL|PASS|{"evidenceId":"E-101","service":"checkout-api","observedAt":"2026-09-24T14:12:00Z","status":"degraded","errorRate":18.7,"errorSpikeStarted":"2026-09-24T14:09:00Z","databasePoolUtilization":96}
VERIFY|DEPLOYMENT_CALL|PASS|{"evidenceId":"E-201","service":"checkout-api","deployment":"deploy-1842","deploymentStatus":"succeeded","completedAt":"2026-09-24T14:02:00Z","previousDeployment":"deploy-1839","changeSummary":"pricing-rule refresh and structured logging update","rollbackPerformed":false}
VERIFY|RUNBOOK_CALL|PASS|{"runbookId":"RB-CHECKOUT-03","service":"checkout-api","title":"Checkout API degraded-service investigation","guidance":["Compare error-rate onset with recent deployments.","Inspect database connection-pool utilization.","Check connection counts before and after deployment.","Review traces for timeout or connection-acquisition failures.","Compare configuration with the previous known-good release."],"caution":"Runbook guidance suggests investigation steps; it does not establish root cause."}
VERIFY|UNKNOWN_TOOL_REJECTION|PASS|McpProtocolException: Request failed (remote): Unknown tool: 'does_not_exist'
VERIFY|STDIO_END_TO_END|PASS|Client launched server, discovered exactly three tools, and invoked all three over MCP stdio
FINAL|PASS
`

Server diagnostic lines are omitted from this curated view. The raw local output remains in erification-results/mcp-client-output.txt.