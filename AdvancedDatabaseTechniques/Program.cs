﻿using AdvancedDatabaseTechniques;
using BenchmarkDotNet.Running;

// BenchmarkRunner.Run<DatabaseBulkInsertComparison>();
// BenchmarkRunner.Run<DatabaseInsertComparison>();
// BenchmarkRunner.Run<DatabaseDeleteComparison>();
// BenchmarkRunner.Run<DatabaseUpdateComparison>();
// BenchmarkRunner.Run<DatabaseSelectComparison>();
BenchmarkRunner.Run<DatabaseSelectWithIndexComparison>();