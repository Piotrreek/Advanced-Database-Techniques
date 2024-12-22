using AdvancedDatabaseTechniques.Delete;
using AdvancedDatabaseTechniques.Insert;
using AdvancedDatabaseTechniques.Postgres;
using AdvancedDatabaseTechniques.Select;
using AdvancedDatabaseTechniques.Update;
using BenchmarkDotNet.Running;

// Insert
// BenchmarkRunner.Run<InsertComparison>();
// BenchmarkRunner.Run<BulkInsertComparison>();

// Select
// BenchmarkRunner.Run<SelectComparison>();
// BenchmarkRunner.Run<SelectRedisFTSearchComparison>();
// BenchmarkRunner.Run<SelectOneFieldComparison>();
// BenchmarkRunner.Run<SelectTopComparison>();
// BenchmarkRunner.Run<SelectWithCountComparison>();
// BenchmarkRunner.Run<SelectWithBetweenComparison>();
// BenchmarkRunner.Run<SelectWithMatchPatternComparison>();
// BenchmarkRunner.Run<SelectWithWhereComparison>();
// BenchmarkRunner.Run<SelectWithOneJoinsComparison>();
// BenchmarkRunner.Run<SelectWithTwoJoinsComparison>();
// BenchmarkRunner.Run<SelectWithFourJoinsComparison>();
// BenchmarkRunner.Run<SelectWithIndexComparison>();

// Update
// BenchmarkRunner.Run<UpdateComparison>();

// Delete
BenchmarkRunner.Run<DeleteComparison>();
BenchmarkRunner.Run<TruncateComparison>();
