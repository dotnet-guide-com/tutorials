using EfCoreTenantGuardrailsMinimal.Services;

WorkflowSummary summary = await TenantWorkflow.RunAsync();

Console.WriteLine("EF Core Tenant Isolation Lab");
Console.WriteLine($"Visible tasks: alpha={summary.VisibleAlpha}, beta={summary.VisibleBeta}");
Console.WriteLine($"Same local ID: alpha={summary.SameIdAlphaTitle} | beta={summary.SameIdBetaTitle}");
Console.WriteLine($"Alpha including deleted: {summary.AlphaIncludingDeletedCount}, foreign tenants={summary.ForeignTenantsUnderBypass}");
Console.WriteLine($"Spoofed insert stamped tenant: {summary.StampedTenantAfterSpoof}");
Console.WriteLine($"Soft delete: visible alpha={summary.VisibleAlphaAfterSoftDelete}, physical alpha={summary.PhysicalAlphaAfterSoftDelete}, deletedBy={summary.SoftDeleteActor}");
