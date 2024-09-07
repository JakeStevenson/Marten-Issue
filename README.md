NOTE:
My latest update solves the issue with streams being created twice.  I discovered Events.Append CAN create a stream just like Events.StartStream<>, so if both Create and Update were being run on different threads, they could try to create the same stream.  I've forced all messages to a single local queue, and that queue to process sequentially.  

I am still getting random errors on query though.  For example:
```
  Failed EndToEnd.WithFixtureTests.WithFixtureTest(iteration: 315) [5 ms]
  Error Message:
   Assert.Equal() Failure: Values differ
Expected: 2
Actual:   0
  Stack Trace:
     at EndToEnd.WithFixtureTests.WithFixtureTest(Int32 iteration) in /Users/jakes/Projects/My-Flaky-Marten-Test/RepeatingTest.cs:line 128
   at System.Threading.Tasks.Task.<>c.<ThrowAsync>b__128_0(Object state)
  Standard Output Messages:
 Created order command 44fca782-b831-4b0c-bdde-577fe3b85c45
 Iteration 315
 Calling CREATE for 44fca782-b831-4b0c-bdde-577fe3b85c45
 Calling UPDATE for 44fca782-b831-4b0c-bdde-577fe3b85c45
 Calling QUERY for 44fca782-b831-4b0c-bdde-577fe3b85c45
```

This seems to indicate the UPDATE command did not complete before the QUERY was run, or the projection was not complete despite being registered as Inline.  I'll have to keep looking for that issue.


PREVIOUS NOTE:  
I've heavily modified the test by applying a fixture that only instantiates the host once, and a repeatattribute so xunit can handle rerunning the test multiple times.  I've added the output from one run with it repeating 500 times in `testoutput.txt`


PREVIOUS NOTE:
This test is 'flaky' when run over and over again in sequence.  I've had it fail once after 180 sequencial succesful runs, and I'm trying to figure out what I may have misconfigured or be doing wrong.  It feels like I've introduced some sort of eventual consistency.

I'd love to be able to debug and catch the issue, but it is rare enough that I have not been able to catch it.  My best hope to catch it is to run the test in a loop over and over again until failure.  

Here is an example output from a failure, after 95 successful sequential runs via visual studios "Run Until Failure" capability in the test runner:

```
EndToEnd.Tests.Test1
   Source: UnitTest1.cs line 96

Test has multiple result outcomes
   180 Passed
   1 Failed

Results

    Iteration 181)   EndToEnd.Tests.Test1 
      Duration: 3 sec

      Message: 
        System.AggregateException : One or more errors occurred. (Stream #a331268a-f13f-4754-9aca-ad22c7dc4189 already exists in the database)
        ---- Marten.Exceptions.ExistingStreamIdCollisionException : Stream #a331268a-f13f-4754-9aca-ad22c7dc4189 already exists in the database

      Stack Trace: 
        TrackedSession.AssertNoExceptionsWereThrown() line 177
        TrackedSession.ExecuteAndTrackAsync() line 303
        WolverineHostMessageTrackingExtensions.ExecuteAndWaitAsync(IHost host, Func`2 action, Int32 timeoutInMilliseconds) line 165
        WolverineHostMessageTrackingExtensions.InvokeMessageAndWaitAsync[T](IHost host, Object message, Int32 timeoutInMilliseconds) line 106
        Tests.Test1() line 139
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

      Standard Output: 
        Created order command a331268a-f13f-4754-9aca-ad22c7dc4189
        Calling CREATE for a331268a-f13f-4754-9aca-ad22c7dc4189
        Calling UPDATE for a331268a-f13f-4754-9aca-ad22c7dc4189
        Calling QUERY for a331268a-f13f-4754-9aca-ad22c7dc4189
```
