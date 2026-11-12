# DTZero - .NET Runtime Debugger Memory

## Trimming Configuration
- `src/DTZero/DTZero.csproj` has `<IsTrimmable>true</IsTrimmable>` and `<EnableTrimAnalyzer>false</EnableTrimAnalyzer>`
- Transport DTOs (`RemoteRequestDto`, `RemoteResponseDto`, `ObjectJson`) cross the HTTP boundary using System.Text.Json
- Source-generated `DTZeroTransportJsonContext` added in `src/DTZero/Internal/DTZeroTransportJsonContext.cs` to make transport DTO serialization trimming-safe
- `MakeRemoteDelegateRequestHttpCall.cs` uses the source-generated context for `JsonContent.Create()` and `ReadFromJsonAsync()`
- The DTZeroJsonSerializer (custom serializer) uses `DefaultJsonTypeInfoResolver` subclass -- separate concern from transport DTOs

## Key Serialization Paths
- **Client-to-server (HTTP)**: `JsonContent.Create(request, DTZeroTransportJsonContext.Default.RemoteRequestDto)` -- trimming-safe
- **Server-to-client (HTTP)**: `ReadFromJsonAsync(DTZeroTransportJsonContext.Default.RemoteResponseDto)` -- trimming-safe
- **Domain object serialization**: `DTZeroJsonSerializer` with custom `DTZeroJsonTypeInfoResolver` and `DTZeroOrdinalConverterFactory` -- uses reflection, separate trimming concern
- **Test containers**: `MakeSerializedServerStandinDelegateRequest` uses default `JsonSerializer.Serialize/Deserialize` -- not trimmed in test context

## Known Trimming Risk Areas
- `DTZeroOrdinalConverterFactory.CreateConverter()` uses reflection to find `IOrdinalConverterProvider<T>` methods
- `DTZeroOrdinalConverter<T>` static constructor uses reflection to read `IOrdinalSerializationMetadata` properties
- `HandleRemoteDelegateRequest` uses `DynamicInvoke` and `GetProperty("Result")` on Task
- `AddDTZeroServices.RegisterFactories()` uses `Assembly.GetTypes()` reflection
- `DTZeroJsonTypeInfoResolver.GetTypeInfo()` uses `ServiceProviderIsService.IsService()` -- runtime DI check
