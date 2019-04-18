# RepoBrowser
Thie project contains Git tools to grab data about repositories from Github (currently).

Why I think this is great?
The service does three small things that make a big difference:
* Decouples relationships between functionality so that different repositories, different authentication, and different web communication types can be used.
* Collects data and brings it to a 'repository host'-agnostic way making it more consistent to manipulate and get at data.
* Allows flexibility and customizability for future development and changes one might want to make on their own.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on a live system.

### Prerequisites

What things you need to install the software and how to install them

```
Have dotnet 2.2 installed on your machine. Microsoft maintains a listing of current versions. This repo's functionality has been tested on a Mac Darwin OS.
```

## Running the tests

The project uses XUnit unit tests. These can be run from the command line with `dotnet test` in the root directory.

## Deployment

* Publish using dotnet's commands (`dotnet publish`).
* Remaining deployment has not been done, but dotnet wraps itself nicely into being deployed via Docker to AWS or other nice tools.

## Building

To build use `dotnet build` in the root directory.

## Running locally

To run, feel free to use `dotnet <path>` where <path> is the path to your executable. You will want to run two windows (one for the `RepoBrowserService` that runs the web service and one for the `RepoBrowserConsole` that interacts with the service).

## Contributing

Currently, I do not have any set methods for how to contribute. Feel free to contact me if you want to be involved.

## Authors

* **Luke Mynsberge** - *Initial work* - [luke](https://github.com/lmynsberge)

See also the list of [contributors](https://github.com/your/project/contributors) who participated in this project.

## License

TBD

## Acknowledgments

* Thanks to PurpleBooth for a good Readme template: https://gist.github.com/PurpleBooth/109311bb0361f32d87a2
* Thanks to Json2Csharp for creating the POCOs from JSON for Github REST and GraphQL responses.

## Future Enhancements
* Improved error handling - right now it just lets the controller exceptions log the information.
* Persisting of OAuth2 tokens and checking if it's expired. Right now it just regenerates every time it's ran.

## Configuration
The service was defined with configuration and customizability in mind. The most notable parts are defined in `appsettings.json`:
* AuthType - extendable authentication plugin. These two have been delivered with this service:
  * RepoBrowser.Authentication.BasicAuthentication - uses Basic authentication to interact with the repository.
  * RepoBrowser.Authentication.OAuth2Authentication - uses OAuth2 authentication based on Github's structure to interact with the repository. This should be generic, so please create an issue if a non-Github repo uses OAuth2 and this won't work for it.
* TransformType - manipulates requests/responses to a single internal data model that is used for more consistent manipulation. Two formats have been delivered with this service:
  * RepoBrowser.Transformation.GithubRestTransformation - leverages Github's REST (v3) API to fetch data about pull requests.
  * RepoBrowser.Transformation.GithubGQLTransformation - leverages Github's GraphQL (v4) API to fetch data about pull requests.

If you want to use your own, make sure your DLL is in the directory the service is running it and put its FQDN into the appropriate configuration portion.

## Results & Metrics
As of this writing, this was tested with the Ramda organization's Github repositories.

It finds 1799 total pull requests (open & closed/merged) across all repositories. Using the console or directly calling the service's API this can be limited. For example:
* Ramda's 'ramda' repo has 1500 total PRs (104 open, 1396 closed/merged)
* Ramda has 119 total open PRs

Another metric was done to recommend GraphQL over REST at least for Github. The following performance was assessed for all pull requests across all repos:
* REST (first startup) - ~78s
* REST (subsequent request) - ~72s
* GraphQL (first startup) - ~26s
* GraphQL (subsequent request) - ~27s
Of course if you need to expand the internal data model to gather as much data as the REST API, this benefit decreases, but it is unlikely you need it all.

