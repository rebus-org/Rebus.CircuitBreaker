# Rebus.CircuitBreaker

[![install from nuget](https://img.shields.io/nuget/v/Rebus.CircuitBreaker.svg?style=flat-square)](https://www.nuget.org/packages/Rebus.CircuitBreaker)

Circuit breaker plugin for [Rebus](https://github.com/rebus-org/Rebus).

![](https://raw.githubusercontent.com/rebus-org/Rebus/master/artwork/little_rebusbus2_copy-200x200.png)

---

It's just

```csharp
Configure.With(...)
    .(...)
    .Options(o => o.EnableCircuitBreaker(c => c.OpenOn<SomeException>()))
    .Start();
```

and then the circuit break will open on `SomeException`.

## Settings

For each exception you can configure it with the following parameters.

- **Attempts** - The number of attempts with in the the tracking period it takes to Open the circuit breaker
- **TrackingPeriodInSeconds** - The period of which errors will be tracked
- **halfOpenPeriodInSeconds** - The ammount of time it takes from the last occurance of the Exception for the circuit breaker be but in *Half Open* Mode
- **ResetIntervalInSeconds** - The ammount of time it takes from the last occurance of the Exception for the circuit breaker be but in *Closed* Mode


## Listening for changes

If you want to add custom logic when the state of the circuit breaker changes, you can do this i three easy steps

**1. Create your Event Listener**

```csharp
public class MyCustomCircuitBreakerEventListener : IDisposable
{
    CircuitBreakerEvents _circuitBreakerEvents;

    public MyCircuitBreakerEventListener(CircuitBreakerEvents circuitBreakerEvents)
    {
        _circuitBreakerEvents = circuitBreakerEvents;
        _circuitBreakerEvents.CircuitBreakerChanged += CircuitBreakerEvents_CircuitBreakerChanged;
    }

    private void CircuitBreakerEvents_CircuitBreakerChanged(CircuitBreakerState state)
    {
        // Your implementation
    }

    public void Dispose()
    {
        _circuitBreakerEvents = null;
    }
}

```


**2. Create a custom Rebus OptionsConfigurer Extension**
```csharp
public static class MyCustomOptionsConfigurerExtensions
{
    public static void RegisterMyCustomCircuitBreakerEventListener(this OptionsConfigurer self) 
    {
        self.Register(c => new MyCustomCircuitBreakerEventListener(c.Get<CircuitBreakerEvents>()));
    }
}
```

**3. Configure Rebus**

```csharp
Configure.With(...)
    .(...)
    .Options(o => o.EnableCircuitBreaker(c => 
    {
       c.OpenOn<SomeException>()
       c.RegisterMyCustomCircuitBreakerEventListener()
    }))
    .Start();
```