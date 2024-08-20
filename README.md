# Eppo .NET SDK

[Eppo](https://www.geteppo.com/) is a modular flagging and experimentation analysis tool. Eppo's .NET SDK is built to make assignments in multi-user server side contexts, compatible with Dot Net 8.0 Runtime. Before proceeding you'll need an Eppo account.

Despite the name, this SDK runs in both server and client applications.

## Features

- Feature gates
- Kill switches
- Progressive rollouts
- A/B/n experiments
- Mutually exclusive experiments (Layers)
- Dynamic configuration
- Contextual Multi-Armed Bandits

## Installation

In your .NET application, add the Eppo.Sdk Package from Nuget.

```sh
dotnet add package Eppo.Sdk
```

## Client Mode
In order to have the most up-to-date _Assignment_ and _Bandit_ configuration, the SDK frequently polls the configuration server. This is not ideal in a client deployment such as a mobile or web app. For these client applications, this SDK can be run in **Client Mode** where configuration loading is done once on `InitClientMode` and on-demand by calling the `EppoClient.RefreshConfiguration` method.

## Quick start

Begin by initializing a singleton instance of Eppo's client. Once initialized, the client can be used to make assignments anywhere in your app.


#### Initialize once

```cs
var eppoClientConfig = new EppoClientConfig('SDK-KEY-FROM-DASHBOARD');

// For servers; immediately loads configuration and every roughly, 30 seconds after:
var eppoClient = EppoClient.Init(eppoClientConfig);

// For client (such as mobile) applications; initializes and immediately loads configuration once:
var eppoClient = EppoClient.InitClientMode(eppoClientConfig);

// Client applications, On app reload or other trigger to reload the configuration.
eppoClient.RefreshConfiguration();
```


#### Assign anywhere

```cs
var assignedVariation = eppoClient.GetStringAssignment(
    'new-user-onboarding', 
    user.id, 
    user.attributes, 
    'control'
);
```

### Select a Bandit Action
This SDK supports [Multi-armed Contextual Bandits](https://docs.geteppo.com/contextual-bandits/).

```cs
var subjectAttributes = new Dictionary<string, object?>()
 {
     ["age"] = 30, // Gets interpreted as a Numeric Attribute
     ["country"] = "uk", // Categorical Attribute
     ["pricingTier"] = "1"  // NOTE: Deliberately setting to string causes this to be treated as a Categorical Attribute
 };
 var actions = new Dictionary<string, IDictionary<string, object?>>()
 {
     ["nike"] = new Dictionary<string, object?>()
     {
         ["brandLoyalty"] = 0.4,
         ["from"] = "usa"
     },
     ["adidas"] = new Dictionary<string, object?>()
     {
         ["brandLoyalty"] = 2,
         ["from"] = "germany"
     }
 };
 var result = client.GetBanditAction(
     "flagKey",
     "subjecKey",
     subjectAttributes,
     actions,
     "defaultValue");

if (result.Action != null)
{
    // Follow the Bandit action
    DoAction(result.Action);
} else {
    // User was not selected for a Bandit.
    // A variation is still assigned.
    DoSomething(result.Variation);
}
```

## Assignment functions

Every Eppo flag has a return type that is set once on creation in the dashboard. Once a flag is created, assignments in code should be made using the corresponding typed function: 

```cs
GetBooleanAssignment(...)
GetNumericAssignment(...)
GetIntegerAssignment(...)
GetStringAssignment(...)
GetJSONAssignment(...)
```

Each function has the same signature, but returns the type in the function name. For booleans use `getBooleanAssignment`, which has the following signature:

```cs
public bool GetBooleanAssignment(
    string flagKey, 
    string subjectKey, 
    Dictionary<string, object> subjectAttributes, 
    string defaultValue
)
```

## Initialization options

The `Init` and `InitClient` functions accept the following optional configuration arguments.

| Option | Type | Description | Default |
| ------ | ----- | ----- | ----- |
| **`assignmentLogger`**  | [AssignmentLogger](https://github.com/Eppo-exp/python-sdk/blob/ebc1a0b781769fe9d2e2be6fc81779eb8685a6c7/eppo_client/assignment_logger.py#L6-L10) | A callback that sends each assignment to your data warehouse. Required only for experiment analysis. See [example](#assignment-logger) below. | `None` |

### Polling Interval

For additional control in server deployments, the `EppoClientConfig` class can be initialized with a custom interval to override the default of 30sec.

```cs

var config = new EppoClientConfig("YOUR-API-KEY", myAssignmentLogger)
    {
        PollingIntervalInMillis = 5000
    };
```


## Assignment and Bandit Action Logger 

To use the Eppo SDK for experiments that require analysis, pass in a callback logging function to the `init` function on SDK initialization. The SDK invokes the callback to capture assignment data whenever a variation is assigned or a Bandit Action is selected. **The assignment data is needed in the warehouse to perform analysis.**

The code below illustrates an example implementation of a logging callback using Segment. You could also use your own logging system, the only requirement is that the SDK receives a `LogAssignment` and a `LogBanditAction` function. Here we define an implementation of the Eppo `IAssignmentLogger` interface:

```cs
class SegmentLogger : IAssignmentLogger
{
    private readonly Analytics analytics;

    public SegmentLogger(Analytics analytics)
    {
        this.analytics = analytics;
    }

    public void LogAssignment(AssignmentLogData assignmentLogData)
    {
        analytics.Track("Eppo Randomization Assignment", assignmentLogData);
    }

    public void LogBanditAction(BanditLogEvent banditLogEvent)
    {
        analytics.Track("Eppo Bandit Action", banditLogEvent);
    }
}
```

## Full Initialization and Assignment Example

```cs
class Program
{
    public void main()
    {

        // Initialize Segment and Eppo clients.
        var segmentConfig = new Configuration(
                    "<YOUR WRITE KEY>",
                    flushAt: 20,
                    flushInterval: 30);
        var analytics = new Analytics(segmentConfig);

        // Create a logger to send data back to the Segment data warehouse
        var logger = new SegmentLogger(analytics);

        // Initialize the Eppo Client
        var eppoClientConfig = new EppoClientConfig("EPPO-SDK-KEY-FROM-DASHBOARD", logger);
        var eppoClient = EppoClient.Init(eppoClientConfig);

        // Elsewhere in your code, typically just after the user logs in.
        var subjectTraits = new JsonObject()
        {
            ["email"] = "janedoe@liamg.com",
            ["age"] = 35,
            ["accountAge"] = 2,
            ["tier"] = "gold"
        }; // User properties will come from your database/login service etc.
        var userID = "user-123";

        // Identify the user in Segment Analytics.
        analytics.Identify(userID, subjectTraits);


        // Need to reformat user attributes a bit; EppoClient requires `IDictionary<string, object?>`
        var subjectAttributes = subjectTraits.Keys.ToDictionary(key => key, key => (object)subjectTraits[key]);
        // Get an assignment for the user
        var assignedVariation = eppoClient.GetStringAssignment(
            "new-user-onboarding",
            userID,
            subjectAttributes,
            "control"
        );
    }
}

class SegmentLogger : IAssignmentLogger
{
    private readonly Analytics analytics;

    public SegmentLogger(Analytics analytics)
    {
        this.analytics = analytics;
    }

    public void LogAssignment(AssignmentLogData assignmentLogData)
    {
        analytics.Track("Eppo Randomization Assignment", assignmentLogData);
    }

    public void LogBanditAction(BanditLogEvent banditLogEvent)
    {
        analytics.Track("Eppo Bandit Action", banditLogEvent);
    }
}
```


## Philosophy

Eppo's SDKs are built for simplicity, speed and reliability. Flag configurations are compressed and distributed over a global CDN (Fastly), typically reaching your servers in under 15ms. Server SDKs continue polling Eppo’s API at 30-second intervals. Configurations are then cached locally, ensuring that each assignment is made instantly. Evaluation logic within each SDK consists of a few lines of simple numeric and string comparisons. The typed functions listed above are all developers need to understand, abstracting away the complexity of the Eppo's underlying (and expanding) feature set.

### Contributing (OSX)

* Download dotnet 8.0 installer to have access to the cli and runtimes
* Download dotnet binary and copy it to your $PATH

Expected environment:

```sh
✗ dotnet --list-sdks
8.0.303
✗ dotnet --list-runtimes
Microsoft.AspNetCore.App 8.0.7
Microsoft.NETCore.App 8.0.7
```
