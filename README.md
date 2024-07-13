# TestcontainersAPI

This is a very simple ASP.NET Core WebApi application using EntityFramework. 

A sample set of integration tests have been written utilising common testing packages such as Testcontainers to spin up the database, Respawn to reset the data after each test run, and Bogus to generate test data.

# Running the tests

You will need docker installed to run the tests on this project. You should simply be able to build the solution, and run the tests. It may take a minute or two to run the first time, as the MsSql image used by the Testcontainers package will need to download first. Afterwards, the container will create a file mount for the data within the `bin` folder of the integration tests, so that future test runs start up more quickly. To delete the database and start afresh, simply delete the `sql` folder in `TestcontainersAPI.Integration.Tests\bin\Debug\net8.0`.
