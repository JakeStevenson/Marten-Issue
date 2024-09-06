This test is 'flaky' when run over and over again in sequence.  I've had it fail after 180 sequencial succesful runs, and I'm trying to figure out what I may have misconfigured or be doing wrong.

Here is an example output from a failure, after 95 successful sequential runs:

```
 EndToEnd.Tests.Test1
   Source: UnitTest1.cs line 83

Test has multiple result outcomes
   95 Passed
   1 Failed

Results

    Iteration 96)   EndToEnd.Tests.Test1 
      Duration: 2.9 sec

      Message: 
        System.AggregateException : One or more errors occurred. (Stream #7c553cba-69ac-4d4c-968b-136c8f173c7f already exists in the database)
        ---- Marten.Exceptions.ExistingStreamIdCollisionException : Stream #7c553cba-69ac-4d4c-968b-136c8f173c7f already exists in the database

      Stack Trace: 
        TrackedSession.AssertNoExceptionsWereThrown() line 177
        TrackedSession.ExecuteAndTrackAsync() line 303
        WolverineHostMessageTrackingExtensions.ExecuteAndWaitAsync(IHost host, Func`2 action, Int32 timeoutInMilliseconds) line 165
        WolverineHostMessageTrackingExtensions.InvokeMessageAndWaitAsync[T](IHost host, Object message, Int32 timeoutInMilliseconds) line 106
        Tests.Test1() line 123
        <>c.<ThrowAsync>b__128_0(Object state)
        ----- Inner Stack Trace -----
        ExceptionTransformExtensions.TransformAndThrow(IEnumerable`1 transforms, Exception ex)
        AutoClosingLifetime.ExecuteBatchPagesAsync(IReadOnlyList`1 pages, List`1 exceptions, CancellationToken token)
        AutoClosingLifetime.ExecuteBatchPagesAsync(IReadOnlyList`1 pages, List`1 exceptions, CancellationToken token)
        AutoClosingLifetime.ExecuteBatchPagesAsync(IReadOnlyList`1 pages, List`1 exceptions, CancellationToken token)
        <<ExecuteAsync>b__2_0>d.MoveNext()
        --- End of stack trace from previous location ---
        Outcome`1.GetResultOrRethrow()
        ResiliencePipeline.ExecuteAsync[TState](Func`3 callback, TState state, CancellationToken cancellationToken)
        DocumentSessionBase.ExecuteBatchAsync(IUpdateBatch batch, CancellationToken token)
        ExceptionTransformExtensions.TransformAndThrow(IEnumerable`1 transforms, Exception ex)
        DocumentSessionBase.ExecuteBatchAsync(IUpdateBatch batch, CancellationToken token)
        DocumentSessionBase.ExecuteBatchAsync(IUpdateBatch batch, CancellationToken token)
        DocumentSessionBase.SaveChangesAsync(CancellationToken token)
        CreateOrderCommandHandler472055778.HandleAsync(MessageContext context, CancellationToken cancellation)
        CreateOrderCommandHandler472055778.HandleAsync(MessageContext context, CancellationToken cancellation)
        Executor.ExecuteAsync(MessageContext context, CancellationToken cancellation) line 164
```
