using Xunit;

// These are integration tests against a real, shared local SQL Server database (see
// TestServiceProviderFactory's doc comment — no separate test DB exists yet). xUnit runs test
// CLASSES in parallel by default; several tests across different classes here open real
// transactions that touch shared hot rows (OrderNumberCounters' "today" row via
// SqlOrderNumberGenerator, in particular), which produced genuine SQL Server deadlocks under
// concurrent test execution. Disabling parallelization trades wall-clock time for determinism,
// which matters more for a shared-state integration suite than for a unit-test suite.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
