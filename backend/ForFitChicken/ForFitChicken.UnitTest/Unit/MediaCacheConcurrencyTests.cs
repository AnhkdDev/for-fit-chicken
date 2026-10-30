using ForFitChicken.Infrastructure.Media;
namespace ForFitChicken.UnitTest.Unit;
public sealed class MediaCacheConcurrencyTests {
 [Fact] public async Task DifferentKeysFetchConcurrently_AndSameKeySharesOneFetch(){using var gates=new MediaWorkGates();var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var started=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var calls=0;async Task<byte[]> Fetch(){if(Interlocked.Increment(ref calls)==2)started.TrySetResult();await release.Task;return [1,2,3];}var first=gates.PublicBytesAsync("a",Fetch,default);var duplicate=gates.PublicBytesAsync("a",Fetch,default);var other=gates.PublicBytesAsync("b",Fetch,default);try{await started.Task.WaitAsync(TimeSpan.FromSeconds(5));Assert.Equal(2,calls);}finally{release.TrySetResult();}await Task.WhenAll(first,duplicate,other);await gates.PublicBytesAsync("a",()=>throw new Exception("cache miss"),default);Assert.Equal(2,calls);}
}
