# Eppo .NET SDK

[![Test and lint SDK](https://github.com/Eppo-exp/java-server-sdk/actions/workflows/lint-test-sdk.yml/badge.svg)](https://github.com/Eppo-exp/java-server-sdk/actions/workflows/lint-test-sdk.yml)

[Eppo](https://www.geteppo.com/) is a modular flagging and experimentation analysis tool. Eppo's .NET SDK is built to make assignments in multi-user server side contexts, compatible with Dot Net 7.0 Runtime. Before proceeding you'll need an Eppo account.

## Features

- Feature gates
- Kill switches
- Progressive rollouts
- A/B/n experiments
- Mutually exclusive experiments (Layers)
- Dynamic configuration

## Installation

In your .NET application, add the Eppo.Sdk Package from Nuget.

```
dotnet add package Eppo.Sdk
```

## Quick start

Begin by initializing a singleton instance of Eppo's client. Once initialized, the client can be used to make assignments anywhere in your app.

#### Initialize once

```go
var eppoClientConfig = new EppoClientConfig('SDK-KEY-FROM-DASHBOARD');
var eppoClient = EppoClient.Init(eppoClientConfig);
```


#### Assign anywhere

```
var assignedVariation = eppoClient.GetStringAssignment(
    'new-user-onboarding', 
    user.id, 
    user.attributes, 
    'control'
);
```

## Assignment functions

Every Eppo flag has a return type that is set once on creation in the dashboard. Once a flag is created, assignments in code should be made using the corresponding typed function: 

```go
GetBooleanAssignment(...)
GetNumericAssignment(...)
GetIntegerAssignment(...)
GetStringAssignment(...)
GetJSONAssignment(...)
```

Each function has the same signature, but returns the type in the function name. For booleans use `getBooleanAssignment`, which has the following signature:

```
public bool GetBooleanAssignment(
    string flagKey, 
    string subjectKey, 
    Dictionary<string, object> subjectAttributes, 
    string defaultValue
)
```

## Assignment logger 

If you are using the Eppo SDK for experiment assignment (i.e randomization), pass in a callback logging function on SDK initialization. The SDK invokes the callback to capture assignment data whenever a variation is assigned.

The code below illustrates an example implementation of a logging callback using Segment. You could also use your own logging system, the only requirement is that the SDK receives a `LogAssignment` function. Here we define an implementation of the Eppo `IAssignmentLogger` interface containing a single function named `LogAssignment`:


```
using eppo_sdk.dto;
using eppo_sdk.logger;

internal class AssignmentLogger : IAssignmentLogger
{
    public void LogAssignment(AssignmentLogData assignmentLogData)
    {
        Console.WriteLine(assignmentLogData);
    }
}
```

## Philosophy

Eppo's SDKs are built for simplicity, speed and reliability. Flag configurations are compressed and distributed over a global CDN (Fastly), typically reaching your servers in under 15ms. Server SDKs continue polling Eppo’s API at 30-second intervals. Configurations are then cached locally, ensuring that each assignment is made instantly. Evaluation logic within each SDK consists of a few lines of simple numeric and string comparisons. The typed functions listed above are all developers need to understand, abstracting away the complexity of the Eppo's underlying (and expanding) feature set.

### Contributing (OSX)

* Download dotnet 7.0 installer to have access to the cli and runtimes
* Download dotnet binary and copy it to your $PATH

Expected environment:

```
✗ dotnet --list-sdks
7.0.406
✗ dotnet --list-runtimes
Microsoft.AspNetCore.App 7.0.16
Microsoft.NETCore.App 7.0.16
```
