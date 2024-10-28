using AdvancedDatabaseTechniques;
using AdvancedDatabaseTechniques.Postgres;
using AdvancedDatabaseTechniques.Redis;
using BenchmarkDotNet.Running;

// POSTGRES

// BenchmarkRunner.Run<DatabaseBulkInsertComparison>();
// BenchmarkRunner.Run<DatabaseInsertComparison>();
// BenchmarkRunner.Run<DatabaseDeleteComparison>();
// BenchmarkRunner.Run<DatabaseUpdateComparison>();
// BenchmarkRunner.Run<DatabaseSelectComparison>();
// BenchmarkRunner.Run<DatabaseSelectWithIndexComparison>();

// REDIS
// BenchmarkRunner.Run<DatabaseBulkInsertComparisonRedis>();
BenchmarkRunner.Run<DatabaseSelectComparisonRedis>();
// BenchmarkRunner.Run<DatabaseSelectWithIndexComparisonRedis>();
// BenchmarkRunner.Run<DatabaseDeleteComparisonRedis>();
// BenchmarkRunner.Run<DatabaseUpdateComparisonRedis>();