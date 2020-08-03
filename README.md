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