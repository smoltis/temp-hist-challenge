# temp-hist-challenge
This is a repository for Temperature Histogram Coding Challenge

## 1. Requirements

* Write an application that takes the log file historgram input (below) and creates a tsv file containing histogram bins of forecasted high temperatures of the locations discovered in the input log.
* Use Imperial units and the next calendar day’s forecast for values.
* The application should use a public REST based API.
* The application should implement a cache to keep API calls to a minimum including between application executions.
* The application should scrub data to prevent failed API lookups.
summary to the console for rows that could not be included in the histogram. Include reasons (missing data, API lookup failures, etc.) along with counts/percentages for each failure type
* Include a detailed readme file
* The submission needs to include a docker file so we can run it as a container.

## 2. Guidance

* Use freely available data sources, web API’s, and/or downloadable databases to find forecasted weather information for each row in the provided log.
* The sample log file intentionally does not contain a header or description of the payload. It does not contain specific location or temperature data. Use what is available, to achieve the requirements
* Write your code with the quality bar you would use for production code. The code should be professional, maintainable and scalable
* Submit your response as a .zip or compress tarball
* Be sure to scrub your submission of any binaries or executable files (e.g. *.exe, *.dll, *.bat, etc.).

## 3. Examples:
```
CreateWeatherHistogram --input ./ttd_test_data.csv --output ./histogram.tsv --numOfBuckets 5


```
## 4. Example TSV File Contents
*Example tsv content with a bucket count of 5:*

~~~
bucketMin       bucketMax                 count
0                  16.2                    47
16.2               32.4                    191
32.4               48.6                    1586
48.6               64.8                    416
64.8               81                      1161
~~~

# Application description

The application is a console app written in C# for .net core 2.2 runtime.
Redis server is used as an external caching service.
Though the source code compiles into a dotnet core executable and can be used separately 
under OS of choice the Application and the Redis server are supposed to run as a whole in Docker environment.

To set up containers in Docker the `docker-compose.yml` file and docker-compose tool were used.

*Prerequisites to run the application from the command line:*

* OSX or Linux 
* Docker w/ docker-compose

# How to start the application under OSX/Linux?

Decompress the source code and additional Docker files and scripts into a folder.

*Main files and folders:*

* Dockerfile - Docker container description to build and execute the dotnet core application 
* docker-compose.yml - Docker compose YAML script to create an environment for Redis and dot net runtime containers
* CreateWeatherHistogram - bash script to parse the CLI arguments, map volumes and pass them to Docker 
* src/ - source code folder with the application source code in C#
* temp-hist-challenge.sln - Visual Studio solution file referencing the projects in the src/ folder

## Running the application in Docker with docker-compose

Make sure the bash script file is executable by running the command
*chmod a+x ./CreateWeatherHistogram*

Exceute the application from the command line as in Examples, like so:

```
CreateWeatherHistogram --input ./ttd_test_data.csv --output ./histogram.tsv --numOfBuckets 5

```

* Specify the input file name and path and the file name of the output histogram file accordingly.
* The output file destination path is relative to the input file path. 
* The input file path will become a mapped volume for Docker container to avoid data transferring from host to container environments.
* Redis server will be started in **AOF Persistence mode**. The database cache file will be written to the host machine into 

## Running the aplication in Visual Studio on the machine with Redis in Docker container (Debug mode) on OSX/Windows 10

* Open solution file `temp-hist-challenge.sln` in Visual Studio
* You might need to restore packages in CLI by running the command `dotnet restore` in the **src/** folder.
* Run `docker compose up` command in IntegrationTests/ folder to run Redis in the Docker container. Make sure port **6379** is available on the host machine.
* Configure project options. Set the environment variable `"DOTNETCORE_ENVIRONMENT" = "Development"` in Visual Studio for debug mode. It will enable additional loggin to the console and access Redis on **localhost:6379**.
* Specify start arguments in project options i.e. `-i ./data/fileIn.csv -o histogram.tsv -n 8`. Parameters will be passed to the application CLI on execution. 
* Make sure Debug build configuration is selected. At the end of execution console window will be paused for you to review console output.
* Edit **src/appsettings.json** file and specify Redis server name and port if using your own Redis. Build Release configuration.
* Choose `"Run -> Start Debugging"` in Visual Studio.

# Application design

Standard functionality of dotnet core service provider with constructor depenency injection is used to access different services during the runtime.
The entry point `Main()` class is located in **src/Program.cs** file. 
Command line parsing and services configuration is done in the same entry file.
The implementation can be replaced if needed by changing the service provider bindings. More services can be added in the same manner.

### Main service interfaces (src/Services/...):

* `IHistogramService` - main orchestrator
* `IInputFileService` - read and process the input file
* `IOutputFileService` - write to the output file
* `ILocationService` - seek location coordinates by IP address through public web API
* `IWeatherService` - fetch the weather forecast by geolocation

### Services implementation (src/Services/...):

* `HistogramService.cs` calls the service to read the file and get the temperature histogram, it will then bucketize it in accordance with the number of buckets provided by the start parameter, print out the failed API summary with reasons if any and send the histogram data to the output file service to write it to disk.
* `MapReduceFileService.cs` reads the input file using the map-reduce pattern with the number of workers equal to number of logical CPUs available. Near-linear complexity scaling can be achieved. One has to consider memory complexity in this implementation though. As reducer will maintain in-memory dictionary. To alleviate the memory pressure the trade-off was  used at cost of breaking the separation of concerns design principle. The service has nested calls to web API services to find the location and the weather forecast. It makes the overall number of non-unique lines in IP->Location->Temperature sets smaller thus saving memory in the resulting data structure. Raw data cleansing and validationb isperformed in this service.
* `IpStackService.cs` enabled IpStack.com geolocation web service access. Free tier api key is provided separately, configurable in **src/appsettings.json** file. 
* `OpenWeatherMapService.cs` enables weather forecast for given geolocation using OpenWeatherMap.com web service. Free tier api key is provided separately, configurable in src/appsettings.json file.
* `TsvFileService.cs` employs csvHelper nuget package to write histogram data into tab-delimited file on disk.

### Models (src/Models/...):
* `Bucket.cs` class represents the bucket of the resulting histogram
* `TemperatureFileLine.cs` class contains the desired data line read from the file
* `OpenWeatherMapResponseDto.cs` model for json deserializer of OpenWeatherMap web API response
* `ApiStats.cs` class contains the failure summary of API calls and missing data lines. It is registred using Singleton design pattern and accessed via constructor dependency injection by different services.

### Extension methods (src/Extensions/...):
* `FloatExtensions.cs` implements the truncation of digits after the decimal point for the float type
* `HistogramEntensions.cs` implements dictionary collection etension method that divides the histogram into N buckets 






