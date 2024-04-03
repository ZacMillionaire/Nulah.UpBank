# DEVELOPMENT IN PROGRESS
You won't get much value out of this if you aren't a customer of the Australian bank [Up](https://up.com.au/) (Bendigo and Adelaide Bank Ltd).

If you are, you can run the current version of this by opening the project in your IDE of choice, and using the Nulah.Up.Blazor (https) launch profile.

## Sensitive Data Warning

**AVOID HOSTING THIS PROJECT OR DATABASE IN A PUBLICLY ACCESSIBLE NETWORK**

Do note that because this project interfaces with a financial institution and _does_ store as much data required within Postgres that you should
ideally _never_ host this application on a public facing server, same for your Postgres instance. This application is designed to run within
a home network and anything past that is currently out of scope.

**THIS PROJECT HAS THE POTENTIAL TO EXPOSE YOUR ENTIRE FINANCIAL HISTORY STORED WITHIN UP**

In saying that, there are plans to remove this requirement for storing information and only using the response from the API provided by Up. When this
is developed many features may not work as intended - such as the potential to attach reciepts or other notes which is a value add provided by this project.

This is currently in development (and at any point Up may end up implementing and exposing these features via their API), so anything could change.

Currently there is _no way_ to transfer or send money via the API. The Up API we integrate with is [available here](https://developer.up.com.au/). Currently the only
functionality that allows changes to transactions are assigning categories or updating tags. Even though it's not currently possible (at the time of writing)
to send money via the Up API, this could change in the future. However I would need to explicitly code this feature in and it's not a feature high on
my list of future functionality because lmao no way do I want to be responsible for allowing someone to open that can of financial whoop-ass if something
goes wrong.

Tread carefully with this project with the understanding and knowledge that it has the real possibility of exposing your finances outside the confines of
the Up Bank application and Api.

# Requirements

This application requires Postgres to be running. How and where does not matter, but for local development Docker is preferred - but not required, I understand if you have a distaste for it -
so long as you can reach your Postgres instance by connection string then how you host it is irrelevant.

If you don't mind docker, you can quickly get a Postgres instance running below:

```docker run --name nulah-up-postgres -p 55432:5432 -e POSTGRES_PASSWORD=mysecretpassword -d postgres```


## Create the database

### With PGAdmin4
Once done, you'll need to connect to the database once using whatever means possible (pgadmin4 for example), and create a database with the name `Nulah.Up`.

This shouldn't realistically be required if you have the ability to create your own postgres instance or connection as you should have the required means
to create the database as needed.
However, the following docker command should get you a quick and dirty pgadmin4 container running if you have no other way of connecting,
or don't care to run command line 

```docker run --user=pgadmin --env=PGADMIN_DEFAULT_EMAIL=no@no.no --env=PGADMIN_DEFAULT_PASSWORD=no -p 5080:80 -d dpage/pgadmin4```

### Without PGAdmin4
Or if you're comfortable enough with Docker you can bash into your previously created Postgres container:

```docker exec -it nulah-up-postgres bash```

...then login to the Postgres instance within:

```psql -h localhost -U postgres```

...then create the database

```CREATE DATABASE Nulah.Up```

...then exit `pgsql` and `bash` by typing `exit` twice.

Following the above (or doing it your own preferred way) should enable the application to connect with the following string (add this to your `appsettings.Development.json`):

```Host=localhost:55432;Database=Nulah.Up;Username=postgres;Password=mysecretpassword```

## Up Api Token

You'll need an Api token to make requests to and from Up. Simply follow the directions listed [in their documentation here](https://api.up.com.au/getting_started),
and add the token to the `appsettings.Development.json` file.