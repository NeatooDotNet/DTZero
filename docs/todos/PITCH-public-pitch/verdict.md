# Verdict: Is DTZero Useful to Anyone Else?

**Plan:** PITCH-001 · **Written:** 2026-11-13 by Claude · **Ruling:** pending (section 8)

## 1. Short answer

Yes, to a narrow audience, and not for the reason the README gives today.

DTZero is not a mass-market library and will not become one. Its realistic ceiling is the niche CSLA has held for twenty years. Inside that niche it is one of two real options, and the only one that needs no base class.

The README's lead claim, "zero boilerplate", is the claim AI has weakened most. Two claims survive, and together they are the pitch:

- **DTOs and controllers were a workaround for a language boundary.** The browser ran JavaScript, so the domain could not go there. With .NET on both tiers that boundary is gone, and doing it the old way is a habit, not a need.
- **The domain is defined once, runs on both tiers, and the compiler checks the seam.**
- **A public API reuses the same graph.** The pattern seen over twenty-five years: the application can do everything, the API a limited set, and the application's logic ends up written a second time against the API's DTOs. With DTZero an API endpoint on the server calls the same factories and the same domain objects, so there is one copy of the logic (Keith, 2026-11-13).

The positioning that follows is not "if you already use WebAssembly, here is a library". It is "this is a reason to choose WebAssembly".

## 2. What it saved in one production application

Measured read-only on a production Blazor WebAssembly and ASP.NET Core application (.NET 10, library version 1.9.0). Counts only; the commands are in section 9.

| Measure | Count |
|---|---|
| Factory types | 74 (61 classes, 12 static command classes, 1 interface) |
| Operation methods | 233 |
| Operations the client calls across the wire | 132 |
| `[Service]` parameters | 443 |
| Authorization declarations | 47 |
| Event handler subscriptions | 17 |
| Generated code, current build | 103 files, about 25,000 lines |
| Hand-written domain code behind it | about 21,000 lines, comments excluded |
| Client/server mapping code | none |
| Mapping libraries | none |

What the library did not cover:

- **Three controllers.** Cookie sign-in, PDF downloads the browser opens by URL, and image bytes for an `<img>` tag. All three are things a browser fetches itself, which no factory call can serve.
- **About nine hand-written transfer types**, all for sign-in, each kept in two copies. That is what the other 132 operations would have looked like.
- **Seven plain types** that cross through `[Execute]` commands. They are DTOs in all but name, and R-H already says that is fine.

What it cost:

- **A 200-line trimmer configuration** with 63 preserve entries on the client, most of them attributed in comments to the library's serializer.
- **Trimming faults that appear only in a published build.** The development server does not trim, so these were found late, one of them at production cut-over.
- **Generator gaps** hit along the way: a static command class and an interface that got no factory, a command body that survived trimming, and one upstream issue open for two months.

Two cautions on reading the table. Generated lines are not lines someone would have typed; the fair statement is "132 endpoints, their request and response types, and a client call for each, never written". And the application also uses Neatoo, so some of its comfort comes from there. Even so, 22 of the 61 factory classes are plain classes with no Neatoo base.

## 3. Who it is for

A team that has all of these:

- .NET on both tiers: Blazor WebAssembly, MAUI or WPF in front of ASP.NET Core.
- One team owning both tiers and deploying them together.
- A domain model with behaviour, where the same rule should run in the browser and on the server.
- A line-of-business application with many operations, where the seam is a cost and not a product.

## 4. Who should not use it

- **Anyone whose only client is not .NET.** DTZero's endpoint is not a contract anyone else can call. It is for the application. A public API beside the application is a different matter: its endpoints are hand-written, as they should be for a contract, and they call the same domain graph on the server.
- **Blazor Server only.** There is no wire to cross.
- **Thin CRUD over tables.** With no behaviour to share, minimal APIs and a generated client are simpler and familiar to everyone.
- **Teams that version the client and the server separately.** (To confirm with the user: section 8, question 2.)
## 5. The alternatives

Facts as fetched on 2026-11-13; sources in section 9.

| Alternative | What is still written by hand, per operation | Rich object on both tiers | State |
|---|---|---|---|
| CSLA .NET | An operation method on a class that inherits a CSLA base type | Yes | Active: 10.2.0 in October 2026, 11 in alpha; 1.3k stars, 1.4M downloads |
| Microsoft's guidance for WebAssembly and Auto components | A service interface, two implementations, an endpoint, transfer types | No | The documented default |
| Minimal APIs or FastEndpoints with a generated client | Request and response types, the endpoint, mapping | No | Mainstream: FastEndpoints 20.6M downloads |
| Code-first gRPC (protobuf-net.Grpc, MagicOnion) | A contract interface, message types, a server implementation | No | Active; gRPC-Web has streaming limits in the browser |
| Mapping generators (Mapperly, Mapster, AutoMapper) | The transfer types, the endpoint, the client call | No | Active; AutoMapper went commercial in 2025 |
| "No controllers" generators (RemoteService.Net, BlazorServerFunctions) | An interface, a server implementation, transfer types | No | A few hundred downloads each |

Three things follow.

- **CSLA is the only real neighbour**, and it is moving toward DTZero's ground: version 10.1 added a source generator for data portal dispatch, and version 11 calls operation methods directly on partial classes. "Compile-time, not reflection" will not stay a difference for long.
- **What does stay different:** DTZero works on any class, record or interface, with no base class. CSLA requires its base types. That is the line to draw.
- **Microsoft's own guidance is the strongest foil.** It asks for an interface, two implementations, an endpoint and transfer types for every call. DTZero's `[Remote]` method is that same call, written once.

No source found a trim-safety or AOT claim for CSLA. That is "not found", not "not true", so the docs should not make the comparison.

## 6. The AI question

### The case against DTZero

- **Typing is nearly free now.** An agent writes a DTO, a controller and a mapper in seconds. A library sold on saving typing has lost its reason.
- **Agents do worse with niche libraries.** A 2026 study found higher success rates on mainstream frameworks. Another found that across ten agent frameworks, alignment with convention predicted how well an AI could work with it, and the one with the most novel abstractions scored lowest.
- **Agents default to the mainstream habit.** Left alone, an agent will write a controller and a DTO in a DTZero project. The skill's habit-trap rows exist because this already happens.
- **Some writers go further** and argue that with agents, owning a small implementation beats taking a dependency at all. These are opinion pieces; no study was found behind them.

### The case for DTZero

- **The cost of boilerplate was never the typing.** It is that the same fact lives in four places, and they drift. 132 operations means 132 chances for a transfer type to miss a field. An agent adds drift as cheaply as it adds code.
- **The measured trend points that way.** GitClear's 2025 study of 211 million changed lines found copy-pasted code overtaking refactored code for the first time. DORA 2025 calls AI an amplifier: more throughput, more instability.
- **Less code is less context.** An agent that reads one class sees the whole operation. With four layers it must find and hold all four.
- **A generator is a guardrail an agent cannot talk its way past.** `[Remote] public` is a compile error. A hand-written convention is a suggestion.
- **The niche penalty has a known remedy, and DTZero already ships it.** A skill teaches the agent the model. CSLA has made the same bet with an MCP server, and Microsoft reportedly now lets a NuGet package carry a skill.

### Where that leaves it

AI removes "saves typing" as a reason and leaves "one definition, checked by the compiler" standing, arguably stronger. It also makes the skill part of the product, not an extra. A library an agent uses badly will not be adopted by teams who work through agents.

## 7. What a public reader will attack

Each of these needs an answer in the docs, or an admission.

1. **"This is CSLA."** Yes, the idea is the data portal. The difference is no base class and any type. Say so before the reader does.
2. **"The client sends the whole object. What stops tampering?"** The docs do not answer this directly today. It is the first question a security-minded reader asks.
3. **"Persistence in the entity."** A reader who sees `[Insert]` on an entity will assume the persistence model is on the client. It is not, and the docs must say so early. The graph that crosses is the conceptual DDD graph, shaped for the domain. The persistence model (the EF entities) is a different shape, stays on the server, and is reached only through a `[Service]` parameter of a server-run method. The object maps itself to its row and holds no reference to the store. This is the CSLA arrangement. It is an attack to answer, not a reason to stay away (Keith, 2026-11-13).
7. **"DTOs do more than cross a language boundary."** They also version a contract, shape a payload and limit what a caller may set. The pitch concedes all three for a public API and claims only the case where the client and the server are one application.
4. **"Trimming."** The production evidence shows real cost. Some gaps have closed since version 1.9.0; which remain is not established here.
5. **"Blazor WebAssembly is small."** Blazor as a whole is 7% of respondents in the 2025 Stack Overflow survey, and no credible split by render mode was found.
6. **"One maintainer, a repository two days old, twenty open issues."** True. The honest counter is a production application running on it and the test suite.

## 8. Recommended ruling, and what the user must answer

**Recommended ruling:** DTZero is worth publishing as a deliberately niche library. Plans 002 to 007 run at full scope, with these positions:

- **R1. The lead claim** has two parts. First: DTOs and controllers were a workaround for the JavaScript boundary, so why keep them when .NET is on both tiers. Second: one domain model on both tiers, with the seam checked by the compiler. "Zero boilerplate" goes as the headline. The opportunity argument stays and is sharpened; only the phrase "this changes everything" goes.
- **R2. The audience** is section 3, and section 4 appears in the README as "when not to use it".
- **R3. CSLA is named openly** as the ancestor, with "no base class, any type" as the difference. No trimming or AOT comparison is made.
- **R4. The AI argument** appears in the docs in the form of section 6's last paragraph, and the skill is presented as part of the library.
- **R5. Expectation:** success is tens to low hundreds of users. CSLA's 1.3k stars after two decades is the size of the niche, not a target.

**Questions only the user can answer:**

1. Is the recommended ruling accepted, and are R1 to R5 right as worded?
2. Must the client and the server be deployed together, or can an older client call a newer server?
3. What is the answer to tampering (section 7, item 2): what does the server trust in an object the client sends?
4. Has DTZero run in MAUI or WPF, or is Blazor WebAssembly the only client it has been used with?

### Ruling

**2026-11-13, Keith, partial:**

- "Teams that want persistence out of the entity" is struck from section 4. The transferred graph is the DDD conceptual graph, not the EF model, as in CSLA.
- The niche is the client technology: Blazor WebAssembly and WPF. Inside it the argument is "why do it the old way, as when we were stuck with JavaScript". R1 is reworded to lead with it.

**2026-11-13, Keith, second:**

- The README and docs must walk a sceptical reader through the objections in the order this verdict's own author raised them and was corrected: persistence in the entity, "only a niche", "DTOs do more". The author not seeing the value at first is the evidence that the current pages do not carry it.
- Positioning is "another reason to use WebAssembly", not "a library for those who already do". It is for applications; that is not in doubt.
- The public-API claim joins the pitch (section 1, third bullet).

**Gap this opens:** no page and no Design sample shows an API endpoint calling a factory on the server (checked 2026-11-13: the only `Map*` call in `src/` is the library's own endpoint). The claim needs a Design region and a test that pins it before the README makes it. PITCH-003 or PITCH-004 carries that.

Question 1 (the rest of R1 to R5) and questions 2 and 3 remain open. Question 4 is answered for Blazor WebAssembly and WPF; MAUI is not confirmed.

## 9. Sources

**Measurements.** All on the production application's four app projects, excluding `bin`, `obj` and generated files.

- Factory types: `grep -rlE "^\s*\[Factory(\(|\])"` over `*.cs`.
- Operations: one grep per attribute for an attribute line containing it, then an awk pass over each member's attribute lines to split those with `[Remote]`.
- `[Service]`: `grep -rhE "\[Service\]"`, comment lines removed, occurrences counted.
- Endpoints: `grep -rnE "\[ApiController\]|:\s*(Controller|ControllerBase)\b"` and `grep -rnE "\.(MapGet|MapPost|MapPut|MapDelete|MapControllers|MapHub)"`.
- Transfer types: `grep -rnoE "\b(class|record|struct|interface)\s+\w+(Dto|Request|Response|Model|ViewModel)\b"`.
- Mapping libraries: `grep -rE "AutoMapper|Mapster|Mapperly|IMapper|TypeAdapter"`, no hits.
- Generated code: `find` on the generator's output folder, limited to the latest build's date, piped to `wc -l`. The folder also held stale files from older builds, so this figure is approximate; a clean build would confirm it.

**Alternatives.**

- CSLA: https://github.com/MarimerLLC/csla · https://www.nuget.org/packages/Csla · https://github.com/MarimerLLC/csla/releases/tag/v11.0.0-alpha.2 · https://blog.lhotka.net/2025/10/02/A-Simple-CSLA-MCP-Server
- Microsoft guidance: https://learn.microsoft.com/en-us/aspnet/core/blazor/call-web-api?view=aspnetcore-10.0
- gRPC: https://github.com/protobuf-net/protobuf-net.Grpc · https://github.com/Cysharp/MagicOnion · https://learn.microsoft.com/en-us/aspnet/core/grpc/grpcweb?view=aspnetcore-10.0
- Mapping: https://github.com/riok/mapperly · https://www.nuget.org/packages/Mapster · https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/
- Mainstream path: https://github.com/FastEndpoints/FastEndpoints · https://www.nuget.org/packages/NSwag.Core
- Small generators: https://www.nuget.org/packages/BlazorServerFunctions · https://www.nuget.org/packages/RemoteService.Net.Client/
- Blazor share: https://survey.stackoverflow.co/2025/technology

**The AI question.**

- Niche penalty: https://arxiv.org/abs/2509.23261 · https://arxiv.org/abs/2602.11198 · https://arxiv.org/abs/2503.17181
- Against dependencies (opinion): https://levelup.gitconnected.com/if-all-code-is-written-by-ai-agents-do-we-still-need-frameworks-07e5ae635dd7 · https://julien.danjou.info/blog/how-much-of-a-library-do-you-actually-use/
- Code quality under AI: https://www.gitclear.com/ai_assistant_code_quality_2025_research · https://cloud.google.com/blog/products/ai-machine-learning/announcing-the-2025-dora-report · https://www.theregister.com/2025/07/11/ai_code_tools_slow_down/
- Skills in packages (read from a secondary write-up only): https://devblogs.microsoft.com/dotnet/delivering-skills-in-nuget-packages/

**Not verified.** The share of Blazor WebAssembly against Server and Auto. Any trim-safety or AOT claim for CSLA. Which of the trimming gaps seen at version 1.9.0 are closed in 2.0.0. "No other library in this niche" rests on a handful of searches.
